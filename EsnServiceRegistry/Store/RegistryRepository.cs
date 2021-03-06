﻿using EsnCore.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RethinkDb;
using EsnServiceRegistry.Models;

namespace EsnServiceRegistry.Store
{
    public class RegistryRepository : BaseRepository
    {
        public RegistryRepository(RegistryDatabaseFactory databaseFactory)
            : base(databaseFactory)
        {
        }

        #region Services

        public void GetTotals(out int services, out int hosts, out int issues)
        {
            services = 0;
            hosts = 0;

            var date = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout);
            var running = (int)ServiceState.Running;
            services = r.Run(r.Services.Between(date, DateTime.UtcNow, "idx_date").Where(s => s.State == running)).Count();
            hosts = r.Run(r.Hosts.Between(date, DateTime.UtcNow, "idx_date")).Count();
            issues = r.Run(r.Services.Between(DateTime.UtcNow.AddDays(-30), date, "idx_date").OrderByDescending("idx_date").Where(s => s.State == running)).Count();
        }

        public ServiceInfo GetService(string guid)
        {
            ServiceInfo info = null;
            var service = r.Run(r.Services.GetAll(guid, "idx_guid").OrderByDescending(s => s.LastPingDate).Limit(1)).FirstOrDefault();

            if (service != null)
            {
                info = service.ToServiceInfo();
                info.IsDisconnect = (service.State != (int)ServiceState.Running) || IsDisconnect(info.LastPingDate);
                var host = r.Run(r.Hosts.GetAll(info.HostGuid, "idx_guid").Limit(1)).FirstOrDefault();
                if (host != null)
                {
                    info.Host = host.ToHostInfo();
                }
            }
            return info;
        }

        public List<ServiceInfo> GetServiceCluster(string guid)
        {
            var list = new List<ServiceInfo>();
            var service = r.Run(r.Services.GetAll(guid, "idx_guid").OrderByDescending(s => s.LastPingDate).Limit(1)).FirstOrDefault();

            if (service != null)
            {
                var running = (int)ServiceState.Running;
                var services = r.Run(r.Services.GetAll(service.Name, "idx_name").Limit(Limit).OrderByDescending(s => s.LastPingDate).Where(s => s.State == running)).ToList();
                foreach (var item in services)
                {
                    var info = item.ToServiceInfo();
                    info.IsDisconnect = (item.State != (int)ServiceState.Running) || IsDisconnect(info.LastPingDate);
                    list.Add(info);
                }
            }

            return list;
        }

        public List<ServiceInfo> GetServiceInstances(string guid)
        {
            var list = new List<ServiceInfo>();
            var services = r.Run(r.Services.GetAll(guid, "idx_guid").Limit(Limit).OrderByDescending(s => s.LastPingDate)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = (item.State != (int)ServiceState.Running) || IsDisconnect(info.LastPingDate);
                list.Add(info);
            }

            return list;
        }

        public List<ServiceInfo> AllServices()
        {
            var list = new List<ServiceInfo>();
            var stopped = (int)ServiceState.Stopped;
            var removed = (int)ServiceState.Decommissioned;
            var services = r.Run(r.Services.OrderByDescending("idx_date").Where(s => s.State != stopped && s.State != removed)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = (item.State != (int)ServiceState.Running) || IsDisconnect(info.LastPingDate);
                list.Add(info);
            }

            return list;
        }

        public ServiceCluster GetServiceClusterInfo(string guid)
        {
            var stopped = (int)ServiceState.Stopped;
            var removed = (int)ServiceState.Decommissioned;
            var name = r.Run(r.Services.GetAll(guid, "idx_guid").Limit(1)).First().Name;
            var services = r.Run(r.Services
                .OrderByDescending("idx_date")
                .Filter(s => s.State != stopped && s.State != removed && s.Name == name)
                .Group(s => s.Name)
                .Map(s => new ServiceCluster
                {
                    TotalCount = 1,
                    OnlineCount = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout) > s.LastPingDate ? 0 : 1,
                    Name = s.Name,
                    TotalMemory = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout) > s.LastPingDate ? 0 : s.MemoryUsage,
                    TotalCpuTime = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout) > s.LastPingDate ? 0 : s.CpuTime,
                })
                .Reduce((left, right) => new ServiceCluster
                {
                    TotalCount = left.TotalCount + right.TotalCount,
                    OnlineCount = left.OnlineCount + right.OnlineCount,
                    Name = left.Name,
                    TotalMemory = left.TotalMemory + right.TotalMemory,
                    TotalCpuTime = left.TotalCpuTime + right.TotalCpuTime,
                }));

            return services.Values.FirstOrDefault();
        }

        public List<ServiceCluster> GetServiceClusters()
        {
            var list = new List<ServiceCluster>();
            var stopped = (int)ServiceState.Stopped;
            var removed = (int)ServiceState.Decommissioned;
            var services = r.Run(r.Services
                .OrderByDescending("idx_date")
                .Filter(s => s.State != stopped && s.State != removed)
                .Group(s => s.Name)
                .Map(s => new ServiceCluster
                {
                    TotalCount = 1,
                    OnlineCount = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout) > s.LastPingDate ? 0 : 1,
                    Guid = s.Guid,
                    Name = s.Name,
                    TotalMemory = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout) > s.LastPingDate ? 0 : s.MemoryUsage,
                    TotalCpuTime = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout) > s.LastPingDate ? 0 : s.CpuTime,
                    From = s.LastPingDate,
                    To = s.LastPingDate,
                    //HostList = new[] { s.HostGuid }
                    //Hosts = new Dictionary<string, object> { { s.HostGuid, s.HostName } }
                })
                .Reduce((left, right) => new ServiceCluster
                {
                    TotalCount = left.TotalCount + right.TotalCount,
                    OnlineCount = left.OnlineCount + right.OnlineCount,
                    Guid = left.Guid,
                    Name = left.Name,
                    TotalMemory = left.TotalMemory + right.TotalMemory,
                    TotalCpuTime = left.TotalCpuTime + right.TotalCpuTime,
                    From = left.From < right.From ? left.From : right.From,
                    To = left.To > right.To ? left.To : right.To,
                    //HostList = left.HostList.Union(right.HostList).ToArray()
                    //Hosts = left.Hosts.Union(right.Hosts).ToDictionary(h => h.Key, h => h.Value)
                }));

            list = services.Values.ToList();

            return list;
        }

        public List<ServiceInfo> AllServices(ServiceState status)
        {
            var list = new List<ServiceInfo>();
            var stat = (int)status;

            var services = r.Run(r.Services.OrderByDescending("idx_date").Where(s => s.State == stat)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = (item.State != (int)ServiceState.Running) || IsDisconnect(info.LastPingDate);
                list.Add(info);
            }

            return list;
        }

        public List<ServiceInfo> AllActiveServices()
        {
            var list = new List<ServiceInfo>();
            var date = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout);
            var running = (int)ServiceState.Running;

            var services = r.Run(r.Services.Between(date, DateTime.UtcNow, "idx_date").OrderByDescending("idx_date").Where(s => s.State == running)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = (item.State != (int)ServiceState.Running) || IsDisconnect(info.LastPingDate);
                list.Add(info);
            }

            return list;
        }

        public List<ServiceInfo> AllDisconnectServices()
        {
            var list = new List<ServiceInfo>();
            var date = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout);
            var running = (int)ServiceState.Running;

            var services = r.Run(r.Services.Between(DateTime.UtcNow.AddDays(-30), date, "idx_date").OrderByDescending("idx_date").Where(s => s.State == running)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = true;
                list.Add(info);
            }

            return list;
        }

        public ServiceInfo InsertOrUpdateService(ServiceInfo info)
        {
            if (string.IsNullOrEmpty(info.Guid))
            {
                info.Guid = Guid.NewGuid().ToString();
            }

            info.LastPingDate = DateTime.UtcNow;

            var model = ServiceModel.FromServiceInfo(info);
            var host = InsertOrUpdateHost(info.Host);
            
            var service = r.Run(r.Services.OrderByDescending("idx_date").Where(x => x.Guid == info.Guid).Limit(1)).FirstOrDefault();

            if (service == null)
            {
                model.RegisterDate = DateTime.UtcNow;

                var response = r.Run(r.Services.Insert(model));
                model.Id = response.GeneratedKeys.FirstOrDefault();
            }
            else
            {
                // merge tags for db with received ones
                foreach (var tag in service.Tags)
                {
                    if (!model.Tags.Contains(tag))
                    {
                        model.Tags.Add(tag);
                    }
                }

                // insert new if pid changed
                if (info.Pid != service.Pid)
                {
                    model.Id = null;
                    model.RegisterDate = DateTime.UtcNow;

                    var response = r.Run(r.Services.Insert(model));
                    model.Id = response.GeneratedKeys.FirstOrDefault();

                    //update last instance status
                    service.State = (int)ServiceState.Stopped;
                    r.Run(r.Services.Update(h => service));
                }
                else
                {
                    model.Id = service.Id;
                    model.RegisterDate = service.RegisterDate;

                    r.Run(r.Services.Update(h => model));
                }
            }

            return model.ToServiceInfo();
        }

        public async Task<ServiceInfo> InsertOrUpdateServiceAsync(ServiceInfo info)
        {
            if (string.IsNullOrEmpty(info.Guid))
            {
                info.Guid = Guid.NewGuid().ToString();
            }

            info.LastPingDate = DateTime.UtcNow;

            var model = ServiceModel.FromServiceInfo(info);
            var host = InsertOrUpdateHost(info.Host);

            var th = r.RunAsync(r.Services.OrderByDescending("idx_date").Where(x => x.Guid == info.Guid).Limit(1));
            await th.MoveNext();
            var service = th.Current;
            if (service == null)
            {
                model.RegisterDate = DateTime.UtcNow;

                var response = r.Run(r.Services.Insert(model));
                model.Id = response.GeneratedKeys.FirstOrDefault();
            }
            else
            {
                // merge tags for db with received ones
                foreach (var tag in service.Tags)
                {
                    if (!model.Tags.Contains(tag))
                    {
                        model.Tags.Add(tag);
                    }
                }

                // insert new if pid changed
                if (info.Pid != service.Pid)
                {
                    model.Id = null;
                    model.RegisterDate = DateTime.UtcNow;

                    var response = r.Run(r.Services.Insert(model));
                    model.Id = response.GeneratedKeys.FirstOrDefault();

                    //update last instance status
                    service.State = (int)ServiceState.Stopped;
                    r.Run(r.Services.Update(h => service));
                }
                else
                {
                    model.Id = service.Id;
                    model.RegisterDate = service.RegisterDate;

                    r.Run(r.Services.Update(h => model));
                }
            }

            return model.ToServiceInfo();
        }

        public void UpdateServiceTags(string guid, List<string> tags)
        {
            r.Run(r.Services.GetAll(guid, "idx_guid").Update(h => new ServiceModel { Tags = tags }));
        }

        public void UpdateServiceStatus(string guid, ServiceState state)
        {
            var status = (int)state;
            r.Run(r.Services.GetAll(guid, "idx_guid").OrderByDescending(s => s.LastPingDate).Limit(1).Update(s => new ServiceModel { State = status }));
        }

        #endregion

        #region Hosts

        public HostInfo GetHost(string guid)
        {
            HostInfo info = null;
            var running = (int)ServiceState.Running;

            var host = r.Run(r.Hosts.GetAll(guid, "idx_guid").Limit(1)).FirstOrDefault();

            if (host != null)
            {
                info = host.ToHostInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
                info.ServicesCount = r.Run(r.Services.GetAll(guid, "idx_host").Where(s => s.State == running)).Count();
            }
            return info;
        }

        public List<ServiceInfo> AllHostServices(string guid)
        {
            var list = new List<ServiceInfo>();
            var running = (int)ServiceState.Running;

            var services = r.Run(r.Services.GetAll(guid, "idx_host").Where(s => s.State == running).OrderByDescending(s => s.LastPingDate)).ToList();
            foreach (var item in services)
            {
                var srv = item.ToServiceInfo();
                srv.IsDisconnect = IsDisconnect(srv.LastPingDate);
                list.Add(srv);
            }

            return list;
        }

        public List<HostInfo> AllHosts()
        {
            var list = new List<HostInfo>();
            var running = (int)ServiceState.Running;

            var hosts = r.Run(r.Hosts.OrderByDescending("idx_date")).ToList();
            foreach (var item in hosts)
            {
                var info = item.ToHostInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
                info.ServicesCount = r.Run(r.Services.GetAll(item.Guid, "idx_host").Where(s => s.State == running)).Count();
                list.Add(info);
            }

            return list;
        }

        public List<HostInfo> AllActiveHosts(int activeMinutesAgo)
        {
            var list = new List<HostInfo>();
            var date = DateTime.UtcNow.AddMinutes((-1) * activeMinutesAgo);

            var hosts = r.Run(r.Hosts.OrderByDescending("idx_date").Between(date, DateTime.UtcNow, "idx_date")).ToList();
            foreach (var item in hosts)
            {
                var info = item.ToHostInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
                list.Add(info);
            }

            return list;
        }

        public HostInfo InsertOrUpdateHost(HostInfo info)
        {
            var id = info.Id;
            info.LastPingDate = DateTime.UtcNow;
            var model = HostModel.FromHostInfo(info);
            var host = r.Run(r.Hosts.Filter(x => x.Guid == info.Guid).Limit(1)).FirstOrDefault();
            if (host == null)
            {
                model.RegisterDate = DateTime.UtcNow;
                var response = r.Run(r.Hosts.Insert(model));
                model.Id = response.GeneratedKeys.FirstOrDefault();
            }
            else
            {
                model.Id = host.Id;
                model.RegisterDate = host.RegisterDate;
                model.Location = host.Location;

                foreach (var tag in host.Tags)
                {
                    if (!model.Tags.Contains(tag))
                    {
                        model.Tags.Add(tag);
                    }
                }
                r.Run(r.Hosts.Update(h => model));
            }

            return model.ToHostInfo();
        }

        public void UpdateHostTagsLocation(string guid, string location, List<string> tags)
        {
            r.Run(r.Hosts.GetAll(guid, "idx_guid").Update(h => new HostModel { Location = location, Tags = tags }));
        }

        #endregion

    }
}
