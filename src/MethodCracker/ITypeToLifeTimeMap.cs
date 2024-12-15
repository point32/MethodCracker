namespace MethodCracker;

public interface ITypeToLifeTimeMap
{
    /// <summary>
    /// Get the life-time instance of the given type.
    /// Would return null if the type is not mapped by current instance.
    /// </summary>
    /// <returns></returns>
    ILifeTime? GetLifeTime(Type type);
}