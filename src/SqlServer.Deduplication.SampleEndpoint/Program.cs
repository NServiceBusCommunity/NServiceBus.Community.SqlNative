using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus.Transport.SqlServerDeduplication;
using SampleNamespace;

class Program
{
    const string connection = @"Server=.\SQLExpress;Database=DedupeSample; Integrated Security=True;Max Pool Size=100;TrustServerCertificate=True";
    static async Task Main(string[] args)
    {
        Console.Title = "SampleEndpoint Press Ctrl-C to Exit.";

        var configuration = new EndpointConfiguration("SampleEndpoint");
        configuration.EnableInstallers();
        configuration.UsePersistence<LearningPersistence>();
        configuration.EnableDedupe(ConnectionBuilder);
        configuration.UseSerialization<NewtonsoftJsonSerializer>();
        configuration.PurgeOnStartup(true);
        var transport = configuration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(connection);
        transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Services.AddNServiceBusEndpoint(configuration);
        var host = builder.Build();
        await host.StartAsync();

        var session = host.Services.GetRequiredService<IMessageSession>();
        await SendMessages(session);

        Console.ReadKey(true);
        await host.StopAsync();
    }

    static async Task<SqlConnection> ConnectionBuilder(Cancel cancel)
    {
        var sqlConnection = new SqlConnection(connection);
        try
        {
            await sqlConnection.OpenAsync(cancel);
            return sqlConnection;
        }
        catch
        {
            await sqlConnection.DisposeAsync();
            throw;
        }
    }

    static async Task SendMessages(IMessageSession session)
    {
        var guid = Guid.NewGuid();
        var dedupeOutcome1 = await SendMessage(session, guid);
        Console.WriteLine($"DedupeOutcome:{dedupeOutcome1.DedupeOutcome}. Context:{dedupeOutcome1.Context}");
        var dedupeOutcome2 = await SendMessage(session, guid);
        Console.WriteLine($"DedupeOutcome:{dedupeOutcome2.DedupeOutcome}. Context:{dedupeOutcome2.Context}");
    }

    static Task<DedupeResult> SendMessage(IMessageSession session, Guid guid)
    {
        var message = new SampleMessage();
        var options = new SendOptions();
        options.RouteToThisEndpoint();
        return session.SendWithDedupe(guid, message, options);
    }
}
