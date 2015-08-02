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
        public ServiceInfo Registry { get; set; }
    }
}
