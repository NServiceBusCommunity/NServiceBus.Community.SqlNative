public class DelayedReaderTests :
    TestBase
{
    string table = "DelayedReaderTests";

    [Test]
    public async Task Single()
    {
        await DelayedTestDataBuilder.SendData(table);
        var reader = new DelayedQueueManager(table, SqlConnection);
        await using var result = await reader.Read(1);
        await Verify(result!.ToVerifyTarget());
    }

    [Test]
    public async Task Single_nulls()
    {
        await DelayedTestDataBuilder.SendNullData(table);
        var reader = new DelayedQueueManager(table, SqlConnection);
        await using var result = await reader.Read(1);
        await Verify(result!.ToVerifyTarget());
    }

    [Test]
    public async Task Batch()
    {
        await DelayedTestDataBuilder.SendMultipleData(table);

        var reader = new DelayedQueueManager(table, SqlConnection);
        var messages = new ConcurrentBag<IncomingDelayedVerifyTarget>();
        var result = await reader.Read(
            size: 3,
            startRowVersion: 2,
            func: (message, _) =>
            {
                messages.Add(message.ToVerifyTarget());
                return Task.CompletedTask;
            });
        await Assert.That(result.LastRowVersion).IsEqualTo(4);
        await Assert.That(result.Count).IsEqualTo(3);
        await Verify(messages.OrderBy(_ => _.Due));
    }

    public DelayedReaderTests()
    {
        var manager = new DelayedQueueManager(table, SqlConnection);
        manager.Drop().Await();
        manager.Create().Await();
    }
}
