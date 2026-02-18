public class DelayedConsumerTests :
    TestBase
{
    string table = "DelayedConsumerTests";

    [Test]
    public async Task Single()
    {
        await DelayedTestDataBuilder.SendData(table);
        var consumer = new DelayedQueueManager(table, SqlConnection);
        await using var result = await consumer.Consume();
        await Verify(result!.ToVerifyTarget());
    }

    [Test]
    public async Task Single_nulls()
    {
        await DelayedTestDataBuilder.SendNullData(table);
        var consumer = new DelayedQueueManager(table, SqlConnection);
        await using var result = await consumer.Consume();
        await Verify(result!.ToVerifyTarget());
    }

    [Test]
    public async Task Batch()
    {
        await DelayedTestDataBuilder.SendMultipleData(table);

        var consumer = new DelayedQueueManager(table, SqlConnection);
        var messages = new ConcurrentBag<IncomingDelayedVerifyTarget>();
        var result = await consumer.Consume(
            size: 3,
            func: (message, _) =>
            {
                messages.Add(message.ToVerifyTarget());
                return Task.CompletedTask;
            });
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.LastRowVersion).IsEqualTo(3);
        await Verify(messages.OrderBy(_ => _.Due));
    }

    public DelayedConsumerTests()
    {
        var manager = new DelayedQueueManager(table, SqlConnection);
        manager.Drop().Await();
        manager.Create().Await();
    }
}
