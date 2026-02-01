namespace NServiceBus.Transport.SqlServerNative;

/// <summary>
/// Represents a message.
/// </summary>
[DebuggerDisplay("Id = {Id}, RowVersion = {RowVersion}, Expires = {Expires}")]
public class IncomingMessage :
    IIncomingMessage
{
    Func<ValueTask>[] cleanups;
    bool disposed;
    volatile int disposeSignaled;

    public IncomingMessage(Guid id, long rowVersion, DateTime? expires, string headers, Stream? body, Func<ValueTask>[] cleanups)
    {
        Guard.AgainstNegativeAndZero(rowVersion);
        this.cleanups = cleanups;
        Id = id;
        RowVersion = rowVersion;
        Expires = expires;
        Headers = headers;
        Body = body;
    }

    public Guid Id
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    public long RowVersion
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    public DateTime? Expires
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