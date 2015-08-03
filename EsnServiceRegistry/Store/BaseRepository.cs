using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public abstract class BaseRepository
    {
        public int DisconnectTimeout { get; set; } = 2;

        internal protected RegistryConnection r;

        public BaseRepository(RegistryDatabaseFactory databaseFactory)
        {
            r = databaseFactory.Create();

            databaseFactory.ApplySchema();
        }

        internal bool IsDisconnect(DateTime lastPing)
        {
            var offset = DateTime.UtcNow.AddMinutes((-1) * DisconnectTimeout);
            return offset > lastPing;
        }
    }
}
