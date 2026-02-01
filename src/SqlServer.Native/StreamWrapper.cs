class StreamWrapper(long length, Stream inner) :
    Stream
{
    long position;

    public override void EndWrite(IAsyncResult asyncResult) =>
        throw new NotImplementedException();

    public override void Flush() =>
        inner.Flush();

    public override Task FlushAsync(Cancel cancel) =>
        inner.FlushAsync(cancel);

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, Cancel cancel)
    {
        var bytesRead = await inner.ReadAsync(buffer, offset, count, cancel);
        position += bytesRead;
        return bytesRead;
    }

    public override int ReadByte()
    {
        var readByte = inner.ReadByte();
        if (readByte != -1)
        {
            position++;
        }
        return readByte;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (offset == 0 && origin == SeekOrigin.Current)
        {
            return position;
        }

        var seek = inner.Seek(offset, origin);
        position = seek;
        return seek;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = inner.Read(buffer, offset, count);
        position += bytesRead;
        return bytesRead;
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanTimeout => inner.CanTimeout;
    public override bool CanWrite => false;

    public override long Length { get; } = length;
    public override int ReadTimeout => inner.ReadTimeout;

    public override long Position
    {
        get => position;
        set
        {
            if (position == value)
            {
                return;
            }

            throw new NotImplementedException();
        }
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        inner.BeginRead(buffer, offset, count, callback, state);

    public override int EndRead(IAsyncResult asyncResult)
    {
        var readBytes = inner.EndRead(asyncResult);
        position += readBytes;
        return readBytes;
    }

#if !NET48
    public override int Read(Span<byte> buffer)
    {
        var bytesRead = inner.Read(buffer);
        position += bytesRead;
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, Cancel cancel = default)
    {
        var bytesRead = await inner.ReadAsync(buffer,  cancel);
        position += bytesRead;
        return bytesRead;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        inner.CopyTo(destination, bufferSize);
        position = Length;
    }

    public override void Write(ReadOnlySpan<byte> buffer) =>
        throw new NotImplementedException();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, Cancel cancel = default) =>
        throw new NotImplementedException();
#endif

    public override void Close()
    {
        inner.Close();
        base.Close();
    }

    protected override void Dispose(bool disposing)
    {
        inner.Dispose();
        base.Dispose(disposing);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, Cancel cancel)
    {
        position = Length;
        return inner.CopyToAsync(destination, bufferSize, cancel);
    }

#if !NET48
    [Obsolete("This Remoting API is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0010", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
    // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8764
    public override object? InitializeLifetimeService() =>
#pragma warning restore CS8764
        inner.InitializeLifetimeService();

    public override bool Equals(object? obj) =>
        inner.Equals(obj);

    public override int GetHashCode() =>
        inner.GetHashCode();

    // ReSharper disable once ReturnTypeCanBeNotNullable
    public override string? ToString() =>
        inner.ToString();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();

    public override Task WriteAsync(byte[] buffer, int offset, int count, Cancel cancel) =>
        throw new NotImplementedException();

    public override void WriteByte(byte value) =>
        throw new NotImplementedException();

    public override int WriteTimeout => throw new NotImplementedException();

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        throw new NotImplementedException();
}