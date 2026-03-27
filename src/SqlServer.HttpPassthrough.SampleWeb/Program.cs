using NServiceBus.SqlServer.HttpPassthrough;
using NServiceBus.Transport.SqlServerNative;

var builder = WebApplication.CreateBuilder();

var configuration = new PassthroughConfiguration(
    connectionFunc: () => new(Connection.ConnectionString),
    callback: AmendMessage,
    dedupCriticalError: exception => Environment.FailFast("", exception));
configuration.AppendClaimsToMessageHeaders();
builder.Services.AddSqlHttpPassthrough(configuration);

var app = builder.Build();

app.UseMiddleware<LogContextMiddleware>();
app.AddSqlHttpPassthroughBadRequestMiddleware();

app.MapGet("/test", () =>
{
    var file = Path.Combine(Directory.GetCurrentDirectory(), "test.html");
    return Results.File(file, "text/html");
});

app.MapPost("/SendMessage", async (ISqlPassthrough sender, HttpContext context, Cancel cancel) =>
{
    try
    {
        await sender.Send(context, cancel);
    }
    catch (SendFailureException exception)
    {
        exception.Data.Add("message", exception.PassthroughMessage.ToDictionary());
        exception.CaptureAndThrow();
    }
}).RequireAuthorization();

app.Run();

static Task<Table> AmendMessage(HttpContext context, PassthroughMessage message)
{
    message.ExtraHeaders = new()
    {
        {"MessagePassthrough.Version", AssemblyVersion.Version},
        {"{}\":", "{}\":"}
    };
    return Task.FromResult((Table) message.Destination!);
}
