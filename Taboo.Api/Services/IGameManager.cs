using Taboo.Api.Models;

namespace Taboo.Api.Services;

public interface IGameManager
{
    bool CreateRoom(string roomCode, string connectionId, string userName);

    void AddPlayerToRoom(string roomCode, string connectionId, string userName);

    IReadOnlyList<Player> GetPlayersInRoom(string roomCode);

    void RemovePlayerFromRoom(string roomCode, string connectionId);

    bool IsPlayerInRoom(string roomCode, string connectionId);

    bool RoomExists(string roomCode);

    string? TryGetPlayerName(string roomCode, string connectionId);

    string? GetRoomCodeByConnectionId(string connectionId);
}
