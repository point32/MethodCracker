namespace MethodCracker;

public interface IHookableClass<TClass> where TClass : IHookableClass<TClass>
{
	public static abstract ILifeTime LifeTime { get; }
	public static abstract HooksManager HooksManager { get; }
}
