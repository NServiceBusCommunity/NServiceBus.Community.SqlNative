using Microsoft.Data.SqlClient;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Transport;
using NServiceBus.Transport.SqlServerDeduplication;

class SendBehavior(Table table, Func<Cancel, Task<SqlConnection>> builder) :
        Behavior<IOutgoingPhysicalMessageContext>
{
    ILog logger = LogManager.GetLogger("DeduplicationSendBehavior");

    public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
    {
        if (!DedupePipelineState.TryGet(context, out var dedupePipelineState))
        {
            await next();
            return;
        }

        var connectionTask = builder(Cancel.None);
        if (context.Extensions.TryGet(out TransportTransaction? _))
        {
            throw new NotSupportedException("Deduplication is currently designed to be used from outside the NServiceBus pipeline. For example to dedup messages being sent from inside a web service endpoint.");
        }

        var messageId = GetMessageId(context);

        var transportTransaction = new TransportTransaction();
        context.Extensions.Set(transportTransaction);
        using var connection = await connectionTask;
        using var transaction = connection.BeginTransaction();
        transportTransaction.Set(connection);
        transportTransaction.Set(transaction);

        var dedupeManager = new DedupeManager(transaction, table);
        var writeResult = await dedupeManager.WriteDedupRecord(messageId, dedupePipelineState.Context, context.CancellationToken);
        dedupePipelineState.DedupeOutcome = writeResult.DedupeOutcome;
        dedupePipelineState.Context = writeResult.Context;
        if (dedupePipelineState.DedupeOutcome == DedupeOutcome.Deduplicated)
        {
            logger.Info($"Message deduplicated. MessageId: {messageId}");
            return;
        }

        await next();
        var commitResult = await dedupeManager.CommitWithDedupCheck(messageId, dedupePipelineState.Context);
        dedupePipelineState.DedupeOutcome = commitResult.DedupeOutcome;
        dedupePipelineState.Context = commitResult.Context;
        if (commitResult.DedupeOutcome == DedupeOutcome.Deduplicated)
        {
            logger.Info($"Message deduplicated. MessageId: {messageId}");
        }
    }

    static Guid GetMessageId(IOutgoingPhysicalMessageContext context)
    {
        if (Guid.TryParse(context.MessageId, out var messageId))
        {
            return messageId;
        }

        throw new($"Only Guids are supported for message Ids. Invalid value: {context.MessageId}");
    }
}