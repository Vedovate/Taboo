namespace Tipoo.Api.DTOs;

public record BuzzerEventDto(
    string BuzzedByConnectionId,
    string BuzzedByName,
    string BuzzerTeam,
    string InfractionWord,
    string InfractionType,
    int ExplanationTimeSeconds
);
