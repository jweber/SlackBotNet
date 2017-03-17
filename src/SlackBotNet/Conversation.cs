using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using SlackBotNet.Messages;
using SlackBotNet.State;

namespace SlackBotNet
{
    public class Conversation
    {
        private readonly SlackBot bot;
        private readonly Func<Message, bool> messageAddressesBot;

        internal Conversation(SlackBot bot, User @from, Hub hub, string text, Match[] matches, Func<Message, bool> messageAddressesBot)
        {
            this.bot = bot;
            this.messageAddressesBot = messageAddressesBot;

            this.From = @from;
            this.Hub = hub;
            this.Text = text;
            this.Matches = matches;
        }

        public User From { get; }
        public Hub Hub { get; }
        public string Text { get; }
        public Match[] Matches { get; }

        public async Task<Conversation> ReplyAsync(string message)
        {
            await this.bot.SendAsync(this.Hub.Id, message);

            bool MessageAddressesBot(Message msg)
            {
                if (msg.User.Equals(this.bot.State.BotUserId))
                    return false;

                if (this.Hub.HubType == HubType.DirectMessage)
                    return true;

                if (this.messageAddressesBot(msg))
                    return true;

                return false;
            }

            var reply = await bot.Linger<Message>(@where: msg => 
                msg.User == this.From.Id 
                && msg.Channel == this.Hub.Id
                && MessageAddressesBot(msg));

            return new Conversation(
                this.bot, 
                this.From, 
                this.Hub, 
                reply.Text, 
                null, 
                this.messageAddressesBot);
        }
    }
}