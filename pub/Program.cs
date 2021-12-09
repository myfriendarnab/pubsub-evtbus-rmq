using System;
using System.Threading;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Extensions.DependencyInjection;
using IntegrationEvents;
using Maersk.Integrations.EventBus;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.EventBus.Extensions;
using Maersk.Integrations.Events.Entities;
using Maersk.Integrations.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace pub
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging(b => b.AddConsole());

            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

            ILogger<DefaultRabbitMQPersistentConnection> logger =
                new Logger<DefaultRabbitMQPersistentConnection>(loggerFactory);

            ILogger<EventBusRabbitMQ> busLogger =
                new Logger<EventBusRabbitMQ>(loggerFactory);

            ILogger<Program> appLogger =
                new Logger<Program>(loggerFactory);

            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    DispatchConsumersAsync = true,
                    UserName = "guest",
                    Password = "guest"
                };

                IRabbitMQPersistentConnection conn = new DefaultRabbitMQPersistentConnection(factory, logger, 1);
                return conn;
            });

            services.AddSingleton<IEventBusSubscriptionManager, InMemorySubscriptionManager>();
            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var eventBusSubscriptionManager = sp.GetRequiredService<IEventBusSubscriptionManager>();
                var bus = new EventBusRabbitMQ(rabbitMQPersistentConnection, busLogger, iLifetimeScope, eventBusSubscriptionManager, "wxyz", 1);
                return bus;
            });

            var container = new ContainerBuilder();
            container.Populate(services);

            var serviceProvider = new AutofacServiceProvider(container.Build());

            var bus = serviceProvider.GetRequiredService<IEventBus>();



            Console.WriteLine("ENTER to publish new Message\nESC to Abort");
            var key = Console.ReadKey();

            var continueFlag = key.Key != ConsoleKey.Escape;

            /*
                failedscanmsg A-->B
                
             */

            while (continueFlag)
            {
                var @event = new IntegrationEvent<string>(new Header("1.0", EventSystem.FACT, DateTime.Now),
                    new Data(DataObject.CUSTOMER_ORDER, id: Guid.NewGuid()), "hello sub");

                bus.Publish(JsonConvert.SerializeObject(@event), @event.Data.Id, "some-event");
                appLogger.LogInformation($"new event with id:{@event.Data.Id} is published");
                
                Thread.Sleep(1000);
                
                var newid = Guid.NewGuid();
                bus.Publish(JsonConvert.SerializeObject(@event), newid, "some-event.some");
                appLogger.LogInformation($"new event with id:{newid} is published");


                var @newEvent = new IntegrationEvent<ThirdIntegrationEvent>(new Header("1.0", EventSystem.FACT, DateTime.Now),
                    new Data(DataObject.CUSTOMER_ORDER, id: Guid.NewGuid()), new ThirdIntegrationEvent("deepak","hyderabad"));
                newid = Guid.NewGuid();

                bus.Publish(@newEvent);
                appLogger.LogInformation($"new event with eventtypename:{@newEvent.GetType().GetGenericTypeName()} is published");

                key = Console.ReadKey();
                continueFlag = key.Key != ConsoleKey.Escape;
            }
            
            Console.ReadKey();
        }
    }
}
