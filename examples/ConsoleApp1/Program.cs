using MethodCracker;
using MethodCracker.Attributes;

namespace ConsoleApp1;

public class Program : IHookableClass<Program>
{
	public static ILifeTime LifeTime => ModuleLifeTime.Instance;

	public static HooksManager HooksManager { get; } = new (typeof(Program));
	public static void Main()
	{
		HooksManager.AddHook("Foo", NewFoo, HookOption.Replace, LifeTime);
		Foo(111);
	}

	[CrackableMethod]
	public static void Foo(int a)
	{
		Console.WriteLine($"正在调用原版 Foo，a 的值为：{a}");
	}

	private static void NewFoo(object instance, int a)
	{
		Console.WriteLine($"原版 Foo 已经被拦截，a 的值为：{a}");
	}
}
