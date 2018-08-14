using SlackBotNet.Drivers;
using SlackBotNet.Messages;
using SlackBotNet.Messages.Subtypes;
using SlackBotNet.State;
using SlackBotNet.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SlackBotNet.Tests
{
    public class BotInitializeTests
    {
        private SlackBotState state;
        private ISlackBotConfig config;
        private IDriver driver;
        private IMessageBus bus;

        public BotInitializeTests()
        {
            this.state = SlackBotState.Initialize("1", "testbot");
            this.config = new TestConfig();

            this.driver = new TestDriver(this.state);
            this.bus = new RxMessageBus();
        }


        [Theory]
        [MemberData(nameof(HubJoinedData))]
        public async Task HubAddedToState_WhenBotJoinsHub(Func<IHubJoined> generator)
        {
            var bot = await SlackBot.InitializeAsync(this.driver, this.bus);

            var msg = generator();

            msg.Channel = new Channel
            {
                Id = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString()
            };

            bot.WaitForMessageToBeReceived<IHubJoined>(
                m => m.Channel.Id == msg.Channel.Id,
                () => this.bus.Publish(msg));

            var hub = this.state.GetHubById(msg.Channel.Id);

            Assert.NotNull(hub);

            Assert.Equal(hub.Id, msg.Channel.Id);
            Assert.Contains(msg.Channel.Name, hub.Name);
            Assert.Equal(hub.HubType, msg.HubType);
        }

        [Theory]
        [MemberData(nameof(HubLeftData))]
        public async Task HubRemovedFromState_WhenBotLeavesHub(Func<IHubLeft> generator)
        {
            var bot = await SlackBot.InitializeAsync(this.driver, this.bus);

            var msg = generator();
            msg.Channel = Guid.NewGuid().ToString();

            this.state.AddHub(msg.Channel, msg.Channel, msg.HubType);

            bot.WaitForMessageToBeReceived<IHubLeft>(
                m => m.Channel == msg.Channel,
                () => this.bus.Publish(msg));

            Assert.Null(this.state.GetHubById(msg.Channel));
        }

        public static IEnumerable<object[]> HubJoinedData()
        {
            yield return new object[] { new Func<IHubJoined>(() => new ImCreated()) };
            yield return new object[] { new Func<IHubJoined>(() => new GroupJoined()) };
            yield return new object[] { new Func<IHubJoined>(() => new ChannelJoined()) };
        }

        public static IEnumerable<object[]> HubLeftData()
        {
            yield return new object[] { new Func<IHubLeft>(() => new ImClose()) };
            yield return new object[] { new Func<IHubLeft>(() => new GroupLeft()) };
            yield return new object[] { new Func<IHubLeft>(() => new ChannelLeft()) };
        }
    }
}
