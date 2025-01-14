using System.IO;
using System.Threading.Tasks;

namespace SudokuWriter.Library;

public class GameEngineSerializer
{
    public Task<GameEngine> LoadGameEngineAsync(Stream source) => throw null;

    public Task SaveGameEngineAsync(GameEngine game, Stream destinatino) => throw null;
}