using System.Linq;
using System.Reflection;

using MethodCracker.Attributes;

namespace MethodCracker;

/// <summary>
/// Provided to manage hooks of a class inherit <see cref="IHookableClass{IClass}"/>
/// </summary>
public sealed class HooksManager<TClass> where TClass : IHookableClass<TClass>
{
    public HooksManager(Type hookedClass)
    {
        if (hookedClass.GetInterfaces().All(x => !x.IsGenericType || x.GetGenericTypeDefinition() != typeof(IHookableClass<>)))
        {
               throw new InvalidOperationException($"\"{hookedClass.FullName}\" isn't implement \"{typeof(IHookableClass<>).FullName}\"");
        }

        HookedClass = hookedClass;
    }

    public Type HookedClass { get; }
    
    private Dictionary<string, IHookCollection> m_collectionByName = [];

    public IReadOnlyDictionary<string, IHookCollection> CollectionByName => m_collectionByName.AsReadOnly();

    private void ValidateAndInitializeCollection<THookType>(string name) where THookType : Delegate
    {
        if (CollectionByName.ContainsKey(name))
        {
            return;
        }

        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        bindingFlags |= BindingFlags.Static | BindingFlags.Instance;

        var hookingMethod = HookedClass.GetMethod(name, bindingFlags);
        if (hookingMethod is null)
        {
            throw new ArgumentOutOfRangeException($"Hook name \"{name}\" doesn't exist in type \"{HookedClass.FullName}\".");
        }

        if (hookingMethod.CustomAttributes.All(x => x.AttributeType != typeof(CrackableMethodAttribute)))
        {
            throw new InvalidOperationException($"Specific hook didn't marked with \"{typeof(CrackableMethodAttribute).FullName}\".");
        }

        var attribute = hookingMethod.GetCustomAttribute<ProcessedAttribute>();
        if (attribute is null)
        {
            throw new InvalidProgramException($"Cannot find \"{typeof(ProcessedAttribute)}\" on method \"{name}\".");
        }

        var originMethod = HookedClass.GetMethod(attribute.OriginMethodName, bindingFlags);
        if (originMethod is null)
        {
            throw new OriginMethodNotFoundException();
        }

        m_collectionByName[name] = new HookCollection<THookType>(originMethod, TClass.LifeTime);
    }

    public void AddHook<THookType>(string name, Hook hook) where THookType : Delegate
    {
		ValidateAndInitializeCollection<THookType>(name);
        m_collectionByName[name].AddHook(hook);
    }

    public void AddHook<THookType>(string name, THookType hook, HookOption option, ILifeTime hookLifeTime) where THookType : Delegate
    {
        ValidateAndInitializeCollection<THookType>(name);
        AddHook<THookType>(name, new Hook(hook.Method, option, hook.Target, hookLifeTime));
    }

    public HookCollection<THookType>? GetHookCollection<THookType>(string name) where THookType : Delegate
    {
		ValidateAndInitializeCollection<THookType>(name);
        return m_collectionByName[name] as HookCollection<THookType>;
    }
}
