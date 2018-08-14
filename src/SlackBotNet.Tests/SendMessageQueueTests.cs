using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SlackBotNet.Drivers;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;
using Xunit;

namespace SlackBotNet.Tests
{
    public class SendMessageQueueTests
    {
        [Fact]
        public void Queue_SendsMessageToDriver()
        {
            var driver = Substitute.For<IDriver>();
            var logger = Substitute.For<ILogger>();

            var q = new MessageThrottleQueue(
                TimeSpan.Zero,
                driver,
                logger,
                null);

            var message = new PostMessage("channel");
            q.Enqueue(message);

            Thread.Sleep(10);
            
            driver
                .Received()
                .SendMessageAsync(message, logger);
        }
        
        [Fact]
        public void Queue_OnError()
        {
            var driver = Substitute.For<IDriver>();
            var logger = Substitute.For<ILogger>();
            
            var message = new PostMessage("channel");

            driver.SendMessageAsync(message, logger)
                .Returns(
                    _ => throw new Exception("test"), 
                    _ => Task.CompletedTask);

            var resetEvent = new AutoResetEvent(false);
            
            var q = new MessageThrottleQueue(
                TimeSpan.Zero, 
                driver, 
                logger, 
                async (queue, msg, l, ex) =>
                {
                    await Task.Delay(10);
                    
                    Assert.Same(msg, message);
                    Assert.Same(logger, l);
                    Assert.Equal("test", ex.Message);
                    Assert.Equal(1, msg.SendAttempts);
                    
                    queue.Enqueue(msg);
                    
                    resetEvent.Set();
                });

            q.Enqueue(message);

            if (!resetEvent.WaitOne(100))
                Assert.True(false, "reset event not set in callback");
            
            driver
                .Received(2)
                .SendMessageAsync(message, logger);
        }        
    }
}