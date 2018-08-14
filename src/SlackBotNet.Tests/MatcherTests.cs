using SlackBotNet.Matcher;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SlackBotNet.Tests
{
    public class MatcherTests
    {
        [Fact]
        public async Task Regex_NoMatch()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new RegexMatcher("^test$");
            var matches = await matcher.GetMatches(message);

            Assert.Null(matches);
        }

        [Fact]
        public async Task Regex_Simple()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new RegexMatcher("world$");
            var matches = await matcher.GetMatches(message);

            Assert.Single(matches);
            Assert.Equal("world", matches[0].Text);
            Assert.Equal("0", matches[0].Category);
        }

        [Fact]
        public async Task Regex_NamedGroup()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new RegexMatcher("wor(?<g1>ld)$");
            var matches = await matcher.GetMatches(message);

            Assert.Equal(2, matches.Length);
            Assert.Equal("world", matches[0].Text);
            Assert.Equal("0", matches[0].Category);

            Assert.Equal("ld", matches[1].Text);
            Assert.Equal("g1", matches[1].Category);
        }

        [Fact]
        public async Task Text_NoMatch()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new TextMatcher("test");
            var matches = await matcher.GetMatches(message);

            Assert.Null(matches);
        }

        [Fact]
        public async Task Text_Match()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new TextMatcher("hello world");
            var matches = await matcher.GetMatches(message);

            Assert.Single(matches);
            Assert.Equal("hello world", matches[0].Text);
        }

        [Fact]
        public async Task Predicate_NoMatch()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new PredicateMatcher(msg => msg.Text == "test");
            var matches = await matcher.GetMatches(message);

            Assert.Null(matches);
        }

        [Fact]
        public async Task Predicate_Match()
        {
            var message = new Messages.Message();
            message.Text = "hello world";

            var matcher = new PredicateMatcher(msg => msg.Text.Equals("HELLO WORLD", StringComparison.OrdinalIgnoreCase));
            var matches = await matcher.GetMatches(message);

            Assert.Single(matches);
            Assert.Equal(message.Text, matches[0].Text);
        }
    }
}
