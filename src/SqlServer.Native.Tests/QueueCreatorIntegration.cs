public class QueueCreatorIntegration
{
    [Test]
    public async Task Run()
    {
        var resetEvent = new ManualResetEvent(false);
        var configuration = await EndpointCreator.Create("IntegrationSend");
        configuration.RegisterComponents(_ => _.AddSingleton(resetEvent));
        var endpoint = await Endpoint.Start(configuration);
        await SendStartMessage(endpoint);
        resetEvent.WaitOne();
        await endpoint.Stop();
    }

    static Task SendStartMessage(IEndpointInstance endpoint)
    {
        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();
        return endpoint.Send(new SendMessage(), sendOptions);
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
