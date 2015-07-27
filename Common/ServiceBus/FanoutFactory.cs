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

        private volatile bool stopPending;

        public string ExchangeName { get; set; }
        public bool DurableExchange { get; set; }

        public FanoutFactory(ConnectionFactory connectionFactory, string exchangeName = "esn.fanout", bool durableExchange = true)
        {
            amqpConnectionFactory = connectionFactory;
            DurableExchange = durableExchange;
            ExchangeName = exchangeName;
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
                            var delivery = consumer.Queue.Dequeue();
                            var json = Encoding.UTF8.GetString(delivery.Body);

                            try
                            {
                                Console.WriteLine(" [PubSub] Message received from '{0}' with topics len {1}",
                                    queueName, json.Length);

                                var message = JsonConvert.DeserializeObject<T>(json);

                                processMessage(message);

                            }
                            catch (Exception ex)
                            {
                                //TODO: store message in error queue
                                Console.WriteLine("Fanout consumer message processing error {0}", ex.Message);
                            }

                            // remove message from q
                            amqpChannel.BasicAck(delivery.DeliveryTag, false);
                        }
                        catch (EndOfStreamException eox)
                        {
                            if (stopPending)
                            {
                                Console.WriteLine("Exiting! The fanout consumer received a stop signal");
                                break;
                            }

                            Console.WriteLine($"Fanout consumer connection error {eox.Message} connection closed {!consumerConnection.IsOpen}");

                            // wait for the connection to be restored
                            Thread.Sleep(amqpConnectionFactory.NetworkRecoveryInterval);

                            // reinitialize consumer
                            if (consumerConnection.IsOpen && amqpChannel.IsOpen)
                            {
                                Console.WriteLine($"Restoring fanout queue and consumer");
                                queueName = SetUpQueue(amqpChannel);
                                consumer = SetUpConsumer(amqpChannel, queueName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exiting! The fanout consumer encountered a fatal error {ex.Message}");

                            break;
                        }
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
                    props.ContentType = "application/json";
                    props.ContentEncoding = "utf-8";

                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
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
