using System;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Maersk.Integrations.RabbitMQ
{
    public class DefaultRabbitMQPersistentConnection:IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly ILogger<DefaultRabbitMQPersistentConnection> logger;
        private readonly int retryCount;
        private IConnection connection;
        private bool disposed;

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory,
            ILogger<DefaultRabbitMQPersistentConnection> logger, int retryCount = 5)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.retryCount = retryCount;
        }

        private object syncRoot = new object();
        
        public void Dispose()
        {
            if(disposed)
                return;
            disposed = true;

            try
            {
                connection.Dispose();
            }
            catch (IOException ex)
            {
                logger.LogCritical(ex.ToString());
            }
        }

        public bool IsConnected => connection != null && connection.IsOpen && !disposed;

        public bool TryConnect()
        {
            logger.LogInformation("RabbitMQ client is trying to connect");

            lock (syncRoot)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (ex, time) =>
                        {
                            logger.LogWarning(ex,
                                $"RabbitMQ client could not connect after {time.TotalSeconds:n1} ({ex.Message})");
                        });

                policy.Execute(() =>
                {
                    connection = connectionFactory.CreateConnection();
                });

                if (IsConnected)
                {
                    connection.ConnectionShutdown += ConnectionOnConnectionShutdown;
                    connection.ConnectionBlocked += ConnectionOnConnectionBlocked;
                    connection.CallbackException += ConnectionOnCallbackException;
                    
                    logger.LogInformation($"RabbitMQ client acquired a persistent connection to '{connection.Endpoint.HostName}' and is subscribed to failure events");
                    return true;
                }
                else
                {
                    logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                    return false;
                }
            }
        }

        private void ConnectionOnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (disposed) return;
            
            logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect");

            TryConnect();
        }

        private void ConnectionOnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (disposed) return;
            
            logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect");

            TryConnect();
        }

        private void ConnectionOnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (disposed) return;
            
            logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect");

            TryConnect();
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return connection.CreateModel();
        }
    }
}