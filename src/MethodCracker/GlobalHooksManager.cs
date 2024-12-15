namespace MethodCracker;

public static class GlobalHooksManager
{
    public static readonly Dictionary<string, ModuleHooksManager> ModuleHooksManagers = [];

    public static ModuleHooksManager GetModuleHooksManager(string assemblyName)
    {
        if (ModuleHooksManagers.TryGetValue(assemblyName, out ModuleHooksManager? moduleHooksManager))
            return moduleHooksManager;

        moduleHooksManager = new ModuleHooksManager(assemblyName);
        ModuleHooksManagers.Add(assemblyName, moduleHooksManager);
        return moduleHooksManager;
    }

    public static HooksManager GetHooksManager(Type hookedClassType)
    {
        return GetModuleHooksManager(hookedClassType.Assembly.FullName!).GetManager(hookedClassType);
    }
}