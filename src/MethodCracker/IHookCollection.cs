namespace MethodCracker;

public interface IHookCollection
{
    void AddHook(Hook hook);

    void RemoveHook(Hook hook);

    IReadOnlyList<Hook> Hooks { get; }

    object? Execute(object?[] parameters);
}