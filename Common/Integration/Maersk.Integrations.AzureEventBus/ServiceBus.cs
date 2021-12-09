using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Azure.Messaging.ServiceBus;
using Maersk.Integrations.EventBus;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.EventBus.Extensions;
using Maersk.Integrations.Events.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Maersk.Integrations.AzureEventBus
{
    public class ServiceBus : IEventBus
    {
        private readonly IServiceBusPresistentConnection serviceBusPresistentConnection;
        private readonly ILogger<ServiceBus> logger;
        private readonly IEventBusSubscriptionManager eventBusSubscriptionManager;
        private readonly ILifetimeScope autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "customer_configuration_event_bus";
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private ServiceBusProcessorOptions serviceBusProcessorOptions;

        private Dictionary<string, ServiceBusProcessor> processors;
        private readonly string subscriptionClientName;

        public ServiceBus(IServiceBusPresistentConnection serviceBusPresistentConnection, ILogger<ServiceBus> logger,
            IEventBusSubscriptionManager eventBusSubscriptionManager, string subscriptionClientName, ILifetimeScope autofac, JsonSerializerSettings jsonSerializerSettings)
        {
            this.serviceBusPresistentConnection = serviceBusPresistentConnection ??
                                                  throw new ArgumentNullException(
                                                      nameof(serviceBusPresistentConnection));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.eventBusSubscriptionManager = eventBusSubscriptionManager ??
                                               throw new ArgumentNullException(nameof(eventBusSubscriptionManager));
            this.subscriptionClientName = !string.IsNullOrWhiteSpace(subscriptionClientName)
                ? subscriptionClientName
                : throw new ArgumentNullException(nameof(subscriptionClientName)); 
            this.autofac = autofac ?? throw new ArgumentNullException(nameof(autofac));
            serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 10,
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(240)
            };
            processors = new Dictionary<string, ServiceBusProcessor>();
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
        }

        private async Task<bool> ProcessEvent(string eventName, string messageData)
        {
            var processed = false;
            if (eventBusSubscriptionManager.HasSubscriptionForEvent(eventName))
            {
                using (var scope = autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
                {
                    var subscriptions = eventBusSubscriptionManager.GetHandlersForEvent(eventName);
                    foreach (var subscription in subscriptions)
                    {
                        if (subscription.IsDynamic)
                        {
                            // Note handle dynamic event subscription
                        }
                        else
                        {
                            var handler = scope.ResolveOptional(subscription.HandlerType);
                            if (handler == null) continue;
                            var eventType = eventBusSubscriptionManager.GetEventTypeByName(eventName);
                            var integrationEvent = JsonConvert.DeserializeObject(messageData, eventType);
                            var messageType = eventBusSubscriptionManager.GetMessageTypeByName(eventName);
                            var types = new Type[]
                            {
                                messageType, 
                                eventType
                            };
                            var concreteType = typeof(IIntegrationEventHandler<,>).MakeGenericType(types);

                            await Task.Yield();
                            await (Task) concreteType.GetMethod("Handle")
                                .Invoke(handler, new object[] {integrationEvent});
                            processed = true;
                        }
                    }
                }
            }

            return processed;
        }

        public void Publish<TMessage>(IntegrationEvent<TMessage> @event) where TMessage : class
        {
            var eventName = typeof(TMessage).Name;
            
            serviceBusPresistentConnection.ManageSubscriptions(serviceBusPresistentConnection.ConnectionString,
                subscriptionClientName, eventName);
            
            var jsonMessage = JsonConvert.SerializeObject(@event);

            var serviceBusMessage = new ServiceBusMessage(jsonMessage)
            {
                MessageId = @event.Header.MessageId.ToString(),
                PartitionKey = eventName
            };

            var sender = serviceBusPresistentConnection.CreateClient().CreateSender(eventName);
            sender.SendMessageAsync(serviceBusMessage).GetAwaiter().GetResult();
        }

        public void Publish(string eventBody, Guid eventId, string eventName)
        {
            serviceBusPresistentConnection.ManageTopics(serviceBusPresistentConnection.ConnectionString, eventName)
                .GetAwaiter().GetResult();
            
            var serviceBusMessage = new ServiceBusMessage(eventBody)
            {
                MessageId = eventId.ToString(),
                PartitionKey = eventName
            };

            var sender = serviceBusPresistentConnection.CreateClient().CreateSender(eventName);
            sender.SendMessageAsync(serviceBusMessage).GetAwaiter().GetResult();
        }

        public void Subscribe<TMessage, TEvent, TH>(string eventName = "") where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            eventName = string.IsNullOrWhiteSpace(eventName) ? typeof(TMessage).Name : eventName;

            serviceBusPresistentConnection.ManageSubscriptions(serviceBusPresistentConnection.ConnectionString,
                subscriptionClientName, eventName).GetAwaiter().GetResult();
            
            logger.LogInformation($"Subscribing to dynamic event {eventName}");
            var containsKey = eventBusSubscriptionManager.HasSubscriptionForEvent<TMessage, TEvent>(eventName);
            if (!containsKey)
            {
                if (!processors.ContainsKey(eventName))
                {
                    var serviceBusProcessor = serviceBusPresistentConnection.CreateClient()
                        .CreateProcessor(eventName, subscriptionClientName, serviceBusProcessorOptions);
                    serviceBusProcessor.ProcessMessageAsync += ServiceBusProcessorOnProcessMessageAsync;
                    serviceBusProcessor.ProcessErrorAsync += ServiceBusProcessorOnProcessErrorAsync;
                    serviceBusProcessor.StartProcessingAsync();

                    processors.Add(eventName, serviceBusProcessor);
                }
            }

            logger.LogInformation($"Subscribing to event {eventName} with {typeof(TH).GetGenericTypeName()}");
            
            eventBusSubscriptionManager.AddSubscription<TMessage, TEvent, TH>(eventName);
        }

        private Task ServiceBusProcessorOnProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            logger.LogError($"Unable to process message {arg.EntityPath}");
            return Task.CompletedTask;
        }

        private async Task ServiceBusProcessorOnProcessMessageAsync(ProcessMessageEventArgs arg)
        {
            var body = arg.Message.Body.ToString();
            var eventName = arg.Message.PartitionKey;

            //TODO: Investigate in future
            if (arg.Message.DeliveryCount > 3)
            {
                logger.LogCritical(
                    $"Message couldn't reprocess for {arg.Message.DeliveryCount} counts, {arg.Message.MessageId}");
                await arg.DeadLetterMessageAsync(arg.Message);
                return;
            }

            if (await ProcessEvent(eventName, body))
            {
                await arg.CompleteMessageAsync(arg.Message);
                logger.LogInformation($"{nameof(arg.CompleteMessageAsync)} called");
            }
            else
            {
                await arg.DeadLetterMessageAsync(arg.Message);
                logger.LogInformation($"{nameof(arg.DeadLetterMessageAsync)} called");
            }

            logger.LogInformation("Delivery Count: " + arg.Message.DeliveryCount);
            logger.LogInformation("Enqueued Time: " + arg.Message.EnqueuedTime);
            logger.LogInformation("Expires Time: " + arg.Message.ExpiresAt);
            logger.LogInformation("Locked Until Time: " + arg.Message.LockedUntil);
            logger.LogInformation("Scheduled Enqueue Time: " + arg.Message.ScheduledEnqueueTime);
        }

        public void Unsubscribe<TMessage, TEvent, TH>() where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            var eventName = typeof(TMessage).Name;
            if (processors.ContainsKey(eventName))
            {
                var serviceBusProcessor = processors[eventName];
                if (serviceBusProcessor != null)
                {
                    serviceBusProcessor.StopProcessingAsync();
                }
                processors.Remove(eventName);
            };
            
            logger.LogInformation($"Unsubscribing from event {eventName}");
            
            eventBusSubscriptionManager.RemoveSubscription<TMessage, TEvent, TH>();
        }
    }
}