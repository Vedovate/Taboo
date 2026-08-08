using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tipoo.Api.Data;
using Tipoo.Api.Models;
using Tipoo.Api.Services;

namespace Tipoo.Api.Tests.Services;

public class GameManagerTests
{
    private readonly GameManager _sut;
    private readonly Mock<IGameDataStore> _dataStore;

    public GameManagerTests()
    {
        _dataStore = new Mock<IGameDataStore>();
        _sut = new GameManager(NullLogger<GameManager>.Instance, _dataStore.Object);
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
        Assert.Contains(players, p => p.Name == "Player2" && p.ConnectionId == "conn2");
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
        Assert.Equal("Player2Renamed", player.Name);
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
    public void RenamePlayer_NameTooLong_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RenamePlayer("ABC12", "conn1", "Nome muito longo aqui");

        Assert.Null(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Player1", player.Name);
    }

    [Fact]
    public void RenamePlayer_NameAfterSanitizeTooLong_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RenamePlayer("ABC12", "conn1", "Nome\u0000Muito\u200BLongo\uFEFFAqui");

        Assert.Null(result);
    }

    [Fact]
    public void RenamePlayer_ControlChars_RemovesControlChars()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RenamePlayer("ABC12", "conn1", "Play\u0000\u0001er1");

        Assert.Equal("Player1", result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Player1", player.Name);
    }

    [Fact]
    public void RenamePlayer_ZeroWidthChars_RemovesZeroWidth()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RenamePlayer("ABC12", "conn1", "Play\u200B\u200Cer1");

        Assert.Equal("Player1", result);
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
    public void AddPlayerToRoom_UsesUserName()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "AnyName");

        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("AnyName", player.Name);
    }

    [Fact]
    public void AddPlayerToRoom_DuplicateName_UsesFallback()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player1");

        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("Jogador 2", player.Name);
    }

    [Fact]
    public void AddPlayerToRoom_DuplicateName_SkipsOccupiedFallback()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Jogador 2");
        _sut.AddPlayerToRoom("ABC12", "conn3", "Jogador 2");

        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn3");
        Assert.Equal("Jogador 3", player.Name);
    }

    [Fact]
    public void AddPlayerToRoom_ReconnectWithDuplicateName_UsesFallback()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player1");

        var players = _sut.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("Jogador 3", player.Name);
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
    public void RemovePlayerFromRoom_HostLeaves_TransfersHostToFirstPlayer()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        _sut.RemovePlayerFromRoom("ABC12", "conn1");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.True(players[0].IsHost);
        Assert.Equal("conn2", players[0].ConnectionId);
    }

    [Fact]
    public void RemovePlayerFromRoom_NonHostLeaves_DoesNotTransferHost()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        _sut.RemovePlayerFromRoom("ABC12", "conn2");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.True(players[0].IsHost);
        Assert.Equal("conn1", players[0].ConnectionId);
    }

    [Fact]
    public void RemovePlayerFromRoom_HostLeavesSinglePlayer_RemovesRoom()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        _sut.RemovePlayerFromRoom("ABC12", "conn1");

        Assert.False(_sut.RoomExists("ABC12"));
    }

    [Fact]
    public void RemovePlayerFromRoom_ExistingPlayer_DoesNotRemoveRoomIfPlayersRemain()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.RemovePlayerFromRoom("ABC12", "conn1");

        Assert.True(_sut.RoomExists("ABC12"));
        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.Equal("conn2", players[0].ConnectionId);
    }

    [Fact]
    public void EscolherTime_JoinsTeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        Assert.True(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Vermelho", player.Team);
    }

    [Fact]
    public void EscolherTime_LeavesTeam_WhenClickingOwnTeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        var result = _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        Assert.True(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal("", player.Team);
    }

    [Fact]
    public void EscolherTime_SwitchesTeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        var result = _sut.EscolherTime("ABC12", "conn1", "Azul");

        Assert.True(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Azul", player.Team);
    }

    [Fact]
    public void EscolherTime_ReturnsFalse_WhenTeamFull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.EscolherTime("ABC12", "conn2", "Vermelho");

        var result = _sut.EscolherTime("ABC12", "conn2", "Azul");

        Assert.True(result);
    }

    [Fact]
    public void EscolherTime_NonExistentRoom_ReturnsFalse()
    {
        var result = _sut.EscolherTime("NONEXIST", "conn1", "Vermelho");

        Assert.False(result);
    }

    [Fact]
    public void EscolherTime_PlayerNotFound_ReturnsFalse()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.EscolherTime("ABC12", "nonExistent", "Vermelho");

        Assert.False(result);
    }

    [Fact]
    public void AlternarPronto_ReturnsFalse_WhenNotInTeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.AlternarPronto("ABC12", "conn1");

        Assert.False(result);
    }

    [Fact]
    public void AlternarPronto_TogglesOn()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        var result = _sut.AlternarPronto("ABC12", "conn1");

        Assert.True(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.True(player.IsReady);
    }

    [Fact]
    public void AlternarPronto_TogglesOff()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");
        _sut.AlternarPronto("ABC12", "conn1");

        var result = _sut.AlternarPronto("ABC12", "conn1");

        Assert.False(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.False(player.IsReady);
    }

    [Fact]
    public void AlternarPronto_ResetsOnTeamLeave()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");
        _sut.AlternarPronto("ABC12", "conn1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal("", player.Team);
        Assert.False(player.IsReady);
    }

    [Fact]
    public void RandomizarTime_AssignsToATeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.RandomizarTime("ABC12", "conn1");

        Assert.NotNull(result);
        Assert.Contains(result, new[] { "Vermelho", "Azul" });
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.Equal(result, player.Team);
    }

    [Fact]
    public void RandomizarTime_RespectsTeamCapacity()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.AddPlayerToRoom("ABC12", "conn3", "Player3");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");
        _sut.EscolherTime("ABC12", "conn2", "Vermelho");

        var result = _sut.RandomizarTime("ABC12", "conn3");

        Assert.Equal("Azul", result);
        var player = _sut.GetPlayersInRoom("ABC12").First(p => p.ConnectionId == "conn3");
        Assert.Equal("Azul", player.Team);
    }

    [Fact]
    public void RandomizarTime_ResetsIsReady()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");
        _sut.AlternarPronto("ABC12", "conn1");

        _sut.RandomizarTime("ABC12", "conn1");

        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.False(player.IsReady);
    }

    [Fact]
    public void RandomizarTime_NonExistentRoom_ReturnsNull()
    {
        var result = _sut.RandomizarTime("NONEXIST", "conn1");

        Assert.Null(result);
    }

    [Fact]
    public void ForcarIniciar_AutoAssignsHost_WhenHostNotInTeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.ForcarIniciar("ABC12", "conn1");

        Assert.True(result);
        var player = _sut.GetPlayersInRoom("ABC12").First();
        Assert.NotEmpty(player.Team);
        var room = _sut.GetRoom("ABC12");
        Assert.True(room!.IsActive);
    }

    [Fact]
    public void ForcarIniciar_ReturnsFalse_WhenNonHost()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        var result = _sut.ForcarIniciar("ABC12", "conn2");

        Assert.False(result);
    }

    [Fact]
    public void ForcarIniciar_Success_HostInTeam()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");

        var result = _sut.ForcarIniciar("ABC12", "conn1");

        Assert.True(result);
        var room = _sut.GetRoom("ABC12");
        Assert.True(room!.IsActive);
    }

    [Fact]
    public void ForcarIniciar_AssignsUnassignedPlayers()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.EscolherTime("ABC12", "conn1", "Vermelho");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _sut.AddPlayerToRoom("ABC12", "conn3", "Player3");

        _sut.ForcarIniciar("ABC12", "conn1");

        var players = _sut.GetPlayersInRoom("ABC12");
        Assert.All(players, p => Assert.NotEmpty(p.Team));
    }

    private static GameSettings CriarSettingsValidas()
    {
        return new GameSettings
        {
            RoundTimeSeconds = 60,
            NumberOfRounds = 4,
            SkipLimit = 3,
            SkipCostsPoints = false,
            TipooLeadLimit = null,
            ExplanationTimeSeconds = 5,
            Difficulties = new List<string> { "Fácil", "Médio", "Difícil" },
            BuzzerSounds = new List<string> { "air-horn", "censura", "erro" },
            RandomBuzzerSound = true,
            PanicMode = false,
            PointsPerCorrect = 1,
            PointsPerError = 1,
            PointsPerSkip = 1,
            Categories = new List<string> { "Objeto", "Tecnologia" },
            StartingTeam = "aleatorio",
            TiebreakMode = "rodada-extra",
            PauseBetweenRoundsSeconds = 5
        };
    }

    [Fact]
    public void ConfigurarPartida_NonHost_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.AddPlayerToRoom("ABC12", "conn2", "Player2");

        var result = _sut.ConfigurarPartida("ABC12", "conn2", CriarSettingsValidas());

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_NonExistentRoom_ReturnsNull()
    {
        var result = _sut.ConfigurarPartida("NONEXIST", "conn1", CriarSettingsValidas());

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_OddNumberOfRounds_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.NumberOfRounds = 5;

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_TipooLeadBelowMin_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.TipooLeadLimit = 5;

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_EmptyDifficulties_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.Difficulties = new List<string>();

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_EmptyCategories_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.Categories = new List<string>();

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_EmptyBuzzerSounds_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.BuzzerSounds = new List<string>();

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_InvalidStartingTeam_ReturnsNull()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.StartingTeam = "amarelo";

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurarPartida_Valid_UpdatesRoomSettings()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        var result = _sut.ConfigurarPartida("ABC12", "conn1", CriarSettingsValidas());

        Assert.NotNull(result);
        Assert.Equal(60, result!.RoundTimeSeconds);
        Assert.Equal(4, result.NumberOfRounds);
        var room = _sut.GetRoom("ABC12");
        Assert.Equal(result, room!.Settings);
    }

    [Fact]
    public void ConfigurarPartida_ClampsOutOfRangeValues()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.RoundTimeSeconds = 999;
        settings.ExplanationTimeSeconds = 500;

        var result = _sut.ConfigurarPartida("ABC12", "conn1", settings);

        Assert.NotNull(result);
        Assert.Equal(GameSettings.MaxRoundTimeSeconds, result!.RoundTimeSeconds);
        Assert.Equal(GameSettings.MaxExplanationTimeSeconds, result.ExplanationTimeSeconds);
    }

    [Fact]
    public void ConfigurarPartida_Valid_SavesHostCache()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1", "host-session-1");

        _sut.ConfigurarPartida("ABC12", "conn1", CriarSettingsValidas());

        _dataStore.Verify(
            d => d.SaveHostSettings("host-session-1", It.IsAny<GameSettings>()),
            Times.Once);
    }

    [Fact]
    public void ConfigurarPartida_WithoutHostSession_DoesNotSaveCache()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        _sut.ConfigurarPartida("ABC12", "conn1", CriarSettingsValidas());

        _dataStore.Verify(
            d => d.SaveHostSettings(It.IsAny<string>(), It.IsAny<GameSettings>()),
            Times.Never);
    }

    [Fact]
    public void CreateRoom_WithHostSessionId_AppliesCachedSettings()
    {
        var cached = CriarSettingsValidas();
        cached.RoundTimeSeconds = 90;
        _dataStore.Setup(d => d.LoadHostSettings("host-session-1")).Returns(cached);

        _sut.CreateRoom("ABC12", "conn1", "Player1", "host-session-1");

        var room = _sut.GetRoom("ABC12");
        Assert.NotNull(room);
        Assert.Equal("host-session-1", room!.HostSessionId);
        Assert.Equal(90, room.Settings.RoundTimeSeconds);
    }

    [Fact]
    public void CreateRoom_WithoutHostSessionId_DoesNotCallDataStore()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");

        _dataStore.Verify(d => d.LoadHostSettings(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ObterConfiguracoes_ExistingRoom_ReturnsSettings()
    {
        _sut.CreateRoom("ABC12", "conn1", "Player1");
        _sut.ConfigurarPartida("ABC12", "conn1", CriarSettingsValidas());

        var result = _sut.ObterConfiguracoes("ABC12");

        Assert.Equal(4, result.NumberOfRounds);
    }

    [Fact]
    public void ObterConfiguracoes_NonExistentRoom_ReturnsDefaults()
    {
        var result = _sut.ObterConfiguracoes("NONEXIST");

        Assert.NotNull(result);
        Assert.Equal(60, result.RoundTimeSeconds);
    }
}
