namespace Taboo.Api.Models;

public class GameRoom
{
    public string RoomCode { get; set; } = string.Empty;
    public string HostSessionId { get; set; } = string.Empty; // Token do localStorage do Host
    public List<Player> Players { get; set; } = new();
    
    // Estado do jogo
    public int ScoreTeamBlue { get; set; }
    public int ScoreTeamRed { get; set; }
    public string CurrentTurnTeam { get; set; } = "Azul"; // "Azul" ou "Vermelho"
    public string CurrentSpokespersonId { get; set; } = string.Empty; // Quem está dando as dicas
    public bool IsActive { get; set; }
    public int TimeRemaining { get; set; } = 60;
}
