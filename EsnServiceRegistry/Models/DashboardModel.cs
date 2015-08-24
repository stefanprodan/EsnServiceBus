using EsnCore.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Models
{
    public class DashboardModel
    {
        public string AmqpAdmin { get; set; }
        public string RethinkAdmin { get; set; }
        public int ServicesCount { get; set; }
        public int HostsCount { get; set; }
        public int IssuesCount { get; set; }
        public ServiceInfo Registry { get; set; }
    }

    public class ServiceCluster
    {
        public int TotalCount { get; set; }
        public int OnlineCount { get; set; }
        public string Guid { get; set; }
        public string Name { get; set; }
        public long TotalMemory { get; set; }
        public double TotalCpuTime { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public Dictionary<string, object> Hosts { get; set; }
        public string[] HostList { get; set; }
    }
}
