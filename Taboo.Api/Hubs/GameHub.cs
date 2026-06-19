// src/Taboo.Api/Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using Taboo.Api.Services;

namespace Taboo.Api.Hubs;

public class GameHub : Hub
{
    private readonly GameManager _gameManager;

    public GameHub(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public async Task EntrarNaSala(string codigoSala, string nomeUsuario)
    {
        var sala = codigoSala.Trim();
        var usuario = nomeUsuario.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(usuario))
        {
            return;
        }

        _gameManager.AddPlayerToRoom(sala, Context.ConnectionId, usuario);
        await Groups.AddToGroupAsync(Context.ConnectionId, sala);
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