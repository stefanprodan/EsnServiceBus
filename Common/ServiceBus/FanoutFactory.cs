using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.ServiceBus
{
    /// <summary>
    /// Publish/Subscribe provider without message or queue persistence
    /// On server restart or network failure the consumers and queues are automatically restored
    /// </summary>
    public class FanoutFactory
    {
        private ConnectionFactory amqpConnectionFactory;
        private IConnection consumerConnection;
        private IConnection producerConnection;

        private readonly IMessageSerializer serializer;
        private readonly ILog logger;

        private volatile bool stopPending;
        private readonly string version;

        public string ExchangeName { get; set; }
        public bool DurableExchange { get; set; }

        public Action OnConsumerExit { get; set; }
        public Action<string> OnUpdateNeeded { get; set; }

        public FanoutFactory(ConnectionFactory connectionFactory, IMessageSerializer serializer, ILog logger, string version, string exchangeName = "esn.fanout", bool durableExchange = true)
        {
            amqpConnectionFactory = connectionFactory;
            DurableExchange = durableExchange;
            ExchangeName = exchangeName;
            this.version = version;
            this.serializer = serializer;
            this.logger = logger;
        }

        private void SetUpExchange(IModel amqpChannel)
        {
            amqpChannel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout, durable: DurableExchange);
        }

        private string SetUpQueue(IModel amqpChannel)
        {
            SetUpExchange(amqpChannel);

            // generate non durable queue
            var queueName = amqpChannel.QueueDeclare().QueueName;

            // bind queue to exchange
            amqpChannel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: "");

            return queueName;
        }

        private QueueingBasicConsumer SetUpConsumer(IModel amqpChannel, string queueName)
        {
            var consumer = new QueueingBasicConsumer(amqpChannel);

            // set maximum unacknowledged messages to be received at once
            amqpChannel.BasicQos(0, 1, false);

            var consumerTag = amqpChannel.BasicConsume(queue: queueName, noAck: false, consumer: consumer);

            return consumer;
        }

        public void StartConsumer<T>(Action<T> processMessage)
        {
            using (consumerConnection = amqpConnectionFactory.CreateConnection())
            {
                using (var amqpChannel = consumerConnection.CreateModel())
                {
                    var queueName = SetUpQueue(amqpChannel);
                    var consumer = SetUpConsumer(amqpChannel, queueName);

                    while (!stopPending)
                    {
                        try
                        {
                            logger.Info($"The fanout consumer has started");

                            var delivery = consumer.Queue.Dequeue();

                            try
                            {
                                logger.Debug($" Fanout consumer  message received from {queueName} via {ExchangeName}");

                                //check version
                                if (delivery.BasicProperties.Headers != null && delivery.BasicProperties.Headers.Keys.Contains("x-version"))
                                {
                                    var versionHeader = Encoding.UTF8.GetString(delivery.BasicProperties.Headers["x-version"] as byte[]);
                                    var msgVersion = Version.Parse(versionHeader);
                                    var curVersion = Version.Parse(version);

                                    if (msgVersion > curVersion)
                                    {
                                        logger.Warn($"Upgrade needed to {versionHeader} from {version}");

                                        if (OnUpdateNeeded != null)
                                        {
                                            OnUpdateNeeded(versionHeader);
                                        }
                                    }

                                    if (curVersion > msgVersion)
                                    {
                                        logger.Warn($"Consumer version is {version}. Publisher upgrade needed to {version} from {versionHeader}.");
                                    }

                                }

                                var message = serializer.DeserializeObject<T>(delivery.Body);

                                processMessage(message);

                            }
                            catch (Exception ex)
                            {
                                //TODO: store message in error queue
                                logger.LogException(ex, $"Fanout consumer message processing error {ex.Message}");
                            }

                            // remove message from q
                            amqpChannel.BasicAck(delivery.DeliveryTag, false);
                        }
                        catch (EndOfStreamException eox)
                        {
                            if (stopPending)
                            {
                                logger.Info("Exiting! The fanout consumer received a stop signal");
                                break;
                            }

                            logger.Warn($"Retrying! Fanout consumer connection error {eox.Message}");

                            // wait for the connection to be restored
                            Thread.Sleep(amqpConnectionFactory.NetworkRecoveryInterval);

                            // reinitialize consumer
                            if (consumerConnection.IsOpen && amqpChannel.IsOpen)
                            {
                                logger.Info($"Restoring fanout queue and consumer");
                                queueName = SetUpQueue(amqpChannel);
                                consumer = SetUpConsumer(amqpChannel, queueName);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogException(ex, $"Exiting! The fanout consumer encountered a fatal error {ex.Message}");

                            break;
                        }
                    }

                    if (OnConsumerExit != null)
                    {
                        OnConsumerExit();
                    }
                }
            }
        }

        public void StartConsumerInBackground<T>(Action<T> processMessage)
        {
            var backgroundThread = new Thread(t =>
            {
                StartConsumer(processMessage);
            });
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }

        public void StopConsumer()
        {
            stopPending = true;

            if (consumerConnection != null && consumerConnection.IsOpen)
            {
                consumerConnection.Close();
            }
        }

        public void PublishMessage<T>(T message)
        {
            using (producerConnection = amqpConnectionFactory.CreateConnection())
            {
                using (var amqpChannel = producerConnection.CreateModel())
                {
                    SetUpExchange(amqpChannel);

                    var props = amqpChannel.CreateBasicProperties();
                    props.Headers = new Dictionary<string, object>();
                    props.Headers.Add("x-version", version);
                    props.ContentType = serializer.ContentType;
                    props.ContentEncoding = serializer.ContentEncoding;

                    var body = serializer.SerializeObject(message);
                    amqpChannel.BasicPublish(exchange: ExchangeName, routingKey: "", basicProperties: props, body: body);
                }
            }
        }

        public void PublishMessageInBackground<T>(T message)
        {
            var backgroundThread = new Thread(t =>
            {
                PublishMessage(message);
            });
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }
    }
}
