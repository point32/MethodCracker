using MethodCracker.Attributes;
using MethodCracker.ProcessorConfig;
using Mono.Cecil;

namespace MethodCracker.MonoCecil;

/// <summary>
/// Processor for a config file, will process all the modules in the config,
/// needs a module resolver.
/// NOTE: The streams which the resolver return won't be dispose
/// or close, before calling methods in the processor,
/// it is necessary to make sure that all the streams can be read and write.
/// </summary>
/// <param name="processorConfig">
/// The config file which determines the modules to process.
/// </param>
/// <param name="modulesResolver">
/// A resolver for the processor to find the stream of the module.
/// </param>
public class MethodCrackerProcessor(CrackableMethodsList processorConfig, Func<string, Stream> modulesResolver)
{
    public void Process()
    {
        foreach (CrackableModule? crackableModule in processorConfig.CrackableModules)
        {
            var module = crackableModule.ModuleName;
            var stream = modulesResolver(module);
            stream.Position = 0;
            ModuleDefinition? moduleDefinition = ModuleDefinition.ReadModule(stream);
            ProcessModule(moduleDefinition, crackableModule);

            stream.Position = 0;
            moduleDefinition.Write(stream);
        }

        return;

        static void ProcessModule(ModuleDefinition moduleDefinition, CrackableModule crackableModule)
        {
            foreach (CrackableMethodInfo crackableMethod in crackableModule.CrackableMethods)
            {
                ProcessMethod(moduleDefinition, crackableMethod);
            }
        }

        static void ProcessMethod(ModuleDefinition moduleDefinition, CrackableMethodInfo crackableMethod)
        {
            TypeDefinition? typeDefinition = moduleDefinition.GetType(crackableMethod.TypeName);
            var methods = typeDefinition.Methods
                .Where(method => method.Name == crackableMethod.MethodName);

            // Matching method by method signature
            MethodDefinition? method = methods.FirstOrDefault(m =>
            {
                if (crackableMethod.Parameters.Length != m.Parameters.Count) return false;

                for (var i = 0; i < crackableMethod.Parameters.Length; i++)
                {
                    TypeReference? expectedParameterType = m.Parameters[i].ParameterType;
                    MethodParameter actualParameterType = crackableMethod.Parameters[i];
                    if (crackableMethod.Parameters[i].ModuleName != null)
                    {
                        if (expectedParameterType.FullName != actualParameterType.TypeName) return false;

                        if (actualParameterType.ModuleName != expectedParameterType.Module.Name) return false;

                        continue;
                    }

                    if (TypeFullNameByShortName[actualParameterType.TypeName] != expectedParameterType.FullName)
                        return false;
                }

                return true;
            });

            if (method == null) throw new MethodNotFoundException(crackableMethod);

            var crackableAttribute =
                moduleDefinition.ImportReference(typeof(CrackableMethodAttribute).GetConstructor([]));
            method.CustomAttributes.Add(new CustomAttribute(crackableAttribute));
            if (!new MethodProcessor(method).Process())
                throw new Exception($"Method {method.FullName} is not processed");
        }
    }

    private static readonly Dictionary<string, string> TypeFullNameByShortName = new()
    {
        ["byte"] = "System.Byte",
        ["sbyte"] = "System.SByte",
        ["short"] = "System.Int16",
        ["int"] = "System.Int32",
        ["uint"] = "System.UInt32",
        ["long"] = "System.Int64",
        ["ulong"] = "System.UInt64",
        ["string"] = "System.String",
        ["bool"] = "System.Boolean",
        ["float"] = "System.Single",
        ["double"] = "System.Double",
        ["object"] = "System.Object",
        ["void"] = "System.Void",
        ["char"] = "System.Char"
    };
}