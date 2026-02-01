namespace NServiceBus.Transport.SqlServerNative;

/// <summary>
/// Base class for <see cref="DelayedQueueManager"/> and <see cref="QueueManager"/>.
/// Not to be used as an extension point.
/// </summary>
public abstract partial class BaseQueueManager<TIncoming, TOutgoing>
    where TIncoming : class, IIncomingMessage
{
    protected abstract SqlCommand BuildConsumeCommand(int batchSize);

    protected abstract Task<TIncoming> ReadMessage(SqlDataReader dataReader, params Func<ValueTask>[] cleanups);

    public virtual async Task<IncomingResult> Consume(int size, Func<TIncoming, Cancel, Task> func, Cancel cancel = default)
    {
        Guard.AgainstNegativeAndZero(size);
        using var command = BuildConsumeCommand(size);
        return await ReadMultiple(command, func, cancel);
    }
}