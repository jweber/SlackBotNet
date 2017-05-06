using Newtonsoft.Json;

namespace SlackBotNet.Messages.WebApi
{
    public class Field
    {
        [JsonConstructor]
        private Field()
        {}

        public Field(string title, string value)
        {
            this.Title = title;
            this.Value = value;
        }

        public Field(string title, string value, bool isShort)
        {
            this.Title = title;
            this.Value = value;
            this.Short = isShort;
        }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "short")]
        public bool? Short { get; set; }
    }
}