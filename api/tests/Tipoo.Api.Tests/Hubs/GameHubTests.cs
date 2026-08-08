using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tipoo.Api.Data;
using Tipoo.Api.DTOs;
using Tipoo.Api.Hubs;
using Tipoo.Api.Models;
using Tipoo.Api.Services;

namespace Tipoo.Api.Tests.Hubs;

public class GameHubTests
{
    private readonly GameManager _gameManager;
    private readonly Mock<IGameDataStore> _dataStore;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockGroupProxy;
    private readonly Mock<ISingleClientProxy> _mockCallerProxy;
    private readonly Mock<ISingleClientProxy> _mockTargetProxy;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;

    public GameHubTests()
    {
        _dataStore = new Mock<IGameDataStore>();
        _gameManager = new GameManager(NullLogger<GameManager>.Instance, _dataStore.Object);
        _mockClients = new Mock<IHubCallerClients>();
        _mockGroupProxy = new Mock<IClientProxy>();
        _mockCallerProxy = new Mock<ISingleClientProxy>();
        _mockTargetProxy = new Mock<ISingleClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();

        _mockContext.Setup(c => c.ConnectionId).Returns("conn1");
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockGroupProxy.Object);
        _mockClients.Setup(c => c.Caller).Returns(_mockCallerProxy.Object);
        _mockClients.Setup(c => c.Client(It.IsAny<string>())).Returns(_mockTargetProxy.Object);
        _mockGroups
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _mockGroups
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _mockGroupProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);
        _mockCallerProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);
        _mockTargetProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);
    }

    private GameHub CreateHub()
    {
        return new GameHub(_gameManager, _dataStore.Object, NullLogger<GameHub>.Instance)
        {
            Clients = _mockClients.Object,
            Context = _mockContext.Object,
            Groups = _mockGroups.Object
        };
    }

    [Fact]
    public async Task CriarSala_ValidInput_CreatesRoomAndJoinsGroup()
    {
        var hub = CreateHub();

        var result = await hub.CriarSala("ABC12", "Player1");

        Assert.True(result);
        Assert.True(_gameManager.RoomExists("ABC12"));

        var players = _gameManager.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.Equal("Player1", players[0].Name);
        Assert.True(players[0].IsHost);
    }

    [Fact]
    public async Task CriarSala_ValidInput_CallsGroupAndSendAsync()
    {
        var hub = CreateHub();

        await hub.CriarSala("ABC12", "Player1");

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn1", "ABC12", default),
            Times.Once);

        _mockClients.Verify(
            c => c.Group("ABC12"),
            Times.Once);

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task CriarSala_EmptyCode_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.CriarSala("", "Player1");

        Assert.False(result);
    }

    [Fact]
    public async Task CriarSala_WhitespaceName_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.CriarSala("ABC12", "   ");

        Assert.False(result);
    }

    [Fact]
    public async Task CriarSala_DuplicateRoom_ReturnsFalse()
    {
        var hub = CreateHub();
        await hub.CriarSala("ABC12", "Player1");

        var result = await hub.CriarSala("ABC12", "Player2");

        Assert.False(result);
        Assert.Single(_gameManager.GetPlayersInRoom("ABC12"));
    }

    [Fact]
    public async Task EntrarNaSala_ExistingRoom_AddsPlayer()
    {
        _gameManager.CreateRoom("ABC12", "hostConn", "Host");
        var hub = CreateHub();

        var result = await hub.EntrarNaSala("ABC12", "Player2");

        Assert.True(result);
        var players = _gameManager.GetPlayersInRoom("ABC12");
        Assert.Equal(2, players.Count);
        Assert.Contains(players, p => p.ConnectionId == "conn1");
    }

    [Fact]
    public async Task EntrarNaSala_ExistingRoom_JoinsGroupAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "hostConn", "Host");
        var hub = CreateHub();

        await hub.EntrarNaSala("ABC12", "Player2");

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn1", "ABC12", default),
            Times.Once);

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task EntrarNaSala_FullRoom_ReturnsFalseAndSendsSalaCheia()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        var room = _gameManager.GetRoom("ABC12");
        room!.MaxPlayers = 1;

        var hub = CreateHub();
        var result = await hub.EntrarNaSala("ABC12", "Player2");

        Assert.False(result);
        _mockCallerProxy.Verify(
            p => p.SendCoreAsync("SalaCheia", It.Is<object?[]>(args => (string)args[0]! == "A sala já está cheia."), default),
            Times.Once);
    }

    [Fact]
    public async Task EntrarNaSala_FullRoom_DoesNotAddPlayer()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        var room = _gameManager.GetRoom("ABC12");
        room!.MaxPlayers = 1;

        var hub = CreateHub();
        await hub.EntrarNaSala("ABC12", "Player2");

        var players = _gameManager.GetPlayersInRoom("ABC12");
        Assert.Single(players);
    }

    [Fact]
    public async Task EntrarNaSala_NonExistentRoom_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.EntrarNaSala("NONEXIST", "Player1");

        Assert.False(result);
        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task EntrarNaSala_EmptyInput_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.EntrarNaSala("", "");

        Assert.False(result);
    }

    [Fact]
    public async Task AlterarNome_ValidInput_ReturnsTrueAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.AlterarNome("ABC12", "NovoNome");

        Assert.True(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);

        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.Equal("NovoNome", player.Name);
    }

    [Fact]
    public async Task AlterarNome_EmptyInput_ReturnsFalse()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.AlterarNome("ABC12", "");

        Assert.False(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task AlterarNome_DuplicateName_ReturnsFalse()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "ignored");
        _gameManager.RenamePlayer("ABC12", "conn2", "Player2");

        _mockClients.Setup(c => c.Group("ABC12")).Returns(_mockGroupProxy.Object);
        _mockContext.Setup(c => c.ConnectionId).Returns("conn2");
        var hub = CreateHub();

        var result = await hub.AlterarNome("ABC12", "Player1");

        Assert.False(result);
        var players = _gameManager.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("Player2", player.Name);
    }

    [Fact]
    public async Task AlterarNome_NameTooLong_ReturnsFalse()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.AlterarNome("ABC12", "Nome muito longo aqui");

        Assert.False(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task AlterarNome_ControlChars_RemovesAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.AlterarNome("ABC12", "Play\u0000er1");

        Assert.True(result);
        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Player1", player.Name);
    }

    [Fact]
    public async Task AlterarNome_ZeroWidthChars_RemovesAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.AlterarNome("ABC12", "Play\u200Ber1");

        Assert.True(result);
        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Player1", player.Name);
    }

    [Fact]
    public async Task EnviarMensagem_ValidInput_SendsMessageToGroup()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.EnviarMensagem("ABC12", "Hello!");

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(
                "ReceberMensagem",
                It.Is<object?[]>(args => args.Length == 1 && ((string)args[0]!).Contains("Player1: Hello!")),
                default),
            Times.Once);
    }

    [Fact]
    public async Task EnviarMensagem_PlayerNotInRoom_DoesNotSend()
    {
        _gameManager.CreateRoom("ABC12", "otherConn", "OtherPlayer");
        var hub = CreateHub();

        await hub.EnviarMensagem("ABC12", "Hello!");

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task EnviarMensagem_EmptyInput_DoesNothing()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.EnviarMensagem("ABC12", "");

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ExpulsarJogador_HostExpelsPlayer_RemovesFromGroupAndRoom()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "conn2");
        var hub = CreateHub();

        await hub.ExpulsarJogador("ABC12", "conn2");

        Assert.False(_gameManager.IsPlayerInRoom("ABC12", "conn2"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("conn2", "ABC12", default),
            Times.Once);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ExpulsarJogador_NonHostCaller_DoesNothing()
    {
        _gameManager.CreateRoom("ABC12", "hostConn", "Host");
        _gameManager.AddPlayerToRoom("ABC12", "conn1", "conn1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "conn2");
        var hub = CreateHub();

        await hub.ExpulsarJogador("ABC12", "conn2");

        Assert.True(_gameManager.IsPlayerInRoom("ABC12", "conn2"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ExpulsarJogador_ExpelledPlayerReceivesFoiExpulso()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "Player2");
        var hub = CreateHub();

        await hub.ExpulsarJogador("ABC12", "conn2");

        _mockClients.Verify(
            c => c.Client("conn2"),
            Times.Once);
        _mockTargetProxy.Verify(
            p => p.SendCoreAsync("FoiExpulso", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ExpulsarJogador_HostCannotExpelSelf()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        var hub = CreateHub();

        await hub.ExpulsarJogador("ABC12", "conn1");

        Assert.True(_gameManager.IsPlayerInRoom("ABC12", "conn1"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ExpulsarJogador_InvalidInput_DoesNothing()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        var hub = CreateHub();

        await hub.ExpulsarJogador("", "conn2");

        Assert.True(_gameManager.IsPlayerInRoom("ABC12", "conn1"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ExpulsarJogador_NonExistentTarget_DoesNothing()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Host");
        var hub = CreateHub();

        await hub.ExpulsarJogador("ABC12", "nonExistentConn");

        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task SairDaSala_PlayerInRoom_RemovesFromRoomAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "Player2");
        var hub = CreateHub();

        var result = await hub.SairDaSala("ABC12");

        Assert.True(result);
        Assert.False(_gameManager.IsPlayerInRoom("ABC12", "conn1"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("conn1", "ABC12", default),
            Times.Once);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task SairDaSala_HostLeaves_TransfersHost()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "Player2");
        var hub = CreateHub();

        await hub.SairDaSala("ABC12");

        var players = _gameManager.GetPlayersInRoom("ABC12");
        Assert.Single(players);
        Assert.True(players[0].IsHost);
        Assert.Equal("conn2", players[0].ConnectionId);
    }

    [Fact]
    public async Task SairDaSala_PlayerNotInRoom_ReturnsFalse()
    {
        _gameManager.CreateRoom("ABC12", "hostConn", "Player1");
        var hub = CreateHub();

        var result = await hub.SairDaSala("ABC12");

        Assert.False(result);
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task SairDaSala_InvalidCode_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.SairDaSala("");

        Assert.False(result);
    }

    [Fact]
    public async Task OnDisconnectedAsync_PlayerInRoom_RemovesFromRoomAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "Player2");
        var hub = CreateHub();

        await hub.OnDisconnectedAsync(null);

        Assert.False(_gameManager.IsPlayerInRoom("ABC12", "conn1"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("conn1", "ABC12", default),
            Times.Once);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_LastPlayer_RemovesRoom()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.OnDisconnectedAsync(null);

        Assert.False(_gameManager.RoomExists("ABC12"));
    }

    [Fact]
    public async Task OnDisconnectedAsync_PlayerNotInRoom_DoesNothing()
    {
        var hub = CreateHub();

        await hub.OnDisconnectedAsync(null);

        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task EscolherTime_ValidInput_JoinsTeamAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.EscolherTime("Vermelho");

        Assert.True(result);
        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.Equal("Vermelho", player.Team);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task EscolherTime_PlayerNotInRoom_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.EscolherTime("Vermelho");

        Assert.False(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task AlternarPronto_PlayerInTeam_TogglesAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.EscolherTime("ABC12", "conn1", "Vermelho");
        var hub = CreateHub();

        var result = await hub.AlternarPronto();

        Assert.True(result);
        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.True(player.IsReady);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task AlternarPronto_PlayerNotInRoom_ReturnsFalse()
    {
        var hub = CreateHub();

        var result = await hub.AlternarPronto();

        Assert.False(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task RandomizarTime_AssignsPlayerAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.RandomizarTime();

        Assert.NotNull(result);
        Assert.Contains(result, new[] { "Vermelho", "Azul" });
        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.Equal(result, player.Team);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task RandomizarTime_PlayerNotInRoom_ReturnsNull()
    {
        var hub = CreateHub();

        var result = await hub.RandomizarTime();

        Assert.Null(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ForcarIniciar_HostInTeam_ReturnsTrueAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.EscolherTime("ABC12", "conn1", "Vermelho");
        var hub = CreateHub();

        var result = await hub.ForcarIniciar();

        Assert.True(result);
        var room = _gameManager.GetRoom("ABC12");
        Assert.NotNull(room);
        Assert.True(room.IsActive);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ForcarIniciar_HostNotInTeam_AutoAssignsAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.ForcarIniciar();

        Assert.True(result);
        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.NotEmpty(player.Team);
        var room = _gameManager.GetRoom("ABC12");
        Assert.NotNull(room);
        Assert.True(room.IsActive);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
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
            StartingTeam = "aleatorio",
            TiebreakMode = "rodada-extra",
            PauseBetweenRoundsSeconds = 30
        };
    }

    [Fact]
    public async Task CriarSala_WithHostSessionId_StoresTokenOnRoom()
    {
        var hub = CreateHub();

        var result = await hub.CriarSala("ABC12", "Player1", "host-session-1");

        Assert.True(result);
        var room = _gameManager.GetRoom("ABC12");
        Assert.NotNull(room);
        Assert.Equal("host-session-1", room!.HostSessionId);
    }

    [Fact]
    public async Task CriarSala_Valid_SendsSettingsAndOptionsToCaller()
    {
        _dataStore.Setup(d => d.GetAllCards()).Returns(new List<Card>
        {
            new() { MainWord = "CLIPE", Difficulty = "Fácil", Category = "Objeto" },
            new() { MainWord = "SOFTWARE", Difficulty = "Fácil", Category = "Tecnologia" },
            new() { MainWord = "ALFABETO", Difficulty = "Difícil", Category = "Conceito" }
        });
        var hub = CreateHub();

        await hub.CriarSala("ABC12", "Player1");

        _mockCallerProxy.Verify(
            p => p.SendCoreAsync("ReceberConfiguracoes", It.IsAny<object?[]>(), default),
            Times.Once);
        _mockCallerProxy.Verify(
            p => p.SendCoreAsync("ReceberOpcoesCartas", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ConfigurarPartida_Host_ReturnsSettingsAndBroadcasts()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = await hub.ConfigurarPartida(CriarSettingsValidas());

        Assert.NotNull(result);
        Assert.Equal(60, result!.RoundTimeSeconds);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarConfiguracoes", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ConfigurarPartida_NonHost_ReturnsNullAndDoesNotBroadcast()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "Player2");
        _mockContext.Setup(c => c.ConnectionId).Returns("conn2");
        var hub = CreateHub();

        var result = await hub.ConfigurarPartida(CriarSettingsValidas());

        Assert.Null(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarConfiguracoes", It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ConfigurarPartida_InvalidSettings_ReturnsNull()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var settings = CriarSettingsValidas();
        settings.NumberOfRounds = 5;
        var hub = CreateHub();

        var result = await hub.ConfigurarPartida(settings);

        Assert.Null(result);
        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarConfiguracoes", It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public void ObterConfiguracoes_PlayerInRoom_ReturnsRoomSettings()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        var result = hub.ObterConfiguracoes();

        Assert.NotNull(result);
        Assert.Equal(180, result.RoundTimeSeconds);
    }

    [Fact]
    public void ObterConfiguracoes_PlayerNotInRoom_ReturnsDefaults()
    {
        var hub = CreateHub();

        var result = hub.ObterConfiguracoes();

        Assert.NotNull(result);
        Assert.Equal(180, result.RoundTimeSeconds);
    }

    [Fact]
    public void ObterOpcoesCartas_ReturnsDistinctValues()
    {
        _dataStore.Setup(d => d.GetAllCards()).Returns(new List<Card>
        {
            new() { MainWord = "CLIPE", Difficulty = "Fácil", Category = "Objeto" },
            new() { MainWord = "PASTA", Difficulty = "Fácil", Category = "Objeto" },
            new() { MainWord = "SOFTWARE", Difficulty = "Médio", Category = "Tecnologia" },
            new() { MainWord = "ALFABETO", Difficulty = "Difícil", Category = "Conceito" }
        });
        var hub = CreateHub();

        var result = hub.ObterOpcoesCartas();

        Assert.Equal(new[] { "Difícil", "Fácil", "Médio" }, result.Dificuldades);
        Assert.Equal(new[] { "Conceito", "Objeto", "Tecnologia" }, result.Categorias);
    }
}
