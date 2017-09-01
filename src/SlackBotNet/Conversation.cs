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
        User From { get; }
        Hub Hub { get; }
        string Text { get; }
        Match[] Matches { get; }

        /// <summary>
        /// Sends a message to the channel that the bot is listening on.
        /// </summary>
        /// <param name="message">The message text. Emoji and markdown are supported.</param>
        /// <param name="attachments">Optional list of attachments to add to the message.</param>
        /// <returns></returns>
        Task PostMessage(string message, params Attachment[] attachments);
        
        /// <summary>
        /// Sends a message to the channel that the bot is listening on.
        /// </summary>
        /// <param name="message">The message text. Emoji and markdown are supported.</param>
        /// <param name="linkNames"></param>
        /// <param name="attachments">Optional list of attachments to add to the message.</param>
        /// <returns></returns>
        Task PostMessage(string message, bool linkNames, params Attachment[] attachments);

        /// <summary>
        /// Sends a message to the channel that the bot is listening on.
        /// </summary>
        /// <param name="attachments">List of attachments to add to the message.</param>
        /// <returns></returns>
        Task PostMessage(params Attachment[] attachments);

        /// <summary>
        /// Tells the bot to wait until a message comes in that meets the following criteria:
        /// <para>1. If the bot is conversing directly in a channel, another message is posted by original User that triggered the bot</para>
        /// <para>2. If the bot is conversing in a thread, any message posted in that thread will be looked at</para>
        /// <para>3. If a <paramref name="match"/> is defined, the message must pass the matcher(s).</para>
        /// </summary>
        /// <param name="match">Criteria that the message must meet</param>
        /// <param name="onNotMatch">If a message does not meet the <paramref name="match"/> criteria, this action is invoked.</param>
        /// <returns></returns>
        Task<IReply> WaitForReply(MessageMatcher match = null, Action<Message> onNotMatch = null);

        /// <summary>
        /// Ends the conversation.
        /// </summary>
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
                return this.bot.ReplyAsync(this.rootMessage.Channel, message, this.rootMessage, false, attachments);

            return this.bot.SendAsync(this.rootMessage.Channel, message, false, attachments);
        }

        public Task PostMessage(string message, bool linkNames, params Attachment[] attachments)
        {
            if (this.tokenSource.IsCancellationRequested)
                return Task.CompletedTask;

            if (this.IsThreaded)
                return this.bot.ReplyAsync(this.rootMessage.Channel, message, this.rootMessage, linkNames, attachments);

            return this.bot.SendAsync(this.rootMessage.Channel, message, linkNames, attachments);
        }

        public Task PostMessage(params Attachment[] attachments)
        {
            if (this.tokenSource.IsCancellationRequested)
                return Task.CompletedTask;

            if (this.IsThreaded)
                return this.bot.ReplyAsync(this.rootMessage.Channel, this.rootMessage, false, attachments);

            return this.bot.SendAsync(this.rootMessage.Channel, false, attachments);
        }

        public async Task<IReply> WaitForReply(MessageMatcher match = null, Action<Message> onNotMatch = null)
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

                if (match == null)
                    return true;

                var (matchSuccess, results) = this.MessageMatches(msg, match).GetAwaiter().GetResult();
                matches = results;

                if (!matchSuccess)
                    onNotMatch?.Invoke(msg);

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