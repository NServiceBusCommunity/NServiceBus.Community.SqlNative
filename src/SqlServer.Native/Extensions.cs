using System.Data;

static class Extensions
{
    public static void SetValueOrDbNull(this SqlParameter corrParam, DateTime? value)
    {
        if (value == null)
        {
            corrParam.Value = DBNull.Value;
            return;
        }

        corrParam.Value = value;
    }

    public static void SetBinaryOrDbNull(this SqlParameter corrParam, object? value)
    {
        if (value == null)
        {
            corrParam.Value = DBNull.Value;
            return;
        }

        corrParam.Value = value;
    }

    public static async Task RunCommand(this SqlConnection connection, SqlTransaction? transaction, string sql, Cancel cancel = default)
    {
        using var command = connection.CreateCommand(transaction, sql);
        await command.RunNonQuery(cancel);
    }

#if NET48
    public static ValueTask DisposeAsync(this IDisposable disposable)
    {
        disposable.Dispose();
        return default;
    }
#endif

    public static SqlCommand CreateCommand(this SqlConnection connection, SqlTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    public static Task<long?> LongOrNull(this SqlDataReader dataReader, int index)
    {
        if (dataReader.IsDBNull(index))
        {
            return Task.FromResult<long?>(default);
        }

        return dataReader.GetFieldValueAsync<long?>(index);
    }

    public static Task<DateTime?> DatetimeOrNull(this SqlDataReader dataReader, int index)
    {
        if (dataReader.IsDBNull(index))
        {
            return Task.FromResult<DateTime?>(default);
        }

        return dataReader.GetFieldValueAsync<DateTime?>(index);
    }

    public static async Task<SqlDataReader> RunSequentialReader(this SqlCommand command, Cancel cancel)
    {
        try
        {
            return await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancel);
        }
        catch (SqlException exception)
        {
            SetCommandData(command, exception);
            throw;
        }
    }

    public static async Task<object?> RunScalar(this SqlCommand command, Cancel cancel)
    {
        try
        {
            return await command.ExecuteScalarAsync(cancel);
        }
        catch (SqlException exception)
        {
            SetCommandData(command, exception);
            throw;
        }
    }

    public static async Task<SqlDataReader> RunSingleRowReader(this SqlCommand command, Cancel cancel)
    {
        try
        {
            return await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancel);
        }
        catch (SqlException exception)
        {
            SetCommandData(command, exception);
            throw;
        }
    }

    public static async Task RunNonQuery(this SqlCommand command, Cancel cancel)
    {
        try
        {
            await command.ExecuteNonQueryAsync(cancel);
        }
        catch (SqlException exception)
        {
            SetCommandData(command, exception);
            throw;
        }
    }

    public static SqlParameter AddStringParam(this SqlCommand command, string name, string value)
    {
        var bodyParameter = command.CreateParameter();
        bodyParameter.ParameterName = name;
        bodyParameter.DbType = DbType.String;
        bodyParameter.Value = value;
        command.Parameters.Add(bodyParameter);
        return bodyParameter;
    }

    public static void SetCommandData(this SqlCommand command, SqlException exception) =>
        exception.Data["Sql"] = command.CommandText;
}