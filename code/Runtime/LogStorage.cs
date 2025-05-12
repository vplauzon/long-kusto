using Azure.Storage.Blobs.Models;
using Runtime.Entity.RowItem;
using Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Runtime
{
    public class LogStorage
    {
        private const string LATEST_PATH = "logs/latest.log";
        private const string HISTORICAL_LOG_ROOT_PATH = "logs/historical/";
        private const string TEMP_PATH = "logs/temp/";

        private readonly IFileSystem _fileSystem;
        private long _lastShardIndex;

        #region Constructor
        private LogStorage(IFileSystem fileSystem, long lastShardIndex)
        {
            _fileSystem = fileSystem;
            _lastShardIndex = lastShardIndex;
        }

        public static async Task<LogStorage> CreateAsync(
            IFileSystem fileSystem,
            CancellationToken ct)
        {
            using (var latestStream = await fileSystem.OpenReadAsync(LATEST_PATH, ct))
            {
                if (latestStream != null)
                {
                    var versionHeaderText = await ReadLineGreedilyAsync(latestStream, ct);
                    var viewHeaderText = await ReadLineGreedilyAsync(latestStream, ct);
                    var versionHeader = versionHeaderText != null
                        ? JsonSerializer.Deserialize(
                            versionHeaderText,
                            RowJsonContext.Default.FileVersionHeader)
                        : null;

                    if (versionHeader == null)
                    {
                        throw new InvalidDataException(
                            "Latest view blob doesn't contain file version header");
                    }

                    var viewHeader = viewHeaderText != null
                        ? JsonSerializer.Deserialize(
                            viewHeaderText,
                            RowJsonContext.Default.ViewHeader)
                        : null;

                    if (viewHeader == null)
                    {
                        throw new InvalidDataException(
                            "Latest view blob doesn't contain view header");
                    }

                    throw new NotImplementedException();
                }
                else
                {
                    return new LogStorage(fileSystem, 0);
                }
            }
        }
        #endregion

        private static async Task<string> ReadLineGreedilyAsync(
            Stream stream,
            CancellationToken ct)
        {
            var buffer = new byte[1];
            var accumulatedBytes = new List<byte>();

            while (await stream.ReadAsync(buffer, 0, 1, ct) == 1
                && buffer[0] != (byte)'\n')
            {
                accumulatedBytes.Add(buffer[0]);
            }

            var text = ASCIIEncoding.UTF8.GetString(accumulatedBytes.ToArray());

            return text;
        }
    }
}