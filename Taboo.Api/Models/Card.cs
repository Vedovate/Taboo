namespace Taboo.Api.Models;

public class Card
{
    public int Id { get; set; }
    public string MainWord { get; set; } = string.Empty;
    public string Taboo1 { get; set; } = string.Empty;
    public string Taboo2 { get; set; } = string.Empty;
    public string Taboo3 { get; set; } = string.Empty;
    public string Taboo4 { get; set; } = string.Empty;
    public string Taboo5 { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}