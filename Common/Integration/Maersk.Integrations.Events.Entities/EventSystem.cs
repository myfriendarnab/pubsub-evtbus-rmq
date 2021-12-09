using Ardalis.SmartEnum;

namespace Maersk.Integrations.Events.Entities
{
    public class EventSystem : SmartEnum<EventSystem>
    {
        public static EventSystem NSCP_ORDER_MANAGEMENT = new EventSystem(nameof(NSCP_ORDER_MANAGEMENT), 1);
        public static EventSystem NSCP_ORIGIN = new EventSystem(nameof(NSCP_ORIGIN), 2);
        public static EventSystem NSCP_DESTINATION = new EventSystem(nameof(NSCP_DESTINATION), 3);
        public static EventSystem NSCP_WAREHOUSING = new EventSystem(nameof(NSCP_WAREHOUSING), 4);

        public static EventSystem NSCP_TRANSPORTATION_MANAGEMENT =
            new EventSystem(nameof(NSCP_TRANSPORTATION_MANAGEMENT), 5);

        public static EventSystem NSCP_BRAIN = new EventSystem(nameof(NSCP_BRAIN), 6);
        public static EventSystem NSCP_ALERT_AND_EXCEPTION = new EventSystem(nameof(NSCP_ALERT_AND_EXCEPTION), 7);
        public static EventSystem SEEB_CARRIER_INTEGRATION = new EventSystem(nameof(SEEB_CARRIER_INTEGRATION), 8);
        public static EventSystem SEEB_CUSTOMER_EDI = new EventSystem(nameof(SEEB_CUSTOMER_EDI), 9);
        public static EventSystem NSCP_EVENT_AND_MILESTONE = new EventSystem(nameof(NSCP_EVENT_AND_MILESTONE), 10);

        public static EventSystem NSCP_DOCUMENTATION_MANAGEMENT =
            new EventSystem(nameof(NSCP_DOCUMENTATION_MANAGEMENT), 11);

        public static EventSystem NSCP_FINANCIAL_OPERATION = new EventSystem(nameof(NSCP_FINANCIAL_OPERATION), 12);

        public static EventSystem NSCP_USER_ACCESS_MANAGEMENT =
            new EventSystem(nameof(NSCP_USER_ACCESS_MANAGEMENT), 13);

        public static EventSystem NSCP_MASTER_DATA_MANAGEMENT =
            new EventSystem(nameof(NSCP_MASTER_DATA_MANAGEMENT), 14);

        public static EventSystem MANUAL = new EventSystem(nameof(MANUAL), 15);
        public static EventSystem MORE = new EventSystem(nameof(MORE), 16);
        public static EventSystem MODS = new EventSystem(nameof(MODS), 17);
        public static EventSystem GCSS = new EventSystem(nameof(GCSS), 18);
        public static EventSystem RKEM = new EventSystem(nameof(RKEM), 19);
        public static EventSystem FACT = new EventSystem(nameof(FACT), 20);
        public static EventSystem OPENTEXT = new EventSystem(nameof(OPENTEXT), 21);
        public static EventSystem SEEBURGER = new EventSystem(nameof(SEEBURGER), 22);
        public static EventSystem OB2B = new EventSystem(nameof(OB2B), 23);
        public static EventSystem NSCP_FLOW_OPTIMIZATION = new EventSystem(nameof(NSCP_FLOW_OPTIMIZATION), 24);

        public EventSystem(string name, int value) : base(name, value)
        {
        }
    }
}