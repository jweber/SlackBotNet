using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using SlackBotNet.Matcher;
using SlackBotNet.Messages;

namespace SlackBotNet
{
    public abstract class MessageMatcher
    {
        public void SetupLogger(ILoggerFactory factory)
        {
            this.Logger = factory.CreateLogger(this.GetType());
        }
        
        protected ILogger Logger { get; private set; }
        
        [NotNull]
        public abstract Task<Match[]> GetMatches(Message message);

        public static Task<Match[]> NoMatch => Task.FromResult((Match[])null);

        public static MessageMatcher operator |(MessageMatcher left, MessageMatcher right)
            => new OrMatcher(left, right);

        public static MessageMatcher operator &(MessageMatcher left, MessageMatcher right)
            => new AndMatcher(left, right);

        // Always evaluate to false to force both sides to be evaluated
        public static bool operator true(MessageMatcher m) => false;
        public static bool operator false(MessageMatcher m) => false;

        /// <summary>
        /// Implicit conversion of <see cref="string"/> -> <see cref="TextMatcher"/>
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator MessageMatcher(string text) => new TextMatcher(text);
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