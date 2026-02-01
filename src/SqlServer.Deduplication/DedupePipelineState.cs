using System.Diagnostics.CodeAnalysis;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Transport.SqlServerDeduplication;

class DedupePipelineState
{
    public DedupeOutcome DedupeOutcome;
    public string? Context;

    public static bool TryGet(
        IOutgoingPhysicalMessageContext context,
        [NotNullWhen(true)]
        out DedupePipelineState? state)
    {
        if (context.Extensions.TryGet(out DedupePipelineState? found))
        {
            state = found;
            return true;
        }

        state = null;
        return false;
    }

    public static void Set(SendOptions options, DedupePipelineState state) =>
        options.GetExtensions().Set(state);
}