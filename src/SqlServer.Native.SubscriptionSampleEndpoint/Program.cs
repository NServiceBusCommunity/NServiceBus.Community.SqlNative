using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus.Transport.SqlServerNative;
using SampleNamespace;

class Program
{
    const string connection = @"Server=.\SQLExpress;Database=SubscriptionSample; Integrated Security=True;Max Pool Size=100;TrustServerCertificate=True";
    static async Task Main(string[] args)
    {
        await CreateTables();
        Console.Title = "SampleEndpoint Press Ctrl-C to Exit.";

        var configuration = new EndpointConfiguration("SampleEndpoint");
        configuration.UsePersistence<LearningPersistence>();
        configuration.UseSerialization<NewtonsoftJsonSerializer>();
        configuration.PurgeOnStartup(true);
        var transport = configuration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(connection);
        transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
        configuration.EnableInstallers();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Services.AddNServiceBusEndpoint(configuration);
        var host = builder.Build();
        await host.StartAsync();

        var session = host.Services.GetRequiredService<IMessageSession>();
        await Publish(session);

        Console.ReadKey(true);
        await host.StopAsync();
    }

    static async Task CreateTables()
    {
        await using var dbConnection = await ConnectionBuilder();
        var main = new QueueManager("SampleEndpoint", dbConnection);
        await main.Create();
        var delayed = new DelayedQueueManager("SampleEndpoint.Delayed", dbConnection);
        await delayed.Create();
        var subscription = new SubscriptionManager("SubscriptionRouting", dbConnection);
        await subscription.Create();
    }

    static async Task<SqlConnection> ConnectionBuilder()
    {
        var sqlConnection = new SqlConnection(connection);
        try
        {
            await sqlConnection.OpenAsync();
            return sqlConnection;
        }
        catch
        {
            await sqlConnection.DisposeAsync();
            throw;
        }
    }

    static Task Publish(IMessageSession session)
    {
        var message = new SampleMessage();
        return session.Publish(message);
    }
}
