using Newtonsoft.Json;

namespace SlackBotNet.Messages.WebApi
{
    /// <summary>
    /// 
    /// </summary>
    /// <see cref="https://api.slack.com/methods/files.upload"/>
    public class FilesUploadResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        
        // todo: Add File object
    }
}