using System;

namespace SlackBotNet.Tests.Infrastructure
{
    public class TestBus : IMessageBus
    {
        public IObservable<TMessage> Observe<TMessage>()
        {
            throw new NotImplementedException();
        }

        public void Publish<TMessage>(TMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
