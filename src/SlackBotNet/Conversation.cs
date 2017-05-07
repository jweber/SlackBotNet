using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;
using SlackBotNet.State;

namespace SlackBotNet
{
    public interface IConversation : IDisposable
    {
        Task PostMessage(string message, params Attachment[] attachments);
        Task<IReply> WaitForReply(MessageMatcher match = null);
        void End();
    }

    public interface IReply
    {
        User From { get; }
        string Text { get; }
        Match[] Matches { get; }
    }

    internal class Reply : IReply
    {
        public Reply(User @from, string text, Match[] matches)
        {
            this.From = @from;
            this.Text = text;
            this.Matches = matches;
        }

        public User From { get; }
        public string Text { get; }
        public Match[] Matches { get; }
    }

    internal class Conversation : IConversation
    {
        private readonly SlackBot bot;
        private readonly CancellationTokenSource tokenSource;

        private Message rootMessage;
        private Modes modes;

        internal Conversation(SlackBot bot, Modes modes, Message message, Match[] matches)
            : this(bot, modes, message, matches, new CancellationTokenSource())
        { }

        private Conversation(SlackBot bot, Modes modes, Message message, Match[] matches, CancellationTokenSource tokenSource)
        {
            this.bot = bot;
            this.modes = modes;
            this.rootMessage = message;
            this.tokenSource = tokenSource;

            this.From = bot.State.GetUser(message.User);
            this.Hub = bot.State.GetHubById(message.Channel);
            this.Text = message.Text;
            this.Matches = matches;
        }

        public User From { get; }
        public Hub Hub { get; }
        public string Text { get; }
        public Match[] Matches { get; }

        private bool IsThreaded => (this.modes & Modes.StartThread) == Modes.StartThread;

        public Task PostMessage(string message, params Attachment[] attachments)
        {
            if (this.tokenSource.IsCancellationRequested)
                return Task.CompletedTask;

            if (this.IsThreaded)
                return this.bot.ReplyAsync(this.rootMessage.Channel, message, this.rootMessage, attachments);

            return this.bot.SendAsync(this.rootMessage.Channel, message, attachments);
        }

        public async Task<IReply> WaitForReply(MessageMatcher match = null)
        {
            if (this.tokenSource.IsCancellationRequested)
                return null;

            // msg.User == this.rootMessage.User

            bool MessageRepliesToThread(Message msg) => this.IsThreaded && msg.RawThreadTimestamp != null && (msg.RawThreadTimestamp == this.rootMessage.ChannelTimestamp || msg.RawThreadTimestamp == this.rootMessage.RawThreadTimestamp);
            bool MessageRepliesInChannel(Message msg) => !this.IsThreaded && msg.User == this.rootMessage.User;

            Match[] matches = null;

            var reply = await this.bot.Linger<Message>(msg =>
            {
                bool preconditionsMatch = msg.Channel == this.rootMessage.Channel
                                          && this.MessageAddressesBot(msg)
                                          && (MessageRepliesToThread(msg) || MessageRepliesInChannel(msg));

                if (!preconditionsMatch)
                    return false;

                var (matchSuccess, results) = this.MessageMatches(msg, match).GetAwaiter().GetResult();
                matches = results;

                return matchSuccess;
            });

            if (this.tokenSource.IsCancellationRequested)
                return null;

            // flip to threaded?
            if (!this.IsThreaded && reply.RawThreadTimestamp != null)
            {
                this.modes |= Modes.StartThread;
                this.rootMessage = reply;
            }
            
            return new Reply(
                this.bot.State.GetUser(reply.User),
                reply.Text,
                matches);
        }

        public void End() => this.tokenSource.Cancel();

        private async Task<(bool success, Match[] matches)> MessageMatches(Message msg, MessageMatcher matcher)
        {
            if (matcher == null)
                return (true, null);

            var matches = await matcher.GetMatches(msg);

            bool success = matches != null && matches.Sum(m => m.Score) >= 0;

            if (!success)
                return (false, null);

            return (true, matches);
        }

        private bool MessageAddressesBot(Message msg)
        {
            if (msg.User == null)
                return false;

            if (msg.User.Equals(this.bot.State.BotUserId))
                return false;

            return this.Hub.HubType == HubType.DirectMessage
                || (this.modes & Modes.ObserveAllMessages) == Modes.ObserveAllMessages
                || msg.Text.Contains(this.bot.State.BotUserId, StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains(this.bot.State.BotUsername, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            this.End();
            this.tokenSource.Dispose();
        } 
    }
}