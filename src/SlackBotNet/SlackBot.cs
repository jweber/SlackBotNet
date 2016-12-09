using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlackBotNet.Messages;
using SlackBotNet.State;
using SlackBotNet.Drivers;

namespace SlackBotNet
{
    public class SlackBot : IDisposable
    {
        private readonly IMessageBus messageBus;
        private readonly SlackBotState state;
        private readonly ISlackBotConfig config;

        private ConcurrentQueue<WhenHandler> whenHandlers;

        private IDriver driver;

        private IDisposable sendTimer = null;

        private SlackBot(
            SlackBotState state,
            IDriver driver,
            IMessageBus bus,
            ISlackBotConfig config)
        {
            this.state = state;
            this.config = config;
            this.driver = driver;

            this.messageBus = bus;

            this.whenHandlers = new ConcurrentQueue<WhenHandler>();
        }

        public IReadOnlyState State => this.state;

        public static Task<SlackBot> InitializeAsync(string slackToken, Action<ISlackBotConfig> config = null)
            => InitializeAsync(slackToken, new SlackRtmDriver(), new RxMessageBus(), config);

        internal static async Task<SlackBot> InitializeAsync(string slackToken, IDriver driver, IMessageBus bus, Action<ISlackBotConfig> config = null)
        {
            var defaultConfig = new DefaultSlackBotConfig();
            config?.Invoke(defaultConfig);

            var state = await driver.ConnectAsync(slackToken, bus);

            var bot = new SlackBot(state, driver, bus, defaultConfig);
            bot.StartSendListener();

            bot.On<IHubJoined>(msg =>
            {
                bot.state.AddHub(msg.Channel.Id, msg.Channel.Name, msg.HubType);
                bot.config.TraceHandler($"Joined hub {msg.Channel.Name} (Id: {msg.Channel.Id})");
            });

            bot.On<IHubLeft>(msg =>
            {
                bot.config.TraceHandler($"Left hub {bot.state.GetHubById(msg.Channel).Name}");
                bot.state.RemoveHub(msg.Channel);
            });

            // Handle .When setups
            bot.On<Message>(async msg =>
            {
                // Ignore messages with reply_to that is set.
                // They appear to be sent after the initial connection that the bot establishes.
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                    return;

                (decimal score, Match[] matches, WhenHandler handler) bestMatch = (-1m, null, null);

                foreach (var handler in bot.whenHandlers)
                {
                    var matches = await handler.MatchGenerator.Invoke(msg);
                    if (matches == null)
                        continue;

                    decimal score = matches.Sum(m => m.Score);

                    if (score < 0)
                        continue;

                    if (bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.AllMatches || bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.FirstMatch)
                    {
                        await handler.MessageHandler(msg, matches);

                        if (bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.FirstMatch)
                            break;
                    }

                    if (score > bestMatch.score)
                        bestMatch = (score, matches, handler);
                }

                if (bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.BestMatch && bestMatch.handler != null)
                    await bestMatch.handler.MessageHandler(msg, bestMatch.matches);
            });

            return bot;
        }

        #region Send

        private readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// Limits outgoing messages to 1/second
        /// </summary>
        private void StartSendListener()
        {
            this.sendTimer = Observable
                .Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
                .Subscribe(async _ =>
                {
                    if (this.messageQueue.TryDequeue(out string message))
                    {
                        await this.driver.SendMessageAsync(message);
                    }
               });
        }

        public Task SendAsync(Hub hub, string message)
            => this.SendAsync(hub.Id, message);

        public Task SendAsync(string channel, string message)
        {
            var msg = new
            {
                id = Guid.NewGuid().GetHashCode(),
                type = "message",
                channel = channel,
                text = this.EncodeMessage(message)
            };

            var json = JsonConvert.SerializeObject(msg,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            this.messageQueue.Enqueue(json);
            return Task.CompletedTask;
        }

        private string EncodeMessage(string message)
            => message
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

        #endregion

        #region Receive

        public IDisposable On<TMessage>(Action<TMessage> handler)
            where TMessage : IRtmMessage
        {
            return this.messageBus.Observe<TMessage>().Subscribe(handler);
        }

        public IDisposable When(MessageMatcher match, Action<Conversation> handler)
            => this.When(match, HubType.Channel | HubType.DirectMessage | HubType.Group, handler);

        public IDisposable When(MessageMatcher match, HubType hubs, Action<Conversation> handler)
        {
            var whenHandler = new WhenHandler(this,
                msg =>
                {
                    var messageHubType = this.state.GetHubById(msg.Channel).HubType;
                    if ((hubs & messageHubType) != messageHubType)
                        return null;

                    if (messageHubType != HubType.DirectMessage)
                    {
                        bool messageAddressesBot = msg.Text?.IndexOf(this.state.BotUsername, StringComparison.OrdinalIgnoreCase) > -1;
                        if (!messageAddressesBot)
                            return null;
                    }

                    return match.GetMatches(msg);
                },
                (msg, matches) =>
                {
                    var conversation = new Conversation(
                        this,
                        this.state.GetUser(msg.User),
                        this.state.GetHubById(msg.Channel),
                        msg.Text,
                        matches
                    );

                    handler(conversation);
                    return Task.CompletedTask;
                });

            this.whenHandlers.Enqueue(whenHandler);
            return whenHandler;
        }

        class WhenHandler : IDisposable
        {
            private readonly SlackBot bot;

            public WhenHandler(
                SlackBot bot, 
                Func<Message, Task<Match[]>> matchGenerator,
                Func<Message, Match[], Task> messageHandler)
            {
                this.bot = bot;
                this.MatchGenerator = matchGenerator;
                this.MessageHandler = messageHandler;
            }

            public Func<Message, Task<Match[]>> MatchGenerator { get; }
            public Func<Message, Match[], Task> MessageHandler { get; }

            public void Dispose()
            {
                this.bot.whenHandlers = new ConcurrentQueue<WhenHandler>(
                    this.bot.whenHandlers.Where(m => m != this)
                );
            }
        }

        /// <summary>
        /// Returns the first instance of a message of type <typeparamref name="TMessage"/>
        /// that matches the predicate <paramref name="where"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public async Task<TMessage> Linger<TMessage>(Func<TMessage, bool> where)
            where TMessage : IRtmMessage
        {
            return await this.messageBus
                .Observe<TMessage>()
                .Where(where)
                .FirstAsync();
        }

        #endregion

        public void Dispose()
        {
            this.sendTimer?.Dispose();
        }
    }
}