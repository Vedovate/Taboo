namespace Tipoo.Api.Infrastructure;

public class ConnectionStringProvider
{
    public string ConnectionString { get; }

    public ConnectionStringProvider(string configuredConnectionString, string contentRootPath)
    {
        var fileName = configuredConnectionString.Replace("Data Source=", "");
        ConnectionString = $"Data Source={Path.Combine(contentRootPath, "Database", fileName)}";
    }
}
