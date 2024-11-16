using MethodCracker;
using MethodCracker.Attributes;

namespace ConsoleApp1;

class Hooker
{
	public int Test { get; set; }
	public int Hook(object instance, int a, int b)
	{
		Console.WriteLine(Test);
		return 111;
	}
}
public class Program : IHookableClass<Program>
{
	public static ILifeTime LifeTime => ModuleLifeTime.Instance;

	public static HooksManager<Program> HooksManager { get; } = new (typeof(Program));
	public static void Main()
	{
		var test = new Hooker();
		test.Test = 99;
		HooksManager.AddHook("Foo", test.Hook, HookOption.SoftReplace, LifeTime);

		Program test1 = new Program { Test2 = 11 };
		
		Console.WriteLine("The result is: " + test1.Foo(1, 2));
	}

	private static int FooOverride(object instance, int a, int b)
	{
		Console.WriteLine("There is a new Foo but not the origin one.");
		return 12;
	}

	public int Test2 { get; set; }
	[CrackableMethod]
	public int Foo(int a, int b)
	{
		Console.WriteLine($"Origin foo with {Test2}");
		return a + b;
	}

        public int Expected(int a, int b)
        {
            return (int)HooksManager.GetHookCollection<Func<int, int, int>>("Foo").Execute([this, a, b]);
        }
}
