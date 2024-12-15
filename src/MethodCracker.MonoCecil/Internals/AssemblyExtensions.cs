using System.Reflection;
using Mono.Cecil;

namespace MethodCracker.MonoCecil.Internals;

internal static class AssemblyExtensions
{
    public static ModuleDefinition? GetDefinition(this Assembly @this)
    {
        return !File.Exists(@this.Location) ? null : ModuleDefinition.ReadModule(@this.Location);
    }
}