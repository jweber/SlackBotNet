using System;

namespace SlackBotNet
{
    internal static class DateTimeExtensions
    {
        public static DateTimeOffset FromRawTimestamp(this string timestamp)
            => DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp.Split('.')[0]));
    }

    internal static class StringExtensions
    {
        public static bool Contains(this string input, string substring, StringComparison stringComparison)
            => !string.IsNullOrEmpty(input) && input.IndexOf(substring, stringComparison) > -1;
    }
}