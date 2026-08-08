namespace Tipoo.Api.Models;

public class GameSettings
{
    public const int MinRoundTimeSeconds = 30;
    public const int MaxRoundTimeSeconds = 300;
    public const int MinNumberOfRounds = 2;
    public const int MaxNumberOfRounds = 20;
    public const int MinTipooLeadLimit = 10;
    public const int MaxExplanationTimeSeconds = 15;
    public const int MaxSkipLimit = 10;
    public const int MaxPoints = 10;
    public const int MaxPauseBetweenRoundsSeconds = 30;

    public int RoundTimeSeconds { get; set; } = 60;
    public int NumberOfRounds { get; set; } = 4;
    public int SkipLimit { get; set; } = 3;
    public bool SkipCostsPoints { get; set; }
    public int? TipooLeadLimit { get; set; }
    public int ExplanationTimeSeconds { get; set; } = 5;
    public List<string> Difficulties { get; set; } = new() { "Fácil", "Médio", "Difícil" };
    public List<string> BuzzerSounds { get; set; } = new() { "air-horn", "censura", "erro" };
    public bool RandomBuzzerSound { get; set; } = true;
    public bool PanicMode { get; set; }
    public int PointsPerCorrect { get; set; } = 1;
    public int PointsPerError { get; set; } = 1;
    public int PointsPerSkip { get; set; } = 1;
    public List<string> Categories { get; set; } = new();
    public string StartingTeam { get; set; } = "aleatorio";
    public string TiebreakMode { get; set; } = "rodada-extra";
    public int PauseBetweenRoundsSeconds { get; set; } = 5;

    public GameSettings Clone()
    {
        return new GameSettings
        {
            RoundTimeSeconds = RoundTimeSeconds,
            NumberOfRounds = NumberOfRounds,
            SkipLimit = SkipLimit,
            SkipCostsPoints = SkipCostsPoints,
            TipooLeadLimit = TipooLeadLimit,
            ExplanationTimeSeconds = ExplanationTimeSeconds,
            Difficulties = (Difficulties ?? new List<string>()).ToList(),
            BuzzerSounds = (BuzzerSounds ?? new List<string>()).ToList(),
            RandomBuzzerSound = RandomBuzzerSound,
            PanicMode = PanicMode,
            PointsPerCorrect = PointsPerCorrect,
            PointsPerError = PointsPerError,
            PointsPerSkip = PointsPerSkip,
            Categories = (Categories ?? new List<string>()).ToList(),
            StartingTeam = StartingTeam,
            TiebreakMode = TiebreakMode,
            PauseBetweenRoundsSeconds = PauseBetweenRoundsSeconds
        };
    }
}
