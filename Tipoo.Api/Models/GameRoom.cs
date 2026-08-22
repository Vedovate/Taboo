using Tipoo.Api.DTOs;

namespace Tipoo.Api.Models;

public class GameRoom
{
    public string RoomCode { get; set; } = string.Empty;
    public string HostSessionId { get; set; } = string.Empty; // Token do localStorage do Host
    public List<Player> Players { get; set; } = new();

    public int MaxPlayers { get; set; } = 3;

    public GameSettings Settings { get; set; } = new();

    // Estado da partida em andamento
    public bool IsActive { get; set; }
    public int RoundNumber { get; set; } = 1;
    public string CurrentTurnTeam { get; set; } = "Azul"; // "Azul" ou "Vermelho"
    public string CurrentSpokespersonId { get; set; } = string.Empty; // Quem está dando as dicas
    public string CurrentSpokespersonName { get; set; } = string.Empty;
    public string Phase { get; set; } = "jogando"; // "jogando", "explicacao_buzina", "revisao", "fim_partida"
    public int TimeRemaining { get; set; } = 180;
    public int SkipsLeft { get; set; } = 3;
    public int RoundScore { get; set; }
    public int ScoreTeamBlue { get; set; }
    public int ScoreTeamRed { get; set; }

    // Baralho e cartas da rodada
    public List<Card> Deck { get; set; } = new();
    public int CurrentCardIndex { get; set; }
    public DateTime CurrentCardStartedAt { get; set; } = DateTime.UtcNow;
    public List<PlayedCardDto> RoundCards { get; set; } = new();
    public List<ChatMessageDto> ChatMessages { get; set; } = new();
    public BuzzerEventDto? ActiveBuzzer { get; set; }

    // Reanálise e Julgamento pós-rodada
    public List<int> ContestedCardIndexes { get; set; } = new();
    public int CurrentJudgingIndex { get; set; }
    public HashSet<string> ReadyToAdvancePlayerIds { get; set; } = new();
    public int PhaseTimeRemaining { get; set; }

    // Rotação de explicadores
    public int LastSpokespersonIndexRed { get; set; } = -1;
    public int LastSpokespersonIndexBlue { get; set; } = -1;

    // Estatísticas da partida
    public Dictionary<string, int> PlayerCorrectCounts { get; set; } = new();
    public Dictionary<string, int> PlayerBuzzerCounts { get; set; } = new();
    public Dictionary<string, int> PlayerBuzzedCounts { get; set; } = new();
    public string FastestCardWord { get; set; } = string.Empty;
    public int FastestCardSeconds { get; set; } = int.MaxValue;
    public int TotalCorrect { get; set; }
    public int TotalErrors { get; set; }
    public int TotalSkips { get; set; }
    public int TotalContestedReversed { get; set; }

    // Registro da partida no banco
    public DateTime? StartedAt { get; set; }
    public string? MatchKey { get; set; } // Código da sala + data/hora de início
}
