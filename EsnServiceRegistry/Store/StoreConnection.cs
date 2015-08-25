using RethinkDb;
using RethinkDb.QueryTerm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Store
{
    public class StoreConnection
    {
        public readonly IConnection Connection;
        private readonly string _database;

        private readonly List<Type> tables = new List<Type>();

        public StoreConnection(IConnection connection, string database)
        {
            Connection = connection;
            _database = database;
        }

        public void AddTable<T>()
        {
            tables.Add(typeof(T));
        }

        public DbQuery Database => Query.Db(_database);

        public T Run<T>(IScalarQuery<T> queryObject,
            IQueryConverter queryConverter = null, CancellationToken? cancellationToken = null)
        {
            return Connection.Run(queryObject, queryConverter, cancellationToken);
        }

        public IEnumerable<T> Run<T>(ISequenceQuery<T> queryObject,
            IQueryConverter queryConverter = null, CancellationToken? cancellationToken = null)
        {
            return Connection.Run(queryObject, queryConverter, cancellationToken);
        }

        Task<T> RunAsync<T>(IQueryConverter queryConverter, IScalarQuery<T> queryObject, 
            CancellationToken cancellationToken)
        {
            return Connection.RunAsync(queryConverter, queryObject, cancellationToken);
        }

        IAsyncEnumerator<T> RunAsync<T>(IQueryConverter queryConverter, ISequenceQuery<T> queryObject)
        {
            return Connection.RunAsync(queryConverter, queryObject);
        }

        public Task<T> RunAsync<T>(IScalarQuery<T> queryObject, IQueryConverter queryConverter = null, 
            CancellationToken? cancellationToken = null)
        {
            return Connection.RunAsync(queryObject, queryConverter, cancellationToken);
        }

        public IAsyncEnumerator<T> RunAsync<T>(ISequenceQuery<T> queryObject,
            IQueryConverter queryConverter = null)
        {
            return Connection.RunAsync(queryObject, queryConverter);
        }
    }
}
