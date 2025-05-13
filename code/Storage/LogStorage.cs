using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Storage
{
    public class LogStorage
    {
        private const string LATEST_PATH = "logs/latest.log";
        private const string HISTORICAL_LOG_ROOT_PATH = "logs/historical/";
        private const string TEMP_PATH = "logs/temp/";

        private readonly IFileSystem _fileSystem;

        #region Constructor
        private LogStorage(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public static async Task<LogStorage> CreateAsync(
            IFileSystem fileSystem,
            CancellationToken ct)
        {
            await fileSystem.RemoveFolderAsync(TEMP_PATH, ct);

            return new LogStorage(fileSystem);
        }
        #endregion

        public async Task<Stream?> OpenReadLatestViewAsync(CancellationToken ct)
        {
            return await _fileSystem.OpenReadAsync(LATEST_PATH, ct);
        }

        public async Task<TemporaryBlobOutput> OpenWriteTempLatestViewAsync(CancellationToken ct)
        {
            var tempPath = $"{TEMP_PATH}{Guid.NewGuid()}.log";
            var storage = await _fileSystem.OpenWriteAsync(tempPath, ct);

            return new TemporaryBlobOutput(
                storage,
                async ct2 => await _fileSystem.MoveAsync(tempPath, LATEST_PATH, ct2));
        }

        public async Task<Stream?> OpenReadLogShardAsync(long index, CancellationToken ct)
        {
            return await _fileSystem.OpenReadAsync(GetLogShardPath(index), ct);
        }

        public async Task<IAppendStorage> OpenWriteLogShardAsync(long index, CancellationToken ct)
        {
            return await _fileSystem.OpenWriteAsync(GetLogShardPath(index), ct);
        }

        private static string GetLogShardPath(long index)
        {
            return $"{HISTORICAL_LOG_ROOT_PATH}{index:18}.log";
        }
    }
}