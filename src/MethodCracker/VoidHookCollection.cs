using System;

namespace MethodCracker;

public class VoidHookCollection
{
    private HookCollection<Delegate> m_collection = new();

    /// <summary>
    /// See <see cref="HookCollection{THookType}.Execute(object[])"/>
    /// <summary>
    public void Execute(object[] args) => m_collection.Execute(args);

    /// <summary>
    /// See <see cref="HookCollection{THookType}.AddHook(Hook)"/>
    /// <summary>
    public void AddHook(Hook hook) => m_collection.AddHook(hook);

    /// <summary>
    /// See <see cref="HookCollection{THookType}.RemoveHook(Hook)"/>
    /// <summary>
    public void RemoveHook(Hook hook) => m_collection.RemoveHook(hook);

    /// <summary>
    /// See <see cref="HookCollection{THookType}.AddHook(Hook)"/>
    /// <summary>
    /// <returns>
    /// The instance created through the parameter.
    /// </returns>
    public Hook AddHook(Delegate hook, HookOption option, IHookLifeTime lifeTime) => AddHook(new Hook(hook.Method, option, null, hookLifeTime));
}
