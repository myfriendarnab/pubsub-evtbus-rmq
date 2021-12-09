using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Maersk.Integrations.EventBus;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.EventBus.Extensions;
using Maersk.Integrations.Events.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Maersk.Integrations.RabbitMQ
{
    public class EventBusRabbitMQ: IEventBus, IDisposable
    {
        private const string BROKER_NAME = "origin_event_bus";
        private const string AUTOFAC_SCOPE_NAME = "order_management_event_bus";

        private readonly IRabbitMQPersistentConnection rabbitMqPersistentConnection;
        private readonly ILogger<EventBusRabbitMQ> logger;
        private readonly IEventBusSubscriptionManager eventBusSubscriptionManager;
        private readonly ILifetimeScope autofac;
        private readonly int retryCount;
        private readonly string exchangeType;

        private IModel consumerChannel;
        private string queueName;

        public EventBusRabbitMQ(IRabbitMQPersistentConnection rabbitMqPersistentConnection,
            ILogger<EventBusRabbitMQ> logger, ILifetimeScope autofac,
            IEventBusSubscriptionManager eventBusSubscriptionManager, string queueName = null, int retryCount = 5, string exchangeType="direct")
        {
            this.rabbitMqPersistentConnection = rabbitMqPersistentConnection ??
                                                throw new ArgumentNullException(nameof(rabbitMqPersistentConnection));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.eventBusSubscriptionManager = eventBusSubscriptionManager ??
                                               throw new ArgumentNullException(nameof(eventBusSubscriptionManager));
            this.autofac = autofac ?? throw new ArgumentNullException(nameof(autofac));
            this.queueName = queueName; 
            this.exchangeType = exchangeType?? throw new ArgumentNullException(nameof(exchangeType));
            this.consumerChannel = CreateConsumerChannel();
            this.retryCount = retryCount;
            eventBusSubscriptionManager.OnEventRemoved += EventBusSubscriptionManagerOnOnEventRemoved;
        }

        private void EventBusSubscriptionManagerOnOnEventRemoved(object sender, string eventName)
        {
            if (!rabbitMqPersistentConnection.IsConnected)
            {
                rabbitMqPersistentConnection.TryConnect();
            }

            using (var channel = rabbitMqPersistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: queueName, exchange: BROKER_NAME, routingKey: eventName);

                if (eventBusSubscriptionManager.IsEmpty)
                {
                    queueName = string.Empty;
                    consumerChannel.Close();
                }
            }
        }

        private IModel CreateConsumerChannel()
        {
            if (!rabbitMqPersistentConnection.IsConnected)
            {
                rabbitMqPersistentConnection.TryConnect();
            }
            
            logger.LogTrace("Creating RabbitMQ consumer channel");

            var channel = rabbitMqPersistentConnection.CreateModel();
            //channel.ExchangeDeclare(BROKER_NAME, "direct");
            channel.ExchangeDeclare(BROKER_NAME, exchangeType);

            channel.QueueDeclare(queueName, true, false, false, null);
            channel.CallbackException += (sender, ea) =>
            {
                logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");
                
                consumerChannel.Dispose();
                consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };
            return channel;
        }

        private void StartBasicConsume()
        {
            logger.LogTrace("Starting RabbitMQ basic consume");

            if (consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(consumerChannel);
                consumer.Received += ConsumerOnReceived;

                consumerChannel.BasicConsume(queueName, false, consumer);
            }
            else
            {
                logger.LogError("Start Basic Consume can't call on consumer channel == null");
            }
        }

        private async Task ConsumerOnReceived(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                // Add Unit test for this later

                if (await ProcessEvent(eventName, message))
                {
                    consumerChannel.BasicAck(@event.DeliveryTag, multiple: false);       
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"----- Error Processing message \"{message}");
            }
        }

        private async Task<bool> ProcessEvent(string eventName, string message)
        {
            logger.LogTrace($"Processing RabbitMQ event: {eventName}");

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
                            // handle dynamic
                        }
                        else
                        {
                            var handler = scope.ResolveOptional(subscription.HandlerType);
                            if (handler == null) continue;
                            var eventType = eventBusSubscriptionManager.GetEventTypeByName(eventName);
                            var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
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
            else
            {
                logger.LogInformation($"No subscription for RabbitMQ event: {eventName}");
            }

            return processed;
        }

        public void Publish<TMessage>(IntegrationEvent<TMessage> @event) where TMessage : class
        {
            var message = JsonConvert.SerializeObject(@event);

            var eventName = @event.GetGenericTypeName();
            Publish(message, @event.Header.MessageId, eventName);
        }

        private RetryPolicy GetRetryPolicy(Guid eventId) 
        {
            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        logger.LogWarning(ex,
                            $"Could not publish event: {eventId} after {time.TotalSeconds:n1} ({ex.Message})");
                    });
            return policy;
        }

        public void Publish(string eventBody, Guid eventId, string eventName)
        {
            if (!rabbitMqPersistentConnection.IsConnected)
            {
                rabbitMqPersistentConnection.TryConnect();
            }

            var policy = GetRetryPolicy(eventId);

            logger.LogTrace($"Creating RabbitMQ channel to publish event: {eventId} ({eventName})");

            using (var channel = rabbitMqPersistentConnection.CreateModel())
            {
                logger.LogTrace($"Declaring RabbitMQ exchange to publish event: {eventId}");

                //channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");
                channel.ExchangeDeclare(exchange: BROKER_NAME, type: exchangeType);

                var body = Encoding.UTF8.GetBytes(eventBody);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2;

                    logger.LogTrace($"Publishing event to RabbitMQ: {eventId}");
                    
                    channel.BasicPublish(BROKER_NAME, eventName, true, properties, body);
                });
            }

        }

        public void Subscribe<TMessage, TEvent, TH>(string eventName = "") where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            eventName = string.IsNullOrWhiteSpace(eventName) ? typeof(TMessage).Name : eventName;

            DoInternalSubscription(eventName);
            
            logger.LogInformation($"Subscribing to event {eventName} with {typeof(TH).GetGenericTypeName()}");

            eventBusSubscriptionManager.AddSubscription<TMessage, TEvent, TH>(eventName);
            StartBasicConsume();
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = eventBusSubscriptionManager.HasSubscriptionForEvent(eventName);
            if (!containsKey)
            {
                if (!rabbitMqPersistentConnection.IsConnected)
                {
                    rabbitMqPersistentConnection.TryConnect();
                }

                using (var channel = rabbitMqPersistentConnection.CreateModel())
                {
                    channel.QueueBind(queueName, BROKER_NAME, eventName);
                }
            }
        }

        public void Unsubscribe<TMessage, TEvent, TH>() where TMessage : class
            where TEvent : IntegrationEvent<TMessage>
            where TH : IIntegrationEventHandler<TMessage, TEvent>
        {
            var eventName = typeof(TMessage).Name;
            
            logger.LogInformation($"Unsubscribing from event {eventName}");
            
            eventBusSubscriptionManager.RemoveSubscription<TMessage, TEvent, TH>();
        }

        public void Dispose()
        {
            if (consumerChannel != null)
            {
                consumerChannel.Dispose();
            }
        }
    }
}