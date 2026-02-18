using System.Threading.Tasks;

public class WithDedupeTests :
    TestBase
{
    static DateTime dateTime = new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

    string table = "WithDedupeTests";

    [Test]
    public async Task Single()
    {
        var message = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
        await Send(message);
        await Verify( SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Single_WithDuplicate()
    {
        var message = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
        await Send(message);
        await Send(message);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    //[Fact]
    //public void Single_WithPurgedDuplicate()
    //{
    //    var message = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
    //    Send(message);
    //    Send(message);
    //    await Verify(SqlHelper.ReadData(table));
    //}

    [Test]
    public async Task Batch()
    {
        var messages = new List<OutgoingMessage>
        {
            BuildBytesMessage("00000000-0000-0000-0000-000000000001"),
            BuildBytesMessage("00000000-0000-0000-0000-000000000002")
        };
        await Send(messages);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Batch_WithFirstDuplicate()
    {
        var message = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
        await Send(message);
        var messages = new List<OutgoingMessage>
        {
            BuildBytesMessage("00000000-0000-0000-0000-000000000001"),
            BuildBytesMessage("00000000-0000-0000-0000-000000000002")
        };
        await Send(messages);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Batch_WithSecondDuplicate()
    {
        var message = BuildBytesMessage("00000000-0000-0000-0000-000000000002");
        await Send(message);
        var messages = new List<OutgoingMessage>
        {
            BuildBytesMessage("00000000-0000-0000-0000-000000000001"),
            BuildBytesMessage("00000000-0000-0000-0000-000000000002")
        };
        await Send(messages);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    Task Send(List<OutgoingMessage> messages)
    {
        var sender = new QueueManager(table, SqlConnection, "Deduplication");
        return sender.Send(messages);
    }

    Task<long> Send(OutgoingMessage message)
    {
        var sender = new QueueManager(table, SqlConnection, "Deduplication");
        return sender.Send(message);
    }

    static OutgoingMessage BuildBytesMessage(string guid) =>
        new(new(guid), dateTime, "headers", "{}"u8.ToArray());

    public WithDedupeTests()
    {
        var manager = new QueueManager(table, SqlConnection, "Deduplication");
        manager.Drop().Await();
        manager.Create().Await();
        var dedupeManager = new DedupeManager(SqlConnection, "Deduplication");
        dedupeManager.Drop().Await();
        dedupeManager.Create().Await();
    }
}