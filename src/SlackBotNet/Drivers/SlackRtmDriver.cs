using Newtonsoft.Json.Linq;
using SlackBotNet.State;
using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Drivers
{
    class SlackRtmDriver : IDriver
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private ClientWebSocket websocket;

        private readonly string slackToken;
        private readonly CancellationTokenSource tokenSource;

        public SlackRtmDriver(string slackToken)
        {
            this.slackToken = slackToken;
            this.tokenSource = new CancellationTokenSource();
        }

        public async Task<SlackBotState> ConnectAsync(IMessageBus bus)
        {
            var json = await HttpClient.GetStringAsync($"https://slack.com/api/rtm.start?token={this.slackToken}");
            var jData = JObject.Parse(json);

            string websocketUrl = jData["url"].Value<string>();

            var state = SlackBotState.InitializeFromRtmStart(jData);

            this.websocket = new ClientWebSocket();
            this.websocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            await this.websocket.ConnectAsync(new Uri(websocketUrl), this.tokenSource.Token);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await this.Listen(bus));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return state;
        }

        public async Task DisconnectAsync()
        {
            this.tokenSource.Cancel();
            await this.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", this.tokenSource.Token);
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
                this.tokenSource.Token);
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
                var buffer = new ArraySegment<byte>(new byte[8192]);

                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await this.websocket.ReceiveAsync(buffer, this.tokenSource.Token);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            await this.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                                this.tokenSource.Token);
                            break;
                        case WebSocketMessageType.Text:
                            using (var reader = new StreamReader(ms, Encoding.UTF8))
                            {
                                var rawMessage = reader.ReadToEnd();
                                var message = Serialization.Deserialize(rawMessage);

                                if (message != null)
                                    bus.Publish(message);
                            }
                            break;
                    }
                }
            }
        }

        public void Dispose()
        {
            this.tokenSource.Cancel();
            this.websocket?.Dispose();
        }
    }
}
