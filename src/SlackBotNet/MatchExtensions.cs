using SlackBotNet.Matcher;
using SlackBotNet.Messages;
using System;

namespace SlackBotNet
{
    public interface IMatchExt
    { }

    class MatchExtImpl : IMatchExt
    {
        private MatchExtImpl()
        { }

        public static MatchExtImpl Instance { get; } = new MatchExtImpl();
    }

    public static class MatchFactory
    {
        public static IMatchExt Matches => MatchExtImpl.Instance;
    }

    public static class DefaultMatchExt
    {
        public static MessageMatcher TextContaining(this IMatchExt _, string text)
            => new TextMatcher(text);

        public static MessageMatcher Regex(this IMatchExt _, string pattern)
            => new RegexMatcher(pattern);

        public static MessageMatcher Message(this IMatchExt _, Func<Message, bool> predicate)
            => new PredicateMatcher(predicate);

        public static MessageMatcher Or(this MessageMatcher left, MessageMatcher right)
            => new OrMatcher(left, right);

        public static MessageMatcher And(this MessageMatcher left, MessageMatcher right)
            => new AndMatcher(left, right);
    }
}
