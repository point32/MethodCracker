using System;
using System.Linq;

using Mono.Cecil;

namespace MethodCracker.MonoCecil;

public struct MethodProcessor
{
    public static bool UseSpecialCharacterInMethodName = false;

    private TypeDefinition ParentType => m_methodDefinition.DeclaringType;

    private MethodDefinition m_methodDefinition;
    public MethodProcessor(MethodDefinition methodDefinition)
    {
        m_methodDefinition = methodDefinition;
    }

    public bool Process()
    {
        if(!IsProcessEnabled)
        {
            return false;
        }

        var methodName = m_methodDefinition.Name;
        var safeName = GetSafeNameByName(methodName);

        if(ParentType.Methods.Any(x => x.Name == safeName))
        {
            return false;
        }
        
        var newMethod = new MethodDefinition(safeName, m_methodDefinition.Attributes, m_methodDefinition.ReturnType);
        newMethod.IsIL = true;
        var ilProcessor = newMethod.Body.GetILProcessor();
        
        var oldMethodProcessor = m_methodDefinition.Body.GetILProcessor();


        newMethod.Body.InitLocals = m_methodDefinition.Body.InitLocals;
        
        // Move old code into a new method
        // It must through a ILProcessor, because the 'Instructions' property is read-only
        foreach(var oldInstruction in m_methodDefinition.Body.Instructions)
        {
            ilProcessor.Append(oldInstruction);
        }

        oldMethodProcessor.Clear();
        oldMethodProcessor.Emit(Mono.Cecil.Cil.OpCodes.Ret);
        ParentType.Methods.Add(newMethod);

        // Add 'ProcessedAttribute' to the two
        TypeReference attributeTypeReference = TypeReference("MethodCracker.Attributes", "ProcessedAttribute")
        CustomAttribute attribute = new();
        return false;

        static string GetSafeNameByName(string name)
        {
            return UseSpecialCharacterInMethodName ? $"<MethodCrackerGenerated_OriginMethod>{name}"
                : $"__MethodCracker_Generated__{name}";
        }
    }

    public bool IsProcessed
    {
        get
        {
            if(!m_methodDefinition.IsIL)
            {
                return false;
            }

            foreach(var attribute in m_methodDefinition.CustomAttributes)
            {
                if(attribute.AttributeType.FullName is "MethodCracker.Attributes.ProcessedAttribute")
                {
                    return true;
                }
            }

            return false;
        }
    }


    public bool IsProcessEnabled
    {
        get
        {
            return !IsProcessed & m_methodDefinition.CustomAttributes.Any(x => x.AttributeType.FullName
                        is "MethodCracker.Attributes.CrackableMethodAttribute");
        }
    }
}
