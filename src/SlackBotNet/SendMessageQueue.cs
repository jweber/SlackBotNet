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
    public interface IThrottleQueue<TMessage>
        where TMessage : class
    {
        void Enqueue(TMessage message);
    }

    internal class MessageThrottleQueue : ThrottleQueue<IMessage>
    {
        public MessageThrottleQueue(
            TimeSpan timespan,
            IDriver driver, 
            ILogger logger, 
            Action<IThrottleQueue<IMessage>, IMessage, ILogger, Exception> sendErrorHandler) 
            : base(
                timespan,
                logger, 
                driver.SendMessageAsync, 
                (q, msg, lg, ex) =>
                {
                    if (msg is PostMessage pm)
                        pm.SendAttempts += 1;

                    sendErrorHandler?.Invoke(q, msg, lg, ex);
                })
        { }
    }
    
    internal class ThrottleQueue<TMessage> : IThrottleQueue<TMessage>, IDisposable
        where TMessage : class
    {
        private readonly ILogger logger;
        private readonly ConcurrentQueue<TMessage> messageQueue = new ConcurrentQueue<TMessage>();
        private readonly IDisposable sendTimer;

        private bool isDisposing = false;
        
        public ThrottleQueue(
            TimeSpan interval,
            ILogger logger, 
            Func<TMessage, ILogger, Task> handler,
            Action<IThrottleQueue<TMessage>, TMessage, ILogger, Exception> sendErrorHandler)
        {
            this.logger = logger;
            
            this.sendTimer = Observable
                .Timer(TimeSpan.Zero, interval)
                .Subscribe(async _ =>
                {
                    if (this.messageQueue.TryDequeue(out TMessage message))
                    {
                        try
                        {
                            await handler(message, logger);
                        }
                        catch (Exception e)
                        {
                            sendErrorHandler?.Invoke(this, message, logger, e);
                            
                            if (sendErrorHandler == null)
                                logger.LogError($"Failed when attempting to send the message to Slack. Exception: {e.Message}");
                        }
                    }
                });
        }

        public void Enqueue(TMessage message)
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