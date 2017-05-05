using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SlackBotNet.Messages;
using System.Collections.Generic;

namespace SlackBotNet.Matcher
{
    internal class RegexMatcher : MessageMatcher
    {
        private readonly Regex regex;

        public RegexMatcher(string pattern)
        {
            this.regex = new Regex(pattern, RegexOptions.Compiled);
        }

        public override Task<Match[]> GetMatches(Message message)
        {
            if (message.Text == null)
                return NoMatch;

            var matches = this.regex.Matches(message.Text);

            if (matches.Count == 0)
                return NoMatch;

            var result = new List<Match>();

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (!match.Success)
                    continue;

                GroupCollection collection = match.Groups;
                for (int i = 0; i < collection.Count; i++)
                {
                    var group = collection[i];
                    string name = this.regex.GroupNameFromNumber(i);

                    result.Add(new Match(group.Value, name ?? group.Index.ToString(), 1));
                }
            }

            if (!result.Any())
                return NoMatch;

            return Task.FromResult(result.ToArray());
        }
    }
}