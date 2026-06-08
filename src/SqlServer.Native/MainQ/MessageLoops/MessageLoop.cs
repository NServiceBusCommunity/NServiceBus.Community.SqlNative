namespace NServiceBus.Transport.SqlServerNative;

public abstract class MessageLoop :
    IAsyncDisposable
{
    Action<Exception> errorCallback;
    Task? task;
    CancelSource? tokenSource;
    TimeSpan delay;

    public MessageLoop(
        Action<Exception> errorCallback,
        TimeSpan? delay = null)
    {
        Guard.AgainstNegativeAndZero(delay);
        this.errorCallback = errorCallback.WrapFunc(nameof(errorCallback));
        this.delay = delay.GetValueOrDefault(TimeSpan.FromMinutes(1));
    }

    public void Start()
    {
        tokenSource = new();
        var cancel = tokenSource.Token;

        // Do not pass cancel to Task.Run: if it is signaled before the thread pool starts the
        // delegate (e.g. disposed straight after Start) the returned task would be canceled,
        // surfacing as a TaskCanceledException from Stop. The loop observes cancel internally.
        task = Task.Run(async () =>
            {
                while (!cancel.IsCancellationRequested)
                {
                    try
                    {
                        await RunBatch(cancel);

                        await Task.Delay(delay, cancel);
                    }
                    catch (OperationCanceledException)
                    {
                        // noop
                    }
                    catch (Exception ex)
                    {
                        errorCallback(ex);
                    }
                }
            });
    }

    protected abstract Task RunBatch(Cancel cancel);

    public Task Stop()
    {
        tokenSource?.Cancel();
        tokenSource?.Dispose();
        if (task == null)
        {
            return Task.CompletedTask;
        }

        return task;
    }

    public ValueTask DisposeAsync() => new(Stop());
}