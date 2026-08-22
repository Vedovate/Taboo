namespace Tipoo.Api.DTOs;

public record CardDto(
    int Id,
    string MainWord,
    List<string> Forbidden,
    string Difficulty,
    string Category
);
