using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackBotNet.Drivers;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet
{
    public interface ISendMessageQueue
    {
        void Enqueue(IMessage message);
    }
    
    internal class SendMessageQueue : ISendMessageQueue, IDisposable
    {
        private readonly ILogger logger;
        private readonly ConcurrentQueue<IMessage> messageQueue = new ConcurrentQueue<IMessage>();
        private readonly IDisposable sendTimer;

        private bool isDisposing = false;
        
        public SendMessageQueue(
            TimeSpan interval,
            IDriver driver, 
            ILogger logger, 
            Action<ISendMessageQueue, IMessage, ILogger, Exception> sendErrorHandler)
        {
            this.logger = logger;
            
            this.sendTimer = Observable
                .Timer(TimeSpan.Zero, interval)
                .Subscribe(async _ =>
                {
                    if (this.messageQueue.TryDequeue(out IMessage message))
                    {
                        try
                        {
                            await driver.SendMessageAsync(message, logger);
                        }
                        catch (Exception e)
                        {
                            if (message is PostMessage pm)
                                pm.SendAttempts += 1;

                            sendErrorHandler?.Invoke(this, message, logger, e);
                            
                            if (sendErrorHandler == null)
                                logger.LogError($"Failed when attempting to send the message to Slack. Exception: {e.Message}");
                        }
                    }
                });
        }

        public void Enqueue(IMessage message)
        {
            if (this.isDisposing)
            {
                this.logger.LogInformation("The SendMessageQueue is disposing and is not accepting new messages");
                return;
            }
            
            this.messageQueue.Enqueue(message);
        }

        public void Dispose()
        {
            this.isDisposing = true;
            
            if (!this.messageQueue.IsEmpty)
            {
                this.logger.LogInformation("Trying to dispose, but the SendMessageQueue is not empty. Waiting for it to drain");
                Thread.Sleep(TimeSpan.FromSeconds(this.messageQueue.Count));
            }
            
            this.sendTimer?.Dispose();
        }
    }
}