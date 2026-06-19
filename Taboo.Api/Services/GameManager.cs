// src/Taboo.Api/Services/GameManager.cs
using System.Collections.Concurrent;
using Taboo.Api.Models;

namespace Taboo.Api.Services;

public class GameManager : IGameManager
{
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

    public void AddPlayerToRoom(string roomCode, string connectionId, string userName)
    {
        var gameRoom = GetOrCreateRoom(roomCode);

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                gameRoom.Players.Add(new Player
                {
                    ConnectionId = connectionId,
                    Name = userName
                });
                return;
            }

            player.Name = userName;
        }
    }

    public void RemovePlayerFromRoom(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                return;
            }

            gameRoom.Players.Remove(player);

            if (gameRoom.Players.Count == 0)
            {
                GameRooms.TryRemove(roomCode, out _);
            }
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

    public string? GetRoomCodeByConnectionId(string connectionId)
    {
        foreach (var room in GameRooms)
        {
            if (room.Value.Players.Any(player => player.ConnectionId == connectionId))
            {
                return room.Key;
            }
        }

        return null;
    }
}