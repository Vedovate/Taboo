using System.Collections.Concurrent;
using Taboo.Api.Models;

namespace Taboo.Api.Services;

public class GameManager : IGameManager
{
    private readonly ILogger<GameManager> _logger;

    public GameManager(ILogger<GameManager> logger)
    {
        _logger = logger;
    }
    public ConcurrentDictionary<string, GameRoom> GameRooms { get; } = new();

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
                gameRoom.Players.Add(new Player
                {
                    ConnectionId = connectionId,
                    Name = connectionId
                });
                _logger.LogInformation("Jogador {ConnectionId} entrou na sala {RoomCode}", connectionId, roomCode);
                return;
            }

            player.Name = connectionId;
            _logger.LogInformation("Jogador {ConnectionId} reconectou na sala {RoomCode}", connectionId, roomCode);
        }
    }

    public string? RenamePlayer(string roomCode, string connectionId, string newName)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de renomear jogador em sala inexistente {RoomCode}", roomCode);
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

            if (gameRoom.Players.Any(p => p.ConnectionId != connectionId && p.Name == newName))
            {
                _logger.LogWarning("Nome {NewName} já está em uso na sala {RoomCode}", newName, roomCode);
                return null;
            }

            player.Name = newName;
            _logger.LogInformation("Jogador {ConnectionId} renomeado para {NewName} na sala {RoomCode}", connectionId, newName, roomCode);
            return newName;
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
            gameRoom.Players.Remove(player);
            _logger.LogInformation("Jogador {UserName} ({ConnectionId}) saiu da sala {RoomCode}", userName, connectionId, roomCode);

            if (gameRoom.Players.Count == 0)
            {
                GameRooms.TryRemove(roomCode, out _);
                _logger.LogInformation("Sala {RoomCode} removida — sem jogadores restantes", roomCode);
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
}