using Azure.Identity;
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
            var dataLakeRootUrl = Environment.GetEnvironmentVariable("dataLakeRootUrl");
            var dataLakeRootUrlInstance = $"{dataLakeRootUrl}/{Guid.NewGuid()}";
            var fileSystem = new AzureBlobFileSystem(
                dataLakeRootUrlInstance,
                new AzureCliCredential());
            var logStorage = await LogStorage.CreateAsync(fileSystem, ct);
        }
    }
}