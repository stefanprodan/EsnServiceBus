using EsnCore.Registry;
using EsnCore.ServiceBus;
using Microsoft.Owin.Hosting;
using EsnServiceRegistry.Models;
using System;
using EsnServiceRegistry.Store;
using EsnServiceRegistry.Consumers;
using System.Linq;

namespace EsnServiceRegistry
{
    class Program
    {
        private static ServiceInfo serviceDefinition;
        private static RpcServer<ServiceInfo> serviceRegistryServer;
        private static TopicFactory statsConsumer;
        private static IDisposable webServer;

        static void Main(string[] args)
        {
            serviceDefinition = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            var store = new RegistryDatabaseFactory();
            store.Create();
            store.ApplySchema();
            store.r.Connection.Dispose();

            if (args.Length == 0 || args.Contains("-ampq"))
            {
                serviceRegistryServer = new RpcServer<ServiceInfo>(ConnectionConfig.GetFactoryDefault(), RegistrySettings.RegistryQueue, RegisterService);
                serviceRegistryServer.StartInBackground();
                statsConsumer = StartStatsConsumer();

                Console.WriteLine("Registry AMPQ started");
            }

            if (args.Length == 0 || args.Contains("-web"))
            {
                webServer = WebApp.Start<Startup>(url: ServiceConfig.Reader.GetBaseAddress());
                Console.WriteLine("Web server started");
            }
            Console.ReadLine();

            if (statsConsumer != null)
            {
                statsConsumer.StopConsumer();
                serviceRegistryServer.Stop();
            }

            if (webServer != null)
            {
                webServer.Dispose();
            }
        }

        static ServiceInfo RegisterService(ServiceInfo info)
        {
            ServiceInfo result = null;

            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                result = registryRepo.InsertOrUpdateService(info);
            }

            return result;
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
