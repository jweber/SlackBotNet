using Newtonsoft.Json.Linq;
using SlackBotNet.State;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlackBotNet.Drivers
{
    class SlackRtmDriver : IDriver
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private ClientWebSocket websocket;

        public async Task<SlackBotState> ConnectAsync(string slackToken, IMessageBus bus)
        {
            var json = await HttpClient.GetStringAsync($"https://slack.com/api/rtm.start?token={slackToken}");
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

        public Task SendMessageAsync(string message)
        {
            return this.websocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
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
    }
}
