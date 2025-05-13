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
            var dataLakeRootUrl = Environment.GetEnvironmentVariable("dataLakeRootUrl");
            var credentials = new AzureCliCredential();
            var text = GetResource("Query.SimpleQuery.kql");
            var runtimeGateway = await RuntimeGateway.CreateAsync(
                credentials,
                new Version(),
                "LK-TEST",
                dataLakeRootUrl!,
                ct);
            var procOutput = await runtimeGateway.RunProcedureAsync(text, kustoDbUri, ct);
            var storedQueryResult = await procOutput.CompletionTask;

        }
    }
}