using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using IntegrationEvents;
using Maersk.Integrations.EventBus;
using Maersk.Integrations.EventBus.Abstractions;
using Maersk.Integrations.Events.Entities;
using Maersk.Integrations.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace sub
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
            services.AddTransient<FirstHandler>();
            services.AddTransient<SecondHandler>();
            services.AddTransient<ThirdHandler>();
            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var eventBusSubscriptionManager = sp.GetRequiredService<IEventBusSubscriptionManager>();
                var bus = new EventBusRabbitMQ(rabbitMQPersistentConnection, busLogger, iLifetimeScope, eventBusSubscriptionManager, "abcd", 1);
                return bus;
            });

            var container = new ContainerBuilder();
            container.Populate(services);

            var serviceProvider = new AutofacServiceProvider(container.Build());

            var bus = serviceProvider.GetRequiredService<IEventBus>();

            bus.Subscribe<string, IntegrationEvent<string>, FirstHandler>("some-event");
            bus.Subscribe<string, IntegrationEvent<string>, SecondHandler>("some-event.some");
            bus.Subscribe<ThirdIntegrationEvent, IntegrationEvent<ThirdIntegrationEvent>, ThirdHandler>();


            Console.ReadKey();
        }
    }
}
