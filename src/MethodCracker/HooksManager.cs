namespace MethodCracker;

/// <summary>
/// Provided to manage hooks of a class inherit <see cref="IHookableClass"/>
/// <summary>
public class HookManager<THookedClass>(object? instance) where THookedClass : IHookableClass
{
    public object? Instance = instance;
    private Dictionary<string, IHookCollection> m_collectionByName = [];

    public IReadOnlyDictionary<string, IHookCollection> CollectionByName => m_collectionByName.AsReadOnly();

    public void AddHook<THookType>(string name, Hook hook)
    {
        if(!m_collectionByName.ContainsKey(name))
        {
            var originMethod = typeof(THookedClass).GetMe
            m_collectionByName[name] = new HookCollection<THookType>();:
        }
    }
}
