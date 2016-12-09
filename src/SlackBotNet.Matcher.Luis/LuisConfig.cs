namespace SlackBotNet.Matcher
{
    public static class LuisConfig
    {
        public static string SubscriptionKey { get; set; }
        public static string AppKey { get; set; }

        internal static bool Configured =>
            !string.IsNullOrEmpty(SubscriptionKey) && 
            !string.IsNullOrEmpty(AppKey);
    }
}