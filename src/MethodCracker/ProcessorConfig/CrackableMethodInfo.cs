using System.Text;

namespace MethodCracker.ProcessorConfig;

public readonly struct CrackableMethodInfo(
    string moduleName,
    string typeName,
    string methodName,
    MethodParameter[] parameters)
{
    public string ModuleName { get; } = moduleName;
    public string TypeName { get; } = typeName;
    public string MethodName { get; } = methodName;
    public MethodParameter[] Parameters { get; } = parameters;

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"[{ModuleName}]{TypeName}::{MethodName}(");

        string? parametersString = string.Join(", ", Parameters
            .Select(x => x.ModuleName is null
                             ? x.TypeName
                             : $"[{x.ModuleName}]{x.TypeName}"));
        builder.Append(parametersString);
        builder.Append(')');
        return builder.ToString();
    }
}