using System.Reflection;
using MethodCracker.MonoCecil;
using MethodCracker.ProcessorConfig;

string thisLocation = new DirectoryInfo(typeof(Program).Assembly.Location).Parent!.FullName;
Dictionary<string, MemoryStream> modules = [];

using var stringReader = new StringReader(
    @"
[ConsoleApp1]
ConsoleApp1.Program:Foo(int)
");
CrackableMethodsList processorConfig = CrackableMethodsList.Deserialize(stringReader);
var processor = new MethodCrackerProcessor(processorConfig, x =>
{
    modules[x] = ResolveModule(x);
    return modules[x];
});

processor.Process();

Assembly? consoleApp1 = null;

foreach (KeyValuePair<string, MemoryStream> moduleNameStreamPair in modules)
{
    Assembly asm = Assembly.Load(moduleNameStreamPair.Value.ToArray());
    if (moduleNameStreamPair.Key == "ConsoleApp1")
    {
        consoleApp1 = asm;
    }

    moduleNameStreamPair.Value.Dispose();
}

var method = consoleApp1?.GetType("ConsoleApp1.Program")?.GetMethod("Run") ?? throw new Exception("Entry point not found");
method.Invoke(null, null);
return;

MemoryStream ResolveModule(string name)
{
    switch (name)
    {
        case "ConsoleApp1":
        {
            var stream = new MemoryStream();
            using FileStream file = File.OpenRead(Path.Combine(thisLocation, "ConsoleApp1.dll"));
            file.CopyTo(stream);
            return stream;
        }
        default:
        {
            throw new Exception("Unknown module");
        }
    }
}