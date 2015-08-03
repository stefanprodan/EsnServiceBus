using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.Registry
{
    public class ServiceInfo
    {
        // identifiers
        public string Id { get; set; }
        public string HostGuid { get; set; }
        public string Guid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime BuildDate { get; set; }
        public DateTime RegisterDate { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        // network
        public string HostName { get; set; }
        public int Port { get; set; }

        // process
        public int Pid { get; set; }
        public DateTime StartDate { get; set; }       
        public long MemoryUsage { get; set; }
        public TimeSpan CpuTime { get; set; }
        public string LocalPath { get; set; }

        // status
        public DateTime LastPingDate { get; set; }
        public ServiceState State { get; set; }
        public bool IsDisconnect { get; set; }

        public HostInfo Host { get; set; }

    }
}
