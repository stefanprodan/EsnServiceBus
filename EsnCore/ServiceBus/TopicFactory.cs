using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EsnCore.ServiceBus
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

        /// <summary>
        ///  This setting determines the maximum number of times the consumer will automatically try to reconnect
        /// </summary>
        public int RetryMax { get; set; }
        
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
        public void StartConsumer<T>(string topic, IConsumer<T> topicConsumer)
        {
            var exitArgs = new ConsumerExitEventArgs();
            using (consumerConnection = amqpConnectionFactory.CreateConnection())
            {
                using (var amqpChannel = consumerConnection.CreateModel())
                {
                    SetUpExchange(amqpChannel);
                    var queueName = SetUpQueue(amqpChannel, topic);
                    var consumer = SetUpConsumer(amqpChannel, queueName);
                    var retryCount = 0;

                    logger.Info($"The topic {topic} consumer has started");

                    while (!stopPending)
                    {
                        try
                        {
                            exitArgs = new ConsumerExitEventArgs();
                            exitArgs.Exchange = ExchangeName;
                            exitArgs.Queue = queueName;
                            exitArgs.RoutingKey = GetTopicRoutingKey(topic);

                            var reject = false;

                            // blocks till the a message is received
                            var delivery = consumer.Queue.Dequeue();

                            exitArgs.Message = delivery.Body;
                            exitArgs.Headers = delivery.BasicProperties.Headers;

                            try
                            {
                                //logger.Debug($"Topic {topic} consumer message received from {queueName} with topics {delivery.RoutingKey}");

                                //check version
                                if (delivery.BasicProperties.Headers != null && delivery.BasicProperties.Headers.Keys.Contains("x-version"))
                                {
                                    var versionHeader = Encoding.UTF8.GetString(delivery.BasicProperties.Headers["x-version"] as byte[]);
                                    var msgVersion = Version.Parse(versionHeader);
                                    var curVersion = Version.Parse(version);

                                    if (msgVersion != curVersion)
                                    {
                                        logger.Warn($"Upgrade needed to {versionHeader} from {version}");

                                        topicConsumer.OnVersionMismatch(msgVersion, curVersion);
                                    }
                                }

                                var message = serializer.DeserializeObject<T>(delivery.Body);

                                topicConsumer.ProcessMessage(message);

                                //reset counter
                                retryCount = 0;

                            }
                            catch(ConsumerException cex) when (cex.IsRetryable)
                            {
                                if (RetryMax > 0 && retryCount > RetryMax)
                                {
                                    logger.LogException(cex, $"Exiting! The maximum of {RetryMax} retries has been reached");
                                    break;
                                }

                                retryCount++;
                                reject = true;
                                logger.LogException(cex.InnerException, $"Topic {topic} consumer encounter a processing error that will retry, error {cex.Message}");
                            }
                            catch (ConsumerException cex) when (cex.StopSignal)
                            {
                                exitArgs.UnderlyingException = cex;
                                logger.LogException(cex, $"Topic {topic} consumer encounter a processing error");
                                break;
                            }
                            catch (Exception ex)
                            {
                                exitArgs.UnderlyingException = ex;

                                //TODO: store message in error queue
                                logger.LogException(ex, $"Message deleted! Topic {topic} consumer message encounter a processing error {ex.Message}");
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
                                exitArgs.StopSignalReceived = stopPending;
                                logger.Info($"Exiting! The topic {topic} consumer received a stop signal");
                                break;
                            }

                            if(RetryMax > 0 && retryCount > RetryMax)
                            {
                                logger.LogException(eox, $"Exiting! The maximum of {RetryMax} retries has been reached, error {eox.Message}");
                                break;
                            }

                            logger.Warn($"Retrying! Topic consumer connection error {eox.Message}");

                            // wait for the connection to be restored
                            Thread.Sleep(amqpConnectionFactory.NetworkRecoveryInterval);

                            // reinitialize consumer
                            if (consumerConnection.IsOpen && amqpChannel.IsOpen)
                            {
                                logger.Info($"Restoring topic {topic} queue and consumer");
                                SetUpExchange(amqpChannel);
                                queueName = SetUpQueue(amqpChannel, topic);
                                consumer = SetUpConsumer(amqpChannel, queueName);
                            }
                            retryCount++;
                        }
                        catch (Exception ex)
                        {
                            logger.LogException(ex, $"Exiting! Topic {topic} consumer encountered a fatal error {ex.Message}");
                            break;
                        }
                    }

                    topicConsumer.OnConsumerExit(exitArgs);
                }
            }
        }

        public void StartConsumerInBackground<T>(string topic, IConsumer<T> topicConsumer)
        {
            var backgroundThread = new Thread(t =>
            {
                StartConsumer<T>(topic, topicConsumer);
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
                    props.Headers.Add("x-timestamp", DateTime.UtcNow.ToBinary().ToString());
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
