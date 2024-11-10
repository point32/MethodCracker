using Mono.Cecil;

namespace MethodCracker.MonoCecil.Internals;

internal static class MethodCrackerLibrary
{
    public static ModuleDefinition MethodCrackerDefinition { get; } = typeof(Hook).Assembly.GetDefinition()!;

}