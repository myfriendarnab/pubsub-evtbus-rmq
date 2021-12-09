using Newtonsoft.Json;
using System;

namespace Maersk.Integrations.Events.Entities
{
    /// <summary>
    /// This class should be created as a record type
    /// introduced in C# 9. Which in fact are distinct
    /// classes and use value-based equality
    /// </summary>
    public class IntegrationEvent<T> where T : class
    {
        [JsonConstructor]
        public IntegrationEvent(Header header, Data data, T message)
        {
            Header = header;
            Data = data;
            Message = message;
        }

        [JsonProperty] public Header Header { get; private set; }

        [JsonProperty] public Data Data { get; private set; }

        [JsonProperty]
        public T Message { get; private set; }
    }
}