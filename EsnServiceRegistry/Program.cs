using EsnCore.Registry;
using EsnCore.ServiceBus;
using Microsoft.Owin.Hosting;
using EsnServiceRegistry.Models;
using System;
using EsnServiceRegistry.Store;
using EsnServiceRegistry.Consumers;

namespace EsnServiceRegistry
{
    class Program
    {
        private static ServiceInfo serviceDefinition;
        private static RpcServer<ServiceInfo> serviceRegistryServer;

        static void Main(string[] args)
        {
            serviceDefinition = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());

            serviceRegistryServer = new RpcServer<ServiceInfo>(ConnectionConfig.GetFactoryDefault(), RegistrySettings.RegistryQueue, registryRepo.InsertOrUpdateService);
            serviceRegistryServer.StartInBackground();
            var statsConsumer = StartStatsConsumer();

            var webServer = WebApp.Start<Startup>(url: ServiceConfig.Reader.GetBaseAddress());

            Console.WriteLine("Service Registry Server started");
            Console.ReadLine();

            statsConsumer.StopConsumer();
            serviceRegistryServer.Stop();
            webServer.Dispose();
        }

        static TopicFactory StartStatsConsumer()
        {
            var consumer = new TopicFactory(
                ConnectionConfig.GetFactoryDefault(RegistrySettings.Reader.AmqpUri),
                new JsonMessageSerializer(),
                new ConsoleLog(),
                serviceDefinition.Version,
                RegistrySettings.RegistryStatsExchange);

            consumer.RetryMax = 10;
            consumer.StartConsumerInBackground("status", new StatsConsumer());

            return consumer;
        }
    }
}
