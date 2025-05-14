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
            var text = GetResource("Query.SimpleQuery.kql");
            var procOutput = RuntimeGateway.RunProcedure(text, KustoDbUri);
            var storedQueryResult = await procOutput.CompletionTask;
        }
    }
}