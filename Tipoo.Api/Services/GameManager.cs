using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Taboo.Api.Models;

namespace Taboo.Api.Services;

public partial class GameManager : IGameManager
{
    private readonly ILogger<GameManager> _logger;

    public GameManager(ILogger<GameManager> logger)
    {
        _logger = logger;
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

    public bool CreateRoom(string roomCode, string connectionId, string userName)
    {
        var newRoom = new GameRoom
        {
            RoomCode = roomCode
        };

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
            _logger.LogInformation("Partida iniciada na sala {RoomCode} pelo host {ConnectionId}", roomCode, connectionId);
            return true;
        }
    }
}