// src/Tipoo.Api/Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using Tipoo.Api.Data;
using Tipoo.Api.DTOs;
using Tipoo.Api.Models;
using Tipoo.Api.Services;

namespace Tipoo.Api.Hubs;

public class GameHub : Hub
{
    private readonly IGameManager _gameManager;
    private readonly IGameDataStore _dataStore;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameManager gameManager, IGameDataStore dataStore, ILogger<GameHub> logger)
    {
        _gameManager = gameManager;
        _dataStore = dataStore;
        _logger = logger;
    }

    private List<PlayerDto> MapearJogadores(string sala)
    {
        return _gameManager.GetPlayersInRoom(sala)
            .Select(player => new PlayerDto(
                player.ConnectionId,
                player.Name,
                player.IsHost,
                player.Team,
                player.IsReady))
            .ToList();
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

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);

        await EnviarConfiguracoesAoCaller(sala);

        if (gameRoom is not null && gameRoom.IsActive)
        {
            var estado = _gameManager.ObterEstadoJogo(sala, Context.ConnectionId);
            if (estado is not null)
            {
                await Clients.Caller.SendAsync("AtualizarEstadoJogo", estado);
            }
        }

        _logger.LogInformation("Jogador {ConnectionId} entrou na sala {Sala}", Context.ConnectionId, sala);
        return true;
    }

    public async Task<bool> CriarSala(string codigoSala, string nomeUsuario, string hostSessionId = "")
    {
        var sala = codigoSala.Trim();
        var usuario = nomeUsuario.Trim();

        if (string.IsNullOrWhiteSpace(sala) || string.IsNullOrWhiteSpace(usuario))
        {
            _logger.LogWarning("CriarSala chamado com dados inválidos: sala='{Sala}', usuario='{Usuario}'", sala, usuario);
            return false;
        }

        if (!_gameManager.CreateRoom(sala, Context.ConnectionId, usuario, hostSessionId))
        {
            _logger.LogWarning("CriarSala: sala {Sala} já existe", sala);
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sala);
        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);

        await EnviarConfiguracoesAoCaller(sala);
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

    public async Task<bool> AlterarNome(string codigoSala, string novoNome)
    {
        var sala = codigoSala.Trim();

        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("AlterarNome chamado com dados inválidos");
            return false;
        }

        var resultado = _gameManager.RenamePlayer(sala, Context.ConnectionId, novoNome);
        if (resultado is null)
        {
            _logger.LogWarning("AlterarNome: não foi possível renomear {ConnectionId} na sala {Sala}", Context.ConnectionId, sala);
            return false;
        }

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {ConnectionId} renomeado para {Nome} na sala {Sala}", Context.ConnectionId, resultado, sala);
        return true;
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

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {Alvo} expulso da sala {Sala} pelo host", alvo, sala);
    }

    public async Task<bool> SairDaSala(string codigoSala)
    {
        var sala = codigoSala.Trim();

        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("SairDaSala chamado com código de sala inválido");
            return false;
        }

        if (!_gameManager.IsPlayerInRoom(sala, Context.ConnectionId))
        {
            _logger.LogWarning("SairDaSala: jogador não está na sala {Sala}", sala);
            return false;
        }

        _gameManager.RemovePlayerFromRoom(sala, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);

        var players = MapearJogadores(sala);

        if (players.Count > 0)
        {
            await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        }

        _logger.LogInformation("Jogador {ConnectionId} saiu voluntariamente da sala {Sala}", Context.ConnectionId, sala);
        return true;
    }

    public async Task<bool> EscolherTime(string cor)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("EscolherTime: jogador não está em nenhuma sala");
            return false;
        }

        var resultado = _gameManager.EscolherTime(sala, Context.ConnectionId, cor);
        if (!resultado)
        {
            _logger.LogWarning("EscolherTime: não foi possível escolher time {Cor} para {ConnectionId}", cor, Context.ConnectionId);
            return false;
        }

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {ConnectionId} escolheu time {Cor} na sala {Sala}", Context.ConnectionId, cor, sala);
        return true;
    }

    public async Task<bool> AlternarPronto()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("AlternarPronto: jogador não está em nenhuma sala");
            return false;
        }

        var (isReady, todosProntosIniciou, estadoJogo) = _gameManager.AlternarPronto(sala, Context.ConnectionId);
        if (!isReady && !_gameManager.IsPlayerInRoom(sala, Context.ConnectionId))
        {
            return false;
        }

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {ConnectionId} alternou pronto para {Pronto} na sala {Sala}", Context.ConnectionId, isReady, sala);

        if (todosProntosIniciou && estadoJogo is not null)
        {
            await Clients.Group(sala).SendAsync("PartidaIniciada", estadoJogo);
            _logger.LogInformation("Partida iniciada automaticamente por prontos na sala {Sala}", sala);
        }

        return isReady;
    }

    public async Task<string?> RandomizarTime()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("RandomizarTime: jogador não está em nenhuma sala");
            return null;
        }

        var cor = _gameManager.RandomizarTime(sala, Context.ConnectionId);
        if (cor is null)
        {
            _logger.LogWarning("RandomizarTime: não foi possível alocar {ConnectionId} na sala {Sala}", Context.ConnectionId, sala);
            return null;
        }

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        _logger.LogInformation("Jogador {ConnectionId} foi para time {Cor} (aleatório) na sala {Sala}", Context.ConnectionId, cor, sala);
        return cor;
    }

    public async Task<bool> ForcarIniciar()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("ForcarIniciar: jogador não está em nenhuma sala");
            return false;
        }

        var resultado = _gameManager.ForcarIniciar(sala, Context.ConnectionId);
        if (!resultado)
        {
            _logger.LogWarning("ForcarIniciar: não foi possível iniciar na sala {Sala} pelo jogador {ConnectionId}", sala, Context.ConnectionId);
            return false;
        }

        var players = MapearJogadores(sala);
        await Clients.Group(sala).SendAsync("AtualizarJogadores", players);

        var estado = _gameManager.ObterEstadoJogo(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("PartidaIniciada", estado);
        }

        _logger.LogInformation("Partida iniciada na sala {Sala} pelo jogador {ConnectionId}", sala, Context.ConnectionId);
        return true;
    }

    public async Task<GameSettings?> ConfigurarPartida(GameSettings configuracoes)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("ConfigurarPartida: jogador não está em nenhuma sala");
            return null;
        }

        var resultado = _gameManager.ConfigurarPartida(sala, Context.ConnectionId, configuracoes);
        if (resultado is null)
        {
            _logger.LogWarning("ConfigurarPartida: configurações rejeitadas na sala {Sala}", sala);
            return null;
        }

        await Clients.Group(sala).SendAsync("AtualizarConfiguracoes", resultado);
        _logger.LogInformation("Configurações atualizadas na sala {Sala} por {ConnectionId}", sala, Context.ConnectionId);
        return resultado;
    }

    public GameSettings ObterConfiguracoes()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogWarning("ObterConfiguracoes: jogador não está em nenhuma sala");
            return new GameSettings();
        }

        return _gameManager.ObterConfiguracoes(sala);
    }

    public CartasOpcoesDto ObterOpcoesCartas()
    {
        var cartas = _dataStore.GetAllCards() ?? Array.Empty<Card>();

        var dificuldades = cartas
            .Select(c => c.Difficulty)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var categorias = cartas
            .Select(c => c.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return new CartasOpcoesDto(dificuldades, categorias);
    }

    // =========================================================================
    // ENDPOINTS DE JOGO EM TEMPO REAL
    // =========================================================================

    public GameStateDto? ObterEstadoJogo()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala))
        {
            return null;
        }
        return _gameManager.ObterEstadoJogo(sala, Context.ConnectionId);
    }

    public async Task<GameStateDto?> AcertarCarta()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return null;

        var estado = _gameManager.AcertarCarta(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
        return estado;
    }

    public async Task<GameStateDto?> PularCarta()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return null;

        var estado = _gameManager.PularCarta(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
        return estado;
    }

    public async Task Buzinar(string palavraInfracao, string tipoInfracao)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var (estado, buzzer) = _gameManager.Buzinar(sala, Context.ConnectionId, palavraInfracao, tipoInfracao);
        if (buzzer is not null)
        {
            await Clients.Group(sala).SendAsync("ReceberBuzina", buzzer);
        }
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task EnviarPalpite(string palpite)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var (estado, msg) = _gameManager.EnviarPalpite(sala, Context.ConnectionId, palpite);
        if (msg is not null)
        {
            await Clients.Group(sala).SendAsync("ReceberPalpite", msg);
        }
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task FinalizarTempoExplicacao()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var estado = _gameManager.FinalizarTempoExplicacao(sala);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task FinalizarRodada()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var estado = _gameManager.FinalizarRodada(sala);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task MarcarCartaParaJulgamento(int cardIndex, bool contestar)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var estado = _gameManager.MarcarCartaParaJulgamento(sala, Context.ConnectionId, cardIndex, contestar);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task ConfirmarSelecaoReanalise()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var estado = _gameManager.ConfirmarSelecaoReanalise(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task VotarJulgamentoCarta(int cardIndex, string opcaoVoto)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var (estado, carta, empateSorteado) = _gameManager.VotarJulgamentoCarta(sala, Context.ConnectionId, cardIndex, opcaoVoto);
        if (carta is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarVotacaoCarta", carta, empateSorteado);
        }
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task VotarCarta(int cardIndex, string opcaoVoto)
    {
        await VotarJulgamentoCarta(cardIndex, opcaoVoto);
    }

    public async Task ConfirmarProntoTransicao()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return;

        var estado = _gameManager.ConfirmarProntoTransicao(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
    }

    public async Task<GameStateDto?> AvancarRodada()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return null;

        var estado = _gameManager.AvancarRodada(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
        return estado;
    }

    public async Task<GameStateDto?> ReiniciarPartida()
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);
        if (string.IsNullOrWhiteSpace(sala)) return null;

        var estado = _gameManager.ReiniciarPartida(sala, Context.ConnectionId);
        if (estado is not null)
        {
            await Clients.Group(sala).SendAsync("AtualizarEstadoJogo", estado);
        }
        return estado;
    }

    private async Task EnviarConfiguracoesAoCaller(string sala)
    {
        await Clients.Caller.SendAsync("ReceberConfiguracoes", _gameManager.ObterConfiguracoes(sala));
        await Clients.Caller.SendAsync("ReceberOpcoesCartas", ObterOpcoesCartas());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sala = _gameManager.GetRoomCodeByConnectionId(Context.ConnectionId);

        if (!string.IsNullOrWhiteSpace(sala))
        {
            _logger.LogInformation("Conexão {ConnectionId} desconectada da sala {Sala}", Context.ConnectionId, sala);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);
            _gameManager.RemovePlayerFromRoom(sala, Context.ConnectionId);

            var players = MapearJogadores(sala);
            await Clients.Group(sala).SendAsync("AtualizarJogadores", players);
        }

        if (exception is not null)
        {
            _logger.LogError(exception, "Conexão {ConnectionId} desconectada com erro", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}