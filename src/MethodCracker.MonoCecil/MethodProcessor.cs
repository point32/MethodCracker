using System.Reflection;
using MethodCracker.Attributes;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodCracker.MonoCecil;

/// <summary>
/// A processor to process a method.
/// </summary>
/// <param name="methodDefinition">Definition of the method</param>
public readonly struct MethodProcessor(MethodDefinition methodDefinition)
{
    /// <summary>
    /// A global flag, indicates whether the processor should use special '$&lt;&gt;' as the method name prefix.
    /// </summary>
    public static bool UseSpecialCharacterInMethodName { get; set; } = true;

    /// <summary>
    /// The module which the method belongs to.
    /// </summary>
    public ModuleDefinition Module => methodDefinition.Module;

    /// <summary>
    /// The type which the method belongs to.
    /// </summary>
    public TypeDefinition ParentType => methodDefinition.DeclaringType;

    /// <summary>
    /// Process the method to make it hookable,
    /// may throw an exception under some conditions.
    /// </summary>
    /// <returns>
    /// False when the method has already been processed,
    /// and will be true when the method is successfully processed.
    /// </returns>
    public bool Process()
    {
        if (!IsProcessEnabled) return false;

        string? methodName = methodDefinition.Name;
        string safeName = GetSafeNameByName(methodName);

        if (ParentType.Methods.Any(x => x.Name == safeName)) return false;

        var newMethod = new MethodDefinition(safeName, methodDefinition.Attributes, methodDefinition.ReturnType)
        {
            IsIL = true
        };
        ILProcessor? ilProcessor = newMethod.Body.GetILProcessor();

        ILProcessor? oldMethodProcessor = methodDefinition.Body.GetILProcessor();

        newMethod.Body.InitLocals = methodDefinition.Body.InitLocals;
        newMethod.ReturnType = methodDefinition.ReturnType;
        newMethod.Body.MaxStackSize = methodDefinition.Body.MaxStackSize;
        newMethod.IsStatic = methodDefinition.IsStatic;

        foreach (VariableDefinition? variable in methodDefinition.Body.Variables)
            newMethod.Body.Variables.Add(variable);

        // Move old code into a new method
        // It must through a ILProcessor, because the 'Instructions' property is read-only
        foreach (Instruction? oldInstruction in methodDefinition.Body.Instructions) ilProcessor.Append(oldInstruction);

        // Copy the parameters
        newMethod.Parameters.Clear();

        foreach (ParameterDefinition? parameter in methodDefinition.Parameters) newMethod.Parameters.Add(parameter);

        // Start to replace codes in the old method
        oldMethodProcessor.Clear();

        // To get the hooks manager, we should determine the type of the manager.
        TypeReference? hooksManagerTypeReference = Module.ImportReference(typeof(HooksManager));

        // Get manager:
        // ldtoken MyClass
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldtoken, ParentType));

        // call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Call, Module.ImportReference(
            typeof(Type).GetMethod("GetTypeFromHandle"))));

        // call instance class [MethodCracker]MethodCracker.HooksManager [MethodCracker]MethodCracker.GlobalHooksManager::GetHooksManager([System.Runtime]Type)
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Call, Module.ImportReference(typeof(GlobalHooksManager)
            .GetMethod("GetHooksManager"))));

        // Push the origin method name into the stack,
        // for the manager is using the origin method name to organize the hooks.
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldstr, methodName));

        // This will emit code like this:
        // callvirt instance class [MethodCracker]MethodCracker.HookCollection`1<!!0>
        // [MethodCracker]MethodCracker.HooksManager::GetHookCollection<class [System.Runtime]System.Action`n<.....>>(string)
        bool returnsVoid = methodDefinition.ReturnType.FullName == typeof(void).FullName;
        string delegateTypeName = "System." +
                                  (returnsVoid ? "Action`" : "Func`") +
                                  (methodDefinition.Parameters.Count +
                                   (returnsVoid ? 0 : 1));

        TypeReference? delegateTypeReference =
            Module.ImportReference(Assembly.Load("System.Runtime").GetType(delegateTypeName));
        var delegateTypeInstance = new GenericInstanceType(delegateTypeReference);
        foreach (ParameterDefinition? parameterDefinition in methodDefinition.Parameters)
            delegateTypeInstance.GenericArguments.Add(parameterDefinition.ParameterType);

        if (methodDefinition.ReturnType.FullName != typeof(void).FullName)
            delegateTypeInstance.GenericArguments.Add(methodDefinition.ReturnType);

        TypeReference? hookCollectionInterfaceReference = Module.ImportReference(typeof(IHookCollection));
        // HookCollection<THookType>

        MethodDefinition? getHookCollectionMethod = hooksManagerTypeReference.Resolve().Methods
            .First(x => x.Name == "GetHookCollection"
                        && x.Parameters.Count == 1
                        && x.Parameters[0].ParameterType.FullName == "System.String");
        var getHookCollectionMethodInstance = new GenericInstanceMethod(getHookCollectionMethod);
        getHookCollectionMethodInstance.GenericArguments.Add(delegateTypeInstance);
        // Method signature: IHookCollection HooksManager.GetHookCollection<MyDelegateType>(string name)

        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Callvirt,
            Module.ImportReference(getHookCollectionMethodInstance)));

        // Then, create an array to store the passed parameters and the instance.
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4, methodDefinition.Parameters.Count + 1));
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Newarr, Module.ImportReference(typeof(object))));

        // Push the method target
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Dup));
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4_0));

        bool isMethodStatic = methodDefinition.IsStatic;
        if (isMethodStatic)
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldnull));
        else
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldarg_0));

        if (ParentType.IsValueType) oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Box, ParentType));

        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Stelem_Ref));

        // Push the parameters into an array (object[])
        foreach (ParameterDefinition? parameter in methodDefinition.Parameters)
        {
            // Duplicate the array
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Dup));

            // Prepare an index(int)
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4, parameter.Index + 1));

            // Prepare an argument(object)
            oldMethodProcessor.Append(oldMethodProcessor.Create(
                OpCodes.Ldarg,
                isMethodStatic ? parameter.Index : parameter.Index + 1));
            if (parameter.ParameterType.IsValueType)
                // If the argument is a value type, box it
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Box, parameter.ParameterType));

            // Store the argument into the array
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Stelem_Ref));
        }

        // callvirt instance object class [MethodCracker]MethodCracker.IHookCollection.Execute(object[])
        MethodDefinition? executeMethod = hookCollectionInterfaceReference.Resolve().Methods.First(x =>
            x.Name == "Execute"
            && x.Parameters.Count is 1
            && x.Parameters[0].ParameterType.FullName == "System.Object[]");
        MethodReference? executeMethodReference = Module.ImportReference(executeMethod);
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Callvirt, executeMethodReference));

        // Process the returned value
        if (methodDefinition.ReturnType.FullName != typeof(void).FullName)
        {
            // If the returned value is a value type, unbox it.
            if (methodDefinition.ReturnType.IsValueType)
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Unbox_Any, methodDefinition.ReturnType));
            else
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Castclass, methodDefinition.ReturnType));

            // Return the value
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ret));
        }
        else
        {
            // Discard the returned value, and return
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Pop));
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ret));
        }

        // Add 'ProcessedAttribute' with to the method
        MethodReference? processedAttributeConstructor = Module.ImportReference(
            typeof(ProcessedAttribute).GetConstructor([typeof(string)])
        );

        // Since the origin code is in the new method,
        // specifics the new method so that the hook can find the origin code.
        var attribute = new CustomAttribute(processedAttributeConstructor);
        attribute.ConstructorArguments.Add(
            new CustomAttributeArgument(Module.ImportReference(typeof(string)), safeName));

        methodDefinition.CustomAttributes.Add(attribute);

        // It is no need to add 'CrackableMethod' because developer was supposed to add it,
        // so it means the attribute is already exists.

        ParentType.Methods.Add(newMethod);
        return true;

        static string GetSafeNameByName(string name)
        {
            return UseSpecialCharacterInMethodName
                       ? $"$<>__MethodCrackerGenerated_OriginMethod__{name}"
                       : $"__MethodCracker_Generated__{name}";
        }
    }

    public bool IsProcessed
    {
        get
        {
            if (!methodDefinition.IsIL) return false;

            foreach (CustomAttribute? attribute in methodDefinition.CustomAttributes)
                if (attribute.AttributeType.FullName is "MethodCracker.Attributes.ProcessedAttribute")
                    return true;

            return false;
        }
    }


    public bool IsProcessEnabled
    {
        get
        {
            return !IsProcessed && methodDefinition.CustomAttributes.Any(x => x.AttributeType.FullName
                                                                                  is
                                                                                  "MethodCracker.Attributes.CrackableMethodAttribute");
        }
    }
}