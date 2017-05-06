using System.Collections.Generic;
using Newtonsoft.Json;
using SlackBotNet.Infrastructure;

namespace SlackBotNet.Messages.WebApi
{
    /// <summary>
    /// See <a href="https://api.slack.com/docs/message-attachments">the Slack documentation</a> for full details.
    /// </summary>
    public class Attachment
    {
        [JsonProperty(PropertyName = "fallback")]
        public string Fallback { get; set; }

        [JsonProperty(PropertyName = "color")]
        public AttachmentColor Color { get; set; }

        [JsonProperty(PropertyName = "pretext")]
        public string Pretext { get; set; }

        [JsonProperty(PropertyName = "author_name")]
        public string AuthorName { get; set; }

        [JsonProperty(PropertyName = "author_link")]
        public string AuthorLink { get; set; }

        [JsonProperty(PropertyName = "author_icon")]
        public string AuthorIcon { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "title_link")]
        public string TitleLink { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "fields")]
        public ICollection<Field> Fields { get; set; }

        [JsonProperty(PropertyName = "image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty(PropertyName = "thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonProperty(PropertyName = "footer")]
        public string Footer { get; set; }

        [JsonProperty(PropertyName = "footer_icon")]
        public string FooterIcon { get; set; }

        [JsonProperty(PropertyName = "ts")]
        public int? Timestamp { get; set; }
    }

    [JsonConverter(typeof(ToStringJsonConverter))]
    public class AttachmentColor
    {
        private AttachmentColor(string value) => this.Value = value;

        public string Value { get; }

        public override string ToString() => this.Value;

        public static AttachmentColor Good { get; } = new AttachmentColor("good");
        public static AttachmentColor Warning { get; } = new AttachmentColor("warning");
        public static AttachmentColor Danger { get; } = new AttachmentColor("danger");
        public static AttachmentColor FromHex(string hex) => new AttachmentColor(hex.StartsWith("#") ? hex : "#" + hex);

        public static implicit operator AttachmentColor(string hex) => FromHex(hex);
    }
}