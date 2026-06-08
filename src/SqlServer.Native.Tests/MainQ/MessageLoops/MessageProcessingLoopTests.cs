public class MessageProcessingLoopTests :
    TestBase
{
    static DateTime dateTime = new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

    string table = "MessageProcessingLoopTests";

    [Test]
    public async Task TEMP_repro()
    {
        await SqlConnection.DropTable(null, table);
        var m = new QueueManager(table, SqlConnection);
        await m.Create();
        await SendMessages();

        // Vary the gap between Start and Dispose so disposal frequently lands while the
        // loop is mid-SQL-op (open/read), probing whether cancellation leaks to errorCallback.
        int[] sleeps = [0, 1, 2, 5, 10, 20, 40, 80, 150];
        for (var i = 0; i < 400; i++)
        {
            Exception? captured = null;
            var loop = new MessageProcessingLoop(
                table: table,
                startingRow: 1,
                connectionBuilder: Connection.OpenAsyncConnection,
                callback: (_, _, _) => Task.CompletedTask,
                errorCallback: e => { captured = e; },
                persistRowVersion: (_, _, _) => Task.CompletedTask
            );
            loop.Start();
            Thread.Sleep(sleeps[i % sleeps.Length]);
            await loop.DisposeAsync();
            // give any post-dispose error a chance to surface
            Thread.Sleep(5);
            if (captured != null)
            {
                throw new($"Iteration {i} (sleep {sleeps[i % sleeps.Length]}ms) captured:\n---INNER CHAIN---\n{Flatten(captured)}");
            }
        }
    }

    static string Flatten(Exception e)
    {
        var sb = new System.Text.StringBuilder();
        var cur = (Exception?)e;
        while (cur != null)
        {
            sb.AppendLine($"[{cur.GetType().FullName}] {cur.Message}");
            sb.AppendLine(cur.StackTrace);
            cur = cur.InnerException;
        }

        return sb.ToString();
    }

    [Test]
    public async Task Should_not_throw_when_run_over_end()
    {
        await SqlConnection.DropTable(null, table);
        var manager = new QueueManager(table, SqlConnection);
        await manager.Create();
        await SendMessages();

        Exception? exception = null;
        await using var loop = new MessageProcessingLoop(
            table: table,
            startingRow: 1,
            connectionBuilder: Connection.OpenAsyncConnection,
            callback: (_, _, _) => Task.CompletedTask,
            errorCallback: innerException =>
            {
                exception = innerException;
            },
            persistRowVersion: (_, _, _) => Task.CompletedTask
        );
        loop.Start();
        Thread.Sleep(1000);
        await Assert.That(exception!).IsNull();
    }

    [Test]
    public async Task Should_get_correct_count()
    {
        var resetEvent = new ManualResetEvent(false);
        await SqlConnection.DropTable(null, table);
        var manager = new QueueManager(table, SqlConnection);
        await manager.Create();
        await SendMessages();

        var count = 0;

        Task Callback(SqlConnection connection, IncomingMessage incomingBytesMessage, Cancel arg3)
        {
            count++;
            if (count == 5)
            {
                resetEvent.Set();
            }

            return Task.CompletedTask;
        }

        await using var loop = new MessageProcessingLoop(
            table: table,
            startingRow: 1,
            connectionBuilder: Connection.OpenAsyncConnection,
            callback: Callback,
            errorCallback: _ =>
            {
            },
            persistRowVersion: (_, _, _) => Task.CompletedTask);
        loop.Start();
        resetEvent.WaitOne(TimeSpan.FromSeconds(30));
        await Assert.That(count).IsEqualTo(5);
    }

    [Test]
    public async Task Should_get_correct_next_row_version()
    {
        var resetEvent = new ManualResetEvent(false);
        await SqlConnection.DropTable(null, table);
        var manager = new QueueManager(table, SqlConnection);
        await manager.Create();
        await SendMessages();

        long rowVersion = 0;

        Task PersistRowVersion(SqlConnection sqlConnection, long currentRowVersion, Cancel arg3)
        {
            rowVersion = currentRowVersion;
            if (rowVersion == 6)
            {
                resetEvent.Set();
            }

            return Task.CompletedTask;
        }

        await using var loop = new MessageProcessingLoop(
            table: table,
            startingRow: 1,
            connectionBuilder: Connection.OpenAsyncConnection,
            callback: (_, _, _) => Task.CompletedTask,
            errorCallback: _ =>
            {
            },
            persistRowVersion: PersistRowVersion);
        loop.Start();
        resetEvent.WaitOne(TimeSpan.FromSeconds(30));
        await Assert.That(rowVersion).IsEqualTo(6);
    }

    Task SendMessages()
    {
        var sender = new QueueManager(table, SqlConnection);

        return sender.Send(
        [
            BuildMessage("00000000-0000-0000-0000-000000000001"),
            BuildMessage("00000000-0000-0000-0000-000000000002"),
            BuildMessage("00000000-0000-0000-0000-000000000003"),
            BuildMessage("00000000-0000-0000-0000-000000000004"),
            BuildMessage("00000000-0000-0000-0000-000000000005")
        ]);
    }

    static OutgoingMessage BuildMessage(string guid) =>
        new(new(guid), dateTime, "headers", "{}"u8.ToArray());
}
