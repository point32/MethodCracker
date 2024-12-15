using System.Runtime.Serialization;
using MethodCracker.ProcessorConfig;

namespace MethodCracker.MonoCecil;

/// <summary>
/// Thrown when a method is not found in the module.
/// </summary>
internal class MethodNotFoundException(string? message) : Exception(message)
{
    public MethodNotFoundException(CrackableMethodInfo crackableMethod) : this(
        $"Method not found, signature: {crackableMethod}")
    {
    }
}