namespace MethodCracker.ProcessorConfig;

public class CrackableMethodsList
{
    private CrackableMethodsList(List<CrackableModule> crackableModules)
    {
        m_crackableModules = crackableModules;
    }

    private List<CrackableModule> m_crackableModules = [];
    public IReadOnlyList<CrackableModule> CrackableModules => m_crackableModules.AsReadOnly();
    
    /// <summary>
    /// Deserialize crackable methods list from text file.
    /// </summary>
    /// <returns></returns>
    public static CrackableMethodsList Deserialize(TextReader input)
    {
        string? currentModuleName = null;
        List<CrackableModule> crackableModules = [];
        
        // Crackable methods for the processing module, a temporary list,
        // which will be cleared after a new module statement like '[MyModule]' is found
        List<CrackableMethodInfo> crackableMethods = [];
        
        for(;;)
        {
            string? line = input.ReadLine();
            if (line is null)
            {
                // This means the end of the file
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                // Ignore empty lines
                continue;
            }
            
            if (line.StartsWith("//"))
            {
                // Ignore comments
                continue;
            }

            if (line.StartsWith('['))
            {
                // '[Xxx]' means module statement,
                // for example, '[MyModule]' as a new line,
                // which means the following crackable methods are defined in the module 'MyModule',

                if (currentModuleName != null)
                {
                    CrackableModule module = new(currentModuleName, crackableMethods.ToArray());
                    crackableModules.Add(module);
                    crackableMethods.Clear();
                }

                var moduleName = line[1..line.IndexOf(']')];
                currentModuleName = moduleName;
                continue;
            }

            try
            {
                CrackableMethodInfo methodInfo = ParseMethodInfo(line, currentModuleName!);
                crackableMethods.Add(methodInfo);
            }
            catch
            {
                throw new InvalidDataException($"Invalid line: \"{line}\", please check the format.");
            }
        }

        if (currentModuleName is not null)
        {
            crackableModules.Add(new CrackableModule(currentModuleName!, crackableMethods.ToArray()));
        }

        crackableMethods.Clear();
        return new CrackableMethodsList(crackableModules);
    }
    
    private static CrackableMethodInfo ParseMethodInfo(string line, string moduleName)
    {
        var parts = line.Split(':', StringSplitOptions.TrimEntries);
        var typeName = parts[0];
        var methodSignature = parts[1];
        var methodName = methodSignature[..methodSignature.IndexOf('(')];
        var methodParameters = methodSignature[(methodSignature.IndexOf('(') + 1)..methodSignature.IndexOf(')')]
            .Split(',', StringSplitOptions.TrimEntries)
            .Select(ParseParameter)
            .ToArray();
        return new CrackableMethodInfo(moduleName, typeName, methodName, methodParameters);
    }
    
    private static MethodParameter ParseParameter(string parameterRawString)
    {
        if (!parameterRawString.StartsWith('['))
        {
            return new MethodParameter(null, parameterRawString);
        }

        var moduleName = parameterRawString[1..parameterRawString.IndexOf(']')];
        return new MethodParameter(moduleName, parameterRawString[(parameterRawString.IndexOf(']') - 1)..]);
    }
}