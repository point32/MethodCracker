using System.Reflection;

namespace MethodCracker;

public sealed class Hook
{
    /// <summary>
    /// The default constructor of the class.
    /// </summary>
    public Hook(MethodInfo methodInfo, HookOption option, object? instance, ILifeTime hookLifeTime)
    {
        Method = methodInfo;
        Option = option;
        Instance = instance;
        HookLifeTime = hookLifeTime;
    }

    /// <summary>
    /// Gets the registration option of the hook. 
    /// </summary>
    public HookOption Option { get; }

    /// <summary>
    /// Gets the method of the hook.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Instance that the hook is bound to,
    /// can be null if the hook is from a static method.
    /// </summary>
    public object? Instance { get; }

    /// <summary>
    /// A property to determine whether the hook is available.
    /// </summary>
    public ILifeTime HookLifeTime { get; }
}

