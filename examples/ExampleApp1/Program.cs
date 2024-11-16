using System.Reflection;
using System.IO;
using MethodCracker;
using MethodCracker.MonoCecil;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using static System.Console;

var thisLocation = new DirectoryInfo(typeof(Program).Assembly.Location).Parent.FullName;
ModuleDefinition module = ModuleDefinition.ReadModule(Path.Combine(thisLocation, "ConsoleApp1.dll"));
var moduleProcessor = new ModuleProcessor(module);

foreach (var typeDefinition in module.Types)
{
    var interfaceReference = module.ImportReference(typeof(IHookableClass<>));
    var interfaceInstanceType = new GenericInstanceType(interfaceReference);
    interfaceInstanceType.GenericArguments.Add(module.ImportReference(typeDefinition));
    if (typeDefinition.Interfaces.All(x => x.InterfaceType.FullName != interfaceInstanceType.FullName))
    {
        continue;
    }

    List<MethodProcessor> processors = new();
    foreach (var methodDefinition in typeDefinition.Methods)
    {
        var processor = new MethodProcessor(methodDefinition);
        if (!processor.IsProcessEnabled)
        {
            continue;
        }

        processors.Add(processor);
    }

    foreach(var toProcess in processors)
    {
        Console.WriteLine(toProcess.Process());
    }
}

var stream = new MemoryStream();
moduleProcessor.Save(stream);
var asm = Assembly.Load(stream.ToArray());
asm.EntryPoint.Invoke(null, []);
