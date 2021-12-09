using Ardalis.SmartEnum;

namespace Maersk.Integrations.Events.Entities
{
    public class TransactionType: SmartEnum<TransactionType>
    {
        public static TransactionType CREATE = new TransactionType(nameof(CREATE), 1);
        public static TransactionType READ = new TransactionType(nameof(READ), 2);
        public static TransactionType UPDATE = new TransactionType(nameof(UPDATE), 3);
        public static TransactionType DELETE = new TransactionType(nameof(DELETE), 4);
        
        public TransactionType(string name, int value) : base(name, value)
        {
        }
    }
}