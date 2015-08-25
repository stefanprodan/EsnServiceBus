using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public abstract class BaseRepository: IDisposable
    {
        public int Limit = 1000;
        public int DisconnectTimeout { get; set; } = 35;

        internal protected RegistryConnection r;

        public BaseRepository(RegistryDatabaseFactory databaseFactory)
        {
            r = databaseFactory.Create();
        }

        internal bool IsDisconnect(DateTime lastPing)
        {
            var offset = DateTime.UtcNow.AddSeconds((-1) * DisconnectTimeout);
            return offset > lastPing;
        }

        public void Dispose()
        {
            if(r != null && r.Connection != null)
            {
                r.Connection.Dispose();
            }
        }
    }
}
