using MethodCracker.ProcessorConfig;
using Mono.Cecil;

namespace MethodCracker.MonoCecil;

public struct ModuleProcessor(ModuleDefinition moduleDefinition)
{
    public ModuleProcessor(string modulePath) : this(ModuleDefinition.ReadModule(modulePath))
    {
    }

    public ModuleDefinition Module => moduleDefinition;

    public void Save(string destination)
    {
        using FileStream? stream = File.OpenWrite(destination);
        Save(stream);
    }

    public void Save(Stream output)
    {
        Module.Write(output);
    }
}