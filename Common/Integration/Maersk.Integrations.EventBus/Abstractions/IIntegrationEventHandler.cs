using System.Threading.Tasks;
using Maersk.Integrations.Events.Entities;

namespace Maersk.Integrations.EventBus.Abstractions
{
    public interface IIntegrationEventHandler
    {
        
    }

    public interface IIntegrationEventHandler<TMessage, in TIntegrationEvent> : IIntegrationEventHandler
        where TMessage : class
        where TIntegrationEvent : IntegrationEvent<TMessage>
    {
        Task Handle(TIntegrationEvent @event);
    }
}