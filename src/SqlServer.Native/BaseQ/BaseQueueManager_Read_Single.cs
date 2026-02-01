namespace NServiceBus.Transport.SqlServerNative;

public abstract partial class BaseQueueManager<TIncoming, TOutgoing>
    where TIncoming : class, IIncomingMessage
{
    protected abstract SqlCommand BuildReadCommand(int batchSize, long startRowVersion);

    public virtual async Task<TIncoming?> Read(long rowVersion, Cancel cancel = default)
    {
        Guard.AgainstNegativeAndZero(rowVersion);
        var shouldCleanup = false;
        SqlDataReader? reader = null;
        try
        {
            using var command = BuildReadCommand(1, rowVersion);
            reader = await command.RunSingleRowReader(cancel);
            if (!await reader.ReadAsync(cancel))
            {
                shouldCleanup = true;
                return default;
            }

            return await ReadMessage(reader, reader.DisposeAsync);
        }
        catch
        {
            shouldCleanup = true;
            throw;
        }
        finally
        {
            if (shouldCleanup && reader != null)
            {
#if NET48
                reader.Dispose();
#else
                await reader.DisposeAsync();
#endif
            }
        }
    }
}