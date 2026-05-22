public class QueueCreatorIntegration
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
        var session = host.Services.GetRequiredService<IMessageSession>();
        await SendStartMessage(session);
        resetEvent.WaitOne();
        await host.StopAsync();
    }

    static Task SendStartMessage(IMessageSession session)
    {
        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();
        return session.Send(new SendMessage(), sendOptions);
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
