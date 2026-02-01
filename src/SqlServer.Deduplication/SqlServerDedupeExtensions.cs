using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Transport.SqlServerDeduplication;

namespace NServiceBus;

/// <summary>
/// Extensions to control what messages are audited.
/// </summary>
public static class SqlServerDedupeExtensions
{
    /// <summary>
    /// Enable SQL attachments for this endpoint.
    /// </summary>
    public static DedupeSettings EnableDedupe(
        this EndpointConfiguration configuration,
        string connection) =>
        EnableDedupe(configuration, async cancel =>
        {
            var sqlConnection = new SqlConnection(connection);
            try
            {
                await sqlConnection.OpenAsync(cancel);
                return sqlConnection;
            }
            catch
            {
                sqlConnection.Dispose();
                throw;
            }
        });

    /// <summary>
    /// Enable SQL attachments for this endpoint.
    /// </summary>
    public static DedupeSettings EnableDedupe(
        this EndpointConfiguration configuration,
        Func<Cancel, Task<SqlConnection>> connectionBuilder)
    {
        var recoverability = configuration.Recoverability();
        recoverability.AddUnrecoverableException<NotSupportedException>();
        var settings = configuration.GetSettings();
        var dedupeSettings = new DedupeSettings(connectionBuilder);
        settings.Set(dedupeSettings);
        configuration.EnableFeature<DeduplicationFeature>();
        return dedupeSettings;
    }

    public static Task<DedupeResult> SendLocalWithDedupe(this IMessageSession session, Guid messageId, object message, string? context = null, Cancel cancel = default)
    {
        var options = new SendOptions();
        options.RouteToThisEndpoint();
        return SendWithDedupe(session, messageId, message, options, context, cancel);
    }

    public static Task<DedupeResult> SendWithDedupe(this IMessageSession session, Guid messageId, object message, SendOptions? options = null, string? context = null, Cancel cancel = default)
    {
        Guard.AgainstEmpty(messageId);
        if (options == null)
        {
            options = new();
        }
        else
        {
            ValidateMessageId(options);
        }

        return InnerSendWithDedupe(session, message, messageId, options, context, cancel);
    }

    static void ValidateMessageId(SendOptions options)
    {
        if (options.GetMessageId() != null)
        {
            throw new ArgumentException("Expected a SendOptions with no MessageId defined", nameof(options));
        }
    }

    static async Task<DedupeResult> InnerSendWithDedupe(IMessageSession session, object message, Guid messageId, SendOptions options, string? context, Cancel cancel)
    {
        var pipelineState = new DedupePipelineState
        {
            Context = context
        };
        DedupePipelineState.Set(options, pipelineState);
        options.SetMessageId(messageId.ToString());

        await session.Send(message, options, cancel);

        return new(
            dedupeOutcome: pipelineState.DedupeOutcome,
            context: pipelineState.Context
        );
    }
}