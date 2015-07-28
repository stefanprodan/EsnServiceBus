using EsnWorker.Consumers;
using EsnCore.Registry;
using EsnCore.ServiceBus;
using System;
using System.Collections.Generic;

namespace EsnWorker
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
                var consumer = new TopicFactory(ConnectionConfig.GetFactoryDefault(), new JsonMessageSerializer(), new ConsoleLog(), InstanceInfo.Version);
                consumer.RetryMax = 10;
                consumer.StartConsumerInBackground<ServiceInfo>(item, new TopicConsumer());
                TopicConsumers.Add(consumer);
            }
        }

        static void StartPubSubWorkers(int workers)
        {
            for (int i = 0; i < workers; i++)
            {
                var consumer = new FanoutFactory(ConnectionConfig.GetFactoryDefault(), new JsonMessageSerializer(), new ConsoleLog(), InstanceInfo.Version);
                consumer.StartConsumerInBackground<ServiceInfo>(new FanoutConsumer());
                PubSubConsumers.Add(consumer);
            }
        }

    }
}
