using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

    class PostMessage : IMessage
    {
        [JsonConstructor]
        private PostMessage()
        { }

        public PostMessage(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            this.Channel = channel;
        }

        public PostMessage(string channel, string text)
            : this(channel)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            this.Text = text;
        }

        public PostMessage(string channel, params Attachment[] attachments)
            : this(channel)
        {
            this.Attachments = attachments;
        }

        public PostMessage(string channel, string text, params Attachment[] attachments)
            : this(channel, text)
        {
            this.Attachments = attachments;
        }

        [JsonIgnore]
        public int SendAttempts { get; internal set; }

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
    }

    public enum ParseMode
    {
        Full,
        None
    }
}