namespace Tipoo.Api.Models;

public class GameSettings
{
    public const int MinRoundTimeSeconds = 30;
    public const int MaxRoundTimeSeconds = 600;
    public const int MinNumberOfRounds = 2;
    public const int MaxNumberOfRounds = 20;
    public const int MinTipooLeadLimit = 10;
    public const int MaxExplanationTimeSeconds = 15;
    public const int MaxSkipLimit = 10;
    public const int MaxPoints = 10;
    public const int MinPauseBetweenRoundsSeconds = 15;
    public const int MaxPauseBetweenRoundsSeconds = 300;

    public int RoundTimeSeconds { get; set; } = 180;
    public int NumberOfRounds { get; set; } = 6;
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
    public string StartingTeam { get; set; } = "aleatorio";
    public string TiebreakMode { get; set; } = "empatado";
    public int PauseBetweenRoundsSeconds { get; set; } = 30;
    public int ReanalysisSelectionTimeSeconds { get; set; } = 20;
    public int CardJudgingTimeSeconds { get; set; } = 10;

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
            StartingTeam = StartingTeam,
            TiebreakMode = TiebreakMode,
            PauseBetweenRoundsSeconds = PauseBetweenRoundsSeconds,
            ReanalysisSelectionTimeSeconds = ReanalysisSelectionTimeSeconds,
            CardJudgingTimeSeconds = CardJudgingTimeSeconds
        };
    }
}
