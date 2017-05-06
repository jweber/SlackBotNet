using Newtonsoft.Json.Linq;
using SlackBotNet.State;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Drivers
{
    class SlackRtmDriver : IDriver
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private ClientWebSocket websocket;

        private readonly string slackToken;
        private readonly JsonSerializerSettings serializerSettings;
        
        public SlackRtmDriver(string slackToken)
        {
            this.slackToken = slackToken;

            this.serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public async Task<SlackBotState> ConnectAsync(IMessageBus bus)
        {
            var json = await HttpClient.GetStringAsync($"https://slack.com/api/rtm.start?token={this.slackToken}");
            var jData = JObject.Parse(json);

            string websocketUrl = jData["url"].Value<string>();

            var state = SlackBotState.InitializeFromRtmStart(jData);

            this.websocket = new ClientWebSocket();
            this.websocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            

            await this.websocket.ConnectAsync(new Uri(websocketUrl), CancellationToken.None);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await this.Listen(bus));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return state;
        }

        public async Task DisconnectAsync()
        {
            await this.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
            this.websocket.Dispose();
        }

        public Task SendMessageAsync(PostMessage message)
            => this.SendMessageOverWebApi(message);

        private Task SendMessageOverRtmAsync(PostMessage message)
        {
            return this.websocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(message.Text)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        private async Task<PostMessageResponse> SendMessageOverWebApi(PostMessage message)
        {
            string postUrl = $"https://slack.com/api/chat.postMessage?token={this.slackToken}";

            var requestContent = message.ToKeyValuePairs();

            var response = await HttpClient.PostAsync(postUrl, new FormUrlEncodedContent(requestContent));

            var content = await response.Content.ReadAsStringAsync();
            var parsedContent = JObject.Parse(content);
            bool isOk = parsedContent["ok"].Value<bool>();

            if (!response.IsSuccessStatusCode || !isOk)
                throw new HttpRequestException(content);

            return JsonConvert.DeserializeObject<PostMessageResponse>(content);
        }

        private async Task Listen(IMessageBus bus)
        {
            while (this.websocket.State == WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[4096]);
                var result = await this.websocket.ReceiveAsync(buffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await this.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var rawMessage = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count).TrimEnd('\0');

                    object msg = Serialization.Deserialize(rawMessage);
                    bus.Publish(msg);
                }
            }
        }

        public void Dispose()
        {
            this.websocket?.Dispose();
        }
    }
}
