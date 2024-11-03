using MethodCracker;
using MethodCracker.Attributes;

public class ModuleLifeTime : IHookLifeTime
{
    private ModuleLifeTime()
	{
	}

    public static ModuleLifeTime Instance { get; } = new();
    public bool IsAlive => true;
}
