using SlackBotNet.Drivers;
using SlackBotNet.Messages;
using SlackBotNet.State;
using SlackBotNet.Tests.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using static SlackBotNet.MatchFactory;
using System;

namespace SlackBotNet.Tests
{
    public class WhenHandlerTests
    {
        private SlackBotState state;
        private ISlackBotConfig config;
        private IDriver driver;
        private IMessageBus bus;


        public WhenHandlerTests()
        {
            this.state = SlackBotState.Initialize("a", "testbot");
            this.config = new TestConfig();

            this.driver = new TestDriver(this.state);
            this.bus = new RxMessageBus();
        }

        [Fact]
        public async Task MatchMode_FirstMatch()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus, cfg =>
            {
                cfg.WhenHandlerMatchMode = WhenHandlerMatchMode.FirstMatch;
            });

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.DirectMessage);

            var evt = new AutoResetEvent(false);

            bot.When(Matches.TextContaining("test"), cv =>
            {
                evt.Set();
                return Task.CompletedTask;
            });

            bot.When(Matches.TextContaining("test"), cv =>
            {
                Assert.True(false, "Second handler should not be fired");
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "test"
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "First handler never fired");
        }
        
        [Fact]
        public async Task MatchMode_AllMatches()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus, cfg =>
            {
                cfg.WhenHandlerMatchMode = WhenHandlerMatchMode.AllMatches;
            });

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.DirectMessage);

            var evt = new CountdownEvent(2);

            bot.When(Matches.TextContaining("test"), cv =>
            {
                evt.Signal();
                return Task.CompletedTask;
            });

            bot.When(Matches.TextContaining("test"), cv =>
            {
                evt.Signal();
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "test"
            });

            if (!evt.Wait(100))
                Assert.True(false, "Both handlers were not fired");
        }

        class FixedScoreMatcher : MessageMatcher
        {
            private readonly decimal score;

            public FixedScoreMatcher(decimal score)
            {
                this.score = score;
            }

            public override Task<Match[]> GetMatches(Message message)
                => Task.FromResult(new[] { new Match(message.Text, this.score) });
        }

        [Fact]
        public async Task MatchMode_BestMatch()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus, cfg =>
            {
                cfg.WhenHandlerMatchMode = WhenHandlerMatchMode.BestMatch;
            });

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.DirectMessage);

            var evt = new AutoResetEvent(false);

            bot.When(new FixedScoreMatcher(1), cv =>
            {
                Assert.True(false, "Low score handler should not have been fired");
                return Task.CompletedTask;
            });

            bot.When(new FixedScoreMatcher(2), cv =>
            {
                evt.Set();
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "test"
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "High score handler was not fired");
        }

        [Fact]
        public async Task DirectMessage_DoesNotRequireAddressingBotByName()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.DirectMessage);

            var evt = new AutoResetEvent(false);

            bot.When(Matches.TextContaining("hello"), c =>
            {
                evt.Set();
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "hello"
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "When handler never fired");
        }
        
        [Fact]
        public async Task GroupMessage_BotDoesNotRespondWhenNotAddressedByName()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.Group);

            var evt = new AutoResetEvent(false);

            bot.When(Matches.TextContaining("hello"), c =>
            {
                evt.Set();
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "hello"
            });

            if (evt.WaitOne(100))
                Assert.True(false, "When handler should not have been fired since the message didn't address the bot");
        }

        [Theory]
        [InlineData("{0} hello")]
        [InlineData("hello {0}")]
        public async Task GroupMessage_RespondsWhenAddressingBotByName(string message)
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.Group);

            var evt = new AutoResetEvent(false);

            bot.When(Matches.TextContaining("hello"), c =>
            {
                evt.Set();
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = string.Format(message, this.state.BotUsername)
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "When handler never fired");
        }

        [Fact]
        public async Task ChannelMessage_BotDoesNotRespondWhenNotAddressedByName()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.Channel);

            var evt = new AutoResetEvent(false);

            bot.When(
                Matches.TextContaining("hello"),
                HubType.Channel,
                c =>
                {
                    evt.Set();
                    return Task.CompletedTask;
                });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "hello"
            });

            if (evt.WaitOne(100))
                Assert.True(false, "When handler should not have been fired since the message didn't address the bot");
        }

        [Fact]
        public async Task ChannelMessage_BotRespondWhenNotAddressedByName_WhenConfiguredToListenToAllMessages()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.Channel);

            var evt = new AutoResetEvent(false);

            bot.When(
                Matches.TextContaining("hello"),
                HubType.Channel | HubType.ObserveAllMessages,
                c =>
                {
                    evt.Set();
                    return Task.CompletedTask;
                });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "hello"
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "When handler should have been fired since the message didn't address the bot");
        }

        [Theory]
        [InlineData("{0} hello")]
        [InlineData("hello {0}")]
        public async Task ChannelMessage_RespondsWhenAddressingBotByName(string message)
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "test hub", HubType.Channel);

            var evt = new AutoResetEvent(false);

            bot.When(Matches.TextContaining("hello"), c =>
            {
                evt.Set();
                return Task.CompletedTask;
            });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = string.Format(message, this.state.BotUsername)
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "When handler never fired");
        }

        [Fact]
        public async Task ExceptionInHandler_TriggersOnExceptionCallback()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "", HubType.DirectMessage);

            var evt = new AutoResetEvent(false);

            bot
                .When(Matches.TextContaining("hello"), c => throw new Exception("exception message"))
                .OnException((msg, ex) =>
                {
                    Assert.Equal("exception message", ex.Message);
                    evt.Set();
                });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "hello"
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "When handler never fired");
        }

        class ExceptionThrowingMatcher : MessageMatcher
        {
            private readonly Func<Exception> exceptionGenerator;

            public ExceptionThrowingMatcher(Func<Exception> exceptionGenerator)
            {
                this.exceptionGenerator = exceptionGenerator;
            }

            public override Task<Match[]> GetMatches(Message message) => throw this.exceptionGenerator();
        }

        [Fact]
        public async Task ExceptionInMatcher_TriggersOnExceptionCallback()
        {
            var bot = await SlackBot.InitializeAsync("", this.driver, this.bus);

            this.state.AddUser("1", "user");
            this.state.AddHub("1", "", HubType.DirectMessage);

            var evt = new AutoResetEvent(false);

            bot
                .When(new ExceptionThrowingMatcher(() => new Exception("exception message")), c => Task.CompletedTask)
                .OnException((msg, ex) =>
                {
                    Assert.Equal("exception message", ex.Message);
                    evt.Set();
                });

            this.bus.Publish(new Message
            {
                Channel = "1",
                User = "1",
                Text = "hello"
            });

            if (!evt.WaitOne(100))
                Assert.True(false, "When handler never fired");
        }
    }
}
