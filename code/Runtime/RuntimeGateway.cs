using Azure.Core;
using Kusto;
using Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public class RuntimeGateway
    {
        #region Constructors
        private RuntimeGateway(
            DbClientCache dbClientCache,
            RowGateway rowGateway)
        {
        }

        /// <summary>Factory method hidding all internal dependencies.</summary>>
        /// <param name="credentials"></param>
        /// <param name="appVersion"></param>
        /// <param name="traceApplicationName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<RuntimeGateway> CreateAsync(
            TokenCredential credentials,
            Version appVersion,
            string traceApplicationName,
            string dataLakeRootUrl,
            CancellationToken ct)
        {
            var fileSystem = new AzureBlobFileSystem(dataLakeRootUrl, credentials);
            var logStorage = await LogStorage.CreateAsync(fileSystem, ct);

            return new RuntimeGateway(
                new DbClientCache(credentials, traceApplicationName),
                await RowGateway.CreateAsync(appVersion, logStorage, ct));
        }
        #endregion

        public Task<ProcedureOutput<string?>> RunProcedureAsync(
            string text,
            Uri databaseUri,
            CancellationToken ct)
        {
            //var kustoDbUriBuilder = new UriBuilder(kustoDbUri);
            //var kustoDb = kustoDbUriBuilder.Path.Trim('/');

            //kustoDbUriBuilder.Path = string.Empty;

            //var kustoClusterUri = kustoDbUriBuilder.Uri;
            throw new NotImplementedException();
        }
    }
}