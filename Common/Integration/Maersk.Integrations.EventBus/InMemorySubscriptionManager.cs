using System;
using System.Collections.Generic;
using System.Linq;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.EventBus.Extensions;
using Maersk.Integrations.Events.Entities;

namespace Maersk.Integrations.EventBus
{
    public class InMemorySubscriptionManager : IEventBusSubscriptionManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly Dictionary<string, Type> _eventTypes;
        private readonly Dictionary<string, Type> _messageTypes;

        public InMemorySubscriptionManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new Dictionary<string, Type>();
            _messageTypes = new Dictionary<string, Type>();
        }

        public event EventHandler<string> OnEventRemoved;

        public bool IsEmpty => !_handlers.Keys.Any();

        public void AddSubscription<TMessage, TEvent, TH>(string eventName = "") where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            eventName = string.IsNullOrWhiteSpace(eventName) ? typeof(TMessage).Name : eventName;

            DoSubscription(typeof(TH), eventName, false);

            if (!_eventTypes.ContainsKey(eventName))
            {
                _eventTypes.Add(eventName, typeof(TEvent));
            }

            if (!_messageTypes.ContainsKey(eventName))
            {
                _messageTypes.Add(eventName, typeof(TMessage));
            }
        }

        public void RemoveSubscription<TMessage, TEvent, TH>() where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            var handlerToRemove = FindSubscriptionToRemove<TMessage, TEvent, TH>();
            var eventName = typeof(TMessage).Name;
            DoRemoveHandler(eventName, handlerToRemove);
        }

        public bool HasSubscriptionForEvent<TMessage, TEvent>(string eventName = "")
            where TMessage : class where TEvent : IntegrationEvent<TMessage>
        {
            var key = string.IsNullOrWhiteSpace(eventName) ? typeof(TMessage).Name : eventName;
            return HasSubscriptionForEvent(key);
        }

        public bool HasSubscriptionForEvent(string eventName)
        {
            return _handlers.ContainsKey(eventName);
        }

        public Type GetEventTypeByName(string eventName) => _eventTypes[eventName];

        public Type GetMessageTypeByName(string eventName) => _messageTypes[eventName];

        public void Clear() => _handlers.Clear();

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<TMessage, TEvent>() where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
        {
            var key = typeof(TMessage).Name;
            return GetHandlersForEvent(key);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];
        
        private void DoSubscription(Type handlerType, string eventName, bool isDynamic)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }

            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException($"Handler Type {handlerType.GetGenericTypeName()} already registered for '{eventName}'");
            }

            if (isDynamic)
            {
                // implement dynamic
            }
            else
            {
                _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
            }
        }

        private SubscriptionInfo FindSubscriptionToRemove<TMessage, TEvent, TH>()
            where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            var eventName = typeof(TMessage).Name;
            return DoFindSubscriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                return default;
            }

            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }

        private void DoRemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes[eventName];
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventName);
                    }

                    var messageType = _messageTypes[eventName];
                    if (messageType != null)
                    {
                        _messageTypes.Remove(eventName);
                    }

                    RaiseOnEventRemoved(eventName);
                }
            }
        }

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }
    }
}