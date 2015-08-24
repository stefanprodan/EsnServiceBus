using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public abstract class BaseRepository
    {
        public int Limit = 1000;
        public int DisconnectTimeout { get; set; } = 35;

        internal protected RegistryConnection r;

        public BaseRepository(RegistryDatabaseFactory databaseFactory)
        {
            r = databaseFactory.Create();

            databaseFactory.ApplySchema();
        }

        internal bool IsDisconnect(DateTime lastPing)
        {
            var offset = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout);
            return offset > lastPing;
        }
    }
}
