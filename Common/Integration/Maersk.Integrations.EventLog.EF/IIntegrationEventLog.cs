using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Maersk.Integrations.EventLog.EF
{
    public class IntegrationEventLog
    {
        private IntegrationEventLog()
        {
        }

        public IntegrationEventLog(Guid messageId, DateTime eventTimeStamp, string eventTypeName, string content)
        {
            EventId = messageId;
            CreationTime = eventTimeStamp;
            EventTypeName = eventTypeName;
            Content = content;
            State = EventState.NotPublished;
            TimesSent = 0;
        }

        public Guid EventId { get; private set; }

        public string EventTypeName { get; private set; }

        [NotMapped] public string EventTypeShortName => EventTypeName.Split('.')?.Last();

        public EventState State { get; set; }

        public int TimesSent { get; set; }

        public DateTime CreationTime { get; private set; }

        public string Content { get; private set; }

        public IntegrationEventLog DeserializeJsonEntry(Type type)
        {
            return this;
        }
    }
}