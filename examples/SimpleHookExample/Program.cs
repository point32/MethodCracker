using System;
using System.Reflection;

using MethodCracker;
using MethodCracker.Attributes;

public class Program : IHookableClass<Program>
{
    public static IHookLifeTime ClassLifeTime => ModuleLifeTime.Instance;

    private static HooksManager internal_staticHooksManager = new (typeof(Program), null);
    public static void Main()
	{
		internal_staticHooksManager.AddHook<Action>("Foo", new Hook(
		    typeof(Program).GetMethod("FooOverride", BindingFlags.NonPublic | BindingFlags.Static),
			HookOption.BeforeOrigin, null, ClassLifeTime));

		Foo();
	}

	private static void FooOverride()
	{
		Console.WriteLine("There is a new Foo before the origin one.");
	}

	[CrackableMethod]
	[Processed(originMethodName: "internal_foo")]
	public static void Foo()
    {
	    internal_staticHooksManager.GetHookCollection<Action>("Foo").Execute([]);
	}

	private static void internal_foo()
	{
		Console.WriteLine("Origin foo!");
	}
}
