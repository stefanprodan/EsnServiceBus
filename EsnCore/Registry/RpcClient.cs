using Newtonsoft.Json;
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
    public class RpcClient
    {
        private ConnectionFactory amqpConnectionFactory;
        private IConnection connection;
        private IModel channel;
        private string queueName;
        private string replyQueueName;
        private QueueingBasicConsumer consumer;

        public RpcClient(ConnectionFactory connectionFactory, string queue)
        {
            amqpConnectionFactory = connectionFactory;
            queueName = queue;

            connection = amqpConnectionFactory.CreateConnection();
            channel = connection.CreateModel();
            replyQueueName = channel.QueueDeclare();
            consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(replyQueueName, true, consumer);
        }

        public ServiceInfo Sync(ServiceInfo message, TimeSpan timeout)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueueName;
            props.CorrelationId = corrId;
            props.ContentType = "application/json";
            props.ContentEncoding = "utf-8";

            var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            channel.BasicPublish("", queueName, props, messageBytes);

            BasicDeliverEventArgs ea = null;
            var timeoutDate = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow <= timeoutDate)
            {
                try
                {
                    var ok = consumer.Queue.Dequeue(Convert.ToInt32(timeout.TotalMilliseconds), out ea);
                    if (!ok)
                    {
                        //PRC call timeout
                        return null;
                    }

                    if (ea.BasicProperties.CorrelationId == corrId)
                    {
                        var json = Encoding.UTF8.GetString(ea.Body);
                        return JsonConvert.DeserializeObject<ServiceInfo>(json);

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RPC Sync error {ex.Message} connection is {connection.IsOpen}");
                    Thread.Sleep(500);

                    if(connection.IsOpen)
                    {
                        return null;
                    }
                }

            }

            //PRC call timeout
            return null;
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
