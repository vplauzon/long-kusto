using Azure.Identity;
using FlowPlanning;
using Kusto;
using Runtime;
using Storage;

namespace IntegratedTest.Query
{
    public class QueryTest : IntegratedTestBase
    {
        [Fact]
        public async Task Test1()
        {
            var ct = new CancellationToken();
            var kustoDbUriText = Environment.GetEnvironmentVariable("kustoDbUri");
            var kustoDbUri = new Uri(kustoDbUriText!);
            var kustoDbUriBuilder = new UriBuilder(kustoDbUri);
            var kustoDb = kustoDbUriBuilder.Path.Trim('/');

            kustoDbUriBuilder.Path = string.Empty;

            var kustoClusterUri = kustoDbUriBuilder.Uri;
            var dataLakeRootUrl = Environment.GetEnvironmentVariable("dataLakeRootUrl");
            var dataLakeRootUrlInstance = $"{dataLakeRootUrl}/{Guid.NewGuid()}";
            var credentials = new AzureCliCredential();
            var fileSystem = new AzureBlobFileSystem(dataLakeRootUrlInstance, credentials);
            var logStorage = await LogStorage.CreateAsync(fileSystem, ct);
            var dbClientCache = new DbClientCache(credentials, "LK-TEST");
            var text = GetResource("Query.SimpleQuery.kql");
            var flowPlan = FlowPlan.CreatePlan(text);
            var procedureRuntime = new ProcedureRuntime(
                kustoClusterUri,
                kustoDb,
                dbClientCache,
                flowPlan);
        }
    }
}