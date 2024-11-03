namespace MethodCracker;

public interface IHookableClass<IClass> where IClass : IHookableClass<IClass>
{
    public static IHookLifeTime ClassLifeTime { get; }
}
