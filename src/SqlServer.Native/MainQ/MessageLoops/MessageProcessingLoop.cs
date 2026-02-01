namespace NServiceBus.Transport.SqlServerNative;

public class MessageProcessingLoop :
    MessageLoop
{
    string table;
    long startingRow;
    Func<Cancel, Task<SqlConnection>>? connectionBuilder;
    Func<Cancel, Task<SqlTransaction>>? transactionBuilder;
    Func<SqlTransaction, IncomingMessage, Cancel, Task>? transactionCallback;
    Func<SqlConnection, IncomingMessage, Cancel, Task>? connectionCallback;
    Func<SqlTransaction, long, Cancel, Task>? transactionPersistRowVersion;
    Func<SqlConnection, long, Cancel, Task>? connectionPersistRowVersion;
    int batchSize;

    public MessageProcessingLoop(
        string table,
        long startingRow,
        Func<Cancel, Task<SqlTransaction>> transactionBuilder,
        Func<SqlTransaction, IncomingMessage, Cancel, Task> callback,
        Action<Exception> errorCallback,
        Func<SqlTransaction, long, Cancel, Task> persistRowVersion,
        int batchSize = 10,
        TimeSpan? delay = null) :
        base(errorCallback, delay)
    {
        Guard.AgainstNullOrEmpty(table);
        Guard.AgainstNegativeAndZero(startingRow);
        Guard.AgainstNegativeAndZero(batchSize);
        this.table = table;
        this.startingRow = startingRow;
        this.transactionBuilder = transactionBuilder.WrapFunc(nameof(transactionBuilder));
        transactionCallback = callback.WrapFunc(nameof(transactionCallback));
        transactionPersistRowVersion = persistRowVersion;
        this.batchSize = batchSize;
    }

    public MessageProcessingLoop(
        string table,
        long startingRow,
        Func<Cancel, Task<SqlConnection>> connectionBuilder,
        Func<SqlConnection, IncomingMessage, Cancel, Task> callback,
        Action<Exception> errorCallback,
        Func<SqlConnection, long, Cancel, Task> persistRowVersion,
        int batchSize = 10,
        TimeSpan? delay = null) :
        base(errorCallback, delay)
    {
        Guard.AgainstNullOrEmpty(table);
        Guard.AgainstNegativeAndZero(startingRow);
        Guard.AgainstNegativeAndZero(batchSize);
        connectionCallback = callback.WrapFunc(nameof(connectionCallback));
        this.table = table;
        this.startingRow = startingRow;
        this.connectionBuilder = connectionBuilder.WrapFunc(nameof(connectionBuilder));
        connectionPersistRowVersion = persistRowVersion;
        this.batchSize = batchSize;
    }

    protected override async Task RunBatch(Cancel cancel)
    {
        SqlConnection? connection = null;
        if (connectionBuilder != null)
        {
            using (connection = await connectionBuilder(cancel))
            {
                var reader = new QueueManager(table, connection);
                await RunBatch(
                    reader,
                    messageFunc: (message, cancel) => connectionCallback!(connection, message, cancel),
                    persistFunc: () => connectionPersistRowVersion!(connection, startingRow, cancel),
                    cancel);
            }

            return;
        }

        SqlTransaction? transaction = null;
        try
        {
            transaction = await transactionBuilder!(cancel);
            connection = transaction.Connection;
            var reader = new QueueManager(table, transaction);
            try
            {
                await RunBatch(
                    reader,
                    messageFunc: (message, cancel) => transactionCallback!(transaction, message, cancel),
                    persistFunc: () => transactionPersistRowVersion!(transaction, startingRow, cancel),
                    cancel);
                    transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        finally
        {
#if NET48
            transaction?.Dispose();
#else
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
#endif

            connection?.Dispose();
        }
    }

    async Task RunBatch(QueueManager reader, Func<IncomingMessage, Cancel, Task> messageFunc, Func<Task> persistFunc, Cancel cancel)
    {
        while (true)
        {
            var result = await reader.Read(batchSize, startingRow, messageFunc, cancel);
            if (result.Count == 0)
            {
                break;
            }

            startingRow = result.LastRowVersion.GetValueOrDefault(0) + 1;
            await persistFunc();
            if (result.Count < batchSize)
            {
                break;
            }
        }
    }
}