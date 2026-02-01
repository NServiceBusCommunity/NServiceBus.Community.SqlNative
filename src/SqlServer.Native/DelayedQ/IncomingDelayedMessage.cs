namespace NServiceBus.Transport.SqlServerNative;

/// <summary>
/// Represents a message.
/// </summary>
[DebuggerDisplay("RowVersion = {RowVersion}, Due = {Due}")]
public class IncomingDelayedMessage :
    IIncomingMessage
{
    Func<ValueTask>[] cleanups;
    bool disposed;
    volatile int disposeSignaled;

    public IncomingDelayedMessage(long rowVersion, DateTime? due, string headers, Stream? body, Func<ValueTask>[] cleanups)
    {
        Guard.AgainstNegativeAndZero(rowVersion);
        this.cleanups = cleanups;
        RowVersion = rowVersion;
        Due = due;
        Headers = headers;
        Body = body;
    }

    public long RowVersion
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    public DateTime? Due
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    public string Headers
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    public Stream? Body
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(IncomingMessage));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }

#if NET48
        Body?.Dispose();
#else
        if (Body != null)
        {
            await Body.DisposeAsync();
        }
#endif

        disposed = true;
        foreach (var cleanup in cleanups)
        {
            await cleanup();
        }
    }
}