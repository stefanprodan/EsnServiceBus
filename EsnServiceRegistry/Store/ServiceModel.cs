using EsnCore.Registry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public class ServiceModel
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("host_guid")]
        public string HostGuid { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("build_date")]
        public DateTime BuildDate { get; set; }

        [JsonProperty("register_date")]
        public DateTime RegisterDate { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("host_name")]
        public string HostName { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("pid")]
        public int Pid { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("memory_usage")]
        public long MemoryUsage { get; set; }

        [JsonProperty("cpu_time")]
        public double CpuTime { get; set; }

        [JsonProperty("local_path")]
        public string LocalPath { get; set; }

        [JsonProperty("last_ping_date")]
        public DateTime LastPingDate { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        public static ServiceModel FromServiceInfo(ServiceInfo info)
        {
            var model = new ServiceModel();
            model.BuildDate = info.BuildDate;
            model.Guid = info.Guid;
            model.HostGuid = info.HostGuid;
            model.HostName = info.HostName;
            model.Id = info.Id;
            model.LastPingDate = info.LastPingDate;
            model.LocalPath = info.LocalPath;
            model.MemoryUsage = info.MemoryUsage;
            model.CpuTime = info.CpuTime;
            model.Name = info.Name;
            model.Pid = info.Pid;
            model.Port = info.Port;
            model.RegisterDate = info.RegisterDate;
            model.StartDate = info.StartDate;
            model.State = (int)info.State;
            model.Tags = info.Tags;
            model.Version = info.Version;
            return model;
        }

        public ServiceInfo ToServiceInfo()
        {
            var model = new ServiceInfo();

            model.BuildDate = this.BuildDate;
            model.Guid = this.Guid;
            model.HostGuid = this.HostGuid;
            model.HostName = this.HostName;
            model.Id = this.Id;
            model.LastPingDate = this.LastPingDate;
            model.LocalPath = this.LocalPath;
            model.MemoryUsage = this.MemoryUsage;
            model.CpuTime = this.CpuTime;
            model.Name = this.Name;
            model.Pid = this.Pid;
            model.Port = this.Port;
            model.RegisterDate = this.RegisterDate;
            model.StartDate = this.StartDate;
            model.State = (ServiceState)this.State;
            model.Tags = this.Tags;
            model.Version = this.Version;

            return model;
        }
    }
}
