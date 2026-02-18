using NServiceBus.Attachments.Sql;
using DedupeOutcome = NServiceBus.Transport.SqlServerDeduplication.DedupeOutcome;
using DedupeResult = NServiceBus.Transport.SqlServerDeduplication.DedupeResult;
using System.Threading.Tasks;

public class DedupeIntegrationTests :
    TestBase
{
    static CountdownEvent countdown = new(2);

    [Test]
    public async Task Integration()
    {
        var endpoint = await StartEndpoint();
        var messageId = Guid.NewGuid();
        var result = await SendMessage(messageId, endpoint, "context1");
        await Assert.That(result.Context).IsEqualTo("context1");
        await Assert.That(result.DedupeOutcome).IsEqualTo(DedupeOutcome.Sent);
        result = await SendMessage(messageId, endpoint, "context2");
        await Assert.That(result.Context).IsEqualTo("context1");
        await Assert.That(result.DedupeOutcome).IsEqualTo(DedupeOutcome.Deduplicated);
        if (!countdown.Wait(TimeSpan.FromSeconds(20)))
        {
            throw new("Expected dedup");
        }

        await endpoint.Stop();
    }

    static async Task<DedupeResult> SendMessage(Guid messageId, IEndpointInstance endpoint, string context)
    {
        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();
        var sendWithDedupe = await endpoint.SendWithDedupe(messageId, new MyMessage(), sendOptions,context);
        if (sendWithDedupe.DedupeOutcome == DedupeOutcome.Deduplicated)
        {
            countdown.Signal();
        }
        return sendWithDedupe;
    }

    static Task<IEndpointInstance> StartEndpoint()
    {
        var configuration = new EndpointConfiguration(nameof(DedupeIntegrationTests));
        configuration.UsePersistence<LearningPersistence>();
        configuration.EnableInstallers();
        configuration.EnableDedupe(Connection.ConnectionString);
        configuration.PurgeOnStartup(true);
        configuration.UseSerialization<NewtonsoftJsonSerializer>();

        var attachments = configuration.EnableAttachments(Connection.ConnectionString, TimeToKeep.Default);
        attachments.UseTransportConnectivity();

        var transport = configuration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(Connection.ConnectionString);
        transport.NativeDelayedDelivery();
        return Endpoint.Start(configuration);
    }

    class Handler :
        IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, HandlerContext context)
        {
            countdown.Signal();
            return Task.CompletedTask;
        }
    }

    public DedupeIntegrationTests()
    {
        var dedupeManager = new DedupeManager(SqlConnection, "Deduplication");
        dedupeManager.Drop().Await();
        dedupeManager.Create().Await();
    }

    class MyMessage :
        IMessage
    {
        public string? Property { get; set; }
    }
}