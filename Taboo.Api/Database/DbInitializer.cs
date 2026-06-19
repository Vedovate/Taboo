using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Taboo.Api.Database;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        // Abre a conexão com o SQLite. 
        // O driver 'Microsoft.Data.Sqlite' cria o arquivo automaticamente se ele não existir.
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Caminho do seu script SQL
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Database", "init.sql");
        
        // Se por acaso não achar na pasta bin, tenta buscar na raiz do projeto
        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "init.sql");
        }

        if (File.Exists(scriptPath))
        {
            var sqlScript = File.ReadAllText(scriptPath);
            
            // O Dapper executa todo o script de criação e inserção das cartas de uma vez só
            connection.Execute(sqlScript);
        }
        else
        {
            throw new FileNotFoundException($"Não foi possível encontrar o script de inicialização em: {scriptPath}");
        }
    }
}