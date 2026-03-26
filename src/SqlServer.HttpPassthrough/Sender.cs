using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NServiceBus.Transport.SqlServerNative;
using NServiceBus.Attachments.Sql.Raw;
using NServiceBus.SqlServer.HttpPassthrough;
using Table = NServiceBus.Transport.SqlServerNative.Table;

class Sender(
    Func<Cancel, Task<SqlConnection>> func,
    HeadersBuilder builder,
    Table attachmentsTable,
    Table table,
    ILogger logger)
{
    public async Task<long> Send(PassthroughMessage message, Table destination, Cancel cancel)
    {
        try
        {
           return await InnerSend(message, destination, cancel);
        }
        catch (Exception exception)
        {
            throw new SendFailureException(message, exception);
        }
    }

    async Task<long> InnerSend(PassthroughMessage message, Table destination, Cancel cancel)
    {
        using var connection = await func(cancel);
        using var transaction = connection.BeginTransaction();
        var rowVersion = await SendInsideTransaction(message, destination, cancel, transaction);
        transaction.Commit();
        return rowVersion;
    }

    async Task<long> SendInsideTransaction(PassthroughMessage message, Table destination, Cancel cancel, SqlTransaction transaction)
    {
        var headersString = builder.GetHeadersString(message);
        LogSend(message);
        var outgoingMessage = new OutgoingMessage(
            message.Id,
            headers: headersString,
            bodyBytes: Encoding.UTF8.GetBytes(message.Body));
        var queueManager = new QueueManager(destination, transaction, table);
        var attachmentExpiry = DateTime.UtcNow.AddDays(10);
        await SendAttachments(transaction, attachmentExpiry, cancel, message);
        return await queueManager.Send(outgoingMessage, cancel);
    }

    void LogSend(PassthroughMessage message)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        logger.LogInformation("Send passthrough. Id:{id}, Destination:{destination}, Namespace:{namespace}, Type:{type}, Body:{body}, Attachments:{attachments},",
            message.Id,
            message.Destination,
            message.Namespace,
            message.Type,
            message.Body,
            message.Attachments.Select(_ => _.FileName));
    }

    async Task SendAttachments(SqlTransaction transaction, DateTime expiry, Cancel cancel, PassthroughMessage message)
    {
        var connection = transaction.Connection!;
        var attachments = new Persister(connection.Database, attachmentsTable.UnsanitizedSchema, attachmentsTable.UnsanitizedTableName);
        foreach (var file in message.Attachments)
        {
            await SendAttachment(attachments, transaction, message.Id.ToString(), expiry, cancel, file, connection);
        }
    }

    static async Task SendAttachment(Persister attachments, SqlTransaction transaction, string messageId, DateTime expiry, Cancel cancel, Attachment file, SqlConnection connection)
    {
        using var stream = file.Stream();
        await attachments.SaveStream(connection, transaction, messageId, file.FileName, expiry, stream, cancel: cancel);
    }
}