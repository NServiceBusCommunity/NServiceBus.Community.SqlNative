using NServiceBus.Features;
using NServiceBus.Transport.SqlServerDeduplication;

class PurgeTask(Table table, Func<Cancel, Task<SqlConnection>> builder) :
        FeatureStartupTask
{
    protected override async Task OnStart(IMessageSession session, Cancel cancel = default)
    {
        using var connection = await builder(Cancel.None);
        var dedupeManager = new DedupeManager(connection, table);
        await dedupeManager.PurgeItems(cancel);
    }

    protected override Task OnStop(IMessageSession session, Cancel cancel = default) =>
        Task.CompletedTask;
}