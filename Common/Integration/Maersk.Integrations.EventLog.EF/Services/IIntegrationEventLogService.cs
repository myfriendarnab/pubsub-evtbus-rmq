using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maersk.Integrations.Events.Entities;

namespace Maersk.Integrations.EventLog.EF.Services
{
    public interface IIntegrationEventLogService
    {
        Task<IEnumerable<IntegrationEventLog>> RetrieveEventLogsPendingToPublishAsync();

        Task SaveEventAsync<TMessage>(IntegrationEvent<TMessage> @event, string eventName = "")
            where TMessage : class;

        Task MarkEventAsPublishedAsync(Guid eventId);

        Task MarkEventAsInProgressAsync(Guid eventId);

        Task MarkEventAsFailedAsync(Guid eventId);
    }
}