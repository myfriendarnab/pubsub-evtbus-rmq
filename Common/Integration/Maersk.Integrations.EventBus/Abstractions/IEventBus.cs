using System;
using Maersk.Integrations.Events.Entities;

namespace Maersk.Integrations.EventBus.Abstractions
{
    public interface IEventBus
    {
        void Publish<TMessage>(IntegrationEvent<TMessage> @event)
            where TMessage : class;

        void Publish(string eventBody, Guid eventId, string eventName);

        void Subscribe<TMessage, TEvent, TH>(string eventName = "")
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>;
        
        void Unsubscribe<TMessage, TEvent, TH>()
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>;
    }
}