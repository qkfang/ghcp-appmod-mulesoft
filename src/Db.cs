using MySqlConnector;

namespace BookMyShow.Functions;

internal static class Db
{
    public static MySqlConnection CreateConnection()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST")
            ?? throw new InvalidOperationException("DB_HOST is not set");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
        var user = Environment.GetEnvironmentVariable("DB_USER")
            ?? throw new InvalidOperationException("DB_USER is not set");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? throw new InvalidOperationException("DB_PASSWORD is not set");
        var database = Environment.GetEnvironmentVariable("DB_NAME")
            ?? throw new InvalidOperationException("DB_NAME is not set");

        var builder = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = uint.Parse(port),
            UserID = user,
            Password = password,
            Database = database,
            SslMode = MySqlSslMode.Required,
        };
        return new MySqlConnection(builder.ConnectionString);
    }
}
