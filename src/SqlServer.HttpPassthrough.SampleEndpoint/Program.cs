using Microsoft.Extensions.Hosting;
using NServiceBus.Attachments.Sql;

Console.Title = "SampleEndpoint Press Ctrl-C to Exit.";

var builder = Host.CreateApplicationBuilder(args);

var configuration = new EndpointConfiguration("SampleEndpoint");
configuration.UsePersistence<LearningPersistence>();
var attachments = configuration.EnableAttachments(Connection.ConnectionString, TimeToKeep.Default);
attachments.UseTransportConnectivity();
configuration.UseSerialization<NewtonsoftJsonSerializer>();
configuration.PurgeOnStartup(true);
var transport = configuration.UseTransport<SqlServerTransport>();
transport.ConnectionString(Connection.ConnectionString);
configuration.EnableInstallers();

builder.Services.AddNServiceBusEndpoint(configuration);

await builder.Build().RunAsync();
