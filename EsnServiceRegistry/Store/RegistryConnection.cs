using EsnCore.Registry;
using RethinkDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public class RegistryConnection : StoreConnection
    {
        public RegistryConnection(IConnection connection)
            : base(connection, "registry")
        {
        }

        public ITableQuery<ServiceModel> Services => Database.Table<ServiceModel>("services");
        public ITableQuery<HostModel> Hosts => Database.Table<HostModel>("hosts");
    }
}
