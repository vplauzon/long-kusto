using Kusto.Data.Common;

namespace Kusto
{
    public class DbExportClient
    {
        private readonly ICslAdminProvider _provider;
        private readonly ExecutionQueue _exportQueue;

        public DbExportClient(
            ICslAdminProvider provider,
            ExecutionQueue exportQueue,
            string databaseName)
        {
            _provider = provider;
            _exportQueue = exportQueue;
            DatabaseName = databaseName;
        }

        public string DatabaseName { get; }
    }
}