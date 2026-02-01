namespace NServiceBus.Transport.SqlServerNative;

public class MessageConsumingLoop :
    MessageLoop
{
    string table;
    Func<Cancel, Task<SqlConnection>>? connectionBuilder;
    Func<Cancel, Task<SqlTransaction>>? transactionBuilder;
    Func<SqlTransaction, IncomingMessage, Cancel, Task>? transactionCallback;
    Func<SqlConnection, IncomingMessage, Cancel, Task>? connectionCallback;
    int batchSize;

    public MessageConsumingLoop(
        string table,
        Func<Cancel, Task<SqlTransaction>> transactionBuilder,
        Func<SqlTransaction, IncomingMessage, Cancel, Task> callback,
        Action<Exception> errorCallback,
        int batchSize = 10,
        TimeSpan? delay = null) :
        base(errorCallback, delay)
    {
        Guard.AgainstNullOrEmpty(table);
        Guard.AgainstNegativeAndZero(batchSize);
        this.table = table;
        transactionCallback = callback.WrapFunc(nameof(transactionCallback));
        this.transactionBuilder = transactionBuilder.WrapFunc(nameof(transactionBuilder));
        this.batchSize = batchSize;
    }

    public MessageConsumingLoop(
        string table,
        Func<Cancel, Task<SqlConnection>> connectionBuilder,
        Func<SqlConnection, IncomingMessage, Cancel, Task> callback,
        Action<Exception> errorCallback,
        int batchSize = 10,
        TimeSpan? delay = null) :
        base(errorCallback, delay)
    {
        Guard.AgainstNullOrEmpty(table);
        Guard.AgainstNegativeAndZero(batchSize);
        connectionCallback = callback.WrapFunc(nameof(connectionCallback));
        this.table = table;
        this.connectionBuilder = connectionBuilder.WrapFunc(nameof(this.connectionBuilder));
        this.batchSize = batchSize;
    }

    protected override async Task RunBatch(Cancel cancel)
    {
        SqlConnection? connection = null;
        if (connectionBuilder != null)
        {
            using (connection = await connectionBuilder(cancel))
            {
                var consumer = new QueueManager(table, connection);
                await RunBatch(consumer, (message, cancel) => connectionCallback!(connection, message, cancel), cancel);
            }

            return;
        }
        SqlTransaction? transaction = null;
        try
        {
            transaction = await transactionBuilder!(cancel);
            connection = transaction.Connection;
            var consumer = new QueueManager(table, transaction);
            try
            {
                await RunBatch(consumer, (message, cancel) => transactionCallback!(transaction, message, cancel), cancel);

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

    async Task RunBatch(QueueManager consumer, Func<IncomingMessage, Cancel, Task> action, Cancel cancel)
    {
        while (true)
        {
            var result = await consumer.Consume(batchSize, action, cancel);
            if (result.Count < batchSize)
            {
                break;
            }
        }
    }
}