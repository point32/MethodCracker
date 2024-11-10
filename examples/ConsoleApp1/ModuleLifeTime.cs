using MethodCracker;

namespace ConsoleApp1;
public class ModuleLifeTime : ILifeTime
{
    private ModuleLifeTime()
	{
	}

    public static ModuleLifeTime Instance { get; } = new();
    public bool IsAlive => true;
}
