using Microsoft.Extensions.Logging;
using Moq;
using Taboo.Api.Controllers;

namespace Taboo.Api.Tests.Controllers;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _sut;
    private readonly Mock<ILogger<WeatherForecastController>> _loggerMock;

    public WeatherForecastControllerTests()
    {
        _loggerMock = new Mock<ILogger<WeatherForecastController>>();
        _sut = new WeatherForecastController(_loggerMock.Object);
    }

    [Fact]
    public void Get_ReturnsFiveForecasts()
    {
        var result = _sut.Get();

        Assert.Equal(5, result.Count());
    }

    [Fact]
    public void Get_AllItemsHaveValidSummaries()
    {
        var validSummaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        var result = _sut.Get();

        foreach (var item in result)
        {
            Assert.Contains(item.Summary, validSummaries);
        }
    }

    [Fact]
    public void Get_AllItemsHaveTemperatureCInRange()
    {
        var result = _sut.Get();

        foreach (var item in result)
        {
            Assert.InRange(item.TemperatureC, -20, 54);
        }
    }

    [Fact]
    public void Get_TemperatureFIsDerivedCorrectly()
    {
        var result = _sut.Get();

        foreach (var item in result)
        {
            var expectedF = 32 + (int)(item.TemperatureC / 0.5556);
            Assert.Equal(expectedF, item.TemperatureF);
        }
    }

    [Fact]
    public void Get_DatesAreInAscendingOrder()
    {
        var result = _sut.Get().ToArray();

        for (int i = 1; i < result.Length; i++)
        {
            Assert.True(result[i].Date > result[i - 1].Date,
                $"Date at index {i} ({result[i].Date}) should be after index {i - 1} ({result[i - 1].Date})");
        }
    }

    [Fact]
    public void Get_DatesStartFromTomorrow()
    {
        var result = _sut.Get().ToArray();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tomorrow = today.AddDays(1);

        Assert.Equal(tomorrow, result[0].Date);
    }

    [Fact]
    public void Get_AllItemsHaveNonNullSummary()
    {
        var result = _sut.Get();

        foreach (var item in result)
        {
            Assert.NotNull(item.Summary);
        }
    }
}
