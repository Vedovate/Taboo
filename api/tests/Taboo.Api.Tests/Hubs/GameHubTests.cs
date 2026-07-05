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
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;

    public GameHubTests()
    {
        _gameManager = new GameManager(NullLogger<GameManager>.Instance);
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();

        _mockContext.Setup(c => c.ConnectionId).Returns("conn1");
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockGroups
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _mockClientProxy
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

        _mockClientProxy.Verify(
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
        Assert.Contains(players, p => p.Name == "Player2");
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

        _mockClientProxy.Verify(
            p => p.SendCoreAsync("AtualizarJogadores", It.IsAny<object?[]>(), default),
            Times.Once);
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
    public async Task EnviarMensagem_ValidInput_SendsMessageToGroup()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.EnviarMensagem("ABC12", "Hello!");

        _mockClientProxy.Verify(
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

        _mockClientProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task EnviarMensagem_EmptyInput_DoesNothing()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        var hub = CreateHub();

        await hub.EnviarMensagem("ABC12", "");

        _mockClientProxy.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_PlayerInRoom_RemovesFromRoom()
    {
        _gameManager.CreateRoom("ABC12", "conn1", "Player1");
        _gameManager.AddPlayerToRoom("ABC12", "conn2", "Player2");
        var hub = CreateHub();

        await hub.OnDisconnectedAsync(null);

        Assert.False(_gameManager.IsPlayerInRoom("ABC12", "conn1"));
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("conn1", "ABC12", default),
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
