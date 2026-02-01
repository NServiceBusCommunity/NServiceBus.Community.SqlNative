using NServiceBus.Features;
using NServiceBus.Transport.SqlServerDeduplication;

class CleanupTask(
    Table table,
    CriticalError error,
    Func<Cancel, Task<SqlConnection>> builder) :
        FeatureStartupTask
{
    DedupeCleanerJob? job;

    protected override Task OnStart(IMessageSession session, Cancel cancel = default)
    {
        job = new(table, builder, RaiseError);
        job.Start();
        return Task.CompletedTask;
    }

    void RaiseError(Exception exception) =>
        error.Raise("Dedup cleanup failed", exception);

    protected override Task OnStop(IMessageSession session, Cancel cancel = default) =>
        job == null ? Task.CompletedTask : job.Stop();
}