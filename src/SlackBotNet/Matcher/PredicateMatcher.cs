using System;
using System.Threading.Tasks;
using SlackBotNet.Messages;

namespace SlackBotNet.Matcher
{
    internal class PredicateMatcher : MessageMatcher
    {
        private readonly Func<Message, bool> predicate;

        public PredicateMatcher(Func<Message, bool> predicate)
        {
            this.predicate = predicate 
                ?? throw new NullReferenceException($"{nameof(predicate)} cannot be null");
        }

        public override Task<Match[]> GetMatches(Message message)
        {
            if (!this.predicate.Invoke(message))
                return base.NoMatch;

            return Task.FromResult(new[] { new Match(message.Text, 1) });
        }
    }
}