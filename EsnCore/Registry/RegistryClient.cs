using EsnCore.ServiceBus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EsnCore.Registry
{
    public class RegistryClient
    {
        private readonly ConnectionFactory connectionFactory;
        private readonly ILog logger;
        private readonly IMessageSerializer serializer;

        private readonly TimeSpan syncInterval;
        private readonly TimeSpan timeout;
        private volatile bool stopPending;

        public ServiceInfo ServiceDefinition { get; private set; }

        public RegistryClient(ConnectionFactory connectionFactory, IMessageSerializer serializer, ILog logger, TimeSpan syncInterval, TimeSpan timeout)
        {
            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
            this.logger = logger;
            this.syncInterval = syncInterval;
            this.timeout = timeout;
        }

        public QueueingBasicConsumer SetUpConsumer(IModel channel, ServiceInfo service, string correlationId)
        {
            var replyQueueName = channel.QueueDeclare();
            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(replyQueueName, true, consumer);

            var props = channel.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>();
            props.Headers.Add("x-version", service.Version);
            props.ReplyTo = replyQueueName;
            props.CorrelationId = correlationId;
            props.ContentType = serializer.ContentType;
            props.ContentEncoding = serializer.ContentEncoding;

            var messageBytes = serializer.SerializeObject(service);

            channel.QueueDeclare(queue: RpcSettings.RegistryQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            channel.BasicPublish("", RpcSettings.RegistryQueue, props, messageBytes);

            return consumer;
        }

        public ServiceInfo Register(ServiceInfo service)
        {
            ServiceInfo info = null;

            using (var connection = connectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();
                    var consumer = SetUpConsumer(channel, service, correlationId);

                    BasicDeliverEventArgs ea = null;
                    var timeoutDate = DateTime.UtcNow + timeout;
                    while (DateTime.UtcNow <= timeoutDate)
                    {
                        try
                        {
                            var ok = consumer.Queue.Dequeue(Convert.ToInt32(timeout.TotalMilliseconds), out ea);
                            if (!ok)
                            {
                                logger.Error($"RegistryClient.Register has timeout after {timeout.TotalSeconds} seconds");
                                return null;
                            }

                            if (ea.BasicProperties.CorrelationId == correlationId)
                            {
                                var json = Encoding.UTF8.GetString(ea.Body);
                                info = serializer.DeserializeObject<ServiceInfo>(ea.Body);
                                ServiceDefinition = info;
                                return info;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogException(ex, $"RegistryClient.Register RPC error {ex.Message}");
                            return info;
                        }
                    }

                    logger.Error($"RegistryClient.Register has timeout after {timeout.TotalSeconds} seconds");
                    return info;
                }
            }
        }

        public void SyncInBackground()
        {
            var backgroundThread = new Thread(t =>
            {
                while (!stopPending)
                {
                    Thread.Sleep(syncInterval);
                    try
                    {
                        if (stopPending) break;

                        Sync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex, $"RegistryClient Background sync error {ex.Message}");
                    }
                }
            });

            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }

        private void Sync()
        {
            var service = ServiceInfoFactory.CreateServiceDefinition(ServiceDefinition);

            using (var connection = connectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();
                    var consumer = SetUpConsumer(channel, service, correlationId);

                    BasicDeliverEventArgs ea = null;
                    var timeoutDate = DateTime.UtcNow + timeout;
                    while (DateTime.UtcNow <= timeoutDate)
                    {
                        try
                        {
                            var ok = consumer.Queue.Dequeue(Convert.ToInt32(timeout.TotalMilliseconds), out ea);
                            if (!ok)
                            {
                                logger.Error($"RegistryClient.Sync has timeout after {timeout.TotalSeconds} seconds");
                            }

                            if (ea.BasicProperties.CorrelationId == correlationId)
                            {
                                var json = Encoding.UTF8.GetString(ea.Body);
                                ServiceDefinition = serializer.DeserializeObject<ServiceInfo>(ea.Body);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogException(ex, $"RegistryClient.Sync RPC error {ex.Message}");
                        }
                    }
                }
            }
        }

        public void StopBackgoundSync()
        {
            stopPending = true;
        }
    }
}
