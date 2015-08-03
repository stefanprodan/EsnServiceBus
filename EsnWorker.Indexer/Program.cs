using EsnCore.Registry;
using EsnCore.ServiceBus;
using EsnWorker.Indexer.Consumers;
using System;
using System.Collections.Generic;

namespace EsnWorker.Indexer
{
    class Program
    {
        private static RegistryClient registryClient;
        private static List<TopicFactory> topicConsumers = new List<TopicFactory>();
        private static List<FanoutFactory> pubSubConsumers = new List<FanoutFactory>();

        static void Main(string[] args)
        {
            registryClient = new RegistryClient(
                connectionFactory: ConnectionConfig.GetFactoryDefault(RegistrySettings.Reader.AmqpUri),
                serializer: new JsonMessageSerializer(),
                logger: new ConsoleLog(),
                syncInterval: TimeSpan.FromSeconds(30),
                timeout: TimeSpan.FromSeconds(25));

            var serviceDefinition = ServiceInfoFactory.LoadFromDisk(RegistrySettings.Reader.ServiceName);
            serviceDefinition = ServiceInfoFactory.CreateServiceDefinition(serviceDefinition);
            serviceDefinition.Name = RegistrySettings.Reader.ServiceName;

            var response = registryClient.Register(serviceDefinition);
            if (response != null)
            {
                ServiceInfoFactory.SaveToDisk(response);

                Console.WriteLine($"Service registry provided ID {response.Guid}");

                StartTopicWorkers(Topics.Text, Topics.Url, Topics.Images, Topics.Video);
                StartPubSubWorkers(2);

                registryClient.SyncInBackground();
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
                var consumer = new TopicFactory(
                    ConnectionConfig.GetFactoryDefault(RegistrySettings.Reader.AmqpUri),
                    new JsonMessageSerializer(),
                    new ConsoleLog(),
                    registryClient.ServiceDefinition.Version);

                consumer.RetryMax = 10;
                consumer.StartConsumerInBackground<ServiceInfo>(item, new TopicConsumer());
                topicConsumers.Add(consumer);
            }
        }

        static void StartPubSubWorkers(int workers)
        {
            for (int i = 0; i < workers; i++)
            {
                var consumer = new FanoutFactory(
                    ConnectionConfig.GetFactoryDefault(RegistrySettings.Reader.AmqpUri),
                    new JsonMessageSerializer(),
                    new ConsoleLog(),
                    registryClient.ServiceDefinition.Version);

                consumer.StartConsumerInBackground<ServiceInfo>(new FanoutConsumer());
                pubSubConsumers.Add(consumer);
            }
        }

    }
}
