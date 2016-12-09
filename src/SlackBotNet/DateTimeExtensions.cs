using System;

namespace SlackBotNet
{
    internal static class DateTimeExtensions
    {
        public static DateTimeOffset FromChannelTimestamp(this string timestamp)
            => DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp.Split('.')[0]));
    }
}