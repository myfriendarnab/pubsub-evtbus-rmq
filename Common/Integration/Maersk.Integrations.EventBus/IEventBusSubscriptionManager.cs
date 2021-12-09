using System;
using System.Collections.Generic;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.Events.Entities;

namespace Maersk.Integrations.EventBus
{
    public interface IEventBusSubscriptionManager
    {
        bool IsEmpty { get; }

        event EventHandler<string> OnEventRemoved;
        
        void AddSubscription<TMessage, TEvent, TH>(string eventName = "")
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>;
        
        void RemoveSubscription<TMessage, TEvent, TH>()
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>;

        bool HasSubscriptionForEvent<TMessage, TEvent>(string eventName = "")
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>;

        bool HasSubscriptionForEvent(string eventName);

        Type GetEventTypeByName(string eventName);

        Type GetMessageTypeByName(string eventName);

        void Clear();

        IEnumerable<SubscriptionInfo> GetHandlersForEvent<TMessage, TEvent>()
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>;
        
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
    }
}