using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public abstract class BaseRepository
    {
        internal protected RegistryConnection r;

        public BaseRepository(RegistryDatabaseFactory databaseFactory)
        {
            r = databaseFactory.Create();

            databaseFactory.ApplySchema();
        }
    }
}
