namespace Tipoo.Api.DTOs;

public record GameEndStatsDto(
    string WinnerTeam,
    int ScoreRed,
    int ScoreBlue,
    int TotalRounds,
    string MvpName,
    int MvpPoints,
    string TopBuzzerName,
    int TopBuzzerCount,
    string MostBuzzedName,
    int MostBuzzedCount,
    string FastestCardWord,
    int FastestCardSeconds,
    int TotalCorrect,
    int TotalErrors,
    int TotalSkips,
    int TotalContestedReversed
);
