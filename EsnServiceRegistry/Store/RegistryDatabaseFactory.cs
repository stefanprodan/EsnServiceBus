using RethinkDb;
using RethinkDb.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public class RegistryDatabaseFactory
    {
        private static IConnectionFactory connectionFactory = RethinkDb.Newtonsoft
                               .Configuration.ConfigurationAssembler.CreateConnectionFactory("registry");
        private RegistryConnection r;

        public RegistryConnection Create()
        {
            return r ?? (r = new RegistryConnection(connectionFactory.Get()));
        }

        public void ApplySchema()
        {
            // registry
            if (!r.Run(Query.DbList()).Contains("registry"))
            {
                r.Run(Query.DbCreate("registry"));
            }

            // services
            var tables = r.Run(r.Database.TableList());
            if (!tables.Contains("services"))
            {
                r.Run(r.Database.TableCreate("services"));
            }

            var servicesIdxList = r.Run(r.Services.IndexList());
            if (!servicesIdxList.Contains("idx_date"))
            {
                r.Run(r.Services.IndexCreate("idx_date", i => i.LastPingDate));
                r.Run(r.Services.IndexWait("idx_date"));
            }

            if (!servicesIdxList.Contains("idx_host"))
            {
                r.Run(r.Services.IndexCreate("idx_host", i => i.HostGuid));
                r.Run(r.Services.IndexWait("idx_host"));
            }

            if (!servicesIdxList.Contains("idx_guid"))
            {
                r.Run(r.Services.IndexCreate("idx_guid", i => i.Guid));
                r.Run(r.Services.IndexWait("idx_guid"));
            }

            // hosts
            if (!tables.Contains("hosts"))
            {
                r.Run(r.Database.TableCreate("hosts"));
            }

            var hostsIdxList = r.Run(r.Hosts.IndexList());
            if (!hostsIdxList.Contains("idx_date"))
            {
                r.Run(r.Hosts.IndexCreate("idx_date", i => i.LastPingDate));
                r.Run(r.Hosts.IndexWait("idx_date"));
            }

            if (!hostsIdxList.Contains("idx_guid"))
            {
                r.Run(r.Hosts.IndexCreate("idx_guid", i => i.Guid));
                r.Run(r.Hosts.IndexWait("idx_guid"));
            }
        }
    }
}
