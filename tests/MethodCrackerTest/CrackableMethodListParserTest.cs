using MethodCracker.ProcessorConfig;

namespace MethodCrackerTest;

public partial class Tests
{
    [Test]
    public void TestMethodListParser()
    {
        using Stream? stream = typeof(Tests).Assembly
            .GetManifestResourceStream("MethodCrackerTest.Assets.CrackableMethodsList.txt")!;
        TextReader reader = new StreamReader(stream);
        CrackableMethodsList? list = CrackableMethodsList.Deserialize(reader);
        Assert.Multiple(() =>
        {
            Assert.That(list.CrackableModules, Has.Count.EqualTo(2));
            Assert.That(list.CrackableModules[0].CrackableMethods, Has.Length.EqualTo(3));
            Assert.That(list.CrackableModules[1].CrackableMethods, Has.Length.EqualTo(3));
        });
    }
}