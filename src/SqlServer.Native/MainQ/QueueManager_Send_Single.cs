using System.Data;

namespace NServiceBus.Transport.SqlServerNative;

public partial class QueueManager
{
    protected override SqlCommand CreateSendCommand(OutgoingMessage message)
    {
        var command = Connection.CreateCommand(Transaction, sendSql);
        var parameters = command.Parameters;

        var idParameter = CreateIdParameter(command, parameters);
        var expiresParameter = CreateExpiresParameter(command, parameters);
        var headersParameter = CreateHeadersParameter(command, parameters);
        var bodyParameter = CreateBodyParameter(command, parameters);

        idParameter.Value = message.Id;
        expiresParameter.SetValueOrDbNull(message.Expires);
        headersParameter.Value = message.Headers;
        bodyParameter.SetBinaryOrDbNull(message.Body);

        return command;
    }

    static SqlParameter CreateIdParameter(SqlCommand command, SqlParameterCollection parameters)
    {
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Guid;
        parameters.Add(idParameter);
        return idParameter;
    }

    static SqlParameter CreateExpiresParameter(SqlCommand command, SqlParameterCollection parameters)
    {
        var expiresParameter = command.CreateParameter();
        expiresParameter.ParameterName = "Expires";
        expiresParameter.DbType = DbType.DateTime;
        parameters.Add(expiresParameter);
        return expiresParameter;
    }

    static SqlParameter CreateHeadersParameter(SqlCommand command, SqlParameterCollection parameters)
    {
        var headersParameter = command.CreateParameter();
        headersParameter.ParameterName = "Headers";
        headersParameter.DbType = DbType.String;
        parameters.Add(headersParameter);
        return headersParameter;
    }

    static SqlParameter CreateBodyParameter(SqlCommand command, SqlParameterCollection parameters)
    {
        var bodyParameter = command.CreateParameter();
        bodyParameter.ParameterName = "Body";
        bodyParameter.DbType = DbType.Binary;
        parameters.Add(bodyParameter);
        return bodyParameter;
    }
}