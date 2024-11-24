namespace MethodCracker.ProcessorConfig;

public class CrackableModule(string moduleName, CrackableMethodInfo[] crackableMethods)
{
    public string ModuleName { get; } = moduleName;
    public CrackableMethodInfo[] CrackableMethods => crackableMethods;
}