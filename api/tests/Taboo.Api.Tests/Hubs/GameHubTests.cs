using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Taboo.Api.Hubs;
using Taboo.Api.Services;

namespace Taboo.Api.Tests.Hubs;

public class GameHubTests
{
    private readonly GameManager _gameManager;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockGroupProxy;
    private readonly Mock<ISingleClientProxy> _mockCallerProxy;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;

    public GameHubTests()
    {
        _gameManager = new GameManager(NullLogger<GameManager>.Instance);
        _mockClients = new Mock<IHubCallerClients>();
        _mockGroupProxy = new Mock<IClientProxy>();
        _mockCallerProxy = new Mock<ISingleClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();

        _mockContext.Setup(c => c.ConnectionId).Returns("conn1");
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockGroupProxy.Object);
        _mockClients.Setup(c => c.Caller).Returns(_mockCallerProxy.Object);
        _mockGroups
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _mockGroupProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);
        _mockCallerProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);
    }

    private GameHub CreateHub()
    {
        return new GameHub(_gameManager, NullLogger<GameHub>.Instance)
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
    public async Task AlterarNome_ValidInput_BroadcastsUpdatedPlayers()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.AlterarNome("ABC12", "NovoNome");

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);

        var player = _gameManager.GetPlayersInRoom("ABC12").First();
        Assert.Equal("NovoNome", player.Name);
    }

    [Fact]
    public async Task AlterarNome_EmptyInput_DoesNothing()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.AlterarNome("ABC12", "");

        _mockGroupProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task AlterarNome_DuplicateName_DoesNotRename()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "ignored");
        _gameManager.RenamePlayer("ABC12", "conn2", "Player2");

        _mockClients.Setup(c => c.Group("ABC12")).Returns(_mockGroupProxy.Object);
        _mockContext.Setup(c => c.ConnectionId).Returns("conn2");
        var hub = CreateHub();

        await hub.AlterarNome("ABC12", "Player1");

        var players = _gameManager.GetPlayersInRoom("ABC12");
        var player = Assert.Single(players, p => p.ConnectionId == "conn2");
        Assert.Equal("Player2", player.Name);
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
}
