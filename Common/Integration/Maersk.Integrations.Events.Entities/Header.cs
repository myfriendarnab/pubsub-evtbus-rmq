using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Maersk.Integrations.Events.Entities
{
    public class Header
    {
        [JsonProperty]
        public string EventNotificationName { get; private set; }
        
        [JsonProperty]
        public EventSystem System { get; private set; }

        [JsonProperty]
        public string Version { get; private set; }
        
        [JsonProperty]
        public List<EventSystem> Receivers { get; private set; }

        [JsonProperty]
        public MessageTransactionType MessageTransactionType { get; private set; }

        [JsonProperty]
        public MessageSerializationFormat MessageSerializationFormat { get; private set; }

        [JsonProperty]
        public Guid MessageId { get; private set; }

        [JsonProperty]
        public Guid CorrelationId { get; private set; }

        [JsonProperty]
        public List<CausationId> CausationIds { get; private set; }

        [JsonProperty]
        public DateTime EventTimeStamp { get; private set; }
        
        [JsonProperty]
        public DateTime ExpirationTimeStamp { get; private set; }

        [JsonProperty]
        public int Latency { get; private set; }

        [JsonProperty]
        public bool Retry { get; private set; }

        [JsonProperty]
        public int RetryCount { get; private set; }

        [JsonConstructor]
        public Header(string version, EventSystem system, DateTime eventTimeStamp,
            string eventNotificationName = "", List<EventSystem> receivers = default,
            DateTime expirationTimeStamp = default,
            MessageTransactionType messageTransactionType = default,
            MessageSerializationFormat messageSerializationFormat = default, Guid correlationId = default,
            List<CausationId> causationIds = default, int latency = 0,
            bool retry = false, int retryCount = 0)
        {
            MessageId = Guid.NewGuid();
            Version = version;
            System = system;
            EventTimeStamp = eventTimeStamp;
            EventNotificationName = eventNotificationName;
            Receivers = receivers;
            MessageTransactionType = messageTransactionType;
            MessageSerializationFormat = messageSerializationFormat;
            CorrelationId = correlationId;
            CausationIds = causationIds;
            Latency = latency;
            Retry = retry;
            RetryCount = retryCount;
            ExpirationTimeStamp = expirationTimeStamp;
        }
    }
}