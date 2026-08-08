using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Tipoo.Api.Infrastructure;
using Tipoo.Api.Models;

namespace Tipoo.Api.Data;

public class GameDataStore : IGameDataStore
{
    private readonly string _connectionString;
    private readonly ILogger<GameDataStore> _logger;

    public GameDataStore(ConnectionStringProvider connectionStringProvider, ILogger<GameDataStore> logger)
    {
        _connectionString = connectionStringProvider.ConnectionString;
        _logger = logger;
    }

    public IReadOnlyList<Card> GetAllCards()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cards = connection.Query<Card>(
                "SELECT Id, MainWord, Forbidden1, Forbidden2, Forbidden3, Forbidden4, Forbidden5, Difficulty, Category FROM Cards");
            return cards.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar cartas do banco");
            return Array.Empty<Card>();
        }
    }

    public GameSettings? LoadHostSettings(string hostSessionId)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var json = connection.QueryFirstOrDefault<string>(
                "SELECT SettingsJson FROM GameHostSettings WHERE HostSessionId = @HostSessionId",
                new { HostSessionId = hostSessionId });

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<GameSettings>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar configurações do host {HostSessionId}", hostSessionId);
            return null;
        }
    }

    public void SaveHostSettings(string hostSessionId, GameSettings settings)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var json = JsonSerializer.Serialize(settings);
            connection.Execute(
                @"INSERT INTO GameHostSettings (HostSessionId, SettingsJson, UpdatedAt)
                  VALUES (@HostSessionId, @SettingsJson, CURRENT_TIMESTAMP)
                  ON CONFLICT(HostSessionId) DO UPDATE SET SettingsJson = excluded.SettingsJson, UpdatedAt = CURRENT_TIMESTAMP",
                new { HostSessionId = hostSessionId, SettingsJson = json });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao salvar configurações do host {HostSessionId}", hostSessionId);
        }
    }

    public void CreateMatch(GameMatch match)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            connection.Execute(
                @"INSERT INTO Matches (MatchKey, RoomCode, HostSessionId, StartedAt, SettingsJson, StartedPlayers, WasStarted, Completed)
                  VALUES (@MatchKey, @RoomCode, @HostSessionId, @StartedAt, @SettingsJson, @StartedPlayers, @WasStarted, @Completed)",
                new
                {
                    match.MatchKey,
                    match.RoomCode,
                    match.HostSessionId,
                    match.StartedAt,
                    match.SettingsJson,
                    match.StartedPlayers,
                    match.WasStarted,
                    match.Completed
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar início da partida {MatchKey}", match.MatchKey);
        }
    }

    public GameMatch? GetMatch(string matchKey)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var match = connection.QueryFirstOrDefault<GameMatch>(
                @"SELECT Id, MatchKey, RoomCode, HostSessionId, StartedAt, SettingsJson, StartedPlayers,
                         WasStarted, Completed, EndedAt, FinishedPlayers, FinalScoreRed, FinalScoreBlue, WinnerTeam
                  FROM Matches WHERE MatchKey = @MatchKey",
                new { MatchKey = matchKey });
            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar partida {MatchKey}", matchKey);
            return null;
        }
    }
}
