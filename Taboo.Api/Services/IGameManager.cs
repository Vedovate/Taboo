using Taboo.Api.Models;

namespace Taboo.Api.Services;

public interface IGameManager
{
    bool CreateRoom(string roomCode, string connectionId, string userName);

    void AddPlayerToRoom(string roomCode, string connectionId, string userName);

    IReadOnlyList<Player> GetPlayersInRoom(string roomCode);

    bool IsHost(string roomCode, string connectionId);

    void RemovePlayerFromRoom(string roomCode, string connectionId);

    bool IsPlayerInRoom(string roomCode, string connectionId);

    bool RoomExists(string roomCode);

    GameRoom? GetRoom(string roomCode);

    string? TryGetPlayerName(string roomCode, string connectionId);

    string? GetRoomCodeByConnectionId(string connectionId);

    string? RenamePlayer(string roomCode, string connectionId, string newName);
}
