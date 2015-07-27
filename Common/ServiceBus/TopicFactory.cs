using Newtonsoft.Json;
using RabbitMQ.Client;
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
    /// Topic provider with message and queue persistence
    /// On server restart or network failure the consumers and queues are automatically restored
    /// </summary>
    public class TopicFactory
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
        public bool DurableQueue { get; set; }

        public bool PersistentMessages { get; set; }

        public Action OnConsumerExit { get; set; }
        public Action<string> OnUpdateNeeded { get; set; }
        
        public TopicFactory(ConnectionFactory connectionFactory, IMessageSerializer serializer, ILog logger, string version, string exchangeName = "esn.topic", bool durableExchange = true, bool durableQueue = true, bool persistentMessages = true)
        {
            amqpConnectionFactory = connectionFactory;
            ExchangeName = exchangeName;
            DurableExchange = durableExchange;
            DurableQueue = durableQueue;
            PersistentMessages = persistentMessages;
            this.version = version;
            this.serializer = serializer;
            this.logger = logger;
        }

        private string GetTopicQueue(string topic)
        {
            return $"{ExchangeName}.{topic}";
        }

        private string GetTopicRoutingKey(string topic)
        {
            return $"#.{topic}.#";
        }

        private string GetTopicsRoutingKey(IEnumerable<string> topics)
        {
            return topics.Aggregate((i, j) => i + "." + j);
        }

        private void SetUpExchange(IModel amqpChannel)
        {
            amqpChannel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: DurableExchange);
        }

        private string SetUpQueue(IModel amqpChannel, string topic)
        {
            var qName = GetTopicQueue(topic);
            amqpChannel.QueueDeclare(queue: qName, durable: DurableQueue, exclusive: false, autoDelete: false, arguments: null);
            amqpChannel.QueueBind(queue: qName, exchange: ExchangeName, routingKey: GetTopicRoutingKey(topic));
            return qName;
        }

        private QueueingBasicConsumer SetUpConsumer(IModel amqpChannel, string queueName)
        {
            var consumer = new QueueingBasicConsumer(amqpChannel);

            // set maximum unacknowledged messages to be received at once
            amqpChannel.BasicQos(0, 1, false);

            var consumerTag = amqpChannel.BasicConsume(queue: queueName, noAck: false, consumer: consumer);

            return consumer;
        }

        /// <summary>
        /// Opens a new connection, channel and session to receive and process messages 
        /// </summary>
        /// <typeparam name="T">message type</typeparam>
        /// <param name="topic">topic name</param>
        /// <param name="processMessage">takes the message object and returns a flag if the message should be rejected or not</param>
        public void StartConsumer<T>(string topic, Func<T, bool> processMessage)
        {
            using (consumerConnection = amqpConnectionFactory.CreateConnection())
            {
                using (var amqpChannel = consumerConnection.CreateModel())
                {
                    var queueName = SetUpQueue(amqpChannel, topic);
                    var consumer = SetUpConsumer(amqpChannel, queueName);

                    while (!stopPending)
                    {
                        try
                        {
                            logger.Info($"The topic {topic} consumer has started");

                            var reject = false;
                            var delivery = consumer.Queue.Dequeue();
                            
                            try
                            {
                                logger.Debug($" Topic {topic} consumer  message received from {queueName} with topics {delivery.RoutingKey}");

                                //check version
                                if (delivery.BasicProperties.Headers != null && delivery.BasicProperties.Headers.Keys.Contains("x-version"))
                                {
                                    var versionHeader = Encoding.UTF8.GetString(delivery.BasicProperties.Headers["x-version"] as byte[]);
                                    var msgVersion = Version.Parse(versionHeader);
                                    var curVersion = Version.Parse(version);

                                    if(msgVersion > curVersion)
                                    {
                                        logger.Warn($"Upgrade needed to {versionHeader} from {version}");

                                        if(OnUpdateNeeded!= null)
                                        {
                                            OnUpdateNeeded(versionHeader);
                                        }
                                    }

                                    if(curVersion > msgVersion)
                                    {
                                        logger.Warn($"Consumer version is {version}. Publisher upgrade needed to {version} from {versionHeader}.");
                                    }
                                    
                                }

                                var message = serializer.DeserializeObject<T>(delivery.Body);

                                reject = processMessage(message);

                            }
                            catch (Exception ex)
                            {
                                //TODO: store message in error queue
                                logger.LogException(ex, $"Topic {topic} consumer message processing error");
                            }

                            if (reject)
                            {
                                // requeue message
                                amqpChannel.BasicNack(delivery.DeliveryTag, false, true);
                            }
                            else
                            {
                                // remove message from queue
                                amqpChannel.BasicAck(delivery.DeliveryTag, false);
                            }
                        }
                        catch (EndOfStreamException eox)
                        {
                            if (stopPending)
                            {
                                logger.Info($"Exiting! The topic {topic} consumer received a stop signal");
                                break;
                            }

                            logger.Warn($"Retrying! Topic consumer connection error {eox.Message} connection closed {!consumerConnection.IsOpen}");

                            // wait for the connection to be restored
                            Thread.Sleep(amqpConnectionFactory.NetworkRecoveryInterval);

                            // reinitialize consumer
                            if (consumerConnection.IsOpen && amqpChannel.IsOpen)
                            {
                                logger.Info($"Restoring topic {topic} queue and consumer");
                                queueName = SetUpQueue(amqpChannel, topic);
                                consumer = SetUpConsumer(amqpChannel, queueName);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogException(ex, $"Exiting! Topic {topic} consumer fatal error");
                            break;
                        }
                    }

                    if(OnConsumerExit!= null)
                    {
                        OnConsumerExit();
                    }
                }
            }
        }

        public void StartConsumerInBackground<T>(string topic, Func<T, bool> processMessage)
        {
            var backgroundThread = new Thread(t =>
            {
                StartConsumer(topic, processMessage);
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

        public void PublishMessage<T>(T message, IEnumerable<string> topics)
        {
            using (producerConnection = amqpConnectionFactory.CreateConnection())
            {
                using (var amqpChannel = producerConnection.CreateModel())
                {
                    SetUpExchange(amqpChannel);

                    foreach (var topic in topics)
                    {
                        SetUpQueue(amqpChannel, topic);
                    }

                    var props = amqpChannel.CreateBasicProperties();
                    props.Persistent = PersistentMessages;
                    props.Headers = new Dictionary<string, object>();
                    props.Headers.Add("x-version", version);
                    props.ContentType = serializer.ContentType;
                    props.ContentEncoding = serializer.ContentEncoding;

                    var routingKey = GetTopicsRoutingKey(topics);
                    var body = serializer.SerializeObject(message);
                    amqpChannel.BasicPublish(exchange: ExchangeName, routingKey: routingKey, basicProperties: props, body: body);
                }
            }
        }

        public void PublishMessageInBackground<T>(T message, IEnumerable<string> topics)
        {
            var backgroundThread = new Thread(t =>
            {
                PublishMessage(message, topics);
            });
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }
    }
}
