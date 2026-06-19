namespace Taboo.Api.Models;

public class Player
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty; // "Azul" ou "Vermelho"
    public int Score { get; set; }
    public bool IsHost { get; set; }
}