using System.Threading.Tasks;
using SlackBotNet.Messages;

namespace SlackBotNet.Matcher
{
    internal class AnyOfMatcher : MessageMatcher
    {
        private readonly string[] text;

        public AnyOfMatcher(params string[] text)
        {
            this.text = text;
        }

        public override async Task<Match[]> GetMatches(Message message)
        {

            foreach (var t in this.text)
            {
                var match = await new TextMatcher(t).GetMatches(message);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}