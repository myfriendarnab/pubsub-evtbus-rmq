using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Maersk.Integrations.AzureEventBus
{
    public interface IServiceBusPresistentConnection : IDisposable
    {
        string ConnectionString { get; }

        ServiceBusClient CreateClient();

        Task ManageSubscriptions(string connectionString, string subscriptionName, string topicName);

        Task ManageTopics(string connectionString, string topicName);
    }
}