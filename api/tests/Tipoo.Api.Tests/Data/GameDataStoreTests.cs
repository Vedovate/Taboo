using Microsoft.Extensions.Logging.Abstractions;
using Tipoo.Api.Data;
using Tipoo.Api.Database;
using Tipoo.Api.Infrastructure;
using Tipoo.Api.Models;

namespace Tipoo.Api.Tests.Data;

public class GameDataStoreTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly GameDataStore _sut;

    public GameDataStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"tipoo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Database"));

        var provider = new ConnectionStringProvider("Data Source=test.db", _tempRoot);
        DbInitializer.Initialize(provider.ConnectionString);
        _sut = new GameDataStore(provider, NullLogger<GameDataStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // melhor esforço ao limpar artefatos de teste
        }
    }

    [Fact]
    public void GetAllCards_ReturnsCardsFromDatabase()
    {
        var cards = _sut.GetAllCards();

        Assert.NotEmpty(cards);
        Assert.Contains(cards, c => c.MainWord == "CLIPE");
    }

    [Fact]
    public void GetAllCards_IncludesDifficultiesAndCategories()
    {
        var cards = _sut.GetAllCards();

        Assert.Contains(cards, c => c.Difficulty == "Difícil");
        Assert.Contains(cards, c => c.Category == "Objeto");
    }

    [Fact]
    public void SaveHostSettings_ThenLoadHostSettings_RoundTrips()
    {
        var settings = new GameSettings
        {
            RoundTimeSeconds = 90,
            NumberOfRounds = 6,
            Difficulties = new List<string> { "Fácil" },
            Categories = new List<string> { "Objeto" },
            BuzzerSounds = new List<string> { "censura" },
            StartingTeam = "azul",
            TiebreakMode = "empatado",
            PauseBetweenRoundsSeconds = 10
        };

        _sut.SaveHostSettings("host-1", settings);

        var loaded = _sut.LoadHostSettings("host-1");

        Assert.NotNull(loaded);
        Assert.Equal(90, loaded!.RoundTimeSeconds);
        Assert.Equal(6, loaded.NumberOfRounds);
        Assert.Equal("azul", loaded.StartingTeam);
        Assert.Equal("empatado", loaded.TiebreakMode);
        Assert.Equal(new[] { "Fácil" }, loaded.Difficulties);
        Assert.Equal(new[] { "Objeto" }, loaded.Categories);
        Assert.Equal(new[] { "censura" }, loaded.BuzzerSounds);
    }

    [Fact]
    public void SaveHostSettings_OverwritesPreviousSettings()
    {
        _sut.SaveHostSettings("host-1", new GameSettings { RoundTimeSeconds = 30 });
        _sut.SaveHostSettings("host-1", new GameSettings { RoundTimeSeconds = 120 });

        var loaded = _sut.LoadHostSettings("host-1");

        Assert.NotNull(loaded);
        Assert.Equal(120, loaded!.RoundTimeSeconds);
    }

    [Fact]
    public void LoadHostSettings_NoRow_ReturnsNull()
    {
        var loaded = _sut.LoadHostSettings("host-inexistente");

        Assert.Null(loaded);
    }
}
