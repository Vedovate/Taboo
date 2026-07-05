// src/Taboo.Api/Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using Taboo.Api.DTOs;
using Taboo.Api.Services;

namespace Taboo.Api.Hubs;

public class GameHub : Hub
{
    private readonly IGameManager _gameManager;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameManager gameManager, ILogger<GameHub> logger)
    {
        _gameManager = gameManager;
        _logger = logger;
    }

    public async Task<bool> EntrarNaSala(string codigoSala, string nomeUsuario)
    {
        var sala = codigoSala.Trim();
        var usuario = nomeUsuario.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(usuario))
        {
            _logger.LogWarning("EntrarNaSala chamado com dados inválidos: sala='{Sala}', usuario='{Usuario}'", sala, usuario);
            return false;
        }

        if (!_gameManager.RoomExists(sala))
        {
            _logger.LogWarning("EntrarNaSala: sala {Sala} não encontrada", sala);
            return false;
        }

        _gameManager.AddPlayerToRoom(sala, Context.ConnectionId, usuario);
        await Groups.AddToGroupAsync(Context.ConnectionId, sala);

        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new PlayerDto(player.Name, player.IsHost))
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {Usuario} entrou na sala {Sala}", usuario, sala);
        return true;
    }

    public async Task<bool> CriarSala(string codigoSala, string nomeUsuario)
    {
        var sala = codigoSala.Trim();
        var usuario = nomeUsuario.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(usuario))
        {
            _logger.LogWarning("CriarSala chamado com dados inválidos: sala='{Sala}', usuario='{Usuario}'", sala, usuario);
            return false;
        }

        if (!_gameManager.CreateRoom(sala, Context.ConnectionId, usuario))
        {
            _logger.LogWarning("CriarSala: sala {Sala} já existe", sala);
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sala);
        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new PlayerDto(player.Name, player.IsHost))
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Sala {Sala} criada por {Usuario}", sala, usuario);
        return true;
    }

    public async Task EnviarMensagem(string codigoSala, string mensagem)
    {
        var sala = codigoSala.Trim();
        var texto = mensagem.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(texto))
        {
            _logger.LogWarning("EnviarMensagem chamado com dados inválidos");
            return;
        }

        if (!_gameManager.IsPlayerInRoom(sala, Context.ConnectionId))
        {
            _logger.LogWarning("EnviarMensagem: jogador não está na sala {Sala}", sala);
            return;
        }

        var nomeUsuario = _gameManager.TryGetPlayerName(sala, Context.ConnectionId) ?? "Usuário";
        await Clients.Group(sala).SendAsync("ReceberMensagem", $"{nomeUsuario}: {texto}");
        _logger.LogDebug("Mensagem enviada por {Usuario} na sala {Sala}", nomeUsuario, sala);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);

        if (!string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogInformation("Conexão {ConnectionId} desconectada da sala {Sala}", Context.ConnectionId, sala);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);
            _gameManager.RemovePlayerFromRoom(sala, Context.ConnectionId);
        }

        if (exception is not null)
        {
            _logger.LogError(exception, "Conexão {ConnectionId} desconectada com erro", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}