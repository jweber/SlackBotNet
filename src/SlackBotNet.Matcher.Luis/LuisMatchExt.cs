using SlackBotNet.Matcher;

namespace SlackBotNet
{
    public static class LuisMatchExt
    {
        public static MessageMatcher LuisIntent(this IMatchExt _, string intentName, decimal confidenceThreshold = 0.9m)
            => new LuisIntentMatcher(intentName, confidenceThreshold);
    }
}