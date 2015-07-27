using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Registry
{
    [Description("services")]
    public class ServiceInfo
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        // identifiers
        public string Guid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime BuildDate { get; set; }
        public DateTime RegisterDate { get; set; }

        // network
        public string HostName { get; set; }
        public int Port { get; set; }

        // process
        public int Pid { get; set; }
        public DateTime StartDate { get; set; }       
        public long MemoryUsage { get; set; }
        public string OSVersion { get; set; }
        public string LocalPath { get; set; }

        // status
        public DateTime LastPingDate { get; set; }
        public ServiceState State { get; set; }

    }
}
