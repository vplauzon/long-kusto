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
            var runtimeGateway = await CreateRuntimeGatewayAsync();
            var text = GetResource("Query.SimpleQuery.kql");
            var ct = new CancellationToken();
            var procOutput = await runtimeGateway.RunProcedureAsync(text, GetKustoDbUri(), ct);
            var storedQueryResult = await procOutput.CompletionTask;
        }
    }
}