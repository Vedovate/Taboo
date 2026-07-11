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

        var gameRoom = _gameManager.GetRoom(sala);
        if (gameRoom is not null && gameRoom.Players.Count >= gameRoom.MaxPlayers)
        {
            _logger.LogWarning("EntrarNaSala: sala {Sala} está cheia ({Count}/{Max})", sala, gameRoom.Players.Count, gameRoom.MaxPlayers);
            await Clients.Caller.SendAsync("SalaCheia", "A sala já está cheia.");
            return false;
        }

        _gameManager.AddPlayerToRoom(sala, Context.ConnectionId, usuario);
        await Groups.AddToGroupAsync(Context.ConnectionId, sala);

        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new PlayerDto(player.ConnectionId, player.Name, player.IsHost))
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {ConnectionId} entrou na sala {Sala}", Context.ConnectionId, sala);
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
            .Select(player => new PlayerDto(player.ConnectionId, player.Name, player.IsHost))
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

    public async Task AlterarNome(string codigoSala, string novoNome)
    {
        var sala = codigoSala.Trim();
        var nome = novoNome.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(nome))
        {
            _logger.LogWarning("AlterarNome chamado com dados inválidos");
            return;
        }

        var resultado = _gameManager.RenamePlayer(sala, Context.ConnectionId, nome);
        if (resultado is null)
        {
            _logger.LogWarning("AlterarNome: não foi possível renomear {ConnectionId} para {Nome} na sala {Sala}", Context.ConnectionId, nome, sala);
            return;
        }

        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new PlayerDto(player.ConnectionId, player.Name, player.IsHost))
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {ConnectionId} renomeado para {Nome} na sala {Sala}", Context.ConnectionId, nome, sala);
    }

    public async Task ExpulsarJogador(string codigoSala, string connectionIdAlvo)
    {
        var sala = codigoSala.Trim();
        var alvo = connectionIdAlvo.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(alvo))
        {
            _logger.LogWarning("ExpulsarJogador chamado com dados inválidos");
            return;
        }

        if (!_gameManager.IsHost(sala, Context.ConnectionId))
        {
            _logger.LogWarning("ExpulsarJogador: chamador não é host na sala {Sala}", sala);
            return;
        }

        if (!_gameManager.IsPlayerInRoom(sala, alvo))
        {
            _logger.LogWarning("ExpulsarJogador: alvo {Alvo} não está na sala {Sala}", alvo, sala);
            return;
        }

        if (alvo == Context.ConnectionId)
        {
            _logger.LogWarning("ExpulsarJogador: host tentou expulsar a si mesmo na sala {Sala}", sala);
            return;
        }

        await Groups.RemoveFromGroupAsync(alvo, sala);
        _gameManager.RemovePlayerFromRoom(sala, alvo);

        await Clients.Client(alvo).SendAsync("FoiExpulso");

        var players = _gameManager.GetPlayersInRoom(sala)
            .Select(player => new PlayerDto(player.ConnectionId, player.Name, player.IsHost))
            .ToList();

        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {Alvo} expulso da sala {Sala} pelo host", alvo, sala);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);

        if (!string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogInformation("Conexão {ConnectionId} desconectada da sala {Sala}", Context.ConnectionId, sala);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);
            _gameManager.RemovePlayerFromRoom(sala, Context.ConnectionId);

            var players = _gameManager.GetPlayersInRoom(sala)
                .Select(player => new PlayerDto(player.ConnectionId, player.Name, player.IsHost))
                .ToList();

            await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        }

        if (exception is not null)
        {
            _logger.LogError(exception, "Conexão {ConnectionId} desconectada com erro", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}