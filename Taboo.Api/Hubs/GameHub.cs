// src/Taboo.Api/Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using Taboo.Api.Services;

namespace Taboo.Api.Hubs
{
    // O GameHub herda de Hub, fornecendo acesso a funcionalidades como Context e Clients.
    public class GameHub : Hub
    {
        private readonly IGameManager _gameManager;

        // Injeção de dependência do GameManager via construtor.
        public GameHub(IGameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// Método chamado por um cliente para entrar em uma sala de jogo.
        /// Adiciona o ConnectionId do cliente a um grupo SignalR.
        /// </summary>
        /// <param name="codigoSala">O código da sala a ser unida.</param>
        /// <param name="nomeUsuario">O nome do usuário que está entrando na sala.</param>
        public async Task EntrarNaSala(string codigoSala, string nomeUsuario)
        {
            // Adiciona o ConnectionId do cliente ao grupo SignalR correspondente à sala.
            // Isso permite que o SignalR envie mensagens para todos os membros do grupo.
            await Groups.AddToGroupAsync(Context.ConnectionId, codigoSala);

            // Registra o jogador no nosso GameManager.
            _gameManager.AddPlayerToRoom(codigoSala, Context.ConnectionId, nomeUsuario);

            // Opcional: Notificar outros clientes que um novo usuário entrou.
            // Para o chat de teste, basta a entrada no grupo.
            await Clients.Group(codigoSala).SendAsync("ReceberMensagemTeste", "Sistema", $"{nomeUsuario} entrou na sala.");

            Console.WriteLine($"Cliente {Context.ConnectionId} ({nomeUsuario}) entrou na sala: {codigoSala}");
        }

        /// <summary>
        /// Método chamado por um cliente para enviar uma mensagem de teste para a sala.
        /// A mensagem será transmitida para todos os clientes no mesmo grupo/sala.
        /// </summary>
        /// <param name="codigoSala">O código da sala para a qual a mensagem será enviada.</param>
        /// <param name="mensagem">O conteúdo da mensagem a ser enviada.</param>
        public async Task EnviarMensagemTeste(string codigoSala, string nomeUsuario, string mensagem)
        {
            // Verifica se o jogador que está enviando a mensagem realmente está na sala.
            // Isso adiciona uma camada básica de segurança e validação.
            if (!_gameManager.IsPlayerInRoom(codigoSala, Context.ConnectionId))
            {
                // Opcional: Enviar uma mensagem de erro de volta ao cliente se ele não estiver na sala.
                // await Clients.Caller.SendAsync("ReceberMensagemTeste", "Sistema", "Você não está nesta sala.");
                Console.WriteLine($"Tentativa de enviar mensagem para {codigoSala} por {Context.ConnectionId} que não está na sala.");
                return;
            }

            // Faz o broadcast da mensagem para todos os clientes no grupo 'codigoSala'.
            // O evento 'ReceberMensagemTeste' será escutado pelos clientes Angular.
            await Clients.Group(codigoSala).SendAsync("ReceberMensagemTeste", nomeUsuario, mensagem);

            Console.WriteLine($"Mensagem recebida na sala {codigoSala} de {nomeUsuario}: {mensagem}");
        }

        /// <summary>
        /// Sobrescreve o método OnDisconnectedAsync para lidar com a desconexão de clientes.
        /// Isso é crucial para limpar o estado e remover o cliente dos grupos.
        /// </summary>
        /// <param name="exception">A exceção que causou a desconexão, se houver.</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Percorre as salas para encontrar onde o cliente estava para removê-lo.
            foreach (var roomEntry in _gameManager.GameRooms)
            {
                var roomCode = roomEntry.Key;
                var gameRoom = roomEntry.Value;

                if (gameRoom.Players.Any(p => p.ConnectionId == Context.ConnectionId))
                {
                    var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                    if (player != null)
                    {
                        // Remove o cliente do grupo SignalR.
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

                        // Remove o jogador do GameManager.
                        _gameManager.RemovePlayerFromRoom(roomCode, Context.ConnectionId);

                        // Opcional: Notificar outros clientes que um usuário saiu.
                        await Clients.Group(roomCode).SendAsync("ReceberMensagemTeste", "Sistema", $"{player.Name} saiu da sala.");
                        Console.WriteLine($"Cliente {Context.ConnectionId} ({player.Name}) desconectou-se da sala: {roomCode}");
                    }
                    break; // Supondo que um cliente está em apenas uma sala por vez.
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}