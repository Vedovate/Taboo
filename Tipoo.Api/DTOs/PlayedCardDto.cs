namespace Tipoo.Api.DTOs;

public record PlayedCardDto(
    int CardIndex,
    int CardId,
    string MainWord,
    List<string> Forbidden,
    string Status, // "Acertou", "Errou", "Pulou", "Anulada"
    string? BuzzedByName = null,
    string? InfractionWord = null,
    string? InfractionType = null,
    int VotesKeep = 0,
    int VotesReverse = 0,
    int VotesCancel = 0,
    string VotingStatus = "none", // "none", "voting", "resolved"
    bool WasTiebreakRandomized = false,
    bool IsContested = false,
    Dictionary<string, string>? PlayerVotes = null
);
