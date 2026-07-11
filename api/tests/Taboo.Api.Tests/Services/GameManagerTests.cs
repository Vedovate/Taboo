using Microsoft.Extensions.Logging.Abstractions;
using Taboo.Api.Services;

namespace Taboo.Api.Tests.Services;

public class GameManagerTests
{
    private readonly GameManager _sut;

    public GameManagerTests()
    {
        _sut = new GameManager(NullLogger<GameManager>.Instance);
    }

    [Fact]
    public void CreateRoom_WithNewCode_ReturnsTrue()
    {
        var result = _sut.CreateRoom("ABC12", "conn1", "Player1");

        Assert.True(result);
    }

    [Fact]
    public void CreateRoom_WithNewCode_AddsHostPlayer()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.Equal("Player1", players[0].Name);
        Assert.Equal("conn1", players[0].ConnectionId);
        Assert.True(players[0].IsHost);
    }

    [Fact]
    public void CreateRoom_WithDuplicateCode_ReturnsFalse()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var result = _sut.CreateRoom("ABC12", "conn2", "Player2");

        Assert.False(result);
    }

    [Fact]
    public void AddPlayerToRoom_ExistingRoom_AddsPlayer()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Equal(2, players.Count);
        Assert.Contains(players, p => p.Name == "conn2" && p.ConnectionId == "conn2");
    }

    [Fact]
    public void AddPlayerToRoom_NonExistentRoom_DoesNothing()
    {
        _sut.AddPlayerToRoom("NONEXIST", "conn1", "Player1");

        var players = _sut.GetPlayersInRoom("NONEXIST");
        Assert.Empty(players);
    }

    [Fact]
    public void AddPlayerToRoom_ReconnectingPlayer_UpdatesName()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2Renamed");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Equal(2, players.Count);
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("conn2", player.Name);
    }

    [Fact]
    public void RemovePlayerFromRoom_ExistingPlayer_RemovesPlayer()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.RemovePlayerFromRoom("ABC12", "conn1");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.DoesNotContain(players, p => p.ConnectionId == "conn1");
    }

    [Fact]
    public void RemovePlayerFromRoom_LastPlayer_RemovesRoom()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.RemovePlayerFromRoom("ABC12", "conn1");

        Assert.False(_sut.RoomExists("ABC12"));
        Assert.Empty(_sut.GetPlayersInRoom("ABC12"));
    }

    [Fact]
    public void RemovePlayerFromRoom_NonExistentRoom_DoesNothing()
    {
        _sut.RemovePlayerFromRoom("NONEXIST", "conn1");

        Assert.False(_sut.RoomExists("NONEXIST"));
    }

    [Fact]
    public void GetPlayersInRoom_ExistingRoom_ReturnsPlayerList()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        var players = _sut.GetPlayersInRoom("ABC12");

        Assert.Equal(2, players.Count);
    }

    [Fact]
    public void GetPlayersInRoom_NonExistentRoom_ReturnsEmpty()
    {
        var players = _sut.GetPlayersInRoom("NONEXIST");

        Assert.Empty(players);
    }

    [Fact]
    public void IsPlayerInRoom_PlayerExists_ReturnsTrue()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.IsPlayerInRoom("ABC12", "conn1");

        Assert.True(result);
    }

    [Fact]
    public void IsPlayerInRoom_PlayerNotInRoom_ReturnsFalse()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.IsPlayerInRoom("ABC12", "nonExistentConn");

        Assert.False(result);
    }

    [Fact]
    public void IsPlayerInRoom_NonExistentRoom_ReturnsFalse()
    {
        var result = _sut.IsPlayerInRoom("NONEXIST", "conn1");

        Assert.False(result);
    }

    [Fact]
    public void RoomExists_ExistingRoom_ReturnsTrue()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RoomExists("ABC12");

        Assert.True(result);
    }

    [Fact]
    public void RoomExists_NonExistentRoom_ReturnsFalse()
    {
        var result = _sut.RoomExists("NONEXIST");

        Assert.False(result);
    }

    [Fact]
    public void TryGetPlayerName_PlayerExists_ReturnsName()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var name = _sut.TryGetPlayerName("ABC12", "conn1");

        Assert.Equal("Player1", name);
    }

    [Fact]
    public void TryGetPlayerName_NonExistentPlayer_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var name = _sut.TryGetPlayerName("ABC12", "nonExistentConn");

        Assert.Null(name);
    }

    [Fact]
    public void TryGetPlayerName_NonExistentRoom_ReturnsNull()
    {
        var name = _sut.TryGetPlayerName("NONEXIST", "conn1");

        Assert.Null(name);
    }

    [Fact]
    public void GetRoomCodeByConnectionId_PlayerFound_ReturnsRoomCode()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var roomCode = _sut.GetRoomCodeByConnectionId("conn1");

        Assert.Equal("ABC12", roomCode);
    }

    [Fact]
    public void GetRoomCodeByConnectionId_PlayerNotFound_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var roomCode = _sut.GetRoomCodeByConnectionId("nonExistentConn");

        Assert.Null(roomCode);
    }

    [Fact]
    public void GetRoomCodeByConnectionId_AcrossMultipleRooms_ReturnsCorrectRoom()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.CreateRoom("XYZ99", "conn2", "Player2");

        var roomCode = _sut.GetRoomCodeByConnectionId("conn2");

        Assert.Equal("XYZ99", roomCode);
    }

    [Fact]
    public void RenamePlayer_ExistingPlayer_UpdatesName()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "conn2");

        var result = _sut.RenamePlayer("ABC12", "conn2", "Player2");

        Assert.Equal("Player2", result);
        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("Player2", player.Name);
    }

    [Fact]
    public void RenamePlayer_DuplicateName_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "conn2");

        var result = _sut.RenamePlayer("ABC12", "conn2", "Player1");

        Assert.Null(result);
        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("conn2", player.Name);
    }

    [Fact]
    public void RenamePlayer_NonExistentPlayer_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RenamePlayer("ABC12", "nonExistent", "Player2");

        Assert.Null(result);
    }

    [Fact]
    public void RenamePlayer_NonExistentRoom_ReturnsNull()
    {
        var result = _sut.RenamePlayer("NONEXIST", "conn1", "Player1");

        Assert.Null(result);
    }

    [Fact]
    public void IsHost_HostPlayer_ReturnsTrue()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.IsHost("ABC12", "conn1");

        Assert.True(result);
    }

    [Fact]
    public void IsHost_NonHostPlayer_ReturnsFalse()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        var result = _sut.IsHost("ABC12", "conn2");

        Assert.False(result);
    }

    [Fact]
    public void IsHost_NonExistentPlayer_ReturnsFalse()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.IsHost("ABC12", "nonExistentConn");

        Assert.False(result);
    }

    [Fact]
    public void IsHost_NonExistentRoom_ReturnsFalse()
    {
        var result = _sut.IsHost("NONEXIST", "conn1");

        Assert.False(result);
    }

    [Fact]
    public void AddPlayerToRoom_ConnectionIdIsName()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "AnyName");

        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("conn2", player.Name);
    }

    [Fact]
    public void GetRoom_ExistingRoom_ReturnsRoom()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var room = _sut.GetRoom("ABC12");

        Assert.NotNull(room);
        Assert.Equal("ABC12", room.RoomCode);
        Assert.Single(room.Players);
    }

    [Fact]
    public void GetRoom_NonExistentRoom_ReturnsNull()
    {
        var room = _sut.GetRoom("NONEXIST");

        Assert.Null(room);
    }

    [Fact]
    public void GetRoom_MaxPlayers_DefaultsTo3()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var room = _sut.GetRoom("ABC12");

        Assert.NotNull(room);
        Assert.Equal(3, room.MaxPlayers);
    }

    [Fact]
    public void MultipleRooms_AreIndependent()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.CreateRoom("XYZ99", "conn2", "Player2");

        var room1Players = _sut.GetPlayersInRoom("ABC12");
        var room2Players = _sut.GetPlayersInRoom("XYZ99");

        Assert.Single(room1Players);
        Assert.Equal("Player1", room1Players[0].Name);
        Assert.Single(room2Players);
        Assert.Equal("Player2", room2Players[0].Name);
    }

    [Fact]
    public void RemovePlayerFromRoom_ExistingPlayer_DoesNotRemoveRoomIfPlayersRemain()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "ignored");
        _sut.RemovePlayerFromRoom("ABC12", "conn1");

        Assert.True(_sut.RoomExists("ABC12"));
        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.Equal("conn2", players[0].ConnectionId);
    }
}
