using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace SlackBotNet
{
    internal interface IMessageBus
    {
        IObservable<TMessage> Observe<TMessage>();
        void Publish<TMessage>(TMessage message);
    }

    internal class RxMessageBus : IMessageBus, IDisposable
    {
        private readonly Subject<object> subject = new Subject<object>();

        public IObservable<TMessage> Observe<TMessage>()
        {
            if (typeof(TMessage).GetTypeInfo().IsInterface)
            {
                return this.subject
                    .OfType<TMessage>()
                    .Publish()
                    .RefCount();
            }

            return this.subject
                .Where(msg => msg != null && msg.GetType() == typeof(TMessage))
                .Cast<TMessage>()
                .Publish()
                .RefCount();
        } 
            

        public void Publish<TMessage>(TMessage message)
            => this.subject.OnNext(message);

        public void Dispose()
        {
            this.subject?.Dispose();
        }
    }
}