using Newtonsoft.Json;

namespace SlackBotNet.Messages.WebApi
{
    /// <summary>
    /// See: https://api.slack.com/methods/files.upload
    /// </summary>
    public class FilesUploadResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        
        // todo: Add File object
    }
}