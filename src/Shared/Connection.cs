using Microsoft.Data.SqlClient;

public static class Connection
{
    public static string ConnectionString;

    static Connection()
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Max Pool Size=100;TrustServerCertificate=True";
            return;
        }

        ConnectionString = @"Server=.\;Database=NServiceBusNativeTests; Integrated Security=True;Max Pool Size=100;TrustServerCertificate=True";
    }

    public static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static SqlConnection OpenConnectionFromNewClient()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static async Task<SqlConnection> OpenAsyncConnection(Cancel cancel = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancel);
        return connection;
    }
}