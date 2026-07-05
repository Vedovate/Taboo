using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Taboo.Api.Filters;
using Taboo.Api.Hubs;

namespace Taboo.Api.Tests.Filters;

public class GameHubFilterTests
{
    private readonly GameHubFilter _sut = new();

    [Fact]
    public async Task InvokeMethodAsync_Success_ReturnsNextResult()
    {
        var context = CreateContext(typeof(GameHub).GetMethod("CriarSala")!, "CriarSala");
        var expected = new ValueTask<object?>("resultado");
        var invoked = false;

        var result = await _sut.InvokeMethodAsync(context, _ =>
        {
            invoked = true;
            return expected;
        });

        Assert.True(invoked);
        Assert.Equal("resultado", result);
    }

    [Fact]
    public async Task InvokeMethodAsync_ExceptionOnTaskBool_ReturnsFalse()
    {
        var loggerMock = new Mock<ILogger<GameHubFilter>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<GameHubFilter>)))
            .Returns(loggerMock.Object);

        var context = CreateContext(
            typeof(GameHub).GetMethod("CriarSala")!,
            "CriarSala",
            serviceProviderMock.Object);

        var result = await _sut.InvokeMethodAsync(context, _ =>
            throw new InvalidOperationException("erro simulado"));

        Assert.False((bool)result!);
    }

    [Fact]
    public async Task InvokeMethodAsync_ExceptionOnTask_ThrowsOriginalException()
    {
        var loggerMock = new Mock<ILogger<GameHubFilter>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<GameHubFilter>)))
            .Returns(loggerMock.Object);

        var context = CreateContext(
            typeof(GameHub).GetMethod("EnviarMensagem")!,
            "EnviarMensagem",
            serviceProviderMock.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.InvokeMethodAsync(context, _ =>
                throw new InvalidOperationException("erro simulado")).AsTask());

        Assert.Equal("erro simulado", ex.Message);
    }

    [Fact]
    public async Task InvokeMethodAsync_Exception_LogsError()
    {
        var loggerMock = new Mock<ILogger<GameHubFilter>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<GameHubFilter>)))
            .Returns(loggerMock.Object);

        var context = CreateContext(
            typeof(GameHub).GetMethod("CriarSala")!,
            "CriarSala",
            serviceProviderMock.Object);

        try
        {
            await _sut.InvokeMethodAsync(context, _ =>
                throw new InvalidOperationException("erro simulado"));
        }
        catch
        {
            // ignorado — o log deve ter sido chamado antes do retorno/throw
        }

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CriarSala")),
                It.IsAny<InvalidOperationException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    private static HubInvocationContext CreateContext(
        MethodInfo methodInfo,
        string methodName,
        IServiceProvider? serviceProvider = null)
    {
        var callerContextMock = new Mock<HubCallerContext>();
        var hubMock = new Mock<Hub>();

        return new HubInvocationContext(
            callerContextMock.Object,
            serviceProvider ?? new Mock<IServiceProvider>().Object,
            hubMock.Object,
            methodInfo,
            Array.Empty<object?>());
    }
}
