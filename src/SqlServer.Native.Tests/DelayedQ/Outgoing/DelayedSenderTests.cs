public class DelayedSenderTests :
    TestBase
{
    string table = "DelayedSenderTests";
    static DateTime dateTime = new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

    [Test]
    public async Task Single_bytes()
    {
        var message = BuildBytesMessage();
        await Send(message);
        await Verify(SqlHelper.ReadDelayedData(table, SqlConnection));
    }

    [Test]
    public async Task Single_bytes_nulls()
    {
        var message = BuildBytesNullMessage();
        await Send(message);
        await Verify(SqlHelper.ReadDelayedData(table, SqlConnection));
    }

    [Test]
    public async Task Single_stream()
    {
        var message = BuildStreamMessage();
        await  Send(message);
        await Verify(SqlHelper.ReadDelayedData(table, SqlConnection));
    }

    [Test]
    public async Task Single_stream_nulls()
    {
        var sender = new DelayedQueueManager(table, SqlConnection);

        var message = BuildBytesNullMessage();
        await sender.Send(message);
        await Verify(SqlHelper.ReadDelayedData(table, SqlConnection));
    }

    [Test]
    public async Task Batch()
    {
        var messages = new List<OutgoingDelayedMessage>
        {
            BuildBytesMessage(),
            BuildStreamMessage()
        };
        await Send(messages);
        await Verify(SqlHelper.ReadDelayedData(table, SqlConnection));
    }

    [Test]
    public async Task Batch_nulls()
    {
        var messages = new List<OutgoingDelayedMessage>
        {
            BuildBytesNullMessage(),
            BuildStreamNullMessage()
        };
        await Send(messages);
        await Verify(SqlHelper.ReadDelayedData(table, SqlConnection));
    }

    Task<long> Send(OutgoingDelayedMessage message)
    {
        var sender = new DelayedQueueManager(table, SqlConnection);

        return sender.Send(message);
    }

    Task Send(List<OutgoingDelayedMessage> messages)
    {
        var sender = new DelayedQueueManager(table, SqlConnection);

        return sender.Send(messages);
    }

    static OutgoingDelayedMessage BuildBytesMessage() =>
        new(dateTime, "headers", "{}"u8.ToArray());

    static OutgoingDelayedMessage BuildStreamMessage()
    {
        var stream = new MemoryStream("{}"u8.ToArray());
        return new(dateTime, "headers", stream);
    }

    static OutgoingDelayedMessage BuildBytesNullMessage() =>
        new(dateTime, null, bodyBytes: null);

    static OutgoingDelayedMessage BuildStreamNullMessage() =>
        new(dateTime, null, bodyStream: null);

    public DelayedSenderTests()
    {
        var manager = new DelayedQueueManager(table, SqlConnection);
        manager.Drop().Await();
        manager.Create().Await();
    }
}
