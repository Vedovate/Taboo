using System.Reflection;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Taboo.Api.Database;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Taboo.Api.Database.init.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' não encontrado.");
        using var reader = new StreamReader(stream);
        var sqlScript = reader.ReadToEnd();

        connection.Execute(sqlScript);
    }
}