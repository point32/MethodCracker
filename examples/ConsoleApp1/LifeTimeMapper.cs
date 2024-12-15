using MethodCracker;

namespace ConsoleApp1;

public class LifeTimeMapper : ITypeToLifeTimeMap
{
    ILifeTime ITypeToLifeTimeMap.GetLifeTime(Type type)
    {
        return ModuleLifeTime.Instance;
    }
}