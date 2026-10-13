using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Agentic2D.Tools;

public sealed record ReviewDecisionOperation(string Id, string Decision);
public sealed record ReviewDecisionOutcome(string Id, string Decision, bool Success, string Message, bool IsRetry);
public sealed record ReviewDecisionOperationResult(bool Success, string Message);

/// <summary>Bounded single-consumer FIFO for canonical review decision operations.</summary>
public sealed class ReviewDecisionQueue : IAsyncDisposable
{
    private readonly Channel<QueuedOperation> jobs;
    private readonly ConcurrentQueue<ReviewDecisionOutcome> completions = new();
    private readonly Dictionary<string, ReviewDecisionOperation> failed = new(StringComparer.Ordinal);
    private readonly HashSet<string> active = new(StringComparer.Ordinal);
    private readonly HashSet<string> completed = new(StringComparer.Ordinal);
    private readonly Func<ReviewDecisionOperation, Task<ReviewDecisionOperationResult>> execute;
    private readonly object gate = new();
    private readonly Task worker;
    private TaskCompletionSource idle = CompletedSource();
    private int pending;
    private bool completing;

    public ReviewDecisionQueue(int capacity, Func<ReviewDecisionOperation, Task<ReviewDecisionOperationResult>> execute)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        jobs = Channel.CreateBounded<QueuedOperation>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        worker = ConsumeAsync();
    }

    public int Capacity { get; }
    public int PendingCount => Volatile.Read(ref pending);
    public IReadOnlyList<ReviewDecisionOperation> FailedOperations { get { lock (gate) return failed.Values.ToArray(); } }

    public bool TryEnqueue(ReviewDecisionOperation operation) => Enqueue(operation, isRetry: false);

    public int RetryFailed()
    {
        lock (gate)
        {
            if (completing) return 0;
            var retries = failed.Values.ToArray();
            var accepted = 0;
            foreach (var operation in retries)
            {
                if (EnqueueLocked(operation, isRetry: true))
                {
                    failed.Remove(operation.Id);
                    accepted++;
                }
            }
            return accepted;
        }
    }

    public IEnumerable<ReviewDecisionOutcome> TakeCompletions()
    {
        while (completions.TryDequeue(out var completion)) yield return completion;
    }

    public Task WaitForIdleAsync() { lock (gate) return idle.Task; }

    public async Task CompleteAsync()
    {
        lock (gate)
        {
            if (!completing)
            {
                completing = true;
                jobs.Writer.TryComplete();
            }
        }
        await worker.ConfigureAwait(false);
        await WaitForIdleAsync().ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => new(CompleteAsync());

    private bool Enqueue(ReviewDecisionOperation operation, bool isRetry)
    {
        ArgumentNullException.ThrowIfNull(operation);
        lock (gate) return EnqueueLocked(operation, isRetry);
    }

    private bool EnqueueLocked(ReviewDecisionOperation operation, bool isRetry)
    {
        if (completing || string.IsNullOrWhiteSpace(operation.Id) || string.IsNullOrWhiteSpace(operation.Decision) || active.Contains(operation.Id) || completed.Contains(operation.Id)) return false;
        if (pending >= Capacity) return false;
        if (pending == 0) idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        active.Add(operation.Id);
        Interlocked.Increment(ref pending);
        if (jobs.Writer.TryWrite(new QueuedOperation(operation, isRetry))) return true;
        active.Remove(operation.Id);
        Interlocked.Decrement(ref pending);
        if (pending == 0) idle.TrySetResult();
        return false;
    }

    private async Task ConsumeAsync()
    {
        await foreach (var queued in jobs.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            var operation = queued.Operation;
            ReviewDecisionOperationResult result;
            try { result = await execute(operation).ConfigureAwait(false); }
            catch (Exception exception) { result = new(false, exception.Message); }

            lock (gate)
            {
                active.Remove(operation.Id);
                if (result.Success) { failed.Remove(operation.Id); completed.Add(operation.Id); }
                else failed[operation.Id] = operation;
                completions.Enqueue(new ReviewDecisionOutcome(operation.Id, operation.Decision, result.Success, result.Message, queued.IsRetry));
                if (Interlocked.Decrement(ref pending) == 0) idle.TrySetResult();
            }
        }
    }

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }

    private sealed record QueuedOperation(ReviewDecisionOperation Operation, bool IsRetry);
}
