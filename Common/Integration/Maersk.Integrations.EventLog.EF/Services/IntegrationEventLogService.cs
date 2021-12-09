using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Maersk.Integrations.Events.Entities;
using Newtonsoft.Json;

namespace Maersk.Integrations.EventLog.EF.Services
{
    public class IntegrationEventLogService : IIntegrationEventLogService
    {
        private readonly IntegrationEventLogContext integrationEventLogContext;
        private readonly List<Type> eventTypes;
        private volatile bool disposedValue;

        public IntegrationEventLogService(DbConnection dbConnection)
        {
            this.integrationEventLogContext = new IntegrationEventLogContext(
                new DbContextOptionsBuilder<IntegrationEventLogContext>()
                    .UseNpgsql(dbConnection).Options);
            this.eventTypes = Assembly.Load(Assembly.GetEntryAssembly().FullName)
                .GetTypes()
                .Where(d => d.GetType() == typeof(IntegrationEvent<>))
                .ToList();
        }

        public async Task<IEnumerable<IntegrationEventLog>> RetrieveEventLogsPendingToPublishAsync()
        {
            var result = await integrationEventLogContext.IntegrationEventLogs
                .Where(d => d.State == EventState.NotPublished)
                .ToListAsync();

            if (result != null && result.Any())
            {
                return result.OrderBy(d => d.CreationTime)
                    .Select(d => d.DeserializeJsonEntry(eventTypes.Find(e => e.Name == d.EventTypeShortName)));
            }

            return new List<IntegrationEventLog>();
        }

        public async Task SaveEventAsync<TMessage>(IntegrationEvent<TMessage> @event, string eventName = "")
            where TMessage : class
        {
            eventName = string.IsNullOrWhiteSpace(eventName) ? typeof(TMessage).Name : eventName;
            
            var eventLog = new IntegrationEventLog(@event.Header.MessageId, @event.Header.EventTimeStamp,
                eventName, JsonConvert.SerializeObject(@event));

            integrationEventLogContext.IntegrationEventLogs.Add(eventLog);
            await integrationEventLogContext.SaveChangesAsync();
        }

        public Task MarkEventAsPublishedAsync(Guid eventId)
        {
            return UpdateEventStatus(eventId, EventState.Published);
        }

        public Task MarkEventAsInProgressAsync(Guid eventId)
        {
            return UpdateEventStatus(eventId, EventState.InProgress);
        }

        public Task MarkEventAsFailedAsync(Guid eventId)
        {
            return UpdateEventStatus(eventId, EventState.PublishedFailed);
        }

        private Task UpdateEventStatus(Guid eventId, EventState state)
        {
            var eventLog = integrationEventLogContext.IntegrationEventLogs.Single(d => d.EventId == eventId);
            eventLog.State = state;

            if (state == EventState.InProgress)
                eventLog.TimesSent++;

            integrationEventLogContext.IntegrationEventLogs.Update(eventLog);
            return integrationEventLogContext.SaveChangesAsync();
        }
    }
}