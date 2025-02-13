using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VaettirNet.SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(GameEngine))]
public class GameEngineSerializerTests
{
    [Test]
    public async Task BasicRule()
    {
        GameEngineSerializer serializer = new();
        MemoryStream stream = new();
        await serializer.SaveGameAsync(GameEngine.Default, stream);
    }
}