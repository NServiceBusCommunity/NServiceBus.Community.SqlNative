#if SqlServerDedupe
namespace NServiceBus.Transport.SqlServerDeduplication;
#else
namespace NServiceBus.Transport.SqlServerNative;
#endif

public struct DedupeResult(DedupeOutcome dedupeOutcome, string? context)
{
    public DedupeOutcome DedupeOutcome { get; } = dedupeOutcome;
    public string? Context { get; } = context;
}

public enum DedupeOutcome
{
    Sent,
    Deduplicated
}
