using System;

namespace MethodCracker.Exceptions;

/// <summary>
/// Thrown exception when specefic hook not found.
/// <summary>
///
/// <param name="hookDescription">
/// The description for the hook.
/// </param>
class HookNotFoundException(string hookDescription) : Exception($"Specefic hook not found!\nReason:{hookDescription}")
{
}
