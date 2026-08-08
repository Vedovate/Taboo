using Tipoo.Api.Models;

namespace Tipoo.Api.Data;

public interface IGameDataStore
{
    IReadOnlyList<Card> GetAllCards();

    GameSettings? LoadHostSettings(string hostSessionId);

    void SaveHostSettings(string hostSessionId, GameSettings settings);
}
