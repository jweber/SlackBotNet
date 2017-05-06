using SlackBotNet.State;
using SlackBotNet.Tests.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SlackBotNet.Tests
{
    public class SendMessageTests
    {
        private SlackBotState state;
        private ISlackBotConfig config;
        private IMessageBus bus;

        public SendMessageTests()
        {
            this.state = SlackBotState.Initialize("1", "testbot");
            this.config = new TestConfig();

            this.bus = new RxMessageBus();
        }

        [Fact]
        public async Task OutgoingMessages_LimitedToOnePerSecond()
        {
            var evt = new CountdownEvent(3);
            var driver = new CountdownDriver(evt);

            var bot = await SlackBot.InitializeAsync("", driver, this.bus);

            await bot.SendAsync("test", "test");
            await bot.SendAsync("test", "test");
            await bot.SendAsync("test", "test");

            if (!evt.Wait(TimeSpan.FromSeconds(4)))
                Assert.True(false, "3 messages were not recorded as being sent");

            Assert.Equal(3, driver.RecordedTimings.Count);

            TimeSpan TimeBetween(int p1, int p2)
                => driver
                    .RecordedTimings[Math.Max(p1, p2)]
                    .Subtract(
                        driver
                            .RecordedTimings[Math.Min(p1, p2)]);

            Assert.True(TimeBetween(0, 1) > TimeSpan.FromSeconds(1));
            Assert.True(TimeBetween(1, 2) > TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData('&', "&amp;")]
        [InlineData('<', "&lt;")]
        [InlineData('>', "&gt;")]
        public async Task MessageIsEncoded(char unenc, string enc)
        {
            var evt = new CountdownEvent(1);
            var driver = new CountdownDriver(evt);

            var bot = await SlackBot.InitializeAsync("", driver, this.bus);

            await bot.SendAsync("test", $"test {unenc} test");

            evt.Wait(TimeSpan.FromMilliseconds(1200));

            Assert.True(
                driver.RecordedMessages[0].Text.Contains($"test {enc} test")
            );
        }
    }
}
