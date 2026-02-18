public class DedupeCleanerJobTests :
    TestBase
{
    static DateTime dateTime = new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

    string table = "DedupeCleanerJobTests";

    [Test]
    public async Task Should_only_clean_up_old_item()
    {
        var message1 = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
        await Send(message1);
        Thread.Sleep(1000);
        var now = DateTime.UtcNow;
        Thread.Sleep(1000);

        var message2 = BuildBytesMessage("00000000-0000-0000-0000-000000000002");
        await Send(message2);
        var expireWindow = DateTime.UtcNow - now;
        var cleaner = new DedupeCleanerJob(
            "Deduplication",
            Connection.OpenAsyncConnection,
            _ => { },
            expireWindow,
            frequencyToRunCleanup: TimeSpan.FromMilliseconds(10));
        cleaner.Start();
        Thread.Sleep(100);
        await cleaner.Stop();
        await Verify(SqlHelper.ReadDuplicateData("Deduplication", SqlConnection))
            .DontScrubGuids();
    }

    Task<long> Send(OutgoingMessage message)
    {
        var sender = new QueueManager(table, SqlConnection, "Deduplication");
       return sender.Send(message);
    }

    static OutgoingMessage BuildBytesMessage(string guid) =>
        new(new(guid), dateTime, "headers", "{}"u8.ToArray());

    public DedupeCleanerJobTests()
    {
        var queueManager = new QueueManager(table, SqlConnection, "Deduplication");
        queueManager.Drop().Await();
        queueManager.Create().Await();
        var dedupeManager = new DedupeManager(SqlConnection, "Deduplication");
        dedupeManager.Drop().Await();
        dedupeManager.Create().Await();
    }
}
