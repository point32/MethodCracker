using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Validators;
using MethodCracker;
using MethodCracker.Attributes;

namespace MethodCrackerBenchmark;

public class Test
{
    public const int LoopCount = 2000;
    public static ILifeTime GlobalLifeTime => LifeTimeMapper.GlobalLifeTimeInstance;

    #region OnlyOriginMethod

    [Benchmark]
    public void BenchmarkFoo()
    {
        LifeTimeManager.RegisterMapping(new LifeTimeMapper());
        for (var i = 0; i < LoopCount; i++) Foo();
    }

    [Benchmark]
    public void BenchmarkFooDirectly()
    {
        for (var i = 0; i < LoopCount; i++) FooDirectly();
    }

    [Processed("FooInternal")]
    [CrackableMethod]
    public static void Foo()
    {
        GlobalHooksManager.GetHooksManager(typeof(Test))
            .GetHookCollection<Action>("Foo")
            .Execute([null]);
    }

    public static void FooDirectly()
    {
        FooInternal();
    }

    private static void FooInternal()
    {
        var rnd = new Random();
        A = 0;
        for (var i = 0; i < 20000; i++) A += rnd.Next();
    }

    #endregion

    #region OnlyOriginMethod&SingleParameter

    [Benchmark]
    public void BenchmarkFooWithArgsDirectly()
    {
        for (var i = 0; i < LoopCount; i++) FooWithArgsDirectly(i);
    }

    [Benchmark]
    public void BenchmarkFooWithArgs()
    {
        LifeTimeManager.RegisterMapping(new LifeTimeMapper());
        for (var i = 0; i < LoopCount; i++) FooWithArgs(i);
    }

    [Processed("FooWithArgsInternal")]
    [CrackableMethod]
    private void FooWithArgs(int i)
    {
        GlobalHooksManager.GetHooksManager(typeof(Test)).GetHookCollection<Action<int>>("FooWithArgs")
            .Execute([null, i]);
    }

    private void FooWithArgsDirectly(int i)
    {
        FooWithArgsInternal(i);
    }

    private static void FooWithArgsInternal(int i)
    {
        for (var j = 0; j < LoopCount; j++) A = HashCode.Combine(i, "FooWithArgs");
    }

    #endregion

    #region SeveralHooks

    public const int HookCount = 1;

    [Benchmark]
    public void BenchmarkFooWithHooks()
    {
        LifeTimeManager.RegisterMapping(new LifeTimeMapper());
        HooksManager? hooksManager = GlobalHooksManager.GetHooksManager(typeof(Test));
        if (hooksManager.CollectionByName.Count == 0)
            for (var i = 0; i < HookCount; i++)
                hooksManager.AddHook<Action>("FooWithHooks", FooWithHooksInternal1, HookOption.AfterOrigin,
                    GlobalLifeTime);

        for (var i = 0; i < LoopCount; i++) FooWithHooks();
    }

    [Benchmark]
    public void BenchmarkFooWithHooksDirectly()
    {
        for (var i = 0; i < LoopCount; i++) FooWithHooksDirectly();
    }

    [Processed("FooWithHooksInternal")]
    [CrackableMethod]
    private static void FooWithHooks()
    {
        GlobalHooksManager.GetHooksManager(typeof(Test)).GetHookCollection<Action>("FooWithHooks").Execute([null]);
    }

    public static void FooWithHooksDirectly()
    {
        FooWithHooksInternal();
        for (var i = 0; i < HookCount; i++) FooWithHooksInternal1();
    }

    private static void FooWithHooksInternal()
    {
        for (var i = 0; i < LoopCount; i++) A = HashCode.Combine(i, "FooWithHooks");
    }

    private static void FooWithHooksInternal1()
    {
        for (var i = 0; i < LoopCount; i++) A = HashCode.Combine(i, "FooWithHooks1");
    }

    #endregion

    public static int A { get; set; }
}