using Headers = NServiceBus.Transport.SqlServerNative.Headers;

public class SendIntegration :
    TestBase
{
    [Test]
    public async Task Run()
    {
        var resetEvent = new ManualResetEvent(false);
        var configuration = await EndpointCreator.Create("IntegrationSend");
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddNServiceBusEndpoint(configuration);
        builder.Services.AddSingleton(resetEvent);
        var host = builder.Build();
        await host.StartAsync();
        await SendStartMessage();
        resetEvent.WaitOne();
        await host.StopAsync();
    }

    Task SendStartMessage()
    {
        var sender = new QueueManager("IntegrationSend", SqlConnection);
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.EnclosedMessageTypes", typeof(SendMessage).FullName!}
        };

        var message = new OutgoingMessage(Guid.NewGuid(), DateTime.Now.AddDays(1), Headers.Serialize(headers), "{}"u8.ToArray());
        return sender.Send(message);
    }

    class SendHandler(ManualResetEvent @event) :
        IHandleMessages<SendMessage>
    {
        public Task Handle(SendMessage message, HandlerContext context)
        {
            @event.Set();
            return Task.CompletedTask;
        }
    }

    class SendMessage :
        IMessage;
}
