using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.Registry
{
    public class HostInfo
    {
        public string Id { get; set; }
        public string Guid { get; set; }
        public string Name { get; set; }
        public string OSVersion { get; set; }
        public long TotalMemory { get; set; }
        public long FreeMemory { get; set; }
        public int CPUs { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Location { get; set; }
        public DateTime RegisterDate { get; set; }
        public DateTime LastPingDate { get; set; }
        public bool IsDisconnect { get; set; }
        public List<ServiceInfo> Services { get; set; } = new List<ServiceInfo>();
    }
}
