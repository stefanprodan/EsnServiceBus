using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ServiceBus
{
    public class ConnectionConfig
    {
        public static ConnectionFactory GetFactoryDefault(string host = "localhost")
        {
            //var factory = new ConnectionFactory()
            //{
            //    HostName = host,
            //    Port = 5672,
            //    UserName = "guest",
            //    Password = "guest",
            //    VirtualHost = "/",
            //    AutomaticRecoveryEnabled = true,
            //    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            //    RequestedHeartbeat = 60,
            //};

            var factory = new ConnectionFactory();
            factory.Uri = "amqp://guest:guest@localhost:5672/%2f?heartbeat=60";
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(5);

            return factory;
        }
    }
}
