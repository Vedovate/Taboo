namespace Tipoo.Api.DTOs;

public record GameStateDto(
    string RoomCode,
    int RoundNumber,
    int TotalRounds,
    string ActiveTeam,
    string SpokespersonId,
    string SpokespersonName,
    CardDto? CurrentCard,
    int ScoreRed,
    int ScoreBlue,
    int RoundScore,
    string Phase, // "jogando", "explicacao_buzina", "selecao_reanalise", "julgamento_carta", "resumo_rodada", "fim_partida"
    int SkipsLeft,
    int TimeRemaining,
    BuzzerEventDto? ActiveBuzzer,
    List<PlayedCardDto> RoundCards,
    List<ChatMessageDto> ChatMessages,
    GameEndStatsDto? EndStats,
    int CurrentJudgingIndex = 0,
    List<int>? ContestedCardIndexes = null,
    List<string>? ReadyToAdvancePlayerIds = null,
    int PhaseTimeRemaining = 0
);
