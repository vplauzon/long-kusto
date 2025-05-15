using Kusto.Data.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kusto
{
    internal class OperationAwaiter : IAsyncDisposable
    {
        #region Inner Types
        private record Operation(TaskCompletionSource CompletionSource);

        private record ExportOperationStatus(
            string OperationId,
            TimeSpan Duration,
            string State,
            string Status,
            bool ShouldRetry);
        #endregion

        private static readonly ClientRequestProperties EMPTY_PROPERTIES = new();
        private static TimeSpan REFRESH_PERIOD = TimeSpan.FromSeconds(5);
        private static readonly IImmutableSet<string> FAILED_STATUS =
            ImmutableHashSet.Create(
                [
                "Throttled",
                "Failed",
                "PartiallySucceeded",
                "Abandoned",
                "BadInput",
                "Canceled",
                "Skipped"
                ]);

        private readonly ICslAdminProvider _commandProvider;
        private readonly ExecutionQueue _commandQueue;
        private readonly IDictionary<string, Operation> _operationMap
            = new Dictionary<string, Operation>();
        private readonly Task _refreshLoopTask;
        private readonly TaskCompletionSource _stopLoop = new();

        public OperationAwaiter(
            ICslAdminProvider commandProvider,
            ExecutionQueue commandQueue)
        {
            _commandProvider = commandProvider;
            _commandQueue = commandQueue;
            _refreshLoopTask = RefreshLoopAsync();
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            _stopLoop.TrySetResult();
            await _refreshLoopTask;
        }

        public async Task AwaitOperationCompletionAsync(string operationId)
        {
            TaskCompletionSource? CompletionSource;

            lock (_operationMap)
            {
                CompletionSource = new();
                _operationMap[operationId] = new Operation(CompletionSource);
            }

            await CompletionSource.Task;
        }

        #region Refresh Loop
        private async Task RefreshLoopAsync()
        {
            while (!_stopLoop.Task.IsCompleted)
            {
                await Task.WhenAny(Task.Delay(REFRESH_PERIOD), _stopLoop.Task);

                var results = await _commandQueue.RequestRunAsync(
                    async () =>
                    {
                        var operationIdsText = string.Join(", ", GetOperationIds());
                        var commandText = @$".show operations({operationIdsText})
| project OperationId, Duration, State, Status, ShouldRetry";
                        var reader = await _commandProvider.ExecuteControlCommandAsync(
                            string.Empty,
                            commandText);
                        var results = reader
                            .ToEnumerable(r => new ExportOperationStatus(
                                ((Guid)r["OperationId"]).ToString(),
                                (TimeSpan)r["Duration"],
                                (string)r["State"],
                                (string)r["Status"],
                                Convert.ToBoolean((SByte)r["ShouldRetry"])
                            ))
                            .ToImmutableArray();

                        return results;
                    });

                DetectLostOperationIds(results);
                DetectFailures(results);
                CompleteOperations(results);
            }
        }

        private IImmutableList<string> GetOperationIds()
        {
            lock (_operationMap)
            {
                return _operationMap.Keys
                    .ToImmutableArray();
            }
        }

        #region Handle Operations
        private void DetectLostOperationIds(IImmutableList<ExportOperationStatus> status)
        {
            var statusOperationIdBag = status.Select(s => s.OperationId).ToHashSet();

            foreach (var id in _operationMap.Keys)
            {
                if (!statusOperationIdBag.Contains(id))
                {
                    var operation = _operationMap[id];

                    operation.CompletionSource.TrySetException(
                        new InvalidDataException($"Operation ID '{id}' lost"));
                }
            }
        }

        private void DetectFailures(IImmutableList<ExportOperationStatus> statuses)
        {
            var failedStatuses = statuses
                .Where(s => FAILED_STATUS.Contains(s.State));

            foreach (var status in failedStatuses)
            {
                var operation = _operationMap[status.OperationId];

                operation.CompletionSource.TrySetException(new InvalidDataException(
                    $"Operation ID '{status.OperationId}' failed in state '{status.State}' " +
                    $"with status '{status.Status}'"));
            }
        }

        private void CompleteOperations(IImmutableList<ExportOperationStatus> statuses)
        {
            var completedOperationIds = statuses
                .Where(s => s.State == "Completed")
                .Select(s => s.OperationId);

            foreach (var id in completedOperationIds)
            {
                var operation = _operationMap[id];

                operation.CompletionSource.TrySetResult();
            }
        }
        #endregion
        #endregion
    }
}