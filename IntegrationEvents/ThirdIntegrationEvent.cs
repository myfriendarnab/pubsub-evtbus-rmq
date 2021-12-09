using System;

namespace IntegrationEvents
{
    public class ThirdIntegrationEvent
    {
        public string EmployeeName { get;  }
        public string Address { get;  }

        public ThirdIntegrationEvent(string employeeName, string address)
        {
            EmployeeName = employeeName;
            Address = address;
        }
    }
}
