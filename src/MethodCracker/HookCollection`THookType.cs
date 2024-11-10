using System;
using System.Linq;
using System.Collections.Generic;

using MethodCracker.Exceptions;
using System.Reflection;

namespace MethodCracker;

public sealed class HookCollection<THookType>(MethodInfo originMethod, ILifeTime classLifeTime) : IHookCollection where THookType : Delegate
{
    internal List<Hook> m_hooks = [];
    internal Dictionary<Hook, THookType>? m_cachedHooks;

    public ILifeTime ClassLifeTime { get; } = classLifeTime;
    public MethodInfo OriginMethod { get; } = originMethod;

    /// <summary>
    /// The hooks in the collection,
    /// the property is read-only.
    /// </summary>
    public IReadOnlyList<Hook> Hooks => m_hooks.AsReadOnly();

    /// <summary>
    /// If there is at least living hook, the property will return true.
    /// </summary>
    public bool HasAnyHook => m_hooks.Any(x => x.HookLifeTime.IsAlive);

    /// <summary>
    /// Execute all living hooks and get the returned value
    /// of the first hook which is marked as
    /// <see cref="MethodCracker.HookOption.SoftReplace"/>
    /// or
    /// <see cref="MethodCracker.HookOption.Replace"/>,
    /// when calling this, you should make sure that the first of
    /// the parameters is the instance of the hooked class.
    /// </summary>
    public object? Execute(object[] parameters)
    {
        if (m_cachedHooks is null || m_hooks.Any(x => x.HookLifeTime.IsAlive is false))
        {
            GenerateCache(parameters[0]);
        }

        object? returnedValue = null;
        foreach (var pair in m_cachedHooks!)
        {
            if (pair.Key.Method == OriginMethod)
            {
                returnedValue = pair.Key.Method.Invoke(parameters[0], parameters[1..]);
                continue;
            }
            
            if (pair.Key.Option.HasFlag(HookOption.SoftReplace))
            {
                returnedValue = pair.Value.DynamicInvoke(parameters);
				continue;
            }

            _ = pair.Value.DynamicInvoke(parameters);
        }

        return returnedValue;
    }

    private int GetHookOptionOrder(HookOption option)
    {
        if (option.HasFlag(HookOption.BeforeOrigin))
        {
            return 0;
        }

        if (option.HasFlag(HookOption.AfterOrigin))
        {
            return 2;
        }

        if (option.HasFlag(HookOption.SoftReplace))
        {
            return 1;
        }

        return int.MaxValue;
    }

    internal void GenerateCache(object? target)
    {
        Dictionary<Hook, THookType> cachedDelegates = [];
        bool removeOriginHook = m_hooks.Any(x => x.HookLifeTime.IsAlive && x.Option.HasFlag(HookOption.SoftReplace));

        if (m_hooks.Count(x => x.HookLifeTime.IsAlive && x.Option.HasFlag(HookOption.ConflictWithReplaces)) > 1)
        {
            throw new InvalidOperationException("Hook conflicted.");
        }
	
        var hooks = m_hooks.Where(x => x.HookLifeTime.IsAlive);

        if (!removeOriginHook)
        {
            var originMethodHook = new Hook(OriginMethod, HookOption.SoftReplace, null, ClassLifeTime);
            hooks = hooks.Append(originMethodHook);
        }

        hooks = hooks.OrderBy(x => GetHookOptionOrder(x.Option));
        foreach (var hook in hooks)
        {
            if (hook.HookLifeTime.IsAlive is false)
            {
                continue;
            }

            if (hook.Method == OriginMethod)
            {
                cachedDelegates.Add(hook, null!);
                continue;
            }

            var @delegate = (THookType)Delegate.CreateDelegate(typeof(THookType), hook.Instance, hook.Method);
            cachedDelegates.Add(hook, @delegate);
        }

        m_hooks.RemoveAll(x => x.HookLifeTime.IsAlive is false);

        m_cachedHooks = cachedDelegates;
    }

    /// <summary>
    /// Adds a hook to the collection,
    /// throws <see cref="System.ArgumentNullException"/> when the parameter is null,
    /// <see cref="System.InvalidOperationException"/> when the hook is already existed.
    /// </summary>
    public void AddHook(Hook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);

        if (m_hooks.Contains(hook))
        {
            throw new InvalidOperationException($"The hook '{hook.Method.Name}' in type '{hook.Method.DeclaringType!.FullName}' has already been added.");
        }

        m_hooks.Add(hook);
        m_cachedHooks = null;
    }

    /// <summary>
    /// Removes a hook from the collection.
    /// </summary>
    public void RemoveHook(Hook hook)
    {
        m_hooks.Remove(hook);
        m_cachedHooks = null;
    }
}