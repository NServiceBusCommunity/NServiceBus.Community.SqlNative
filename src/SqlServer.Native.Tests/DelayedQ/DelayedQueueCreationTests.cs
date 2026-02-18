public class DelayedQueueCreationTests
{
    [Test]
    public async Task Run()
    {
        await using var connection = Connection.OpenConnectionFromNewClient();
        var manager = new DelayedQueueManager("DelayedQueueCreationTests", connection);
        await manager.Drop();
        await manager.Create();
        var settings = new VerifySettings();
        settings.SchemaFilter(_ => _.Name == "DelayedQueueCreationTests");
        await Verify(connection, settings);
    }
}
