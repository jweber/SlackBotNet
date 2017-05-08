using System;
using System.Threading.Tasks;
using SlackBotNet.Messages;

namespace SlackBotNet.Matcher
{
    internal class TextMatcher : MessageMatcher
    {
        private readonly string matchText;

        public TextMatcher(string text)
        {
            this.matchText = text;
        }

        public override Task<Match[]> GetMatches(Message message)
        {
            bool bothAreNull = message.Text == null && this.matchText == null;
            bool messageContainsText = message.Text != null && message.Text.Contains(this.matchText, StringComparison.OrdinalIgnoreCase);

            if (bothAreNull || messageContainsText)
                return Task.FromResult(new[] { new Match(message.Text, 1) });

            return NoMatch;
        }
    }
}