using Tipoo.Api.Models;

namespace Tipoo.Api.Data;

public interface IGameDataStore
{
    IReadOnlyList<Card> GetAllCards();

    IReadOnlyList<Card> GetCardsForGame(List<string> difficulties, int count);

    GameSettings? LoadHostSettings(string hostSessionId);

    void SaveHostSettings(string hostSessionId, GameSettings settings);

    void CreateMatch(GameMatch match);

    void UpdateMatchFinished(string matchKey, int scoreRed, int scoreBlue, string winnerTeam, int finishedPlayers);

    GameMatch? GetMatch(string matchKey);
}
