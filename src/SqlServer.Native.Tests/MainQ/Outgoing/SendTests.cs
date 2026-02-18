public class SendTests :
    TestBase
{
    static DateTime dateTime = new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

    string table = "SendTests";

    [Test]
    public async Task Single_bytes()
    {
        var message = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
        await Send(message);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Single_with_transaction()
    {
        var message = BuildBytesMessage("00000000-0000-0000-0000-000000000001");
        await using var transaction = SqlConnection.BeginTransaction();
        var sender = new QueueManager(table, transaction);
        await sender.Send(message);
        await transaction.CommitAsync();
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Single_bytes_nulls()
    {
        var sender = new QueueManager("SendTests", SqlConnection);

        var message = BuildBytesNullMessage("00000000-0000-0000-0000-000000000001");
        await sender.Send(message);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Single_stream()
    {
        var message = BuildStreamMessage("00000000-0000-0000-0000-000000000001");
        await Send(message);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Single_stream_nulls()
    {
        var message = BuildStreamMessage("00000000-0000-0000-0000-000000000001");
        await Send(message);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Batch()
    {
        var messages = new List<OutgoingMessage>
        {
            BuildBytesMessage("00000000-0000-0000-0000-000000000001"),
            BuildStreamMessage("00000000-0000-0000-0000-000000000002")
        };
        await Send(messages);
        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    [Test]
    public async Task Batch_nulls()
    {
        var messages = new List<OutgoingMessage>
        {
            BuildBytesNullMessage("00000000-0000-0000-0000-000000000001"),
            BuildStreamNullMessage("00000000-0000-0000-0000-000000000002")
        };
        await Send(messages);

        await Verify(SqlHelper.ReadData(table, SqlConnection));
    }

    Task Send(List<OutgoingMessage> messages)
    {
        var sender = new QueueManager(table, SqlConnection);
        return sender.Send(messages);
    }

    Task<long> Send(OutgoingMessage message)
    {
        var sender = new QueueManager(table, SqlConnection);
        return sender.Send(message);
    }

    static OutgoingMessage BuildBytesMessage(string guid) =>
        new(new(guid), dateTime, "headers", "{}"u8.ToArray());

    static OutgoingMessage BuildStreamMessage(string guid)
    {
        var stream = new MemoryStream("{}"u8.ToArray());
        return new(new(guid), dateTime, "headers", stream);
    }

    static OutgoingMessage BuildStreamNullMessage(string guid) =>
        new(new(guid), bodyStream: null);

    static OutgoingMessage BuildBytesNullMessage(string guid) =>
        new(new(guid), bodyBytes: null);

    public SendTests()
    {
        var manager = new QueueManager(table, SqlConnection);
        manager.Drop().Await();
        manager.Create().Await();
    }
}
