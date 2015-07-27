using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceRegistry.Models
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

        public string Port
        {
            get
            {
                return ReadString("Port");
            }
        }

        public IEnumerable<string> AuthorizedTokens
        {
            get
            {
                return ReadStringList("AuthorizedTokens");
            }
        }

        public string GetBaseAddress(string host = "*")
        {
            return string.Format("http://{0}:{1}", host, Port);
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
