#if SqlServerDedupe
namespace NServiceBus.Transport.SqlServerDeduplication;
#else
namespace NServiceBus.Transport.SqlServerNative;
#endif
/// <summary>
/// Represents a table and schema.
/// </summary>
public class Table
{
    /// <summary>
    /// Instantiates a new <see cref="Table"/>.
    /// <paramref name="tableName"/> and <paramref name="schema"/> should be non sanitized.
    /// </summary>
    public Table(string tableName, string schema = "dbo") :
        this(tableName, schema, true)
    {
    }

    /// <summary>
    /// Instantiates a new <see cref="Table"/>.
    /// </summary>
    public Table(string tableName, string schema, bool sanitize)
    {
        Guard.AgainstNullOrEmpty(tableName);
        Guard.AgainstNullOrEmpty(schema);
        TableName = tableName;
        Schema = schema;
        if (sanitize)
        {
            TableName = SqlExtensions.Sanitize(TableName);
            Schema = SqlExtensions.Sanitize(Schema);
        }

        FullTableName = $"{Schema}.{TableName}";
    }

    /// <summary>
    /// The sanitized table and schema name.
    /// </summary>
    public string FullTableName { get; }

    /// <summary>
    /// The sanitized table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// The sanitized schema name.
    /// </summary>
    public string Schema { get; }

    /// <summary>
    /// Converts a string into a <see cref="Table"/>.
    /// Assumes and un-sanitized table string with no schema.
    /// </summary>
    public static implicit operator Table(string table)
    {
        Guard.AgainstNullOrEmpty(table);
        return new(table);
    }

    /// <summary>
    /// Returns <see cref="FullTableName"/>.
    /// </summary>
    public override string ToString() => FullTableName;
}
