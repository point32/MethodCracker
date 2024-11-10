using System.Reflection;
using MethodCracker;
using MethodCracker.MonoCecil;
using Mono.Cecil;
using Mono.Cecil.Rocks;

ModuleDefinition module = ModuleDefinition.ReadModule("ConsoleApp1.dll");
var moduleProcessor = new ModuleProcessor(module);
foreach (var typeDefinition in module.Types)
{
    var interfaceReference = module.ImportReference(typeof(IHookableClass<>));
    interfaceReference.GenericParameters.Clear();
    interfaceReference.MakeGenericInstanceType(module.ImportReference(typeDefinition));
    if (typeDefinition.Interfaces.All(x => x.InterfaceType.FullName != interfaceReference.FullName))
    {
        continue;
    }

    foreach (var methodDefinition in typeDefinition.Methods)
    {
        var processor = new MethodProcessor(methodDefinition);
        if (!processor.IsProcessEnabled) continue;

        if (processor.Process()) continue;
        
        Console.WriteLine($"Failed to process {methodDefinition.FullName}");
    }
}

var stream = new MemoryStream();
moduleProcessor.Save(stream);
moduleProcessor.Save("./output.dll");
var asm = Assembly.Load(stream.ToArray());
asm.EntryPoint.Invoke(null, []);