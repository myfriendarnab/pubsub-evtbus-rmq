using System;
using System.Threading.Tasks;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.Events.Entities;

namespace sub
{
    public class FirstHandler:IIntegrationEventHandler<string,
        IntegrationEvent<string>>
    {
        public async Task Handle(IntegrationEvent<string> @event)
        {
            Console.WriteLine($"reading message with id:{@event.Data.Id} from sub1" );
            return;
        }
    }
}