using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SlackBotNet.Matcher;
using SlackBotNet.Messages;

namespace SlackBotNet
{
    public abstract class MessageMatcher
    {
        [NotNull]
        public abstract Task<Match[]> GetMatches(Message message);

        public static Task<Match[]> NoMatch => Task.FromResult((Match[])null);

        public static MessageMatcher operator | (MessageMatcher left, MessageMatcher right)
            => new OrMatcher(left, right);

        // Always evaluate to false to force both sides to be evaluated
        public static bool operator true(MessageMatcher m) => false;
        public static bool operator false(MessageMatcher m) => false;
    }

    [DebuggerDisplay("{Text} [Score: {Score}] [Category: {Category}]")]
    public class Match
    {
        public Match(string text)
        {
            this.Text = text;
        }

        public Match(string text, decimal score)
        {
            this.Text = text;
            this.Score = score;
        }

        public Match(string text, string category)
        {
            this.Text = text;
            this.Category = category;
        }

        public Match(string text, string category, decimal score)
        {
            this.Text = text;
            this.Category = category;
            this.Score = score;
        }

        public string Text { get; }
        public string Category { get; }
        public decimal Score { get; }
    }
}