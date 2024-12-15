namespace MethodCracker;

public class ModuleHooksManager(string moduleName)
{
    public string ModuleName { get; } = moduleName;
    public Dictionary<Type, HooksManager> Managers { get; } = [];

    public HooksManager GetManager(Type hookedClassType)
    {
        if (!Managers.TryGetValue(hookedClassType, out HooksManager? manager))
        {
            manager = new HooksManager(hookedClassType);
            Managers.Add(hookedClassType, manager);
        }

        return manager;
    }
}