using System.Threading.Tasks;

public class MessageConsumingLoopTests :
    TestBase
{
    static DateTime dateTime = new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

    string table = "MessageConsumingLoopTests";

    //TODO
    // [Fact]
    // public async Task Should_not_throw_when_run_over_end()
    // {
    //     var manager = new QueueManager(table, SqlConnection);
    //     await manager.Drop();
    //     await manager.Create();
    //     await SendMessages();
    //
    //     Exception? exception = null;
    //     await using var loop = new MessageConsumingLoop(
    //         table: table,
    //         connectionBuilder: Connection.OpenAsyncConnection,
    //         callback: (_, _, _) => Task.CompletedTask,
    //         errorCallback: innerException => { exception = innerException; }
    //     );
    //     loop.Start();
    //     Thread.Sleep(1000);
    //     Assert.Null(exception!);
    // }

    [Test]
    public async Task Should_get_correct_count()
    {
        var resetEvent = new ManualResetEvent(false);
        await SqlConnection.DropTable(null, table);
        var manager = new QueueManager(table, SqlConnection);
        await manager.Create();
        await SendMessages();

        var count = 0;

        Task Callback(SqlConnection connection, IncomingMessage message, Cancel cancel)
        {
            count++;
            if (count == 5)
            {
                resetEvent.Set();
            }

            return Task.CompletedTask;
        }

        await using var loop = new MessageConsumingLoop(
            table: table,
            connectionBuilder: Connection.OpenAsyncConnection,
            callback: Callback,
            errorCallback: _ =>
            {
            });
        loop.Start();
        resetEvent.WaitOne(TimeSpan.FromSeconds(30));
        await Assert.That(count).IsEqualTo(5);
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