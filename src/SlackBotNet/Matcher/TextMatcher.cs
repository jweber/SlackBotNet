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
            if (message.Text?.IndexOf(this.matchText, StringComparison.OrdinalIgnoreCase) > -1)
                return Task.FromResult(new[] { new Match(message.Text, 1) });

            return NoMatch;
        }
    }
}