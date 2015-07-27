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

        private volatile bool stopPending;

        public string ExchangeName { get; set; }
        public bool DurableExchange { get; set; }
        public bool DurableQueue { get; set; }

        public bool PersistentMessages { get; set; }

        public TopicFactory(ConnectionFactory connectionFactory, string exchangeName = "esn.topic", bool durableExchange = true, bool durableQueue = true, bool persistentMessages = true)
        {
            amqpConnectionFactory = connectionFactory;
            ExchangeName = exchangeName;
            DurableExchange = durableExchange;
            DurableQueue = durableQueue;
            PersistentMessages = persistentMessages;
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

        public void StartConsumer(string topic)
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
                            var delivery = consumer.Queue.Dequeue();
                            var message = Encoding.UTF8.GetString(delivery.Body);

                            try
                            {
                                Console.WriteLine(" [{0}] Message received from '{1}' with topics '{2}' len {3}",
                                   topic.ToUpperInvariant(), queueName, delivery.RoutingKey, message.Length);
                            }
                            catch (Exception ex)
                            {
                                //TODO: store message in error queue
                                Console.WriteLine("Topic consumer message processing error {0}", ex.Message);
                            }

                            // remove message from q
                            amqpChannel.BasicAck(delivery.DeliveryTag, false);
                        }
                        catch (EndOfStreamException eox)
                        {
                            if (stopPending)
                            {
                                Console.WriteLine("Exiting! The topic consumer received a stop signal");
                                break;
                            }

                            Console.WriteLine($"Topic consumer connection error {eox.Message} connection closed {!consumerConnection.IsOpen}");

                            // wait for the connection to be restored
                            Thread.Sleep(amqpConnectionFactory.NetworkRecoveryInterval);

                            // reinitialize consumer
                            if (consumerConnection.IsOpen && amqpChannel.IsOpen)
                            {
                                Console.WriteLine($"Restoring topic {topic} queue and consumer");
                                queueName = SetUpQueue(amqpChannel, topic);
                                consumer = SetUpConsumer(amqpChannel, queueName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exiting! The topic consumer encountered a fatal error {ex.Message}");

                            break;
                        }
                    }
                }
            }
        }

        public void StartConsumerInBackground(string topic)
        {
            var backgroundThread = new Thread(t =>
            {
                StartConsumer(topic);
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
                    props.ContentType = "application/json";
                    props.ContentEncoding = "utf-8";
                    props.Persistent = PersistentMessages;

                    var routingKey = GetTopicsRoutingKey(topics);
                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
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
