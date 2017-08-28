using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Messages
{
    public interface IMessage
    {
        /// <summary>
        /// Number of attempts that the SlackBotNet library has attempted to send this message to the Slack API.
        /// </summary>
        int SendAttempts { get; }
        
        string Channel { get; }
        string Text { get; }
        ParseMode? Parse { get; }
        bool? LinkNames { get; }
        IReadOnlyCollection<Attachment> Attachments { get; }
        bool? UnfurlLinks { get; }
        bool? UnfurlMedia { get; }
        string Username { get; }
        bool? AsUser { get; }
        string IconUrl { get; }
        string IconEmoji { get; }
        string ThreadTimestamp { get; }
        bool? ReplyBroadcast { get; }
    }
    
    internal static class MessageExtensions
    {
        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(this IMessage message)
        {
            string Encode(string input)
                => input
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

            bool Render<T>(T input) => input != null;

            KeyValuePair<string, string> Kvp(string name, string value)
                => new KeyValuePair<string, string>(name, Encode(value));

            KeyValuePair<string, string> Kvpb(string name, bool? value) => Kvp(name, value?.ToString().ToLower());
            
            if (Render(message.Channel)) yield return Kvp("channel", message.Channel);
            if (Render(message.Text)) yield return Kvp("text", message.Text);
            if (Render(message.Parse)) yield return Kvp("parse", message.Parse?.ToString());
            if (Render(message.LinkNames)) yield return Kvpb("link_names", message.LinkNames);
            if (Render(message.Attachments) && message.Attachments.Any())
                yield return Kvp("attachments", JsonConvert.SerializeObject(message.Attachments, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            if (Render(message.UnfurlLinks)) yield return Kvpb("unfurl_links", message.UnfurlLinks);
            if (Render(message.UnfurlMedia)) yield return Kvpb("unfurl_media", message.UnfurlMedia);
            if (Render(message.Username)) yield return Kvp("username", message.Username);
            if (Render(message.AsUser)) yield return Kvpb("as_user", message.AsUser);
            if (Render(message.IconUrl)) yield return Kvp("icon_url", message.IconUrl);
            if (Render(message.IconEmoji)) yield return Kvp("icon_emoji", message.IconEmoji);
            if (Render(message.ThreadTimestamp)) yield return Kvp("thread_ts", message.ThreadTimestamp);
            if (Render(message.ReplyBroadcast)) yield return Kvpb("reply_broadcast", message.ReplyBroadcast);
        }
    }
}