using EsnCore.Registry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public class HostModel
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("os_version")]
        public string OSVersion { get; set; }

        [JsonProperty("total_memory")]
        public long TotalMemory { get; set; }

        [JsonProperty("free_memory")]
        public long FreeMemory { get; set; }

        [JsonProperty("cpus")]
        public int CPUs { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("register_date")]
        public DateTime RegisterDate { get; set; }

        [JsonProperty("last_ping_date")]
        public DateTime LastPingDate { get; set; }

        public HostInfo ToHostInfo()
        {
            var model = new HostInfo();
            model.CPUs = this.CPUs;
            model.FreeMemory = this.FreeMemory;
            model.Guid = this.Guid;
            model.Id = this.Id;
            model.Location = this.Location;
            model.Name = this.Name;
            model.OSVersion = this.OSVersion;
            model.Tags = this.Tags;
            model.TotalMemory = this.TotalMemory;
            model.LastPingDate = this.LastPingDate;
            model.RegisterDate = this.RegisterDate;

            return model;
        }

        public static HostModel FromHostInfo(HostInfo info)
        {
            var model = new HostModel();
            model.CPUs = info.CPUs;
            model.FreeMemory = info.FreeMemory;
            model.Guid = info.Guid;
            model.Id = info.Id;
            model.Location = info.Location;
            model.Name = info.Name;
            model.OSVersion = info.OSVersion;
            model.Tags = info.Tags;
            model.TotalMemory = info.TotalMemory;
            model.LastPingDate = info.LastPingDate;
            model.RegisterDate = info.RegisterDate;

            return model;
        }
    }
}
