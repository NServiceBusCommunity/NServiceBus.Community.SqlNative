using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
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

        var hostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                var configuration = new PassthroughConfiguration(
                    connectionFunc: () => new(Connection.ConnectionString),
                    callback: AmendMessage,
                    dedupCriticalError: exception => Environment.FailFast("", exception));
                configuration.AppendClaimsToMessageHeaders();
                services.AddSqlHttpPassthrough(configuration);
                services.AddRouting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.AddSqlHttpPassthroughBadRequestMiddleware();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapPost("/SendMessage", context =>
                    {
                        var sender = context.RequestServices.GetRequiredService<ISqlPassthrough>();
                        return sender.Send(context, context.RequestAborted);
                    });
                });
            });
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

    static Task<Table> AmendMessage(HttpContext context, PassthroughMessage message) =>
        Task.FromResult((Table) message.Destination!);

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
