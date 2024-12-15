namespace ConsoleApp1;

public class ProgramOverride
{
    public float Test2;

    public void FooOverride(Program @this, int parameter1)
    {
        Console.WriteLine($"This is 'FooOverride' with {parameter1}, value of 'Test' is：{@this.Test}, 'Test2' is：{Test2}");
    }
}