using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tipoo.Api.Data;
using Tipoo.Api.Models;

namespace Tipoo.Api.Services;

public partial class GameManager : IGameManager
{
    private readonly ILogger<GameManager> _logger;
    private readonly IGameDataStore _dataStore;

    public GameManager(ILogger<GameManager> logger, IGameDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;
    }
    public ConcurrentDictionary<string, GameRoom> GameRooms { get; } = new();

    private static readonly Regex ControlCharsRegex = MyRegex();

    [GeneratedRegex(@"[\u0000-\u001F\u007F\u200B-\u200D\uFEFF\u2060]")]
    private static partial Regex MyRegex();

    public GameRoom GetOrCreateRoom(string roomCode)
    {
        return GameRooms.GetOrAdd(roomCode, code => new GameRoom
        {
            RoomCode = code
        });
    }

    public bool RoomExists(string roomCode)
    {
        return GameRooms.ContainsKey(roomCode);
    }

    public bool CreateRoom(string roomCode, string connectionId, string userName, string hostSessionId = "")
    {
        var newRoom = new GameRoom
        {
            RoomCode = roomCode,
            HostSessionId = hostSessionId
        };

        if (!string.IsNullOrEmpty(hostSessionId))
        {
            try
            {
                var cachedSettings = _dataStore.LoadHostSettings(hostSessionId);
                if (cachedSettings is not null)
                {
                    newRoom.Settings = cachedSettings;
                    _logger.LogInformation("Sala {RoomCode} criada com configurações em cache do host {HostSessionId}", roomCode, hostSessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao carregar configurações em cache do host {HostSessionId}", hostSessionId);
            }
        }

        if (!GameRooms.TryAdd(roomCode, newRoom))
        {
            _logger.LogWarning("Falha ao criar sala {RoomCode} — já existe", roomCode);
            return false;
        }

        lock (newRoom)
        {
            newRoom.Players.Add(new Player
            {
                ConnectionId = connectionId,
                Name = userName,
                IsHost = true
            });
        }

        _logger.LogInformation("Sala {RoomCode} criada por {UserName} ({ConnectionId})", roomCode, userName, connectionId);
        return true;
    }

    public void AddPlayerToRoom(string roomCode, string connectionId, string userName)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de adicionar jogador a sala inexistente {RoomCode}", roomCode);
            return;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                var finalName = userName;
                if (gameRoom.Players.Any(p => p.Name == finalName))
                {
                    finalName = GetFallbackName(gameRoom);
                }

                gameRoom.Players.Add(new Player
                {
                    ConnectionId = connectionId,
                    Name = finalName
                });
                _logger.LogInformation("Jogador {ConnectionId} entrou na sala {RoomCode}", connectionId, roomCode);
                return;
            }

            var reconnectName = userName;
            if (gameRoom.Players.Any(p => p.ConnectionId != connectionId && p.Name == reconnectName))
            {
                reconnectName = GetFallbackName(gameRoom);
            }
            player.Name = reconnectName;
            _logger.LogInformation("Jogador {ConnectionId} reconectou na sala {RoomCode}", connectionId, roomCode);
        }
    }

    private static string GetFallbackName(GameRoom gameRoom)
    {
        var counter = gameRoom.Players.Count + 1;
        while (gameRoom.Players.Any(p => p.Name == $"Jogador {counter}"))
        {
            counter++;
        }
        return $"Jogador {counter}";
    }

    public string? RenamePlayer(string roomCode, string connectionId, string newName)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de renomear jogador em sala inexistente {RoomCode}", roomCode);
            return null;
        }

        var sanitized = ControlCharsRegex.Replace(newName.Trim(), "");

        if (sanitized.Length < 1 || sanitized.Length > 16)
        {
            _logger.LogWarning("Nome inválido '{NewName}' (sanitizado: '{Sanitized}') — fora do tamanho permitido", newName, sanitized);
            return null;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("Jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return null;
            }

            if (gameRoom.Players.Any(p => p.ConnectionId != connectionId && p.Name == sanitized))
            {
                _logger.LogWarning("Nome {NewName} já está em uso na sala {RoomCode}", sanitized, roomCode);
                return null;
            }

            player.Name = sanitized;
            _logger.LogInformation("Jogador {ConnectionId} renomeado para {NewName} na sala {RoomCode}", connectionId, sanitized, roomCode);
            return sanitized;
        }
    }

    public IReadOnlyList<Player> GetPlayersInRoom(string roomCode)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return Array.Empty<Player>();
        }

        lock (gameRoom)
        {
            return gameRoom.Players.ToList();
        }
    }

    public void RemovePlayerFromRoom(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de remover jogador de sala inexistente {RoomCode}", roomCode);
            return;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("Jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return;
            }

            var userName = player.Name;
            var eraHost = player.IsHost;
            gameRoom.Players.Remove(player);
            _logger.LogInformation("Jogador {UserName} ({ConnectionId}) saiu da sala {RoomCode}", userName, connectionId, roomCode);

            if (gameRoom.Players.Count == 0)
            {
                GameRooms.TryRemove(roomCode, out _);
                _logger.LogInformation("Sala {RoomCode} removida — sem jogadores restantes", roomCode);
            }
            else if (eraHost)
            {
                var novoHost = gameRoom.Players[0];
                novoHost.IsHost = true;
                _logger.LogInformation("Host transferido para {ConnectionId} ({UserName}) na sala {RoomCode}", novoHost.ConnectionId, novoHost.Name, roomCode);
            }
        }
    }

    public bool IsHost(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return false;
        }

        lock (gameRoom)
        {
            return gameRoom.Players.Any(item => item.ConnectionId == connectionId && item.IsHost);
        }
    }

    public bool IsPlayerInRoom(string roomCode, string connectionId)
    {
        return GameRooms.TryGetValue(roomCode, out var gameRoom) && gameRoom.Players.Any(item => item.ConnectionId == connectionId);
    }

    public string? TryGetPlayerName(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            return gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId)?.Name;
        }
    }

    public GameRoom? GetRoom(string roomCode)
    {
        return GameRooms.TryGetValue(roomCode, out var gameRoom) ? gameRoom : null;
    }

    public string? GetRoomCodeByConnectionId(string connectionId)
    {
        foreach (var room in GameRooms)
        {
            if (room.Value.Players.Any(player => player.ConnectionId == connectionId))
            {
                return room.Key;
            }
        }

        _logger.LogDebug("Nenhuma sala encontrada para conexão {ConnectionId}", connectionId);
        return null;
    }

    public bool EscolherTime(string roomCode, string connectionId, string cor)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("EscolherTime: sala {RoomCode} não encontrada", roomCode);
            return false;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("EscolherTime: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            if (player.Team == cor)
            {
                player.Team = string.Empty;
                player.IsReady = false;
                _logger.LogInformation("Jogador {ConnectionId} saiu do time {Cor} na sala {RoomCode}", connectionId, cor, roomCode);
                return true;
            }

            var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
            var teamCount = gameRoom.Players.Count(p => p.Team == cor);
            if (teamCount >= maxPerTeam)
            {
                _logger.LogWarning("EscolherTime: time {Cor} cheio ({Count}/{Max}) na sala {RoomCode}", cor, teamCount, maxPerTeam, roomCode);
                return false;
            }

            player.Team = cor;
            player.IsReady = false;
            _logger.LogInformation("Jogador {ConnectionId} entrou no time {Cor} na sala {RoomCode}", connectionId, cor, roomCode);
            return true;
        }
    }

    public bool AlternarPronto(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("AlternarPronto: sala {RoomCode} não encontrada", roomCode);
            return false;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("AlternarPronto: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            if (string.IsNullOrEmpty(player.Team))
            {
                _logger.LogWarning("AlternarPronto: jogador {ConnectionId} não está em nenhum time na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            player.IsReady = !player.IsReady;
            _logger.LogInformation("Jogador {ConnectionId} alternou pronto para {Pronto} na sala {RoomCode}", connectionId, player.IsReady, roomCode);
            return player.IsReady;
        }
    }

    public string? RandomizarTime(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("RandomizarTime: sala {RoomCode} não encontrada", roomCode);
            return null;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("RandomizarTime: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return null;
            }

            var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
            var redCount = gameRoom.Players.Count(p => p.Team == "Vermelho" && p.ConnectionId != connectionId);
            var blueCount = gameRoom.Players.Count(p => p.Team == "Azul" && p.ConnectionId != connectionId);

            var available = new List<string>();
            if (redCount < maxPerTeam) available.Add("Vermelho");
            if (blueCount < maxPerTeam) available.Add("Azul");

            if (available.Count == 0)
            {
                _logger.LogWarning("RandomizarTime: nenhum time disponível na sala {RoomCode}", roomCode);
                return null;
            }

            var chosen = available[Random.Shared.Next(available.Count)];
            player.Team = chosen;
            player.IsReady = false;
            _logger.LogInformation("Jogador {ConnectionId} foi para o time {Cor} (aleatório) na sala {RoomCode}", connectionId, chosen, roomCode);
            return chosen;
        }
    }

    public bool ForcarIniciar(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("ForcarIniciar: sala {RoomCode} não encontrada", roomCode);
            return false;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("ForcarIniciar: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            if (!player.IsHost)
            {
                _logger.LogWarning("ForcarIniciar: jogador {ConnectionId} não é host na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            if (string.IsNullOrEmpty(player.Team))
            {
                _logger.LogInformation("ForcarIniciar: host {ConnectionId} sem time — alocando aleatoriamente na sala {RoomCode}", connectionId, roomCode);
                var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
                var redCount = gameRoom.Players.Count(p => p.Team == "Vermelho");
                var blueCount = gameRoom.Players.Count(p => p.Team == "Azul");
                var available = new List<string>();
                if (redCount < maxPerTeam) available.Add("Vermelho");
                if (blueCount < maxPerTeam) available.Add("Azul");
                player.Team = available[Random.Shared.Next(available.Count)];
                player.IsReady = false;
            }

            var unassigned = gameRoom.Players.Where(p => string.IsNullOrEmpty(p.Team)).ToList();
            foreach (var unassignedPlayer in unassigned)
            {
                var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
                var redCount = gameRoom.Players.Count(p => p.Team == "Vermelho");
                var blueCount = gameRoom.Players.Count(p => p.Team == "Azul");

                if (redCount <= blueCount && redCount < maxPerTeam)
                {
                    unassignedPlayer.Team = "Vermelho";
                }
                else
                {
                    unassignedPlayer.Team = "Azul";
                }
                unassignedPlayer.IsReady = false;
            }

            gameRoom.IsActive = true;
            RegistrarInicioDePartida(gameRoom);
            _logger.LogInformation("Partida iniciada na sala {RoomCode} pelo host {ConnectionId}", roomCode, connectionId);
            return true;
        }
    }

    private void RegistrarInicioDePartida(GameRoom gameRoom)
    {
        if (gameRoom.MatchKey is not null)
        {
            return;
        }

        try
        {
            var startedAt = DateTime.UtcNow;
            gameRoom.StartedAt = startedAt;
            gameRoom.MatchKey = $"{gameRoom.RoomCode}-{startedAt:yyyyMMddTHHmmssfff}Z";

            _dataStore.CreateMatch(new GameMatch
            {
                MatchKey = gameRoom.MatchKey,
                RoomCode = gameRoom.RoomCode,
                HostSessionId = gameRoom.HostSessionId,
                StartedAt = startedAt,
                SettingsJson = JsonSerializer.Serialize(gameRoom.Settings),
                StartedPlayers = gameRoom.Players.Count,
                WasStarted = true,
                Completed = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar início da partida na sala {RoomCode}", gameRoom.RoomCode);
        }
    }

    public GameSettings? ConfigurarPartida(string roomCode, string connectionId, GameSettings settings)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("ConfigurarPartida: sala {RoomCode} não encontrada", roomCode);
            return null;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null || !player.IsHost)
            {
                _logger.LogWarning("ConfigurarPartida: jogador {ConnectionId} não é host na sala {RoomCode}", connectionId, roomCode);
                return null;
            }

            if (!TryNormalizarSettings(settings, out var normalized))
            {
                _logger.LogWarning("ConfigurarPartida: configurações inválidas na sala {RoomCode}", roomCode);
                return null;
            }

            gameRoom.Settings = normalized;

            if (!string.IsNullOrEmpty(gameRoom.HostSessionId))
            {
                _dataStore.SaveHostSettings(gameRoom.HostSessionId, normalized);
            }

            _logger.LogInformation("Configurações atualizadas na sala {RoomCode} pelo host {ConnectionId}", roomCode, connectionId);
            return normalized;
        }
    }

    public GameSettings ObterConfiguracoes(string roomCode)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("ObterConfiguracoes: sala {RoomCode} não encontrada", roomCode);
            return new GameSettings();
        }

        lock (gameRoom)
        {
            return gameRoom.Settings;
        }
    }

    private static bool TryNormalizarSettings(GameSettings settings, out GameSettings normalized)
    {
        normalized = settings.Clone();

        normalized.RoundTimeSeconds = Math.Clamp(settings.RoundTimeSeconds, GameSettings.MinRoundTimeSeconds, GameSettings.MaxRoundTimeSeconds);
        normalized.ExplanationTimeSeconds = Math.Clamp(settings.ExplanationTimeSeconds, 0, GameSettings.MaxExplanationTimeSeconds);
        normalized.SkipLimit = Math.Clamp(settings.SkipLimit, 0, GameSettings.MaxSkipLimit);
        normalized.PointsPerCorrect = Math.Clamp(settings.PointsPerCorrect, 0, GameSettings.MaxPoints);
        normalized.PointsPerError = Math.Clamp(settings.PointsPerError, 0, GameSettings.MaxPoints);
        normalized.PointsPerSkip = Math.Clamp(settings.PointsPerSkip, 0, GameSettings.MaxPoints);
        normalized.PauseBetweenRoundsSeconds = Math.Clamp(settings.PauseBetweenRoundsSeconds, GameSettings.MinPauseBetweenRoundsSeconds, GameSettings.MaxPauseBetweenRoundsSeconds);

        if (settings.NumberOfRounds < GameSettings.MinNumberOfRounds
            || settings.NumberOfRounds > GameSettings.MaxNumberOfRounds
            || settings.NumberOfRounds % 2 != 0)
        {
            return false;
        }

        if (settings.TipooLeadLimit.HasValue && settings.TipooLeadLimit.Value < GameSettings.MinTipooLeadLimit)
        {
            return false;
        }

        if (settings.Difficulties.Count == 0)
        {
            return false;
        }

        if (settings.StartingTeam is not ("azul" or "vermelho" or "aleatorio"))
        {
            return false;
        }

        if (settings.TiebreakMode is not ("empatado" or "rodada-extra"))
        {
            return false;
        }

        normalized.Difficulties = settings.Difficulties.Distinct().ToList();
        normalized.BuzzerSounds = settings.BuzzerSounds.Distinct().ToList();
        return true;
    }
}