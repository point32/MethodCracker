namespace MethodCracker.ProcessorConfig;

public readonly struct MethodParameter(string? moduleName, string typeName)
{
    /// <summary>
    /// The type name of the parameter.
    /// </summary>
    public string TypeName { get; } = typeName;
    
    /// <summary>
    /// The module where the type of the parameter is defined,
    /// must be null when the type is basic type like
    /// 'System.Int32.', 'System.String', 'System.Float', 'System.Action'.
    /// </summary>
    public string? ModuleName { get; } = moduleName;
}