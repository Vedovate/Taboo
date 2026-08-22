using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tipoo.Api.Data;
using Tipoo.Api.DTOs;
using Tipoo.Api.Models;

namespace Tipoo.Api.Services;

public partial class GameManager : IGameManager
{
    private readonly ILogger<GameManager> _logger;
    private readonly IGameDataStore _dataStore;

    public GameManager(ILogger<GameManager> logger, IGameDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;
    }

    public ConcurrentDictionary<string, GameRoom> GameRooms { get; } = new();

    private static readonly Regex ControlCharsRegex = MyRegex();

    [GeneratedRegex(@"[\u0000-\u001F\u007F\u200B-\u200D\uFEFF\u2060]")]
    private static partial Regex MyRegex();

    private static readonly List<Card> FallbackCards = new()
    {
        new Card { Id = 1, MainWord = "CLIPE", Forbidden1 = "papel", Forbidden2 = "escritório", Forbidden3 = "grampo", Forbidden4 = "metal", Forbidden5 = "junto", Difficulty = "Fácil", Category = "Objeto" },
        new Card { Id = 2, MainWord = "SOFTWARE", Forbidden1 = "programa", Forbidden2 = "computador", Forbidden3 = "instalar", Forbidden4 = "CD-ROM", Forbidden5 = "linguagem", Difficulty = "Fácil", Category = "Tecnologia" },
        new Card { Id = 3, MainWord = "INTELIGENTE", Forbidden1 = "burro", Forbidden2 = "esperto", Forbidden3 = "intelectual", Forbidden4 = "brilhante", Forbidden5 = "estúpido", Difficulty = "Médio", Category = "Adjetivo" },
        new Card { Id = 4, MainWord = "ÂNCORA", Forbidden1 = "navio", Forbidden2 = "barco", Forbidden3 = "noticiário", Forbidden4 = "jogar", Forbidden5 = "içar", Difficulty = "Fácil", Category = "Objeto" },
        new Card { Id = 5, MainWord = "PRISÃO", Forbidden1 = "cadeia", Forbidden2 = "grades", Forbidden3 = "cárcere", Forbidden4 = "cela", Forbidden5 = "criminoso", Difficulty = "Fácil", Category = "Local" },
        new Card { Id = 6, MainWord = "ROXO", Forbidden1 = "cor", Forbidden2 = "azul", Forbidden3 = "violeta", Forbidden4 = "raiva", Forbidden5 = "lavanda", Difficulty = "Fácil", Category = "Cor" },
        new Card { Id = 7, MainWord = "MARACUJÁ", Forbidden1 = "rugas", Forbidden2 = "azedo", Forbidden3 = "semente", Forbidden4 = "fruta", Forbidden5 = "amarelo", Difficulty = "Fácil", Category = "Alimento" },
        new Card { Id = 8, MainWord = "AUSTRÁLIA", Forbidden1 = "canguru", Forbidden2 = "Sidnei", Forbidden3 = "coala", Forbidden4 = "Dundee", Forbidden5 = "Oceania", Difficulty = "Médio", Category = "Geografia" },
    };

    public GameRoom GetOrCreateRoom(string roomCode)
    {
        return GameRooms.GetOrAdd(roomCode, code => new GameRoom
        {
            RoomCode = code
        });
    }

    public bool RoomExists(string roomCode)
    {
        return GameRooms.ContainsKey(roomCode);
    }

    public bool CreateRoom(string roomCode, string connectionId, string userName, string hostSessionId = "")
    {
        var newRoom = new GameRoom
        {
            RoomCode = roomCode,
            HostSessionId = hostSessionId
        };

        if (!string.IsNullOrEmpty(hostSessionId))
        {
            try
            {
                var cachedSettings = _dataStore.LoadHostSettings(hostSessionId);
                if (cachedSettings is not null)
                {
                    newRoom.Settings = cachedSettings;
                    _logger.LogInformation("Sala {RoomCode} criada com configurações em cache do host {HostSessionId}", roomCode, hostSessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao carregar configurações em cache do host {HostSessionId}", hostSessionId);
            }
        }

        if (!GameRooms.TryAdd(roomCode, newRoom))
        {
            _logger.LogWarning("Falha ao criar sala {RoomCode} — já existe", roomCode);
            return false;
        }

        lock (newRoom)
        {
            newRoom.Players.Add(new Player
            {
                ConnectionId = connectionId,
                Name = userName,
                IsHost = true
            });
        }

        _logger.LogInformation("Sala {RoomCode} criada por {UserName} ({ConnectionId})", roomCode, userName, connectionId);
        return true;
    }

    public void AddPlayerToRoom(string roomCode, string connectionId, string userName)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de adicionar jogador a sala inexistente {RoomCode}", roomCode);
            return;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                var finalName = userName;
                if (gameRoom.Players.Any(p => p.Name == finalName))
                {
                    finalName = GetFallbackName(gameRoom);
                }

                gameRoom.Players.Add(new Player
                {
                    ConnectionId = connectionId,
                    Name = finalName
                });
                _logger.LogInformation("Jogador {ConnectionId} entrou na sala {RoomCode}", connectionId, roomCode);
                return;
            }

            var reconnectName = userName;
            if (gameRoom.Players.Any(p => p.ConnectionId != connectionId && p.Name == reconnectName))
            {
                reconnectName = GetFallbackName(gameRoom);
            }
            player.Name = reconnectName;
            _logger.LogInformation("Jogador {ConnectionId} reconectou na sala {RoomCode}", connectionId, roomCode);
        }
    }

    private static string GetFallbackName(GameRoom gameRoom)
    {
        var counter = gameRoom.Players.Count + 1;
        while (gameRoom.Players.Any(p => p.Name == $"Jogador {counter}"))
        {
            counter++;
        }
        return $"Jogador {counter}";
    }

    public string? RenamePlayer(string roomCode, string connectionId, string newName)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de renomear jogador em sala inexistente {RoomCode}", roomCode);
            return null;
        }

        var sanitized = ControlCharsRegex.Replace(newName.Trim(), "");

        if (sanitized.Length < 1 || sanitized.Length > 16)
        {
            _logger.LogWarning("Nome inválido '{NewName}' (sanitizado: '{Sanitized}') — fora do tamanho permitido", newName, sanitized);
            return null;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("Jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return null;
            }

            if (gameRoom.Players.Any(p => p.ConnectionId != connectionId && p.Name == sanitized))
            {
                _logger.LogWarning("Nome {NewName} já está em uso na sala {RoomCode}", sanitized, roomCode);
                return null;
            }

            player.Name = sanitized;
            _logger.LogInformation("Jogador {ConnectionId} renomeado para {NewName} na sala {RoomCode}", connectionId, sanitized, roomCode);
            return sanitized;
        }
    }

    public IReadOnlyList<Player> GetPlayersInRoom(string roomCode)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return Array.Empty<Player>();
        }

        lock (gameRoom)
        {
            return gameRoom.Players.ToList();
        }
    }

    public void RemovePlayerFromRoom(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("Tentativa de remover jogador de sala inexistente {RoomCode}", roomCode);
            return;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("Jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return;
            }

            var userName = player.Name;
            var eraHost = player.IsHost;
            gameRoom.Players.Remove(player);
            _logger.LogInformation("Jogador {UserName} ({ConnectionId}) saiu da sala {RoomCode}", userName, connectionId, roomCode);

            if (gameRoom.Players.Count == 0)
            {
                GameRooms.TryRemove(roomCode, out _);
                _logger.LogInformation("Sala {RoomCode} removida — sem jogadores restantes", roomCode);
            }
            else if (eraHost)
            {
                var novoHost = gameRoom.Players[0];
                novoHost.IsHost = true;
                _logger.LogInformation("Host transferido para {ConnectionId} ({UserName}) na sala {RoomCode}", novoHost.ConnectionId, novoHost.Name, roomCode);
            }
        }
    }

    public bool IsHost(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return false;
        }

        lock (gameRoom)
        {
            return gameRoom.Players.Any(item => item.ConnectionId == connectionId && item.IsHost);
        }
    }

    public bool IsPlayerInRoom(string roomCode, string connectionId)
    {
        return GameRooms.TryGetValue(roomCode, out var gameRoom) && gameRoom.Players.Any(item => item.ConnectionId == connectionId);
    }

    public string? TryGetPlayerName(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            return gameRoom.Players.FirstOrDefault(item => item.ConnectionId == connectionId)?.Name;
        }
    }

    public GameRoom? GetRoom(string roomCode)
    {
        return GameRooms.TryGetValue(roomCode, out var gameRoom) ? gameRoom : null;
    }

    public string? GetRoomCodeByConnectionId(string connectionId)
    {
        foreach (var room in GameRooms)
        {
            if (room.Value.Players.Any(player => player.ConnectionId == connectionId))
            {
                return room.Key;
            }
        }

        _logger.LogDebug("Nenhuma sala encontrada para conexão {ConnectionId}", connectionId);
        return null;
    }

    public bool EscolherTime(string roomCode, string connectionId, string cor)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("EscolherTime: sala {RoomCode} não encontrada", roomCode);
            return false;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("EscolherTime: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            if (player.Team == cor)
            {
                player.Team = string.Empty;
                player.IsReady = false;
                _logger.LogInformation("Jogador {ConnectionId} saiu do time {Cor} na sala {RoomCode}", connectionId, cor, roomCode);
                return true;
            }

            var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
            var teamCount = gameRoom.Players.Count(p => p.Team == cor);
            if (teamCount >= maxPerTeam)
            {
                _logger.LogWarning("EscolherTime: time {Cor} cheio ({Count}/{Max}) na sala {RoomCode}", cor, teamCount, maxPerTeam, roomCode);
                return false;
            }

            player.Team = cor;
            player.IsReady = false;
            _logger.LogInformation("Jogador {ConnectionId} entrou no time {Cor} na sala {RoomCode}", connectionId, cor, roomCode);
            return true;
        }
    }

    public (bool IsReady, bool TodosProntosIniciou, GameStateDto? Estado) AlternarPronto(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("AlternarPronto: sala {RoomCode} não encontrada", roomCode);
            return (false, false, null);
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("AlternarPronto: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return (false, false, null);
            }

            if (string.IsNullOrEmpty(player.Team))
            {
                _logger.LogWarning("AlternarPronto: jogador {ConnectionId} não está em nenhum time na sala {RoomCode}", connectionId, roomCode);
                return (false, false, null);
            }

            player.IsReady = !player.IsReady;
            _logger.LogInformation("Jogador {ConnectionId} alternou pronto para {Pronto} na sala {RoomCode}", connectionId, player.IsReady, roomCode);

            // Verifica se todos os jogadores estão prontos e há pelo menos 2 jogadores (com no mínimo 1 em cada time)
            bool todosProntos = gameRoom.Players.Count >= 2
                && gameRoom.Players.All(p => p.IsReady && !string.IsNullOrEmpty(p.Team))
                && gameRoom.Players.Any(p => p.Team == "Vermelho")
                && gameRoom.Players.Any(p => p.Team == "Azul");

            if (todosProntos)
            {
                _logger.LogInformation("Todos os jogadores estão prontos na sala {RoomCode} — iniciando partida automaticamente!", roomCode);
                IniciarPartidaInterno(gameRoom);
                return (player.IsReady, true, CriarGameStateDto(gameRoom, string.Empty));
            }

            return (player.IsReady, false, null);
        }
    }

    public string? RandomizarTime(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("RandomizarTime: sala {RoomCode} não encontrada", roomCode);
            return null;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("RandomizarTime: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return null;
            }

            var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
            var redCount = gameRoom.Players.Count(p => p.Team == "Vermelho" && p.ConnectionId != connectionId);
            var blueCount = gameRoom.Players.Count(p => p.Team == "Azul" && p.ConnectionId != connectionId);

            var available = new List<string>();
            if (redCount < maxPerTeam) available.Add("Vermelho");
            if (blueCount < maxPerTeam) available.Add("Azul");

            if (available.Count == 0)
            {
                _logger.LogWarning("RandomizarTime: nenhum time disponível na sala {RoomCode}", roomCode);
                return null;
            }

            var chosen = available[Random.Shared.Next(available.Count)];
            player.Team = chosen;
            player.IsReady = false;
            _logger.LogInformation("Jogador {ConnectionId} foi para o time {Cor} (aleatório) na sala {RoomCode}", connectionId, chosen, roomCode);
            return chosen;
        }
    }

    public bool ForcarIniciar(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("ForcarIniciar: sala {RoomCode} não encontrada", roomCode);
            return false;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null)
            {
                _logger.LogWarning("ForcarIniciar: jogador {ConnectionId} não encontrado na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            if (!player.IsHost)
            {
                _logger.LogWarning("ForcarIniciar: jogador {ConnectionId} não é host na sala {RoomCode}", connectionId, roomCode);
                return false;
            }

            IniciarPartidaInterno(gameRoom);
            return true;
        }
    }

    private void IniciarPartidaInterno(GameRoom gameRoom)
    {
        // Aloca jogadores sem time
        var unassigned = gameRoom.Players.Where(p => string.IsNullOrEmpty(p.Team)).ToList();
        foreach (var unassignedPlayer in unassigned)
        {
            var maxPerTeam = (int)Math.Ceiling(gameRoom.Players.Count / 2.0);
            var redCount = gameRoom.Players.Count(p => p.Team == "Vermelho");
            var blueCount = gameRoom.Players.Count(p => p.Team == "Azul");

            if (redCount <= blueCount && redCount < maxPerTeam)
            {
                unassignedPlayer.Team = "Vermelho";
            }
            else
            {
                unassignedPlayer.Team = "Azul";
            }
            unassignedPlayer.IsReady = false;
        }

        // Inicializa deck de cartas
        IReadOnlyList<Card>? dbCards = null;
        try
        {
            dbCards = _dataStore.GetCardsForGame(gameRoom.Settings.Difficulties, 120);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar GetCardsForGame — tentando GetAllCards");
        }

        if (dbCards == null || dbCards.Count == 0)
        {
            try
            {
                dbCards = _dataStore.GetAllCards();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao obter baralho de cartas do banco");
            }
        }

        var listCards = dbCards != null && dbCards.Count > 0 ? dbCards.ToList() : FallbackCards.ToList();
        Shuffle(listCards);
        gameRoom.Deck = listCards;
        gameRoom.CurrentCardIndex = 0;
        gameRoom.CurrentCardStartedAt = DateTime.UtcNow;

        // Reset de placares e rodada
        gameRoom.IsActive = true;
        gameRoom.RoundNumber = 1;
        gameRoom.ScoreTeamRed = 0;
        gameRoom.ScoreTeamBlue = 0;
        gameRoom.RoundScore = 0;
        gameRoom.SkipsLeft = gameRoom.Settings.SkipLimit;
        gameRoom.TimeRemaining = gameRoom.Settings.RoundTimeSeconds;
        gameRoom.Phase = "jogando";
        gameRoom.RoundCards = new List<PlayedCardDto>();
        gameRoom.ContestedCardIndexes = new List<int>();
        gameRoom.CurrentJudgingIndex = 0;
        gameRoom.ReadyToAdvancePlayerIds = new HashSet<string>();
        gameRoom.ActiveBuzzer = null;
        gameRoom.ChatMessages = new List<ChatMessageDto>();

        // Reseta estatísticas
        gameRoom.PlayerCorrectCounts = new Dictionary<string, int>();
        gameRoom.PlayerBuzzerCounts = new Dictionary<string, int>();
        gameRoom.PlayerBuzzedCounts = new Dictionary<string, int>();
        gameRoom.FastestCardWord = string.Empty;
        gameRoom.FastestCardSeconds = int.MaxValue;
        gameRoom.TotalCorrect = 0;
        gameRoom.TotalErrors = 0;
        gameRoom.TotalSkips = 0;
        gameRoom.TotalContestedReversed = 0;

        // Define time inicial
        var startTeam = gameRoom.Settings.StartingTeam.ToLowerInvariant();
        if (startTeam == "vermelho") gameRoom.CurrentTurnTeam = "Vermelho";
        else if (startTeam == "azul") gameRoom.CurrentTurnTeam = "Azul";
        else gameRoom.CurrentTurnTeam = Random.Shared.Next(2) == 0 ? "Vermelho" : "Azul";

        // Define explicador inicial
        gameRoom.LastSpokespersonIndexRed = -1;
        gameRoom.LastSpokespersonIndexBlue = -1;
        AtribuirProximoExplicador(gameRoom);

        // Registro de início de partida no banco
        RegistrarInicioDePartida(gameRoom);
        _logger.LogInformation("Partida iniciada com sucesso na sala {RoomCode}", gameRoom.RoomCode);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private static void AtribuirProximoExplicador(GameRoom gameRoom)
    {
        var teamPlayers = gameRoom.Players.Where(p => p.Team == gameRoom.CurrentTurnTeam).ToList();
        if (teamPlayers.Count == 0)
        {
            gameRoom.CurrentSpokespersonId = string.Empty;
            gameRoom.CurrentSpokespersonName = "Sem Jogador";
            return;
        }

        if (gameRoom.CurrentTurnTeam == "Vermelho")
        {
            gameRoom.LastSpokespersonIndexRed = (gameRoom.LastSpokespersonIndexRed + 1) % teamPlayers.Count;
            var sp = teamPlayers[gameRoom.LastSpokespersonIndexRed];
            gameRoom.CurrentSpokespersonId = sp.ConnectionId;
            gameRoom.CurrentSpokespersonName = sp.Name;
        }
        else
        {
            gameRoom.LastSpokespersonIndexBlue = (gameRoom.LastSpokespersonIndexBlue + 1) % teamPlayers.Count;
            var sp = teamPlayers[gameRoom.LastSpokespersonIndexBlue];
            gameRoom.CurrentSpokespersonId = sp.ConnectionId;
            gameRoom.CurrentSpokespersonName = sp.Name;
        }
    }

    private void RegistrarInicioDePartida(GameRoom gameRoom)
    {
        if (gameRoom.MatchKey is not null)
        {
            return;
        }

        try
        {
            var startedAt = DateTime.UtcNow;
            gameRoom.StartedAt = startedAt;
            gameRoom.MatchKey = $"{gameRoom.RoomCode}-{startedAt:yyyyMMddTHHmmssfff}Z";

            _dataStore.CreateMatch(new GameMatch
            {
                MatchKey = gameRoom.MatchKey,
                RoomCode = gameRoom.RoomCode,
                HostSessionId = gameRoom.HostSessionId,
                StartedAt = startedAt,
                SettingsJson = JsonSerializer.Serialize(gameRoom.Settings),
                StartedPlayers = gameRoom.Players.Count,
                WasStarted = true,
                Completed = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar início da partida na sala {RoomCode}", gameRoom.RoomCode);
        }
    }

    public GameSettings? ConfigurarPartida(string roomCode, string connectionId, GameSettings settings)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("ConfigurarPartida: sala {RoomCode} não encontrada", roomCode);
            return null;
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player is null || !player.IsHost)
            {
                _logger.LogWarning("ConfigurarPartida: jogador {ConnectionId} não é host na sala {RoomCode}", connectionId, roomCode);
                return null;
            }

            if (!TryNormalizarSettings(settings, out var normalized))
            {
                _logger.LogWarning("ConfigurarPartida: configurações inválidas na sala {RoomCode}", roomCode);
                return null;
            }

            gameRoom.Settings = normalized;

            if (!string.IsNullOrEmpty(gameRoom.HostSessionId))
            {
                _dataStore.SaveHostSettings(gameRoom.HostSessionId, normalized);
            }

            _logger.LogInformation("Configurações atualizadas na sala {RoomCode} pelo host {ConnectionId}", roomCode, connectionId);
            return normalized;
        }
    }

    public GameSettings ObterConfiguracoes(string roomCode)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            _logger.LogWarning("ObterConfiguracoes: sala {RoomCode} não encontrada", roomCode);
            return new GameSettings();
        }

        lock (gameRoom)
        {
            return gameRoom.Settings;
        }
    }

    private static bool TryNormalizarSettings(GameSettings settings, out GameSettings normalized)
    {
        normalized = settings.Clone();

        normalized.RoundTimeSeconds = Math.Clamp(settings.RoundTimeSeconds, GameSettings.MinRoundTimeSeconds, GameSettings.MaxRoundTimeSeconds);
        normalized.ExplanationTimeSeconds = Math.Clamp(settings.ExplanationTimeSeconds, 0, GameSettings.MaxExplanationTimeSeconds);
        normalized.SkipLimit = Math.Clamp(settings.SkipLimit, 0, GameSettings.MaxSkipLimit);
        normalized.PointsPerCorrect = Math.Clamp(settings.PointsPerCorrect, 0, GameSettings.MaxPoints);
        normalized.PointsPerError = Math.Clamp(settings.PointsPerError, 0, GameSettings.MaxPoints);
        normalized.PointsPerSkip = Math.Clamp(settings.PointsPerSkip, 0, GameSettings.MaxPoints);
        normalized.PauseBetweenRoundsSeconds = Math.Clamp(settings.PauseBetweenRoundsSeconds, GameSettings.MinPauseBetweenRoundsSeconds, GameSettings.MaxPauseBetweenRoundsSeconds);

        if (settings.NumberOfRounds < GameSettings.MinNumberOfRounds
            || settings.NumberOfRounds > GameSettings.MaxNumberOfRounds
            || settings.NumberOfRounds % 2 != 0)
        {
            return false;
        }

        if (settings.TipooLeadLimit.HasValue && settings.TipooLeadLimit.Value < GameSettings.MinTipooLeadLimit)
        {
            return false;
        }

        if (settings.Difficulties.Count == 0)
        {
            return false;
        }

        if (settings.StartingTeam is not ("azul" or "vermelho" or "aleatorio"))
        {
            return false;
        }

        if (settings.TiebreakMode is not ("empatado" or "rodada-extra"))
        {
            return false;
        }

        normalized.Difficulties = settings.Difficulties.Distinct().ToList();
        normalized.BuzzerSounds = settings.BuzzerSounds.Distinct().ToList();
        return true;
    }

    // =========================================================================
    // MÉTODOS DE JOGO EM TEMPO REAL
    // =========================================================================

    public GameStateDto? ObterEstadoJogo(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    public GameStateDto? AcertarCarta(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            if (!gameRoom.IsActive || gameRoom.Phase != "jogando")
            {
                return null;
            }

            var card = GetCurrentCard(gameRoom);
            if (card is null)
            {
                return null;
            }

            var pts = gameRoom.Settings.PointsPerCorrect;
            if (gameRoom.CurrentTurnTeam == "Vermelho") gameRoom.ScoreTeamRed += pts;
            else gameRoom.ScoreTeamBlue += pts;

            gameRoom.RoundScore += pts;
            gameRoom.TotalCorrect++;

            // MVP stats
            if (!string.IsNullOrEmpty(gameRoom.CurrentSpokespersonName))
            {
                gameRoom.PlayerCorrectCounts[gameRoom.CurrentSpokespersonName] =
                    gameRoom.PlayerCorrectCounts.GetValueOrDefault(gameRoom.CurrentSpokespersonName) + 1;
            }

            var elapsed = (int)Math.Max((DateTime.UtcNow - gameRoom.CurrentCardStartedAt).TotalSeconds, 1);
            if (elapsed < gameRoom.FastestCardSeconds)
            {
                gameRoom.FastestCardSeconds = elapsed;
                gameRoom.FastestCardWord = card.MainWord;
            }

            gameRoom.RoundCards.Add(new PlayedCardDto(
                CardIndex: gameRoom.RoundCards.Count,
                CardId: card.Id,
                MainWord: card.MainWord,
                Forbidden: new List<string> { card.Forbidden1, card.Forbidden2, card.Forbidden3, card.Forbidden4, card.Forbidden5 },
                Status: "Acertou"
            ));

            gameRoom.ChatMessages.Add(new ChatMessageDto(
                Id: Guid.NewGuid().ToString("N"),
                ConnectionId: connectionId,
                AuthorName: "Sistema",
                Team: gameRoom.CurrentTurnTeam,
                Message: $"🎯 A palavra \"{card.MainWord}\" foi acertada!",
                IsGuess: false,
                IsCorrect: true,
                Timestamp: DateTime.UtcNow
            ));
            if (gameRoom.ChatMessages.Count > 100) gameRoom.ChatMessages.RemoveAt(0);

            gameRoom.CurrentCardIndex++;
            gameRoom.CurrentCardStartedAt = DateTime.UtcNow;

            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    public GameStateDto? PularCarta(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            if (!gameRoom.IsActive || gameRoom.Phase != "jogando")
            {
                return null;
            }

            if (gameRoom.SkipsLeft <= 0)
            {
                return null;
            }

            var card = GetCurrentCard(gameRoom);
            if (card is null)
            {
                return null;
            }

            gameRoom.SkipsLeft--;
            gameRoom.TotalSkips++;

            if (gameRoom.Settings.SkipCostsPoints)
            {
                var pts = gameRoom.Settings.PointsPerSkip;
                if (gameRoom.CurrentTurnTeam == "Vermelho") gameRoom.ScoreTeamRed = Math.Max(0, gameRoom.ScoreTeamRed - pts);
                else gameRoom.ScoreTeamBlue = Math.Max(0, gameRoom.ScoreTeamBlue - pts);
                gameRoom.RoundScore -= pts;
            }

            gameRoom.RoundCards.Add(new PlayedCardDto(
                CardIndex: gameRoom.RoundCards.Count,
                CardId: card.Id,
                MainWord: card.MainWord,
                Forbidden: new List<string> { card.Forbidden1, card.Forbidden2, card.Forbidden3, card.Forbidden4, card.Forbidden5 },
                Status: "Pulou"
            ));

            gameRoom.ChatMessages.Add(new ChatMessageDto(
                Id: Guid.NewGuid().ToString("N"),
                ConnectionId: connectionId,
                AuthorName: "Sistema",
                Team: gameRoom.CurrentTurnTeam,
                Message: $"⏭️ A palavra \"{card.MainWord}\" foi pulada.",
                IsGuess: false,
                IsCorrect: false,
                Timestamp: DateTime.UtcNow
            ));
            if (gameRoom.ChatMessages.Count > 100) gameRoom.ChatMessages.RemoveAt(0);

            gameRoom.CurrentCardIndex++;
            gameRoom.CurrentCardStartedAt = DateTime.UtcNow;

            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    public (GameStateDto? Estado, BuzzerEventDto? Buzzer) Buzinar(string roomCode, string connectionId, string palavraInfracao, string tipoInfracao)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return (null, null);
        }

        lock (gameRoom)
        {
            if (!gameRoom.IsActive || (gameRoom.Phase != "jogando" && gameRoom.Phase != "explicacao_buzina"))
            {
                return (null, null);
            }

            var watcher = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            var watcherTeam = watcher?.Team ?? (gameRoom.CurrentTurnTeam == "Vermelho" ? "Azul" : "Vermelho");
            var watcherName = watcher?.Name ?? "Vigia";

            var card = GetCurrentCard(gameRoom);
            if (card is null)
            {
                return (null, null);
            }

            var pts = gameRoom.Settings.PointsPerError;
            if (gameRoom.CurrentTurnTeam == "Vermelho") gameRoom.ScoreTeamRed = Math.Max(0, gameRoom.ScoreTeamRed - pts);
            else gameRoom.ScoreTeamBlue = Math.Max(0, gameRoom.ScoreTeamBlue - pts);

            gameRoom.RoundScore -= pts;
            gameRoom.TotalErrors++;

            // Stats
            gameRoom.PlayerBuzzerCounts[watcherName] = gameRoom.PlayerBuzzerCounts.GetValueOrDefault(watcherName) + 1;
            if (!string.IsNullOrEmpty(gameRoom.CurrentSpokespersonName))
            {
                gameRoom.PlayerBuzzedCounts[gameRoom.CurrentSpokespersonName] =
                    gameRoom.PlayerBuzzedCounts.GetValueOrDefault(gameRoom.CurrentSpokespersonName) + 1;
            }

            var buzzerEvent = new BuzzerEventDto(
                BuzzedByConnectionId: connectionId,
                BuzzedByName: watcherName,
                BuzzerTeam: watcherTeam,
                InfractionWord: string.IsNullOrWhiteSpace(palavraInfracao) ? "Outros" : palavraInfracao,
                InfractionType: string.IsNullOrWhiteSpace(tipoInfracao) ? "Palavra Proibida" : tipoInfracao,
                ExplanationTimeSeconds: gameRoom.Settings.ExplanationTimeSeconds
            );

            gameRoom.RoundCards.Add(new PlayedCardDto(
                CardIndex: gameRoom.RoundCards.Count,
                CardId: card.Id,
                MainWord: card.MainWord,
                Forbidden: new List<string> { card.Forbidden1, card.Forbidden2, card.Forbidden3, card.Forbidden4, card.Forbidden5 },
                Status: "Errou",
                BuzzedByName: watcherName,
                InfractionWord: buzzerEvent.InfractionWord,
                InfractionType: buzzerEvent.InfractionType
            ));

            // Registra mensagem rica no histórico da rodada
            gameRoom.ChatMessages.Add(new ChatMessageDto(
                Id: Guid.NewGuid().ToString("N"),
                ConnectionId: connectionId,
                AuthorName: "Fiscal",
                Team: watcherTeam,
                Message: $"🚨 A palavra \"{card.MainWord}\" foi rejeitada por {watcherName} (Motivo: {buzzerEvent.InfractionWord} - {buzzerEvent.InfractionType})",
                IsGuess: false,
                IsCorrect: false,
                Timestamp: DateTime.UtcNow
            ));
            if (gameRoom.ChatMessages.Count > 100) gameRoom.ChatMessages.RemoveAt(0);

            if (gameRoom.Settings.ExplanationTimeSeconds > 0)
            {
                gameRoom.Phase = "explicacao_buzina";
                gameRoom.ActiveBuzzer = buzzerEvent;
            }

            gameRoom.CurrentCardIndex++;
            gameRoom.CurrentCardStartedAt = DateTime.UtcNow;

            return (CriarGameStateDto(gameRoom, connectionId), buzzerEvent);
        }
    }

    public (GameStateDto? Estado, ChatMessageDto? Mensagem) EnviarPalpite(string roomCode, string connectionId, string palpite)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return (null, null);
        }

        lock (gameRoom)
        {
            var player = gameRoom.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            var senderName = player?.Name ?? "Jogador";
            var senderTeam = player?.Team ?? string.Empty;

            var raw = palpite.Trim();
            var card = GetCurrentCard(gameRoom);
            bool isCorrect = false;

            if (card is not null && gameRoom.Phase == "jogando" && senderTeam == gameRoom.CurrentTurnTeam && connectionId != gameRoom.CurrentSpokespersonId)
            {
                var normalizedGuess = RemoveAccents(raw).ToUpperInvariant();
                var normalizedMain = RemoveAccents(card.MainWord).ToUpperInvariant();
                if (normalizedGuess == normalizedMain)
                {
                    isCorrect = true;
                }
            }

            var msg = new ChatMessageDto(
                Id: Guid.NewGuid().ToString("N"),
                ConnectionId: connectionId,
                AuthorName: senderName,
                Team: senderTeam,
                Message: raw,
                IsGuess: true,
                IsCorrect: isCorrect,
                Timestamp: DateTime.UtcNow
            );

            gameRoom.ChatMessages.Add(msg);
            if (gameRoom.ChatMessages.Count > 100)
            {
                gameRoom.ChatMessages.RemoveAt(0);
            }

            if (isCorrect)
            {
                // Pontua acerto
                var pts = gameRoom.Settings.PointsPerCorrect;
                if (gameRoom.CurrentTurnTeam == "Vermelho") gameRoom.ScoreTeamRed += pts;
                else gameRoom.ScoreTeamBlue += pts;

                gameRoom.RoundScore += pts;
                gameRoom.TotalCorrect++;

                gameRoom.PlayerCorrectCounts[senderName] = gameRoom.PlayerCorrectCounts.GetValueOrDefault(senderName) + 1;

                var elapsed = (int)Math.Max((DateTime.UtcNow - gameRoom.CurrentCardStartedAt).TotalSeconds, 1);
                if (elapsed < gameRoom.FastestCardSeconds)
                {
                    gameRoom.FastestCardSeconds = elapsed;
                    gameRoom.FastestCardWord = card!.MainWord;
                }

                gameRoom.RoundCards.Add(new PlayedCardDto(
                    CardIndex: gameRoom.RoundCards.Count,
                    CardId: card!.Id,
                    MainWord: card.MainWord,
                    Forbidden: new List<string> { card.Forbidden1, card.Forbidden2, card.Forbidden3, card.Forbidden4, card.Forbidden5 },
                    Status: "Acertou"
                ));

                gameRoom.CurrentCardIndex++;
                gameRoom.CurrentCardStartedAt = DateTime.UtcNow;
            }

            return (CriarGameStateDto(gameRoom, connectionId), msg);
        }
    }

    public GameStateDto? FinalizarTempoExplicacao(string roomCode)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            if (gameRoom.Phase == "explicacao_buzina")
            {
                gameRoom.Phase = "jogando";
                gameRoom.ActiveBuzzer = null;
            }
            return CriarGameStateDto(gameRoom, string.Empty);
        }
    }

    public GameStateDto? FinalizarRodada(string roomCode)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            gameRoom.Phase = "selecao_reanalise";
            gameRoom.ActiveBuzzer = null;
            gameRoom.ContestedCardIndexes.Clear();
            gameRoom.ReadyToAdvancePlayerIds.Clear();
            gameRoom.CurrentJudgingIndex = 0;
            gameRoom.PhaseTimeRemaining = gameRoom.Settings.ReanalysisSelectionTimeSeconds;
            _logger.LogInformation("Fase de seleção para reanálise iniciada na sala {RoomCode}", roomCode);
            return CriarGameStateDto(gameRoom, string.Empty);
        }
    }

    public GameStateDto? MarcarCartaParaJulgamento(string roomCode, string connectionId, int cardIndex, bool contestar)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            if (cardIndex < 0 || cardIndex >= gameRoom.RoundCards.Count)
            {
                return null;
            }

            var card = gameRoom.RoundCards[cardIndex];
            gameRoom.RoundCards[cardIndex] = card with { IsContested = contestar };

            if (contestar && !gameRoom.ContestedCardIndexes.Contains(cardIndex))
            {
                gameRoom.ContestedCardIndexes.Add(cardIndex);
            }
            else if (!contestar)
            {
                gameRoom.ContestedCardIndexes.Remove(cardIndex);
            }

            _logger.LogInformation("Carta {Index} ({Word}) marcada para julgamento={Contestar} na sala {RoomCode}", cardIndex, card.MainWord, contestar, roomCode);
            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    public GameStateDto? ConfirmarSelecaoReanalise(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            gameRoom.ReadyToAdvancePlayerIds.Add(connectionId);
            _logger.LogInformation("Jogador {ConnectionId} confirmou seleção de reanálise na sala {RoomCode} ({Ready}/{Total})",
                connectionId, roomCode, gameRoom.ReadyToAdvancePlayerIds.Count, gameRoom.Players.Count);

            bool todosConfirmaram = gameRoom.Players.Count > 0 && gameRoom.Players.All(p => gameRoom.ReadyToAdvancePlayerIds.Contains(p.ConnectionId));
            if (todosConfirmaram)
            {
                gameRoom.ReadyToAdvancePlayerIds.Clear();
                if (gameRoom.ContestedCardIndexes.Count > 0)
                {
                    gameRoom.Phase = "julgamento_carta";
                    gameRoom.CurrentJudgingIndex = 0;
                    gameRoom.PhaseTimeRemaining = gameRoom.Settings.CardJudgingTimeSeconds;
                    _logger.LogInformation("Avançando para julgamento carta por carta na sala {RoomCode} ({Count} cartas)", roomCode, gameRoom.ContestedCardIndexes.Count);
                }
                else
                {
                    gameRoom.Phase = "resumo_rodada";
                    gameRoom.PhaseTimeRemaining = gameRoom.Settings.PauseBetweenRoundsSeconds;
                    _logger.LogInformation("Nenhuma carta para reanálise — avançando para resumo da rodada na sala {RoomCode}", roomCode);
                }
            }

            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    public (GameStateDto? Estado, PlayedCardDto? Carta, bool EmpateSorteado) VotarJulgamentoCarta(string roomCode, string connectionId, int cardIndex, string opcaoVoto)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return (null, null, false);
        }

        lock (gameRoom)
        {
            if (cardIndex < 0 || cardIndex >= gameRoom.RoundCards.Count)
            {
                return (null, null, false);
            }

            var card = gameRoom.RoundCards[cardIndex];
            var votes = card.PlayerVotes != null ? new Dictionary<string, string>(card.PlayerVotes) : new Dictionary<string, string>();
            var normalizedOption = opcaoVoto.ToLowerInvariant(); // "acerto", "erro", "anular"
            votes[connectionId] = normalizedOption;

            int countAcerto = votes.Values.Count(v => v == "acerto" || v == "aceitar");
            int countErro = votes.Values.Count(v => v == "erro" || v == "reverter");
            int countAnular = votes.Values.Count(v => v == "anular");

            bool empateSorteado = false;
            string newStatus = card.Status;
            bool todosVotaram = gameRoom.Players.Count > 0 && gameRoom.Players.All(p => votes.ContainsKey(p.ConnectionId));

            if (todosVotaram)
            {
                // Resolução do julgamento
                if (countAcerto > countErro && countAcerto > countAnular)
                {
                    if (card.Status == "Errou")
                    {
                        newStatus = "Acertou";
                        AjustarPontosReversao(gameRoom, -gameRoom.Settings.PointsPerError, gameRoom.Settings.PointsPerCorrect);
                        gameRoom.TotalContestedReversed++;
                    }
                    else if (card.Status == "Anulada")
                    {
                        newStatus = "Acertou";
                        AjustarPontosReversao(gameRoom, 0, gameRoom.Settings.PointsPerCorrect);
                        gameRoom.TotalContestedReversed++;
                    }
                }
                else if (countErro > countAcerto && countErro > countAnular)
                {
                    if (card.Status == "Acertou")
                    {
                        newStatus = "Errou";
                        AjustarPontosReversao(gameRoom, -gameRoom.Settings.PointsPerCorrect, -gameRoom.Settings.PointsPerError);
                        gameRoom.TotalContestedReversed++;
                    }
                    else if (card.Status == "Anulada")
                    {
                        newStatus = "Errou";
                        AjustarPontosReversao(gameRoom, 0, -gameRoom.Settings.PointsPerError);
                        gameRoom.TotalContestedReversed++;
                    }
                }
                else if (countAnular > countAcerto && countAnular >= countErro)
                {
                    if (card.Status == "Acertou") AjustarPontosReversao(gameRoom, -gameRoom.Settings.PointsPerCorrect, 0);
                    else if (card.Status == "Errou") AjustarPontosReversao(gameRoom, gameRoom.Settings.PointsPerError, 0);
                    newStatus = "Anulada";
                    gameRoom.TotalContestedReversed++;
                }
                else if (countAcerto == countErro && countAcerto > 0)
                {
                    // Empate partidário por time — decisão por sorteio/moeda!
                    empateSorteado = true;
                    bool sorteadoAcerto = Random.Shared.Next(2) == 0;
                    if (sorteadoAcerto)
                    {
                        if (card.Status == "Errou")
                        {
                            newStatus = "Acertou";
                            AjustarPontosReversao(gameRoom, -gameRoom.Settings.PointsPerError, gameRoom.Settings.PointsPerCorrect);
                        }
                    }
                    else
                    {
                        if (card.Status == "Acertou")
                        {
                            newStatus = "Errou";
                            AjustarPontosReversao(gameRoom, -gameRoom.Settings.PointsPerCorrect, -gameRoom.Settings.PointsPerError);
                        }
                    }
                    gameRoom.TotalContestedReversed++;
                }

                // Avança para o próximo card contestado se houver
                gameRoom.CurrentJudgingIndex++;
                if (gameRoom.CurrentJudgingIndex >= gameRoom.ContestedCardIndexes.Count)
                {
                    gameRoom.Phase = "resumo_rodada";
                    gameRoom.PhaseTimeRemaining = gameRoom.Settings.PauseBetweenRoundsSeconds;
                    gameRoom.ReadyToAdvancePlayerIds.Clear();
                    _logger.LogInformation("Todos os julgamentos concluídos — avançando para resumo da rodada na sala {RoomCode}", roomCode);
                }
            }

            var updatedCard = card with
            {
                VotesKeep = countAcerto,
                VotesReverse = countErro,
                VotesCancel = countAnular,
                PlayerVotes = votes,
                Status = newStatus,
                VotingStatus = todosVotaram ? "resolved" : "voting",
                WasTiebreakRandomized = empateSorteado
            };

            gameRoom.RoundCards[cardIndex] = updatedCard;
            return (CriarGameStateDto(gameRoom, connectionId), updatedCard, empateSorteado);
        }
    }

    public (GameStateDto? Estado, PlayedCardDto? Carta, bool EmpateSorteado) VotarCarta(string roomCode, string connectionId, int cardIndex, string opcaoVoto)
    {
        return VotarJulgamentoCarta(roomCode, connectionId, cardIndex, opcaoVoto);
    }

    public GameStateDto? ConfirmarProntoTransicao(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            gameRoom.ReadyToAdvancePlayerIds.Add(connectionId);
            _logger.LogInformation("Jogador {ConnectionId} pronto na transição ({Phase}) na sala {RoomCode} ({Ready}/{Total})",
                connectionId, gameRoom.Phase, roomCode, gameRoom.ReadyToAdvancePlayerIds.Count, gameRoom.Players.Count);

            bool todosProntos = gameRoom.Players.Count > 0 && gameRoom.Players.All(p => gameRoom.ReadyToAdvancePlayerIds.Contains(p.ConnectionId));
            if (todosProntos)
            {
                gameRoom.ReadyToAdvancePlayerIds.Clear();
                if (gameRoom.Phase == "resumo_rodada" || gameRoom.Phase == "revisao")
                {
                    return AvancarRodada(roomCode, connectionId);
                }
            }

            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    private static void AjustarPontosReversao(GameRoom gameRoom, int removerPts, int adicionarPts)
    {
        if (gameRoom.CurrentTurnTeam == "Vermelho")
        {
            gameRoom.ScoreTeamRed = Math.Max(0, gameRoom.ScoreTeamRed + removerPts + adicionarPts);
        }
        else
        {
            gameRoom.ScoreTeamBlue = Math.Max(0, gameRoom.ScoreTeamBlue + removerPts + adicionarPts);
        }
    }

    public GameStateDto? AvancarRodada(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            if (gameRoom.RoundNumber >= gameRoom.Settings.NumberOfRounds)
            {
                // Verifica desempate
                if (gameRoom.ScoreTeamRed == gameRoom.ScoreTeamBlue && gameRoom.Settings.TiebreakMode == "rodada-extra")
                {
                    gameRoom.Settings.NumberOfRounds += 2;
                    _logger.LogInformation("Desempate por rodada extra adicionado na sala {RoomCode}", roomCode);
                }
                else
                {
                    // Encerra partida
                    gameRoom.Phase = "fim_partida";
                    string winner = gameRoom.ScoreTeamRed > gameRoom.ScoreTeamBlue ? "Vermelho"
                        : gameRoom.ScoreTeamBlue > gameRoom.ScoreTeamRed ? "Azul" : "Empate";

                    _dataStore.UpdateMatchFinished(
                        gameRoom.MatchKey ?? $"{roomCode}-match",
                        gameRoom.ScoreTeamRed,
                        gameRoom.ScoreTeamBlue,
                        winner,
                        gameRoom.Players.Count
                    );

                    return CriarGameStateDto(gameRoom, connectionId);
                }
            }

            gameRoom.RoundNumber++;
            gameRoom.CurrentTurnTeam = gameRoom.CurrentTurnTeam == "Vermelho" ? "Azul" : "Vermelho";
            AtribuirProximoExplicador(gameRoom);

            gameRoom.Phase = "jogando";
            gameRoom.RoundScore = 0;
            gameRoom.SkipsLeft = gameRoom.Settings.SkipLimit;
            gameRoom.TimeRemaining = gameRoom.Settings.RoundTimeSeconds;
            gameRoom.RoundCards = new List<PlayedCardDto>();
            gameRoom.ActiveBuzzer = null;
            gameRoom.CurrentCardStartedAt = DateTime.UtcNow;

            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    public GameStateDto? ReiniciarPartida(string roomCode, string connectionId)
    {
        if (!GameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }

        lock (gameRoom)
        {
            Shuffle(gameRoom.Deck);
            gameRoom.RoundNumber = 1;
            gameRoom.Phase = "jogando";
            gameRoom.CurrentCardIndex = 0;
            gameRoom.CurrentCardStartedAt = DateTime.UtcNow;
            gameRoom.SkipsLeft = gameRoom.Settings.SkipLimit;
            gameRoom.TimeRemaining = gameRoom.Settings.RoundTimeSeconds;
            gameRoom.RoundScore = 0;
            gameRoom.ScoreTeamBlue = 0;
            gameRoom.ScoreTeamRed = 0;
            gameRoom.RoundCards = new List<PlayedCardDto>();
            gameRoom.ChatMessages = new List<ChatMessageDto>();
            gameRoom.ActiveBuzzer = null;

            gameRoom.PlayerCorrectCounts.Clear();
            gameRoom.PlayerBuzzerCounts.Clear();
            gameRoom.PlayerBuzzedCounts.Clear();
            gameRoom.FastestCardWord = string.Empty;
            gameRoom.FastestCardSeconds = int.MaxValue;
            gameRoom.TotalCorrect = 0;
            gameRoom.TotalErrors = 0;
            gameRoom.TotalSkips = 0;
            gameRoom.TotalContestedReversed = 0;

            AtribuirProximoExplicador(gameRoom);

            return CriarGameStateDto(gameRoom, connectionId);
        }
    }

    private static Card? GetCurrentCard(GameRoom gameRoom)
    {
        if (gameRoom.Deck.Count == 0)
        {
            return FallbackCards[0];
        }
        return gameRoom.Deck[gameRoom.CurrentCardIndex % gameRoom.Deck.Count];
    }

    private static GameStateDto CriarGameStateDto(GameRoom gameRoom, string connectionId)
    {
        var card = GetCurrentCard(gameRoom);
        CardDto? cardDto = null;

        if (card is not null)
        {
            cardDto = new CardDto(
                Id: card.Id,
                MainWord: card.MainWord,
                Forbidden: new List<string> { card.Forbidden1, card.Forbidden2, card.Forbidden3, card.Forbidden4, card.Forbidden5 },
                Difficulty: card.Difficulty,
                Category: card.Category
            );
        }

        GameEndStatsDto? endStats = null;
        if (gameRoom.Phase == "fim_partida")
        {
            var mvp = gameRoom.PlayerCorrectCounts.OrderByDescending(p => p.Value).FirstOrDefault();
            var topBuzzer = gameRoom.PlayerBuzzerCounts.OrderByDescending(p => p.Value).FirstOrDefault();
            var mostBuzzed = gameRoom.PlayerBuzzedCounts.OrderByDescending(p => p.Value).FirstOrDefault();

            string winner = gameRoom.ScoreTeamRed > gameRoom.ScoreTeamBlue ? "Vermelho"
                : gameRoom.ScoreTeamBlue > gameRoom.ScoreTeamRed ? "Azul" : "Empate";

            endStats = new GameEndStatsDto(
                WinnerTeam: winner,
                ScoreRed: gameRoom.ScoreTeamRed,
                ScoreBlue: gameRoom.ScoreTeamBlue,
                TotalRounds: gameRoom.Settings.NumberOfRounds,
                MvpName: mvp.Key ?? "Nenhum",
                MvpPoints: mvp.Value,
                TopBuzzerName: topBuzzer.Key ?? "Nenhum",
                TopBuzzerCount: topBuzzer.Value,
                MostBuzzedName: mostBuzzed.Key ?? "Nenhum",
                MostBuzzedCount: mostBuzzed.Value,
                FastestCardWord: string.IsNullOrEmpty(gameRoom.FastestCardWord) ? "Nenhuma" : gameRoom.FastestCardWord,
                FastestCardSeconds: gameRoom.FastestCardSeconds == int.MaxValue ? 0 : gameRoom.FastestCardSeconds,
                TotalCorrect: gameRoom.TotalCorrect,
                TotalErrors: gameRoom.TotalErrors,
                TotalSkips: gameRoom.TotalSkips,
                TotalContestedReversed: gameRoom.TotalContestedReversed
            );
        }

        return new GameStateDto(
            RoomCode: gameRoom.RoomCode,
            RoundNumber: gameRoom.RoundNumber,
            TotalRounds: gameRoom.Settings.NumberOfRounds,
            ActiveTeam: gameRoom.CurrentTurnTeam,
            SpokespersonId: gameRoom.CurrentSpokespersonId,
            SpokespersonName: gameRoom.CurrentSpokespersonName,
            CurrentCard: cardDto,
            ScoreRed: gameRoom.ScoreTeamRed,
            ScoreBlue: gameRoom.ScoreTeamBlue,
            RoundScore: gameRoom.RoundScore,
            Phase: gameRoom.Phase,
            SkipsLeft: gameRoom.SkipsLeft,
            TimeRemaining: gameRoom.TimeRemaining,
            ActiveBuzzer: gameRoom.ActiveBuzzer,
            RoundCards: gameRoom.RoundCards.ToList(),
            ChatMessages: gameRoom.ChatMessages.TakeLast(30).ToList(),
            EndStats: endStats,
            CurrentJudgingIndex: gameRoom.CurrentJudgingIndex,
            ContestedCardIndexes: gameRoom.ContestedCardIndexes.ToList(),
            ReadyToAdvancePlayerIds: gameRoom.ReadyToAdvancePlayerIds.ToList(),
            PhaseTimeRemaining: gameRoom.PhaseTimeRemaining
        );
    }

    private static string RemoveAccents(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}