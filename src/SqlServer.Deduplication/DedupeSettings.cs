using NServiceBus.Transport.SqlServerDeduplication;

namespace NServiceBus;

public class DedupeSettings
{
    internal Table Table = "Deduplication";
    internal Func<Cancel, Task<SqlConnection>> ConnectionBuilder;

    internal DedupeSettings(Func<Cancel, Task<SqlConnection>> connectionBuilder) =>
        ConnectionBuilder = connectionBuilder;

    internal bool RunCleanTask = true;
    internal bool InstallerDisabled;

    /// <summary>
    /// Disable the attachment cleanup task.
    /// </summary>
    public void DisableCleanupTask() =>
        RunCleanTask = false;

    /// <summary>
    /// Control the table and schema used for deduplication.
    /// Defaults to 'dbo.Deduplication'.
    /// </summary>
    public void UseTable(Table table) =>
        Table = table;

    /// <summary>
    /// Disable the table creation installer.
    /// </summary>
    public void DisableInstaller() =>
        InstallerDisabled = true;
}