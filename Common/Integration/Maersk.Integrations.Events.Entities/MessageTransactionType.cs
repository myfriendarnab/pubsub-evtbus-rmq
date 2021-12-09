using Ardalis.SmartEnum;

namespace Maersk.Integrations.Events.Entities
{
    public class MessageTransactionType: SmartEnum<MessageTransactionType>
    {
        public static MessageTransactionType FULL = new MessageTransactionType(nameof(FULL), 1);
        public static MessageTransactionType PARTIAL = new MessageTransactionType(nameof(PARTIAL), 2);
        
        public MessageTransactionType(string name, int value) : base(name, value)
        {
        }
    }
}