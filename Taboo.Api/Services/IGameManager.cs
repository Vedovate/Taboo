// src/Taboo.Api/Services/IGameManager.cs
using System.Collections.Concurrent;
using Taboo.Api.Models;

namespace Taboo.Api.Services
{
    public interface IGameManager
    {
        // Embora o chat de teste não utilize diretamente esta coleção para armazenar
        // as mensagens, é essencial ter a estrutura para gerenciar GameRooms
        // e jogadores no futuro.
        // O GameHub usará esta coleção para verificar a existência de salas e jogadores.
        ConcurrentDictionary<string, GameRoom> GameRooms { get; }

        /// <summary>
        /// Cria uma sala nova com código específico e adiciona o host.
        /// Retorna true apenas se a sala ainda não existir.
        /// </summary>
        bool CreateRoom(string roomCode, string connectionId, string userName);

        /// <summary>
        /// Adiciona um jogador a uma sala de jogo existente.
        /// </summary>
        /// <param name="roomCode">O código da sala.</param>
        /// <param name="connectionId">O ID de conexão SignalR do jogador.</param>
        /// <param name="userName">O nome do usuário.</param>
        void AddPlayerToRoom(string roomCode, string connectionId, string userName);

        /// <summary>
        /// Retorna a lista de jogadores presentes em uma sala.
        /// </summary>
        IReadOnlyList<Player> GetPlayersInRoom(string roomCode);

        /// <summary>
        /// Remove um jogador de uma sala.
        /// </summary>
        /// <param name="roomCode">O código da sala.</param>
        /// <param name="connectionId">O ID de conexão SignalR do jogador.</param>
        void RemovePlayerFromRoom(string roomCode, string connectionId);

        /// <summary>
        /// Verifica se um jogador está em uma sala específica.
        /// </summary>
        /// <param name="roomCode">O código da sala.</param>
        /// <param name="connectionId">O ID de conexão SignalR do jogador.</param>
        /// <returns>Verdadeiro se o jogador estiver na sala, falso caso contrário.</returns>
        bool IsPlayerInRoom(string roomCode, string connectionId);
    }
}