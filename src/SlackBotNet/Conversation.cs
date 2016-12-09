using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SlackBotNet.Messages;
using SlackBotNet.State;

namespace SlackBotNet
{
    public class Conversation
    {
        private readonly SlackBot bot;

        internal Conversation(SlackBot bot, User @from, Hub hub, string text, Match[] matches)
        {
            this.bot = bot;
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
            await bot.SendAsync(this.Hub.Id, message);

            bool MessageAddressesBot(Message msg)
            {
                if (this.Hub.HubType == HubType.DirectMessage)
                    return true;

                bool messageAddressesBot = msg.Text?.IndexOf(this.bot.State.BotUsername, StringComparison.OrdinalIgnoreCase) > -1;
                if (messageAddressesBot)
                    return true;

                return false;
            }

            var reply = await bot.Linger<Message>(@where: msg => 
                msg.User == this.From.Id 
                && msg.Channel == this.Hub.Id
                && MessageAddressesBot(msg));

            return new Conversation(bot, this.From, this.Hub, reply.Text, null);
        }
    }
}