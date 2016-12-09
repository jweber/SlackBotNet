using SlackBotNet.Messages;
using System;
using System.Threading;
using Xunit;

namespace SlackBotNet.Tests.Infrastructure
{
    public static class SlackBotExtensions
    {
        public static void WaitForMessageToBeReceived<T>(this SlackBot bot, Predicate<T> matcher, Action action)
            where T : IRtmMessage
        {
            var evt = new AutoResetEvent(false);
            using (bot.On<T>(msg => { if (matcher(msg)) evt.Set(); }))
            {
                action();

                if (!evt.WaitOne(100))
                    Assert.True(false, $"{typeof(T).Name} message was not received");
            }
        }
    }
}
