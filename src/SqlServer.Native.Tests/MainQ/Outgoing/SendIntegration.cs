using Headers = NServiceBus.Transport.SqlServerNative.Headers;

public class SendIntegration :
    TestBase
{
    [Test]
    public async Task Run()
    {
        var resetEvent = new ManualResetEvent(false);
        var configuration = await EndpointCreator.Create("IntegrationSend");
        configuration.RegisterComponents(_ => _.AddSingleton(resetEvent));
        var endpoint = await Endpoint.Start(configuration);
        await SendStartMessage();
        resetEvent.WaitOne();
        await endpoint.Stop();
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
