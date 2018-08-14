using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlackBotNet.Messages;
using SlackBotNet.State;
using SlackBotNet.Drivers;
using SlackBotNet.Infrastructure;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet
{
    public class SlackBot : IDisposable
    {
        private readonly IMessageBus messageBus;
        private readonly SlackBotState state;
        private readonly ISlackBotConfig config;
        private readonly ILogger<SlackBot> logger;
        private readonly ThrottleQueue<IMessage> sendMessageQueue;
        private readonly ThrottleQueue<File> uploadFileQueue;

        private ConcurrentQueue<WhenHandler> whenHandlers;

        private IDriver driver;

        private SlackBot(
            SlackBotState state,
            IDriver driver,
            IMessageBus bus,
            ISlackBotConfig config,
            ILogger<SlackBot> logger)
        {
            this.state = state;
            this.config = config;
            this.logger = logger;

            this.driver = driver;
            this.messageBus = bus;

            this.whenHandlers = new ConcurrentQueue<WhenHandler>();

            this.sendMessageQueue = new MessageThrottleQueue(
                TimeSpan.FromSeconds(1.0), // ~1/sec (see: https://api.slack.com/methods/chat.postMessage)
                driver,
                logger,
                (queue, msg, lg, ex) =>
                {
                    config?.OnSendMessageFailure?.Invoke(queue, msg, lg, ex);
                });
            
            this.uploadFileQueue = new ThrottleQueue<File>(
                TimeSpan.FromSeconds(3), // ~20/min (see: https://api.slack.com/methods/files.upload)
                logger,
                this.driver.UploadFileAsync,
                null);
        }

        public IReadOnlyState State => this.state;

        public static Task<SlackBot> InitializeAsync(string slackToken, Action<ISlackBotConfig> config = null)
            => InitializeAsync(new SlackRtmDriver(slackToken), new RxMessageBus(), config);

        internal static async Task<SlackBot> InitializeAsync(IDriver driver, IMessageBus bus, Action<ISlackBotConfig> config = null)
        {
            var defaultConfig = new DefaultSlackBotConfig();
            config?.Invoke(defaultConfig);

            var logger = defaultConfig.LoggerFactory.CreateLogger<SlackBot>();

            var state = await driver.ConnectAsync(bus, logger);

            var bot = new SlackBot(state, driver, bus, defaultConfig, logger);

            bot.On<IHubJoined>(msg =>
            {
                bot.state.AddHub(msg.Channel.Id, msg.Channel.Name, msg.HubType);
                logger.LogInformation($"Joined hub {msg.Channel.Name} (Id: {msg.Channel.Id})");
            });

            bot.On<IHubLeft>(msg =>
            {
                logger.LogInformation($"Left hub {bot.state.GetHubById(msg.Channel).Name}");
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
                    var matches = new Match[0];
                    try
                    {
                        matches = await handler.MatchGenerator.Invoke(msg);
                    }
                    catch (Exception exception)
                    {
                        handler.OnException(msg, exception);
                    }

                    if (matches == null)
                        continue;

                    decimal score = matches.Sum(m => m.Score);

                    if (score < 0)
                        continue;

                    if (bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.AllMatches || bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.FirstMatch)
                    {
                        var (success, ex) = await handler.MessageHandler(msg, matches);

                        if (ex != null)
                            handler.OnException(msg, ex);

                        if (success && bot.config.WhenHandlerMatchMode == WhenHandlerMatchMode.FirstMatch)
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

        /// <summary>
        /// Uploads a file to the given <paramref name="hub"/>
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public Task UploadFileAsync(Hub hub, File file)
        {
            if (string.IsNullOrEmpty(file.Channels))
                file.Channels = hub.Id;

            this.uploadFileQueue.Enqueue(file);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Posts a message to the <paramref name="hub"/>.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="message"></param      
        /// <param name="linkNames"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(Hub hub, string message, bool linkNames, params Attachment[] attachments)
            => this.SendAsync(hub.Id, message, linkNames, attachments);


        /// <summary>
        /// Posts a message to the <paramref name="hub"/>.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="message"></param      
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(Hub hub, string message, params Attachment[] attachments)
            => this.SendAsync(hub.Id, message, false, attachments);

        /// <summary>
        /// Posts a message to the <paramref name="hub"/>.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="linkNames"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(Hub hub, bool linkNames, params Attachment[] attachments)
            => this.SendAsync(hub.Id, linkNames, attachments);

        /// <summary>
        /// Posts a message to the <paramref name="hub"/>.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(Hub hub, params Attachment[] attachments)
            => this.SendAsync(hub.Id, false, attachments);

        /// <summary>
        /// Posts a message to the <paramref name="channelId"/>.
        /// </summary>
        /// <param name="channelId">The channel/group/dm id.</param>
        /// <param name="linkNames"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(string channelId, bool linkNames, params Attachment[] attachments)
        {
            this.sendMessageQueue.Enqueue(new PostMessage(channelId, attachments) { LinkNames = linkNames ? true : default(bool?) });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts a message to the <paramref name="channelId"/>.
        /// </summary>
        /// <param name="channelId">The channel/group/dm id.</param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(string channelId, params Attachment[] attachments)
        {
            this.sendMessageQueue.Enqueue(new PostMessage(channelId, attachments));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts a message to the <paramref name="channelId"/>.
        /// </summary>
        /// <param name="channelId">The channel/group/dm id.</param>
        /// <param name="linkNames"></param>
        /// <param name="message"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task SendAsync(string channelId, string message, bool linkNames, params Attachment[] attachments)
        {
            this.sendMessageQueue.Enqueue(new PostMessage(channelId, message, attachments) { LinkNames = linkNames ? true : default(bool?) });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts a message to a thread in the <paramref name="hub"/>. If the thread is not already
        /// started then it is created.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="message"></param>
        /// <param name="linkNames"></param>
        /// <param name="replyTo"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task ReplyAsync(Hub hub, string message, Message replyTo, bool linkNames, params Attachment[] attachments)
            => this.ReplyAsync(hub.Id, message, replyTo, linkNames, attachments);


        /// <summary>
        /// Posts a message to a thread in the <paramref name="hub"/>. If the thread is not already
        /// started then it is created.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="message"></param>
        /// <param name="replyTo"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task ReplyAsync(Hub hub, string message, Message replyTo, params Attachment[] attachments)
            => this.ReplyAsync(hub.Id, message, replyTo, false, attachments);

        /// <summary>
        /// Posts a message to a thread in the <paramref name="hub"/>. If the thread is not already
        /// started then it is created.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="replyTo"></param>
        /// <param name="linkNames"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task ReplyAsync(Hub hub, Message replyTo, bool linkNames, params Attachment[] attachments)
            => this.ReplyAsync(hub.Id, replyTo, linkNames, attachments);

        /// <summary>
        /// Posts a message to a thread in the <paramref name="hub"/>. If the thread is not already
        /// started then it is created.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="replyTo"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task ReplyAsync(Hub hub, Message replyTo, params Attachment[] attachments)
            => this.ReplyAsync(hub.Id, replyTo, false, attachments);

        /// <summary>
        /// Posts a message to a thread in the <paramref name="channelId"/>. If the thread is not already
        /// started then it is created.
        /// </summary>
        /// <param name="channelId">The channel/group/dm id.</param>
        /// <param name="replyTo"></param>
        /// <param name="linkNames"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task ReplyAsync(string channelId, Message replyTo, bool linkNames, params Attachment[] attachments)
        {
            var ts = !string.IsNullOrEmpty(replyTo.RawThreadTimestamp)
                ? replyTo.RawThreadTimestamp
                : replyTo.ChannelTimestamp;
            
            this.sendMessageQueue.Enqueue(new PostMessage(channelId, attachments) { ThreadTimestamp = ts, LinkNames = linkNames ? true : default(bool?) });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts a message to a thread in the <paramref name="channelId"/>. If the thread is not already
        /// started then it is created.
        /// </summary>
        /// <param name="channelId">The channel/group/dm id.</param>
        /// <param name="message"></param>
        /// <param name="replyTo"></param>
        /// <param name="linkNames"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public Task ReplyAsync(string channelId, string message, Message replyTo, bool? linkNames, params Attachment[] attachments)
        {
            var ts = !string.IsNullOrEmpty(replyTo.RawThreadTimestamp)
                ? replyTo.RawThreadTimestamp
                : replyTo.ChannelTimestamp;
            var shouldLinkNames = linkNames.HasValue && linkNames.Value ? true : default(bool?);
            this.sendMessageQueue.Enqueue(new PostMessage(channelId, message, attachments) { ThreadTimestamp = ts, LinkNames = shouldLinkNames });
            return Task.CompletedTask;
        }

        #endregion

        #region Receive

        /// <summary>
        /// Sets up a handler to be run when a message of type <typeparamref name="TMessage"/>
        /// is read from the Slack RTM websocket connection.
        /// </summary>
        /// <remarks>
        /// See <a href="https://api.slack.com/rtm">https://api.slack.com/rtm</a> for a list of events that can be observed.
        /// <para>
        /// The <c>SlackBotNet.Messages</c> namespace contains type definitions for some of these events.
        /// </para>
        /// </remarks>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public IDisposable On<TMessage>(Action<TMessage> handler)
            where TMessage : IRtmMessage
            => this.messageBus.Observe<TMessage>().Subscribe(handler);

        /// <summary>
        /// Sets up a listener on all Slack channels that the bot is a member of. The <paramref name="handler"/> 
        /// is executed when an incoming message matches the given <paramref name="match"/>. 
        /// Messages in <see cref="HubType.Channel"/> and <see cref="HubType.Group"/> are expected to 
        /// contain the name of the bot before the handler  will fire.
        /// </summary>
        /// <param name="match">Defines when to execute this handler</param>
        /// <param name="handler">Function to be run when a message matches</param>
        /// <returns></returns>
        public IWhenHandler When(MessageMatcher match, Func<IConversation, Task> handler)
            => this.When(match, HubType.All, Modes.None, handler);

        /// <summary>
        /// Sets up a listener on the defined channelId types (see <paramref name="hubs"/>) that the bot is a member of. The <paramref name="handler"/> 
        /// is executed when an incoming message matches the given <paramref name="match"/>. 
        /// Messages in <see cref="HubType.Channel"/> and <see cref="HubType.Group"/> are expected to 
        /// contain the name of the bot before the handler  will fire.
        /// </summary>
        /// <param name="match">Defines when to execute this handler</param>
        /// <param name="hubs">The Slack channels to listen on</param>
        /// <param name="handler">Function to be run when a message matches</param>
        /// <returns></returns>
        public IWhenHandler When(MessageMatcher match, HubType hubs, Func<IConversation, Task> handler)
            => this.When(match, hubs, Modes.None, handler);

        /// <summary>
        /// Sets up a listener on all Slack channels that the bot is a member of. The <paramref name="handler"/> 
        /// is executed when an incoming message matches the given <paramref name="match"/>. 
        /// Messages in <see cref="HubType.Channel"/> and <see cref="HubType.Group"/> are expected to 
        /// contain the name of the bot before the handler  will fire.
        /// </summary>
        /// <param name="match">Defines when to execute this handler</param>
        /// <param name="modes">Configuration options</param>
        /// <param name="handler">Function to be run when a message matches</param>
        /// <returns></returns>
        public IWhenHandler When(MessageMatcher match, Modes modes, Func<IConversation, Task> handler)
            => this.When(match, HubType.All, modes, handler);

        /// <summary>
        /// Sets up a listener on the defined channelId types (see <paramref name="hubs"/>) that the bot is a member of. The <paramref name="handler"/> 
        /// is executed when an incoming message matches the given <paramref name="match"/>. 
        /// Messages in <see cref="HubType.Channel"/> and <see cref="HubType.Group"/> are expected to 
        /// contain the name of the bot before the handler  will fire.
        /// </summary>
        /// <param name="match">Defines when to execute this handler</param>
        /// <param name="hubs">The Slack channels to listen on</param>
        /// <param name="modes">Configuration options</param>
        /// <param name="handler">Function to be run when a message matches</param>
        /// <returns></returns>
        public IWhenHandler When(MessageMatcher match, HubType hubs, Modes modes, Func<IConversation, Task> handler)
        {
            bool MessageAddressesBot(Message msg) =>
                (modes & Modes.ObserveAllMessages) == Modes.ObserveAllMessages
                || msg.Text.Contains(this.state.BotUserId, StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains(this.state.BotUsername, StringComparison.OrdinalIgnoreCase);

            var whenHandler = new WhenHandler(this,
                msg =>
                {
                    if (msg.User != null && msg.User.Equals(this.state.BotUserId))
                        return MessageMatcher.NoMatch;

                    var messageHubType = this.state.GetHubById(msg.Channel).HubType;
                    if ((hubs & messageHubType) != messageHubType)
                        return MessageMatcher.NoMatch;

                    if (messageHubType != HubType.DirectMessage)
                    {
                        if (!MessageAddressesBot(msg))
                            return MessageMatcher.NoMatch;
                    }

                    match.SetupLogger(this.config.LoggerFactory);

                    return match.GetMatches(msg);
                },
                async (msg, matches) =>
                {
                    var modesCopy = modes;

                    // Conversation being initiated from another thread? force threaded mode
                    if (msg.RawThreadTimestamp != null)
                        modesCopy |= Modes.StartThread;

                    using (var conversation = new Conversation(this, modesCopy, msg, matches))
                    {
                        try
                        {
                            await handler(conversation);
                            return (true, null);
                        }
                        catch (Exception ex)
                        {
                            return (false, ex);
                        }
                    }
                });

            this.whenHandlers.Enqueue(whenHandler);
            return whenHandler;
        }

        class WhenHandler : IWhenHandler
        {
            internal event Action<Message, Exception> OnExceptionEvt = delegate { };

            private readonly SlackBot bot;

            public WhenHandler(
                SlackBot bot,
                Func<Message, Task<Match[]>> matchGenerator,
                Func<Message, Match[], Task<(bool success, Exception ex)>> messageHandler)
            {
                this.bot = bot;
                this.MatchGenerator = matchGenerator;
                this.MessageHandler = messageHandler;
            }

            public Func<Message, Task<Match[]>> MatchGenerator { get; }
            public Func<Message, Match[], Task<(bool success, Exception ex)>> MessageHandler { get; }

            internal void OnException(Message message, Exception ex)
                => this.OnExceptionEvt(message, ex);

            public IWhenHandler OnException(Action<Message, Exception> handler)
            {
                this.OnExceptionEvt += handler;
                return this;
            }

            public void Dispose()
            {
                this.bot.whenHandlers = new ConcurrentQueue<WhenHandler>(
                    this.bot.whenHandlers.Where(m => m != this)
                );
            }
        }

        public interface IWhenHandler : IDisposable
        {
            IWhenHandler OnException(Action<Message, Exception> handler);
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
            this.driver?.Dispose();
        }
    }
}