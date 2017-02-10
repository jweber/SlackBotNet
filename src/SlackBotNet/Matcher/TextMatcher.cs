using System;
using System.Threading.Tasks;
using SlackBotNet.Messages;
using SlackBotNet.Infrastructure;

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
            if (message.Text.Contains(this.matchText, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new[] { new Match(message.Text, 1) });

            return NoMatch;
        }
    }
}