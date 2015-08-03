﻿using EsnCore.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RethinkDb;

namespace EsnServiceRegistry.Store
{
    public class RegistryRepository : BaseRepository
    {
        

        public RegistryRepository(RegistryDatabaseFactory databaseFactory)
            : base(databaseFactory)
        {
        }

        #region Services

        public void GetTotals(out int services, out int hosts, int activeMinutesAgo = 1000)
        {
            services = 0;
            hosts = 0;

            var date = DateTime.UtcNow.AddMinutes((-1) * activeMinutesAgo);
            var running = (int)ServiceState.Running;
            services = r.Run(r.Services.Between(date, DateTime.UtcNow, "idx_date").Where(s => s.State == running)).Count();
            hosts = r.Run(r.Hosts.Between(date, DateTime.UtcNow, "idx_date")).Count();

        }

        

        public ServiceInfo GetService(string guid)
        {
            ServiceInfo info = null;
            var service = r.Run(r.Services.GetAll(guid, "idx_guid").OrderByDescending(s => s.LastPingDate).Limit(1)).FirstOrDefault();

            if (service != null)
            {
                info = service.ToServiceInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
                var host = r.Run(r.Hosts.GetAll(info.HostGuid, "idx_guid").Limit(1)).FirstOrDefault();
                if (host != null)
                {
                    info.Host = host.ToHostInfo();
                }
            }
            return info;
        }

        public List<ServiceInfo> AllServices(ServiceState status)
        {
            var list = new List<ServiceInfo>();
            var stat = (int)status;

            var services = r.Run(r.Services.OrderByDescending("idx_date").Where(s => s.State == stat)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
                list.Add(info);
            }

            return list;
        }

        public List<ServiceInfo> AllActiveServices(int activeMinutesAgo)
        {
            var list = new List<ServiceInfo>();
            var date = DateTime.UtcNow.AddMinutes((-1) * activeMinutesAgo);
            var running = (int)ServiceState.Running;

            var services = r.Run(r.Services.Between(date, DateTime.UtcNow, "idx_date").OrderByDescending("idx_date").Where(s => s.State == running)).ToList();
            foreach (var item in services)
            {
                var info = item.ToServiceInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
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
                if (info.Pid != model.Pid)
                {
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

        public ServiceInfo UpdateServiceTags(string guid, List<string> tags)
        {
            var service = r.Run(r.Services.GetAll(guid, "idx_guid").OrderByDescending(s => s.LastPingDate).Limit(1)).FirstOrDefault();
            service.Tags = tags;

            r.Run(r.Services.Update(h => service));

            return service.ToServiceInfo();
        }

        #endregion

        #region Hosts

        public HostInfo GetHost(string guid, int serviceActiveMinutesAgo)
        {
            HostInfo info = null;
            var host = r.Run(r.Hosts.GetAll(guid, "idx_guid").Limit(1)).FirstOrDefault();

            if (host != null)
            {
                info = host.ToHostInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
                var date = DateTime.UtcNow.AddMinutes((-1) * serviceActiveMinutesAgo);
                var services = r.Run(r.Services.GetAll(guid, "idx_host").Where(s => s.LastPingDate > date).OrderByDescending(s => s.LastPingDate)).ToList();
                foreach (var item in services)
                {
                    var srv = item.ToServiceInfo();
                    srv.IsDisconnect = IsDisconnect(srv.LastPingDate);
                    info.Services.Add(srv);
                }

            }
            return info;
        }

        public List<HostInfo> AllHosts()
        {
            var list = new List<HostInfo>();

            var hosts = r.Run(r.Hosts.OrderByDescending("idx_date")).ToList();
            foreach (var item in hosts)
            {
                var info = item.ToHostInfo();
                info.IsDisconnect = IsDisconnect(info.LastPingDate);
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

        public HostInfo UpdateHostTagsLocation(string guid, List<string> tags, string location)
        {
            var host = r.Run(r.Hosts.GetAll(guid, "idx_guid").Limit(1)).FirstOrDefault();
            host.Tags = tags;
            host.Location = location;
            r.Run(r.Hosts.Update(h => host));

            return host.ToHostInfo();
        }

        #endregion

    }
}
