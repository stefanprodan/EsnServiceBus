using Common.Registry;
using Common.ServiceBus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Agent
{
    class Program
    {
        internal static ServiceInfo InstanceInfo;
        internal static List<TopicFactory> TopicConsumers = new List<TopicFactory>();
        internal static List<FanoutFactory> PubSubConsumers = new List<FanoutFactory>();
        static void Main(string[] args)
        {
            var rpc = new RpcClient(ConnectionConfig.GetFactoryDefault(), RpcSettings.RegistryQueue);

            var info = ServiceInfoFactory.CreateServiceDefinition();

            var response = rpc.Sync(info, TimeSpan.FromSeconds(60));
            if (response != null)
            {
                InstanceInfo = response;
                Console.WriteLine($"Service registry provided ID {response.Guid}");

                StartTopicWorkers(Topics.Text, Topics.Url, Topics.Images, Topics.Video);
                StartPubSubWorkers(2);

            }
            else
            {
                Console.WriteLine("Exiting! Service Registry Server is unreachable");
            }

            Console.ReadLine();
        }

        static void StartTopicWorkers(params string[] topics)
        {
            foreach (var item in topics)
            {
                var consumer = new TopicFactory(ConnectionConfig.GetFactoryDefault());
                consumer.StartConsumerInBackground<ServiceInfo>(item, ProcessTopicMessage);
                TopicConsumers.Add(consumer);
            }
        }

        static void StartPubSubWorkers(int workers)
        {
            for (int i = 0; i < workers; i++)
            {
                var consumer = new FanoutFactory(ConnectionConfig.GetFactoryDefault());
                consumer.StartConsumerInBackground<ServiceInfo>(ProcessPubSubMessage);
                PubSubConsumers.Add(consumer);
            }
        }

        static bool ProcessTopicMessage(ServiceInfo info)
        {
            var rejectMessage = false;

            if (info != null && !string.IsNullOrEmpty(info.Version))
            {
                Console.WriteLine($"Service info received from {info.Pid}@{info.Name} via Topic");
            }

            return rejectMessage;
        }

        static void ProcessPubSubMessage(ServiceInfo info)
        {
            Console.WriteLine($"Service info received from {info.Pid}@{info.Name} via PubSub");
        }

    }
}
