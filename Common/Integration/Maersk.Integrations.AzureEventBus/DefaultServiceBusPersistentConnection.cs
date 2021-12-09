using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace Maersk.Integrations.AzureEventBus
{
    public class DefaultServiceBusPersistentConnection : IServiceBusPresistentConnection
    {
        private readonly ILogger<DefaultServiceBusPersistentConnection> logger;
        private readonly string connectionString;
        private ServiceBusClient client;
        private int retryCount;

        private readonly ServiceBusClientOptions serviceBusClientOptions;

        private bool disposed;

        public DefaultServiceBusPersistentConnection(string connectionString,  ILogger<DefaultServiceBusPersistentConnection> logger, int retryCount = 5)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connectionString = connectionString ??
                                                     throw new ArgumentNullException(
                                                         nameof(connectionString));
            this.retryCount = retryCount;
            var retryOptions = new ServiceBusRetryOptions
            {
                Delay = TimeSpan.FromSeconds(5),
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = retryCount
            };

            serviceBusClientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp,
                RetryOptions = retryOptions
            };
            client = new ServiceBusClient(connectionString, serviceBusClientOptions);
        }
        
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
        }

        public string ConnectionString => connectionString;

        public ServiceBusClient CreateClient()
        {
            if (client.IsClosed)
            {
                client = new ServiceBusClient(connectionString, serviceBusClientOptions);
            }

            return client;
        }

        public async Task ManageSubscriptions(string connectionString, string subscriptionName, string topicName)
        {
            logger.LogInformation($"subscription details : { connectionString} & {subscriptionName} & {topicName}");

            if (string.IsNullOrWhiteSpace(subscriptionName) || string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException($"Subscription or topic name is empty string");
            }

            await ManageTopics(connectionString, topicName);
            logger.LogInformation($" { nameof(ManageSubscriptions) } creating subscription : {subscriptionName}");
            var adminClient = new ServiceBusAdministrationClient(connectionString);
            if (!string.IsNullOrWhiteSpace(subscriptionName) && !string.IsNullOrWhiteSpace(topicName))
            {
                var subscriptionExists = await adminClient.SubscriptionExistsAsync(topicName, subscriptionName);
                logger.LogInformation($" { nameof(ManageSubscriptions) } subscription exists : {subscriptionExists.Value}");
                if (!subscriptionExists)
                {
                    var options = new CreateSubscriptionOptions(topicName, subscriptionName);
                    logger.LogInformation($" { nameof(ManageSubscriptions) } subscription Creating");

                    await adminClient.CreateSubscriptionAsync(options);
                    logger.LogInformation($" { nameof(ManageSubscriptions) } subscription Created");

                }
            }
        }

        public async Task ManageTopics(string connectionString, string topicName)
        {
            var adminClient = new ServiceBusAdministrationClient(connectionString);
            if (!string.IsNullOrWhiteSpace(topicName))
            {
                logger.LogInformation($"before check Topic exists with name : { topicName}");
                var topicExists = await adminClient.TopicExistsAsync(topicName);
                logger.LogInformation($"Topic exists : { topicExists}");
                if (!topicExists)
                {
                    logger.LogInformation($"Topic Creating : { topicName}");
                    var options = new CreateTopicOptions(topicName)
                    {
                        MaxSizeInMegabytes = 1024
                    };
                    await adminClient.CreateTopicAsync(options);
                    logger.LogInformation($"Topic Created : { topicName}");

                }
            }
        }
    }
}