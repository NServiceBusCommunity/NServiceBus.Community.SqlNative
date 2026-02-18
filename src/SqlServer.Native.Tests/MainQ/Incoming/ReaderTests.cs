using System.Threading.Tasks;

public class ReaderTests :
    TestBase
{
    string table = "ReaderTests";

    [Test]
    public async Task Single()
    {
        await TestDataBuilder.SendData(table);
        var reader = new QueueManager(table, SqlConnection);
        await using var result = await reader.Read(1);
        await Verify(result!.ToVerifyTarget());
    }

    [Test]
    public async Task Single_nulls()
    {
        await TestDataBuilder.SendNullData(table);
        var reader = new QueueManager(table, SqlConnection);
        await using var result = await reader.Read(1);
        await Verify(result!.ToVerifyTarget());
    }

    [Test]
    public async Task Batch()
    {
        await TestDataBuilder.SendMultipleDataAsync(table);

        var reader = new QueueManager(table, SqlConnection);
        var messages = new ConcurrentBag<IncomingVerifyTarget>();
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
    }

    [Test]
    public async Task Batch_all()
    {
        await TestDataBuilder.SendMultipleDataAsync(table);

        var reader = new QueueManager(table, SqlConnection);
        var messages = new ConcurrentBag<IncomingVerifyTarget>();
        await reader.Read(
            size: 10,
            startRowVersion: 1,
            func: (message, _) =>
            {
                messages.Add(message.ToVerifyTarget());
                return Task.CompletedTask;
            });
        await Verify(messages.OrderBy(_ => _.Id));
    }

    public ReaderTests()
    {
        var manager = new QueueManager(table, SqlConnection);
        manager.Drop().Await();
        manager.Create().Await();
    }
}