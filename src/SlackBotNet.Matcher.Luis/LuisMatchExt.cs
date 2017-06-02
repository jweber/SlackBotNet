using SlackBotNet.Matcher;

namespace SlackBotNet
{
    public static class LuisMatchExt
    {
        public static MessageMatcher LuisIntent(this IMatchExt _, string intentName, decimal confidenceThreshold = 0.9m, bool spellCheck = true)
            => new LuisIntentMatcher(intentName, confidenceThreshold, spellCheck);
    }
}