namespace MethodCracker.ProcessorConfig;

public readonly struct CrackableMethodInfo(string moduleName, string typeName, string methodName, MethodParameter[] parameters)
{
    public string ModuleName { get; } = moduleName;
    public string TypeName { get; } = typeName;
    public string MethodName { get; } = methodName;
    public MethodParameter[] Parameters { get; } = parameters;
}