using Ardalis.SmartEnum;

namespace Maersk.Integrations.Events.Entities
{
    public class MessageSerializationFormat:SmartEnum<MessageSerializationFormat>
    {
        public static MessageSerializationFormat JSON = new MessageSerializationFormat(nameof(JSON), 1);
        public static MessageSerializationFormat XML = new MessageSerializationFormat(nameof(XML), 2);
        public static MessageSerializationFormat BSON = new MessageSerializationFormat(nameof(BSON), 3);
        public static MessageSerializationFormat AVRO = new MessageSerializationFormat(nameof(AVRO), 4);
        public static MessageSerializationFormat CSV = new MessageSerializationFormat(nameof(CSV), 5);
        public static MessageSerializationFormat YAML = new MessageSerializationFormat(nameof(YAML), 6);
        public static MessageSerializationFormat PROTOBUF = new MessageSerializationFormat(nameof(PROTOBUF), 7);
        
        public MessageSerializationFormat(string name, int value) : base(name, value)
        {
        }
    }
}