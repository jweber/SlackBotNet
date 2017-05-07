using System.Threading.Tasks;
using SlackBotNet.Messages;

namespace SlackBotNet.Matcher
{
    internal class AndMatcher : MessageMatcher
    {
        private readonly MessageMatcher left;
        private readonly MessageMatcher right;

        public AndMatcher(MessageMatcher left, MessageMatcher right)
        {
            this.left = left;
            this.right = right;
        }

        public override async Task<Match[]> GetMatches(Message message)
        {
            var leftMatches = await this.left.GetMatches(message);
            if (leftMatches == null)
                return null;

            return await this.right.GetMatches(message);
        }
    }
}