namespace Tipoo.Api.DTOs;

public record ChatMessageDto(
    string Id,
    string ConnectionId,
    string AuthorName,
    string Team,
    string Message,
    bool IsGuess,
    bool IsCorrect,
    DateTime Timestamp
);
