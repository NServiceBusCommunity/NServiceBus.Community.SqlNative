class AsyncTimer
{
    public virtual void Start(Func<DateTime, Cancel, Task> callback, TimeSpan interval, Action<Exception> errorCallback, Func<TimeSpan, Cancel, Task> delayStrategy)
    {
        tokenSource = new();
        var cancel = tokenSource.Token;

        task = Task.Run(async () =>
            {
                while (!cancel.IsCancellationRequested)
                {
                    try
                    {
                        var utcNow = DateTime.UtcNow;
                        await delayStrategy(interval, cancel);
                        await callback(utcNow, cancel);
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
            },
            cancel);
    }

    public virtual async Task Stop()
    {
        if (tokenSource == null)
        {
            return;
        }

        tokenSource.Cancel();
        try
        {
            if (task != null)
            {
                await task;
            }
        }
        catch (OperationCanceledException)
        {
            // expected during stop
        }
        finally
        {
            tokenSource.Dispose();
            tokenSource = null;
            task = null;
        }
    }

    Task? task;
    CancelSource? tokenSource;
}