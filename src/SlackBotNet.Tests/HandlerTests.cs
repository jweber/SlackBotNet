using SlackBotNet.Drivers;
using Xunit;
using SlackBotNet.State;
using SlackBotNet.Tests.Infrastructure;
using System.Threading.Tasks;
using System.Threading;
using SlackBotNet.Messages;

namespace SlackBotNet.Tests
{
    public class HandlerTests : IAsyncLifetime
    {
        private SlackBotState state;
        private ISlackBotConfig config;

        private IDriver driver;
        private IMessageBus bus;

        private SlackBot bot;

        public async Task InitializeAsync()
        {
            this.state = SlackBotState.Initialize("1", "testbot");
            this.config = new TestConfig();

            this.driver = new TestDriver(this.state);
            this.bus = new RxMessageBus();

            this.bot = await SlackBot.InitializeAsync("", this.driver, this.bus);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public interface ITestMessage : IRtmMessage
        { }

        public class TestMessage : ITestMessage
        { }

        public class ChildTestMessage : TestMessage
        { }

        [Fact]
        public void OnHandler_WithConcreteTypeRegisterd()
        {
            var evt = new AutoResetEvent(false);
            bot.On<TestMessage>(msg =>
            {
                evt.Set();
            });

            this.bus.Publish(new TestMessage());

            if (!evt.WaitOne(10))
                Assert.True(false, "Callback for Message was not triggered");
        }

        [Fact]
        public void OnHandler_WithInterfaceTypeRegistered()
        {
            var evt = new AutoResetEvent(false);
            bot.On<ITestMessage>(msg =>
            {
                evt.Set();
            });

            this.bus.Publish(new TestMessage());

            if (!evt.WaitOne(10))
                Assert.True(false, "Callback for Message was not triggered");
        }

        [Fact]
        public void OnHandler_BaseMessage_DoesNotTriggerChildHandler()
        {
            bot.On<ChildTestMessage>(msg =>
            {
                Assert.True(false, "ChildTestMessage handler should not have been triggered");
            });

            this.bus.Publish(new TestMessage());
            Thread.Sleep(10);
        }
        
        [Fact]
        public void OnHandler_ChildMessage_DoesNotTriggerBaseHandler()
        {
            bot.On<TestMessage>(msg =>
            {
                Assert.True(false, "TestMessage handler should not have been triggered");
            });

            this.bus.Publish(new ChildTestMessage());
            Thread.Sleep(10);
        }
    }
}
