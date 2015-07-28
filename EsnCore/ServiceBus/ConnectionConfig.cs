using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.ServiceBus
{
    public class ConnectionConfig
    {
        public static ConnectionFactory GetFactoryDefault(string uri = null)
        {
            uri = string.IsNullOrEmpty(uri) ?  "amqp://guest:guest@localhost:5672/%2f" : uri;

            var factory = new ConnectionFactory();
            factory.Uri = uri;
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(5);

            return factory;
        }
    }
}
