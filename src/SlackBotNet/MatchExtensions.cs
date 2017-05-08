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
        /// <summary>
        /// Returns success if the <paramref name="text"/> is a case-insesitive substring
        /// of the message text.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MessageMatcher Text(this IMatchExt _, string text)
            => new TextMatcher(text);

        /// <summary>
        /// Returns success if any of the strings defined in <paramref name="text"/>
        /// exist as a case-insensitive substring of the message text.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MessageMatcher Any(this IMatchExt _, params string[] text)
            => new AnyOfMatcher(text);

        /// <summary>
        /// Returns success if the regex <paramref name="pattern"/> matches the message text.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static MessageMatcher Regex(this IMatchExt _, string pattern)
            => new RegexMatcher(pattern);

        /// <summary>
        /// Returns success if the <paramref name="predicate"/> function matches on the message.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static MessageMatcher Message(this IMatchExt _, Func<Message, bool> predicate)
            => new PredicateMatcher(predicate);

        /// <summary>
        /// Combines two <see cref="MessageMatcher"/> instances and returns success if either one matches. 
        /// The <c>||</c> operator is the same as using this matcher.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static MessageMatcher Or(this MessageMatcher left, MessageMatcher right)
            => new OrMatcher(left, right);

        /// <summary>
        /// Combines two <see cref="MessageMatcher"/> instances and returns success if both match.
        /// The <c>&&</c> operator is the same as using this matcher.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static MessageMatcher And(this MessageMatcher left, MessageMatcher right)
            => new AndMatcher(left, right);
    }
}
