namespace MethodCracker.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ProcessedAttribute : Attribute
{
    public ProcessedAttribute()
	{
	}

    public ProcessedAttribute(string originMethodName)
	{
		OriginMethodName = originMethodName;
	}
    internal string OriginMethodName { get; }
}
