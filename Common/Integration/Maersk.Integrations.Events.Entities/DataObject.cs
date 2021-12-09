using Ardalis.SmartEnum;

namespace Maersk.Integrations.Events.Entities
{
    public class DataObject:SmartEnum<DataObject>
    {
        public static DataObject SHIPPER_BOOKING = new DataObject(nameof(SHIPPER_BOOKING), 1);

        public static DataObject CARGO_STUFFING = new DataObject(nameof(CARGO_STUFFING), 2);

        public static DataObject FILE_UPLOAD = new DataObject(nameof(FILE_UPLOAD), 3);
        public static DataObject CUSTOMER_ORDER = new DataObject(nameof(CUSTOMER_ORDER), 4);

        public DataObject(string name, int value) : base(name, value)
        {
        }
    }
}