using System;
using System.Threading.Tasks;
using IntegrationEvents;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.Events.Entities;

namespace sub
{
    public class ThirdHandler : IIntegrationEventHandler<ThirdIntegrationEvent,
        IntegrationEvent<ThirdIntegrationEvent>>
    {
        public async Task Handle(IntegrationEvent<ThirdIntegrationEvent> @event)
        {
            Console.WriteLine($"reading message with id:{@event.Data.Id} from sub3" );
            return;
        }
    }
}