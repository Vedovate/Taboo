namespace Tipoo.Api.Models;

public class GameMatch
{
    public int Id { get; set; }
    public string MatchKey { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string HostSessionId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string SettingsJson { get; set; } = string.Empty;
    public int StartedPlayers { get; set; }
    public bool WasStarted { get; set; } = true;
    public bool Completed { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? FinishedPlayers { get; set; }
    public int? FinalScoreRed { get; set; }
    public int? FinalScoreBlue { get; set; }
    public string? WinnerTeam { get; set; }
}
