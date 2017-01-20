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
            var match = this.regex.Match(message.Text);
            if (!match.Success)
                return NoMatch;

            var result = new List<Match>();

            GroupCollection collection = match.Groups;
            for (int i = 0; i < collection.Count; i++)
            {
                var group = collection[i];
                string name = this.regex.GroupNameFromNumber(i);
                result.Add(new Match(group.Value, name ?? group.Index.ToString(), 1));
            }

            return Task.FromResult(result.ToArray());
        }
    }
}