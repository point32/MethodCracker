using MethodCracker;

namespace MethodCrackerBenchmark;

public class LifeTimeMapper : ITypeToLifeTimeMap
{
    public class GlobalLifeTime : ILifeTime
    {
        public bool IsAlive => true;
    }

    public static readonly GlobalLifeTime GlobalLifeTimeInstance = new();

    public ILifeTime GetLifeTime(Type type)
    {
        return GlobalLifeTimeInstance;
    }
}