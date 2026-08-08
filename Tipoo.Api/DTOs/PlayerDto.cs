namespace Tipoo.Api.DTOs;

public record PlayerDto(string ConnectionId, string Name, bool IsHost, string Team, bool IsReady);
