using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SlackBotNet.Messages.WebApi
{
    class PostMessageResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("ts")]
        public string RawTimestamp { get; set; }

        public DateTimeOffset Timestamp
            => this.RawTimestamp.FromRawTimestamp();

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("message")]
        public PostMessage Message { get; set; }
    }

    class PostMessage
    {
        [JsonConstructor]
        private PostMessage()
        { }

        public PostMessage(string channel)
        {
            this.Channel = channel;
        }

        public PostMessage(string channel, string text)
        {
            this.Channel = channel;
            this.Text = text;
        }

        public PostMessage(string channel, params Attachment[] attachments)
        {
            this.Channel = channel;
            this.Attachments = attachments;
        }

        public PostMessage(string channel, string text, params Attachment[] attachments)
        {
            this.Channel = channel;
            this.Text = text;
            this.Attachments = attachments;
        }

        [JsonProperty("channel")]
        public string Channel { get; }

        [JsonProperty("text")]
        public string Text { get; }

        [JsonProperty("parse"), JsonConverter(typeof(StringEnumConverter))]
        public ParseMode? Parse { get; set; }

        [JsonProperty("link_names")]
        public bool? LinkNames { get; set; }

        [JsonProperty("attachments")]
        public IReadOnlyCollection<Attachment> Attachments { get; }

        [JsonProperty("unfurl_links")]
        public bool? UnfurlLinks { get; set; }

        [JsonProperty("unfurl_media")]
        public bool? UnfurlMedia { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("as_user")]
        public bool? AsUser { get; set; } = true;

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_emoji")]
        public string IconEmoji { get; set; }

        [JsonProperty("thread_ts")]
        public string ThreadTimestamp { get; set; }

        [JsonProperty("reply_broadcast")]
        public bool? ReplyBroadcast { get; set; }

        public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs()
        {
            string encode(string input)
                => input
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

            bool render<T>(T input) => input != null;

            KeyValuePair<string, string> kvp(string name, string value)
                => new KeyValuePair<string, string>(name, encode(value));

            KeyValuePair<string, string> kvpb(string name, bool? value) => kvp(name, value?.ToString().ToLower());
            
            yield return kvp("channel", this.Channel);

            if (render(this.Text)) yield return kvp("text", this.Text);
            if (render(this.Parse)) yield return kvp("parse", this.Parse?.ToString());
            if (render(this.LinkNames)) yield return kvpb("link_names", this.LinkNames);
            if (render(this.Attachments) && this.Attachments.Any())
                yield return kvp("attachments", JsonConvert.SerializeObject(this.Attachments, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            if (render(this.UnfurlLinks)) yield return kvpb("unfurl_links", this.UnfurlLinks);
            if (render(this.UnfurlMedia)) yield return kvpb("unfurl_media", this.UnfurlMedia);
            if (render(this.Username)) yield return kvp("username", this.Username);
            if (render(this.AsUser)) yield return kvpb("as_user", this.AsUser);
            if (render(this.IconUrl)) yield return kvp("icon_url", this.IconUrl);
            if (render(this.IconEmoji)) yield return kvp("icon_emoji", this.IconEmoji);
            if (render(this.ThreadTimestamp)) yield return kvp("thread_ts", this.ThreadTimestamp);
            if (render(this.ReplyBroadcast)) yield return kvpb("reply_broadcast", this.ReplyBroadcast);
        }
    }

    internal enum ParseMode
    {
        Full,
        None
    }
}