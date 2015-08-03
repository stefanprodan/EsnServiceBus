using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnWorker.Helpers
{
    public class ServiceConfig
    {
        public static readonly ServiceConfig Reader = new ServiceConfig();

        public string ServiceName
        {
            get
            {
                return ReadString("ServiceName");
            }
        }


        public string AmqpUri
        {
            get
            {
                return ReadString("AmqpUri");
            }
        }    

        private static string ReadString(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        private static IEnumerable<string> ReadStringList(string key)
        {
            var items = ConfigurationManager.AppSettings[key];
            return items.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
        }
    }
}
