public class ConsumerIntegrationTests :
    TestBase
{
    static string table = "IntegrationConsumer_Consumer";

    [Test]
    public async Task Run()
    {
        await SqlConnection.DropTable(null, table);
        var manager = new QueueManager(table, SqlConnection);
        await manager.Create();
        var configuration = await EndpointCreator.Create("IntegrationConsumer");
        configuration.SendOnly();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddNServiceBusEndpoint(configuration);
        var host = builder.Build();
        await host.StartAsync();
        var session = host.Services.GetRequiredService<IMessageSession>();
        await SendStartMessage(session);
        var consumer = new QueueManager(table, SqlConnection);
        await using var message = await consumer.Consume();
        await Assert.That(message).IsNotNull();
        await host.StopAsync();
    }

    static Task SendStartMessage(IMessageSession session)
    {
        var sendOptions = new SendOptions();
        sendOptions.SetDestination(table);
        return session.Send(new SendMessage(), sendOptions);
    }

    class SendMessage :
        IMessage;
}
