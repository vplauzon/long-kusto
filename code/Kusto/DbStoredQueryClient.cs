using Kusto.Data.Common;

namespace Kusto
{
    public class DbStoredQueryClient
    {
        private readonly ICslAdminProvider _provider;
        private readonly ExecutionQueue _storedQueryQueue;

        public DbStoredQueryClient(
            ICslAdminProvider provider,
            ExecutionQueue storedQueryQueue,
            string databaseName)
        {
            _provider = provider;
            _storedQueryQueue = storedQueryQueue;
            DatabaseName = databaseName;
        }

        public string DatabaseName { get; }
    }
}