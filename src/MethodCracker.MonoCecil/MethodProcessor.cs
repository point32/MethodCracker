using System.Reflection;
using MethodCracker.Attributes;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodCracker.MonoCecil;

public struct MethodProcessor(MethodDefinition methodDefinition)
{
    public static bool UseSpecialCharacterInMethodName = true;
    private ModuleDefinition Module => methodDefinition.Module;
    private TypeDefinition ParentType => methodDefinition.DeclaringType;

    public bool Process()
    {
        if (!IsProcessEnabled)
        {
            return false;
        }

        var methodName = methodDefinition.Name;
        var safeName = GetSafeNameByName(methodName);

        if (ParentType.Methods.Any(x => x.Name == safeName))
        {
            return false;
        }

        var newMethod = new MethodDefinition(safeName, methodDefinition.Attributes, methodDefinition.ReturnType);
        newMethod.IsIL = true;
        var ilProcessor = newMethod.Body.GetILProcessor();

        var oldMethodProcessor = methodDefinition.Body.GetILProcessor();

        newMethod.Body.InitLocals = methodDefinition.Body.InitLocals;
        newMethod.ReturnType = methodDefinition.ReturnType;
        newMethod.Body.MaxStackSize = methodDefinition.Body.MaxStackSize;
        newMethod.IsStatic = methodDefinition.IsStatic;

        foreach (var variable in methodDefinition.Body.Variables)
        {
            newMethod.Body.Variables.Add(variable);
        }

        // Move old code into a new method
        // It must through a ILProcessor, because the 'Instructions' property is read-only
        foreach (var oldInstruction in methodDefinition.Body.Instructions)
        {
            ilProcessor.Append(oldInstruction);
        }

        // Copy the parameters
        newMethod.Parameters.Clear();

        foreach (var parameter in methodDefinition.Parameters)
        {
            newMethod.Parameters.Add(parameter);
        }

        // Start to replace codes in the old method
        oldMethodProcessor.Clear();

        // To get the hooks manager, we should determine the type of the manager.
        var hooksManagerTypeReference = Module.ImportReference(typeof(HooksManager));

        // Get the property of the manager
        var managerProperty = ParentType.Properties.First(x =>
            x.Name is "HooksManager" &&
            x.PropertyType.FullName == hooksManagerTypeReference.FullName);

        // This will emit this code:
        // call [MethodCracker]MethodCracker.HooksManager`1<class MyClass> MyClass::get_HooksManager()
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Call, managerProperty.GetMethod));

        // Push the origin method name into the stack,
        // for the manager is using the origin method name to organize the hooks.
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldstr, methodName));

        // This will emit code like this:
        // callvirt instance class [MethodCracker]MethodCracker.HookCollection`1<!!0>
        // [MethodCracker]MethodCracker.HooksManager::GetHookCollection<class [System.Runtime]System.Action`n<.....>>(string)
        bool returnsVoid = methodDefinition.ReturnType.FullName == typeof(void).FullName;
        var delegateTypeName = "System." +
            (returnsVoid ? "Action`" : "Func`") + 
            (methodDefinition.Parameters.Count + 
            (returnsVoid ? 0 : 1));

        var delegateTypeReference = Module.ImportReference(Assembly.Load("System.Runtime").GetType(delegateTypeName));
        var delegateTypeInstance = new GenericInstanceType(delegateTypeReference);
        foreach (var parameterDefinition in methodDefinition.Parameters)
        {
            delegateTypeInstance.GenericArguments.Add(parameterDefinition.ParameterType);
        }

        if (methodDefinition.ReturnType.FullName != typeof(void).FullName)
        {
            delegateTypeInstance.GenericArguments.Add(methodDefinition.ReturnType);
        }
        
        var hookCollectionInterfaceReference = Module.ImportReference(typeof(IHookCollection));
        // HookCollection<THookType>

        var getHookCollectionMethod = hooksManagerTypeReference.Resolve().Methods
            .First(x => x.Name == "GetHookCollection"
                        && x.Parameters.Count == 1
                        && x.Parameters[0].ParameterType.FullName == "System.String");
        var getHookCollectionMethodInstance = new GenericInstanceMethod(getHookCollectionMethod);
        getHookCollectionMethodInstance.GenericArguments.Add(delegateTypeInstance);
        // Method signature: IHookCollection HooksManager.GetHookCollection<MyDelegateType>(string name)

        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Callvirt, Module.ImportReference(getHookCollectionMethodInstance)));

        // Then, create an array to store the passed parameters and the instance.
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4, methodDefinition.Parameters.Count + 1));
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Newarr, Module.ImportReference(typeof(object))));
 
        // Push the method target
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Dup));
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4_0));

        bool isMethodStatic = methodDefinition.IsStatic;
        if (isMethodStatic)
        {
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldnull));
        }
        else
        {
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldarg_0));
        }

        if (ParentType.IsValueType)
        {
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Box, ParentType));
        }
        
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Stelem_Ref));

        foreach (var parameter in methodDefinition.Parameters)
        {
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Dup));

            // Pushing index
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4, parameter.Index + 1));

            // Pushing value
            oldMethodProcessor.Append(oldMethodProcessor.Create(
                OpCodes.Ldarg,
                isMethodStatic ? parameter.Index : parameter.Index + 1));
            if (parameter.ParameterType.IsValueType)
            {
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Box, parameter.ParameterType));
            }

            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Stelem_Ref));
        }

        // This will emit this code:
        // callvirt instance object class [MethodCracker]MethodCracker.IHookCollection.Execute(object[])
        var executeMethod = hookCollectionInterfaceReference.Resolve().Methods.First(x =>
            x.Name == "Execute"
            && x.Parameters.Count is 1
            && x.Parameters[0].ParameterType.FullName == "System.Object[]");
        var executeMethodReference = Module.ImportReference(executeMethod);
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Callvirt, executeMethodReference));

        if (methodDefinition.ReturnType.FullName != typeof(void).FullName)
        {
            if (methodDefinition.ReturnType.IsValueType)
            {
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Unbox_Any, methodDefinition.ReturnType));
            }
            else
            {
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Castclass, methodDefinition.ReturnType));
            }

            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ret));
        }
        else
        {
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Pop));
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ret));
        }

        // Add 'ProcessedAttribute'
        var processedAttributeConstructor = Module.ImportReference(
            typeof(ProcessedAttribute).GetConstructor([typeof(string)])
            );

        // Since the origin code is in the new method,
        // specifics the new method so that the hook can find the origin code.
        CustomAttribute attribute = new CustomAttribute(processedAttributeConstructor);
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(Module.ImportReference(typeof(string)), safeName));

        methodDefinition.CustomAttributes.Add(attribute);

        // It is no need to add 'CrackableMethod' because developer was supposed to add it,
        // so it means the attribute is already exists.

        ParentType.Methods.Add(newMethod);
        return true;

        static string GetSafeNameByName(string name)
        {
            return UseSpecialCharacterInMethodName ? $"$<>__MethodCrackerGenerated_OriginMethod__{name}"
                : $"__MethodCracker_Generated__{name}";
        }
    }

    public bool IsProcessed
    {
        get
        {
            if (!methodDefinition.IsIL)
            {
                return false;
            }

            foreach (var attribute in methodDefinition.CustomAttributes)
            {
                if (attribute.AttributeType.FullName is "MethodCracker.Attributes.ProcessedAttribute")
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
            return !IsProcessed && methodDefinition.CustomAttributes.Any(x => x.AttributeType.FullName
                        is "MethodCracker.Attributes.CrackableMethodAttribute");
        }
    }
}
