using MethodCracker;

namespace ConsoleApp1;

public class Program
{
    public static void Run()
    {
        LifeTimeManager.RegisterMapping(new LifeTimeMapper());

        var program = new Program
        {
            Test = 3
        };
        var programOverride = new ProgramOverride()
        {
            Test2 = 0.05f
        };
        
        GlobalHooksManager.GetHooksManager(typeof(Program))
            .AddHook("Foo", programOverride.FooOverride, HookOption.AfterOrigin, ModuleLifeTime.Instance);
        
        program.Foo(111);
    }

    public int Test = 0;

    private void Foo(int a)
    {
        Console.WriteLine($"There is 'NewFoo', value of 'a' is：{a}, value of 'Test' is：{Test}");
    }
}