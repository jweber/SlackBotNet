namespace SlackBotNet.Matcher
{
    public static class LuisConfig
    {
        public static string SubscriptionKey { get; set; }
        public static string AppKey { get; set; }

        /// <summary>
        /// The amount of LUIS responses to keep in cache.
        /// </summary>
        public static int CacheSize { get; set; } = 100;

        internal static bool Configured =>
            !string.IsNullOrEmpty(SubscriptionKey) && 
            !string.IsNullOrEmpty(AppKey);
    }
}