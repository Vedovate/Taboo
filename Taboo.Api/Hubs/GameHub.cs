// src/Taboo.Api/Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using Taboo.Api.Services;

namespace Taboo.Api.Hubs;

public class GameHub : Hub
{
    private readonly GameManager _gameManager;

    public GameHub(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public async Task<bool> EntrarNaSala(string codigoSala, string nomeUsuario)
    {
        var sala = codigoSala.Trim();
        var usuario = nomeUsuario.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(usuario))
        {
            return false;
        }

        if (!_gameManager.RoomExists(sala))
        {
            return false;
        }

        _gameManager.AddPlayerToRoom(sala, Context.ConnectionId, usuario);
        await Groups.AddToGroupAsync(Context.ConnectionId, sala);

        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new { player.Name, player.IsHost })
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        return true;
    }

    public async Task<bool> CriarSala(string codigoSala, string nomeUsuario)
    {
        var sala = codigoSala.Trim();
        var usuario = nomeUsuario.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(usuario))
        {
            return false;
        }

        if (!_gameManager.CreateRoom(sala, Context.ConnectionId, usuario))
        {
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sala);
        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new { player.Name, player.IsHost })
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        return true;
    }

    public async Task EnviarMensagem(string codigoSala, string mensagem)
    {
        var sala = codigoSala.Trim();
        var texto = mensagem.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        if (!_gameManager.IsPlayerInRoom(sala, Context.ConnectionId))
        {
            return;
        }

        var nomeUsuario = _gameManager.TryGetPlayerName(sala, Context.ConnectionId) ?? "Usuário";
        await Clients.Group(sala).SendAsync("ReceberMensagem", $"{nomeUsuario}: {texto}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);

        if (!string.IsNullOrWhiteSpace(sala))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);
            _gameManager.RemovePlayerFromRoom(sala, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}