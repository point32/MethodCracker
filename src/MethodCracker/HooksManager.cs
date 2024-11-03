using System.Linq;
using System.Reflection;

using MethodCracker.Attributes;

namespace MethodCracker;

/// <summary>
/// Provided to manage hooks of a class inherit <see cref="IHookableClass{IClass}"/>
/// <summary>
public class HooksManager
{
    public HooksManager(Type hookedClass, object? instance)
    {
        if (hookedClass.GetInterfaces().All(x => !x.IsGenericType || x.GetGenericTypeDefinition() != typeof(IHookableClass<>)))
        {
               throw new InvalidOperationException($"\"{hookedClass.FullName}\" isn't implement \"{typeof(IHookableClass<>).FullName}\"");
        }

           HookedClass = hookedClass;
           Instance = instance;
       }

    public Type HookedClass { get; }
    public object? Instance { get; }

    private Dictionary<string, IHookCollection> m_collectionByName = [];

    public IReadOnlyDictionary<string, IHookCollection> CollectionByName => m_collectionByName.AsReadOnly();

    private void ValidateAndInitializeCollection<THookType>(string name) where THookType : Delegate
    {
        if (CollectionByName.ContainsKey(name))
        {
            return;
        }

        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        bindingFlags |= Instance is null ? BindingFlags.Static : BindingFlags.Instance;

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

        m_collectionByName[name] = new HookCollection<THookType>(originMethod, (IHookLifeTime)HookedClass.GetProperty("ClassLifeTime").GetValue(null), Instance);
    }

    public void AddHook<THookType>(string name, Hook hook) where THookType : Delegate
    {
		ValidateAndInitializeCollection<THookType>(name);
        m_collectionByName[name].AddHook(hook);
    }

    public HookCollection<THookType>? GetHookCollection<THookType>(string name) where THookType : Delegate
    {
		ValidateAndInitializeCollection<THookType>(name);
        return m_collectionByName[name] as HookCollection<THookType>;
    }
}
