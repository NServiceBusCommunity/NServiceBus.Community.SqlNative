using System.Threading.Tasks;

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
        var endpoint = await Endpoint.Start(configuration);
        await SendStartMessage(endpoint);
        var consumer = new QueueManager(table, SqlConnection);
        await using var message = await consumer.Consume();
        await Assert.That(message).IsNotNull();
    }

    static Task SendStartMessage(IEndpointInstance endpoint)
    {
        var sendOptions = new SendOptions();
        sendOptions.SetDestination(table);
        return endpoint.Send(new SendMessage(), sendOptions);
    }

    class SendMessage :
        IMessage;
}