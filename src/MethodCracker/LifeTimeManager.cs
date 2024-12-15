namespace MethodCracker;

public static class LifeTimeManager
{
    private static readonly HashSet<ITypeToLifeTimeMap> TypeToLifeTimeMaps = [];

    public static void RegisterMapping(ITypeToLifeTimeMap map)
    {
        TypeToLifeTimeMaps.Add(map);
    }

    public static void UnregisterMapping(ITypeToLifeTimeMap map)
    {
        TypeToLifeTimeMaps.Remove(map);
    }

    public static ILifeTime? GetLifeTime(Type type)
    {
        return TypeToLifeTimeMaps
            .Select(map => map.GetLifeTime(type))
            .OfType<ILifeTime>()
            .FirstOrDefault();
    }
}