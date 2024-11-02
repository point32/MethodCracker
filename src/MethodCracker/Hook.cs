using System.Reflection;

namespace MethodCracker;

public class Hook
{
    /// <summary>
    /// The default constructor of the class.
    /// <summary>
    public Hook(MethodInfo info, HookOption option, object instance, IHookLifeTime hookLifeTime)
    {
        Method = info;
        Option = option;
        Instance = instance;
        HookLifeTime = hookLifeTime;
    }

    /// <summary>
    /// Gets the registration option of the hook. 
    /// <summary>
    public HookOption Optiion { get; }

    /// <summary>
    /// Gets the method of the hook.
    /// <summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Instance that the hook is bound to,
    /// can be null if the hook is from a static method.
    /// <summary>
    public object Instance { get; }

    /// <summary>
    /// A property to determine whether the hook is available.
    /// <summary>
    public IHookLifeTime HookLifeTime { get; }
}

