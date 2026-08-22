using System.Collections.Concurrent;
using Tipoo.Api.DTOs;
using Tipoo.Api.Models;

namespace Tipoo.Api.Services;

public interface IGameManager
{
    ConcurrentDictionary<string, GameRoom> GameRooms { get; }
    GameRoom GetOrCreateRoom(string roomCode);
    bool RoomExists(string roomCode);
    bool CreateRoom(string roomCode, string connectionId, string userName, string hostSessionId = "");
    void AddPlayerToRoom(string roomCode, string connectionId, string userName);
    string? RenamePlayer(string roomCode, string connectionId, string newName);
    IReadOnlyList<Player> GetPlayersInRoom(string roomCode);
    void RemovePlayerFromRoom(string roomCode, string connectionId);
    bool IsHost(string roomCode, string connectionId);
    bool IsPlayerInRoom(string roomCode, string connectionId);
    string? TryGetPlayerName(string roomCode, string connectionId);
    GameRoom? GetRoom(string roomCode);
    string? GetRoomCodeByConnectionId(string connectionId);
    bool EscolherTime(string roomCode, string connectionId, string cor);
    (bool IsReady, bool TodosProntosIniciou, GameStateDto? Estado) AlternarPronto(string roomCode, string connectionId);
    string? RandomizarTime(string roomCode, string connectionId);
    bool ForcarIniciar(string roomCode, string connectionId);
    GameSettings? ConfigurarPartida(string roomCode, string connectionId, GameSettings settings);
    GameSettings ObterConfiguracoes(string roomCode);

    // Métodos de jogo em tempo real
    GameStateDto? ObterEstadoJogo(string roomCode, string connectionId);
    GameStateDto? AcertarCarta(string roomCode, string connectionId);
    GameStateDto? PularCarta(string roomCode, string connectionId);
    (GameStateDto? Estado, BuzzerEventDto? Buzzer) Buzinar(string roomCode, string connectionId, string palavraInfracao, string tipoInfracao);
    (GameStateDto? Estado, ChatMessageDto? Mensagem) EnviarPalpite(string roomCode, string connectionId, string palpite);
    GameStateDto? FinalizarTempoExplicacao(string roomCode);
    GameStateDto? FinalizarRodada(string roomCode);
    GameStateDto? MarcarCartaParaJulgamento(string roomCode, string connectionId, int cardIndex, bool contestar);
    GameStateDto? ConfirmarSelecaoReanalise(string roomCode, string connectionId);
    (GameStateDto? Estado, PlayedCardDto? Carta, bool EmpateSorteado) VotarJulgamentoCarta(string roomCode, string connectionId, int cardIndex, string opcaoVoto);
    (GameStateDto? Estado, PlayedCardDto? Carta, bool EmpateSorteado) VotarCarta(string roomCode, string connectionId, int cardIndex, string opcaoVoto);
    GameStateDto? ConfirmarProntoTransicao(string roomCode, string connectionId);
    GameStateDto? AvancarRodada(string roomCode, string connectionId);
    GameStateDto? ReiniciarPartida(string roomCode, string connectionId);
}
