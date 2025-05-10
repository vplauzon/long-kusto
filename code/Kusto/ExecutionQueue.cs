using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kusto
{
    public class ExecutionQueue
    {
        #region Inner Types
        /// <summary>
        /// We use an abstract class to have a non-generic type to
        /// collection over.
        /// </summary>
        private abstract class Request
        {
            public abstract Task ExecuteAsync();
        }

        private class Request<T> : Request
        {
            private readonly Func<Task<T>> _asyncAction;

            public Request(Func<Task<T>> asyncAction)
            {
                _asyncAction = asyncAction;
            }

            public TaskCompletionSource<T> Source { get; } = new TaskCompletionSource<T>();

            public override async Task ExecuteAsync()
            {
                var value = await _asyncAction();

                Source.SetResult(value);
            }
        }
        #endregion

        private readonly ConcurrentQueue<Request> _requestQueue = new();
        private readonly ConcurrentQueue<Task> _runnerTasks = new();
        private volatile int _parallelRunCount = 0;
        private volatile int _maxParallelRunCount = 0;

        public ExecutionQueue(int maxParallelRunCount)
        {
            if (maxParallelRunCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxParallelRunCount));
            }
            _maxParallelRunCount = maxParallelRunCount;
        }

        public int MaxParallelRunCount
        {
            get => _maxParallelRunCount;
            set => _maxParallelRunCount = value;
        }

        public async Task<T> RequestRunAsync<T>(Func<Task<T>> actionAsync)
        {
            try
            {
                //  Optimistic path:  if there is capacity
                if (TryOptimistic())
                {   //  Optimistic try out succeeded!
                    var result = await actionAsync();

                    Interlocked.Decrement(ref _parallelRunCount);
                    TryDequeueRequest();

                    return result;
                }
                else
                {   //  Optimistic try out failed:  get in queue
                    var request = new Request<T>(actionAsync);

                    _requestQueue.Enqueue(request);
                    TryDequeueRequest();

                    //  Wait for our own turn
                    var result = await request.Source.Task;

                    return result;
                }
            }
            finally
            {
                await ObserveRunnerTasksAsync();
            }
        }

        public async Task RequestRunAsync(Func<Task> actionAsync)
        {
            await RequestRunAsync(async () =>
            {
                await actionAsync();

                return 0;
            });
        }

        private bool TryOptimistic()
        {
            var currentSnapshot = _parallelRunCount;

            if (currentSnapshot >= _maxParallelRunCount)
            {   //  We've reached capacity
                return false;
            }
            else
            {
                if (Interlocked.CompareExchange(
                    ref _parallelRunCount,
                    currentSnapshot + 1,
                    currentSnapshot) == currentSnapshot)
                {
                    return true;
                }
                else
                {   //  Somebody else modified in the meantime, we retry
                    return TryOptimistic();
                }
            }
        }

        private void TryDequeueRequest()
        {
            if (TryOptimistic())
            {
                if (_requestQueue.TryDequeue(out var request))
                {
                    var runningTask = Task.Run(async () =>
                    {
                        await request.ExecuteAsync();
                        Interlocked.Decrement(ref _parallelRunCount);
                        TryDequeueRequest();
                    });

                    _runnerTasks.Enqueue(runningTask);
                }
                else
                {   //  Revert increment since there won't be any run
                    Interlocked.Decrement(ref _parallelRunCount);
                }
            }
        }

        private async Task ObserveRunnerTasksAsync()
        {
            while (_runnerTasks.TryDequeue(out var task))
            {
                if (task.IsCompleted)
                {
                    await task;
                }
                else
                {
                    _runnerTasks.Enqueue(task);

                    return;
                }
            }
        }
    }
}