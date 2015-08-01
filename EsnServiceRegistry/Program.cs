using EsnCore.Helpers;
using EsnCore.Registry;
using EsnCore.ServiceBus;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using EsnServiceRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EsnServiceRegistry.Store;

namespace EsnServiceRegistry
{
    class Program
    {
        internal static ServiceInfo ServiceStatus;
        internal static RpcServer<ServiceInfo> ServiceRegistryServer;

        static void Main(string[] args)
        {
            ServiceStatus = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());

            ServiceRegistryServer = new RpcServer<ServiceInfo>(ConnectionConfig.GetFactoryDefault(), RpcSettings.RegistryQueue, registryRepo.InsertOrUpdateService);
            ServiceRegistryServer.StartInBackground();

            var webServer = WebApp.Start<Startup>(url: ServiceConfig.Reader.GetBaseAddress());

            Console.WriteLine("Service Registry Server started");
            Console.ReadLine();

            ServiceRegistryServer.Stop();
            webServer.Dispose();
        }
    }
}
