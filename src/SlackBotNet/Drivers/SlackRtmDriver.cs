using Newtonsoft.Json.Linq;
using SlackBotNet.State;
using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;
using File = SlackBotNet.Messages.WebApi.File;

namespace SlackBotNet.Drivers
{
    class SlackRtmDriver : IDriver
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private ClientWebSocket websocket;

        private readonly string slackToken;
        private CancellationTokenSource tokenSource;

        private static readonly SemaphoreSlim PingSemaphore = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim ReconnectSemaphore = new SemaphoreSlim(1, 1);
        
        public SlackRtmDriver(string slackToken)
        {
            this.slackToken = slackToken;
            this.tokenSource = new CancellationTokenSource();
        }

        public Task<SlackBotState> ConnectAsync(IMessageBus bus, ILogger logger)
        {
            Observable
                .Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))
                .Subscribe(async _ =>
                {
                    if (ReconnectSemaphore.CurrentCount == 0)
                    {
                        logger.LogDebug($"Not pinging because there is an active socket reconnect attempt in process");
                        return;
                    }

                    if (PingSemaphore.CurrentCount == 0)
                        return;
                    
                    try
                    {
                        if (!await PingSemaphore.WaitAsync(100))
                            return;
                        
                        if (this.websocket == null)
                            return;
                        
                        if (this.websocket.State != WebSocketState.Open)
                        {
                            logger.LogWarning($"Not pinging because the socket is not open. Current state is: {this.websocket.State}. Attempting to reconnect.");

                            await this.ReconnectRtmAsync(bus, logger);
                            return;
                        }
                    
                        var ping = new Ping(DateTimeOffset.UtcNow.Ticks);

                        var obs = bus
                            .Observe<Pong>()
                            .Where(m => m.ReplyTo == ping.Id);

                        try
                        {
                            await this.websocket.SendAsync(
                                new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ping, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }))),
                                WebSocketMessageType.Text,
                                true,
                                this.tokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to ping the Slack RTM socket. Attempting to reconnect. Exception: {ex.Message}");
                        
                            await this.ReconnectRtmAsync(bus, logger);
                            return;
                        }

                        logger.LogDebug($"ping? (id: {ping.Id})");
                    
                        try
                        {
                            var pong = await obs
                                .FirstAsync()
                                .Timeout(DateTimeOffset.UtcNow.AddSeconds(10));

                            logger.LogDebug($"pong! (id: {pong.ReplyTo})");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to receive the Pong message from the Slack RTM socket. Attempting to reconnect. Exception: {ex.Message}");

                            await this.ReconnectRtmAsync(bus, logger);
                            return;
                        }
                    }
                    finally
                    {
                        try
                        {
                            PingSemaphore.Release();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"PingSemaphore Release. Exception: {ex.Message}");
                        }
                    }
                });

            return this.ConnectRtmAsync(bus, logger);
        }

        [ItemCanBeNull]
        private async Task<SlackBotState> ReconnectRtmAsync(IMessageBus bus, ILogger logger)
        {
            if (ReconnectSemaphore.CurrentCount == 0)
                return null;
            
            try
            {
                if (!await ReconnectSemaphore.WaitAsync(100))
                    return null;

                this.tokenSource.Cancel();
                this.tokenSource = new CancellationTokenSource();

                int reconnectAttempts = 0;
                while (true)
                {
                    try
                    {
                        return await this.ConnectRtmAsync(bus, logger);
                    }
                    catch (Exception ex)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Min(60 * 2, Math.Pow(2d, reconnectAttempts++)));
                        logger.LogError($"Failed to reconnect to the Slack RTM socket after {reconnectAttempts} attempts. Retrying again after {delay.TotalSeconds} seconds. Exception: {ex.Message}");
                        await Task.Delay(delay);
                    }
                }
            }
            finally
            {
                try
                {
                    ReconnectSemaphore.Release();
                }
                catch (Exception ex)
                {
                    logger.LogError($"ReconnectSemaphore Release. Exception: {ex.Message}");
                }
            }
        }
        
        private async Task<SlackBotState> ConnectRtmAsync(IMessageBus bus, ILogger logger)
        {
            logger.LogDebug("Retrieving websocket URL");
            var json = await HttpClient.GetStringAsync($"https://slack.com/api/rtm.start?token={this.slackToken}");
            var jData = JObject.Parse(json);

            string websocketUrl = jData["url"].Value<string>();

            var state = SlackBotState.InitializeFromRtmStart(jData);

            this.websocket = new ClientWebSocket();
            this.websocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            logger.LogDebug($"Opening connection to {websocketUrl}");

            await this.websocket.ConnectAsync(new Uri(websocketUrl), this.tokenSource.Token);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(async () => await this.Listen(bus, logger), this.tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return state;
        }

        public async Task DisconnectAsync()
        {
            this.tokenSource.Cancel();
            await this.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", this.tokenSource.Token);
            this.websocket.Dispose();
        }

        public Task SendMessageAsync(IMessage message, ILogger logger)
            => this.SendMessageOverWebApi(message, logger);

        public Task UploadFileAsync(File file, ILogger logger)
            => this.UploadFileOverWebApi(file, logger);

        private Task SendMessageOverRtmAsync(PostMessage message)
        {
            return this.websocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(message.Text)),
                WebSocketMessageType.Text,
                true,
                this.tokenSource.Token);
        }

        private async Task<FilesUploadResponse> UploadFileOverWebApi(File file, ILogger logger)
        {
            string postUrl = $"https://slack.com/api/files.upload?token={this.slackToken}";
            var requestContent = file.ToKeyValuePairs();

            HttpResponseMessage response;
            try
            {
                response = await HttpClient.PostAsync(postUrl, new FormUrlEncodedContent(requestContent));
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to POST file upload to Slack. Exception: {ex.Message}");
                throw;
            }
            
            if (!response.IsSuccessStatusCode)
                logger.LogError($"Non-success response was returned when POSTing a file upload to Slack. Response: {response.StatusCode}; {response.ReasonPhrase}");

            var content = await response.Content.ReadAsStringAsync();
            var parsedContent = JObject.Parse(content);
            bool isOk = parsedContent["ok"].Value<bool>();
            
            if (!isOk)
                logger.LogError($"Slack response indicated things were not OK with the file upload. Here's the content: {content}");

            if (!response.IsSuccessStatusCode && !isOk)
                throw new HttpRequestException(content);

            return JsonConvert.DeserializeObject<FilesUploadResponse>(content);
        }
        
        private async Task<PostMessageResponse> SendMessageOverWebApi(IMessage message, ILogger logger)
        {
            string postUrl = $"https://slack.com/api/chat.postMessage?token={this.slackToken}";

            var requestContent = message.ToKeyValuePairs();

            HttpResponseMessage response;
            try
            {
                response = await HttpClient.PostAsync(postUrl, new FormUrlEncodedContent(requestContent));
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to POST message to Slack. Exception: {ex.Message}");
                throw;
            }

            if (!response.IsSuccessStatusCode)
                logger.LogError($"Non-success response was returned when POSTing a message to Slack. Response: {response.StatusCode}; {response.ReasonPhrase}");
            
            var content = await response.Content.ReadAsStringAsync();
            var parsedContent = JObject.Parse(content);
            bool isOk = parsedContent["ok"].Value<bool>();

            if (!isOk)
                logger.LogError($"Slack response indicated things were not OK. Here's the content: {content}");
            
            if (!response.IsSuccessStatusCode || !isOk)
                throw new HttpRequestException(content);

            return JsonConvert.DeserializeObject<PostMessageResponse>(content);
        }

        private async Task Listen(IMessageBus bus, ILogger logger)
        {
            while (this.websocket.State == WebSocketState.Open)
            {
                if (this.tokenSource.Token.IsCancellationRequested)
                    return;
                
                var buffer = new ArraySegment<byte>(new byte[8192]);

                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        if (this.tokenSource.Token.IsCancellationRequested)
                            return;
                        
                        result = await this.websocket.ReceiveAsync(buffer, this.tokenSource.Token);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            logger.LogInformation("Received socket close message. Closing the socket connection");
                            await this.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, this.tokenSource.Token);
                            break;
                        case WebSocketMessageType.Text:
                            using (var reader = new StreamReader(ms, Encoding.UTF8))
                            {
                                var rawMessage = reader.ReadToEnd();
                                var message = Serialization.Deserialize(rawMessage);

                                if (message != null)
                                {
                                    if (message.GetType() != typeof(Pong))
                                        logger.LogInformation($"Received message type: {message.GetType()}");
                                    
                                    bus.Publish(message);
                                }
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
