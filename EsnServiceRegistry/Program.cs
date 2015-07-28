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

namespace EsnServiceRegistry
{
    class Program
    {
        internal static ServiceInfo ServiceStatus;
        internal static RpcServer ServiceRegistryServer;

        static void Main(string[] args)
        {
            ServiceStatus = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            ServiceRegistryServer = new RpcServer(ConnectionConfig.GetFactoryDefault(), RpcSettings.RegistryQueue);
            ServiceRegistryServer.StartInBackground();

            var webServer = WebApp.Start<Startup>(url: ServiceConfig.Reader.GetBaseAddress());

            Console.WriteLine("Service Registry Server started");
            Console.ReadLine();

            ServiceRegistryServer.Stop();
            webServer.Dispose();
        }
    }
}
