﻿using SlackBotNet.State;
using SlackBotNet.Tests.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;
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

            var bot = await SlackBot.InitializeAsync(driver, this.bus);

            await bot.SendAsync("test", "test", false, null);
            await bot.SendAsync("test", "test", false, null);
            await bot.SendAsync("test", "test", false, null);

            if (!evt.Wait(TimeSpan.FromSeconds(4)))
                Assert.True(false, "3 messages were not recorded as being sent");

            Assert.Equal(3, driver.RecordedTimings.Count);

            TimeSpan TimeBetween(int p1, int p2)
                => driver
                    .RecordedTimings[Math.Max(p1, p2)]
                    .Subtract(
                        driver
                            .RecordedTimings[Math.Min(p1, p2)]);

            var first = TimeBetween(0, 1);
            var second = TimeBetween(1, 2);

            // allow some wiggle room with timing
            var measure = TimeSpan.FromSeconds(0.98);
            
            Assert.True(TimeBetween(0, 1) >= measure, $"Expected {first} >= {measure}");
            Assert.True(TimeBetween(1, 2) >= measure, $"Expected {second} >= {measure}");
        }

        [Theory]
        [InlineData('&', "&amp;")]
        [InlineData('<', "&lt;")]
        [InlineData('>', "&gt;")]
        public void MessageIsEncoded(char unenc, string enc)
        {
            var message = new PostMessage("channel", $"test {unenc} test");

            var result = message
                .ToKeyValuePairs()
                .First(m => m.Key == "text");

            Assert.Equal($"test {enc} test", result.Value);
        }
    }
}
