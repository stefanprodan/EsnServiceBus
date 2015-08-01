using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EsnCore.Registry
{
    public class RpcServer<T>
    {
        private ConnectionFactory amqpConnectionFactory;
        private IConnection amqpConnection;
        private string queueName;
        private volatile bool stopPending;
        private Thread backgroundThread;
        Func<T, T> messageProcess;


        public RpcServer(ConnectionFactory connectionFactory, string queue, Func<T, T> process)
        {
            amqpConnectionFactory = connectionFactory;
            queueName = queue;
            messageProcess = process;
            backgroundThread = new Thread(Start);
            backgroundThread.IsBackground = true;
        }

        public void StartInBackground()
        {
            backgroundThread.Start();
        }

        private void Start()
        {
            using (amqpConnection = amqpConnectionFactory.CreateConnection())
            {
                using (var channel = amqpConnection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    channel.BasicQos(0, 1, false);
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(queueName, false, consumer);

                    Console.WriteLine("Service Registry Server is waiting RPC requests");

                    while (!stopPending)
                    {
                        try
                        {
                            string response = null;
                            var ea = consumer.Queue.Dequeue();

                            var body = ea.Body;
                            var props = ea.BasicProperties;
                            var replyProps = channel.CreateBasicProperties();
                            replyProps.CorrelationId = props.CorrelationId;

                            try
                            {
                                var message = Encoding.UTF8.GetString(body);
                                var inObj = JsonConvert.DeserializeObject<T>(message);
                                var outObj = messageProcess(inObj);

                                Console.WriteLine($"Message received from service");

                                response = JsonConvert.SerializeObject(outObj);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error occurred awaiting RPC requests {ex.Message}");
                                response = "";
                            }
                            finally
                            {
                                var responseBytes = Encoding.UTF8.GetBytes(response);
                                channel.BasicPublish("", props.ReplyTo, replyProps, responseBytes);
                                channel.BasicAck(ea.DeliveryTag, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"RPC Sync error {ex.Message} connection is {amqpConnection.IsOpen}");
                            Thread.Sleep(500);

                            if (amqpConnection.IsOpen)
                            {
                                channel.Close();
                                amqpConnection.Close();
                                Start();
                            }
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            stopPending = true;

            if(amqpConnection != null && amqpConnection.IsOpen)
            {
                amqpConnection.Close();
            }
        }
    }
}
