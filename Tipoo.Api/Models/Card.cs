namespace Tipoo.Api.Models;

public class Card
{
    public int Id { get; set; }
    public string MainWord { get; set; } = string.Empty;
    public string Forbidden1 { get; set; } = string.Empty;
    public string Forbidden2 { get; set; } = string.Empty;
    public string Forbidden3 { get; set; } = string.Empty;
    public string Forbidden4 { get; set; } = string.Empty;
    public string Forbidden5 { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}