// src/Taboo.Api/Services/GameManager.cs
using System.Collections.Concurrent;
using Taboo.Api.Models;

namespace Taboo.Api.Services
{
    public class GameManager : IGameManager
    {
        // ConcurrentDictionary é thread-safe e ideal para armazenar o estado das salas em memória.
        public ConcurrentDictionary<string, GameRoom> GameRooms { get; } = new ConcurrentDictionary<string, GameRoom>();

        public void AddPlayerToRoom(string roomCode, string connectionId, string userName)
        {
            // Tenta adicionar a sala se ela não existir. Se já existir, obtém a existente.
            var gameRoom = GameRooms.GetOrAdd(roomCode, _ => new GameRoom { RoomCode = roomCode });

            // Garante thread-safety ao modificar a lista de jogadores da sala.
            lock (gameRoom.Players)
            {
                // Verifica se o jogador já está na sala para evitar duplicatas.
                if (!gameRoom.Players.Any(p => p.ConnectionId == connectionId))
                {
                    gameRoom.Players.Add(new Player { ConnectionId = connectionId, Name = userName });
                    Console.WriteLine($"Player {userName} ({connectionId}) added to room {roomCode}.");
                }
            }
        }

        public void RemovePlayerFromRoom(string roomCode, string connectionId)
        {
            if (GameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                lock (gameRoom.Players)
                {
                    var playerToRemove = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                    if (playerToRemove != null)
                    {
                        gameRoom.Players.Remove(playerToRemove);
                        Console.WriteLine($"Player {playerToRemove.Name} ({connectionId}) removed from room {roomCode}.");

                        // Opcional: Se a sala ficar vazia, você pode removê-la do dicionário.
                        // Isso depende da lógica de negócio. Por enquanto, manteremos a sala.
                        // if (!gameRoom.Players.Any())
                        // {
                        //     GameRooms.TryRemove(roomCode, out _);
                        //     Console.WriteLine($"Room {roomCode} is empty and removed.");
                        // }
                    }
                }
            }
        }

        public bool IsPlayerInRoom(string roomCode, string connectionId)
        {
            if (GameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                lock (gameRoom.Players)
                {
                    return gameRoom.Players.Any(p => p.ConnectionId == connectionId);
                }
            }
            return false;
        }
    }
}