using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
#pragma warning disable ASPDEPR008
#pragma warning disable ASPDEPR004

public class HttpPassthroughDedupTests :
    TestBase
{
    static int count;
    [Test]
    public async Task Integration()
    {
        await using (var connection = Connection.OpenConnection())
        {
            var manager = new DedupeManager(connection, "Deduplication");
            await manager.Create();
        }

        var endpoint = await StartEndpoint();

        var hostBuilder = new WebHostBuilder();
        hostBuilder.UseStartup<SampleStartup>();
        using (var server = new TestServer(hostBuilder))
        {
            using var client = server.CreateClient();
            client.DefaultRequestHeaders.Referrer = new("http://TheReferrer");
            var clientFormSender = new ClientFormSender(client);
            var guid = Guid.NewGuid();
            var first = await SendAsync(clientFormSender, guid);
            await Assert.That(first).IsEqualTo(202);
            var second = await SendAsync(clientFormSender, guid);
            await Assert.That(second).IsEqualTo(208);
        }

        Thread.Sleep(3000);

        await endpoint.Stop();
        await Assert.That(count).IsEqualTo(1);
    }

    static async Task<int> SendAsync(ClientFormSender clientFormSender, Guid guid)
    {
        var message = "{}";
        var send = await clientFormSender.Send(
            route: "/SendMessage",
            message: message,
            typeName: "DedupMessage",
            destination: nameof(HttpPassthroughDedupTests),
            messageId: guid);
        return send.httpStatus;
    }

    static async Task<IEndpointInstance> StartEndpoint()
    {
        var configuration = await EndpointCreator.Create(nameof(HttpPassthroughDedupTests));
        return await Endpoint.Start(configuration);
    }

    class Handler :
        IHandleMessages<DedupMessage>
    {
        public Task Handle(DedupMessage message, HandlerContext context)
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        }
    }
}
class DedupMessage :
    IMessage;
