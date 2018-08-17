using System.Collections.Generic;
using Newtonsoft.Json;

namespace SlackBotNet.Messages.WebApi
{
    public class File
    {
        [JsonConstructor]
        private File()
        { }

        public File(string content)
        {
            this.Content = content;
        }
        
        public string Channels { get; set; }
        public string Content { get; set; }
        public string Filename { get; set; }
        public string FileType { get; set; }
        public string InitialComment { get; set; }
        public string ThreadTs { get; set; }
        public string Title { get; set; }
        
        public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs()
        {
            string Encode(string input)
                => input
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

            bool Render<T>(T input) => input != null;

            KeyValuePair<string, string> Kvp(string name, string value)
                => new KeyValuePair<string, string>(name, Encode(value));

            if (Render(this.Channels)) yield return Kvp("channels", this.Channels);
            if (Render(this.Content)) yield return Kvp("content", this.Content);
            if (Render(this.Filename)) yield return Kvp("filename", this.Filename);
            if (Render(this.FileType)) yield return Kvp("filetype", this.FileType);
            if (Render(this.InitialComment)) yield return Kvp("initial_comment", this.InitialComment);
            if (Render(this.ThreadTs)) yield return Kvp("thread_ts", this.ThreadTs);
            if (Render(this.Title)) yield return Kvp("title", this.Title);
        }
    }
}
