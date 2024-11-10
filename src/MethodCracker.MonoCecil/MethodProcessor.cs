using System;
using System.Linq;
using System.Reflection;
using MethodCracker.Attributes;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodCracker.MonoCecil;

public ref struct MethodProcessor(MethodDefinition methodDefinition)
{
    public static bool UseSpecialCharacterInMethodName = false;
    private ModuleDefinition Module => methodDefinition.Module;
    private TypeDefinition ParentType => methodDefinition.DeclaringType;

    public bool Process()
    {
        if(!IsProcessEnabled)
        {
            return false;
        }

        var methodName = methodDefinition.Name;
        var safeName = GetSafeNameByName(methodName);

        if(ParentType.Methods.Any(x => x.Name == safeName))
        {
            return false;
        }
        
        var newMethod = new MethodDefinition(safeName, methodDefinition.Attributes, methodDefinition.ReturnType);
        newMethod.IsIL = true;
        var ilProcessor = newMethod.Body.GetILProcessor();
        
        var oldMethodProcessor = methodDefinition.Body.GetILProcessor();


        newMethod.Body.InitLocals = methodDefinition.Body.InitLocals;
        
        // Move old code into a new method
        // It must through a ILProcessor, because the 'Instructions' property is read-only
        foreach(var oldInstruction in methodDefinition.Body.Instructions)
        {
            ilProcessor.Append(oldInstruction);
        }
        
        // Copy the parameters
        newMethod.Parameters.Clear();
        
        // "this" parameter
        newMethod.Parameters.Add(new ParameterDefinition(Module.ImportReference(typeof(object))));
        
        foreach (var parameter in methodDefinition.Parameters)
        {
            newMethod.Parameters.Add(parameter);
        }

        ParentType.Methods.Add(newMethod);
        
        // Start to replace codes in the old method
        oldMethodProcessor.Clear();

        // To get the hooks manager, we should determine the type of the manager.
        var hooksManagerTypeReference = Module.ImportReference(typeof(HooksManager<>));
        hooksManagerTypeReference.GenericParameters.Add(new GenericParameter(ParentType));
        
        // Get the property of the manager
        var managerProperty = ParentType.Properties.First(x => 
            x.Name is "HooksManager" &&
            x.GetMethod.ReturnType.FullName == hooksManagerTypeReference.FullName);
        
        // This will emit this code:
        // call [MethodCracker]MethodCracker.HooksManager`1<class MyClass> MyClass::get_HooksManager()
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Call, managerProperty.GetMethod));
        
        // Push the origin method name into the stack,
        // for the manager is using the origin method name to organize the hooks.
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldstr, methodName));
        
        // This will emit this code:
        // callvirt instance class [MethodCracker]MethodCracker.HookCollection`1<!!0>
        // [MethodCracker]MethodCracker.HooksManager::GetHookCollection<class [System.Runtime]System.Action`3<object, int32, int32>>(string)
        var getHookCollectionMethodReference = Module.ImportReference(
            typeof(HooksManager<>).GetMethod("GetHookCollection", BindingFlags.Instance | BindingFlags.Public));
        var delegateTypeReference = Module.ImportReference(methodDefinition.ReturnType.FullName == typeof(void).FullName
                                ? typeof(Action<>)
                                : typeof(Func<>));
        foreach (var parameterDefinition in methodDefinition.Parameters)
        {
            delegateTypeReference.GenericParameters.Add(new GenericParameter(Module.ImportReference(parameterDefinition.ParameterType)));
        }

        if (methodDefinition.ReturnType.FullName != typeof(void).FullName)
        {
            delegateTypeReference.GenericParameters.Add(new GenericParameter(methodDefinition.ReturnType));
        }
        
        getHookCollectionMethodReference.GenericParameters.Add(new GenericParameter(delegateTypeReference));
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Callvirt, getHookCollectionMethodReference));
        
        // Then, create an array to store the passed parameters and the instance.
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4, methodDefinition.Parameters.Count + 1));
        oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Newarr, Module.ImportReference(typeof(object))));

        if (methodDefinition.IsStatic)
        {
            // Push 'null' as the method target
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Dup));
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4_0));
            oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldnull));
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
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldarg, parameter.Index));
                if (parameter.ParameterType.IsValueType)
                {
                    oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Box, parameter.ParameterType));
                }

                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Stelem_Ref));
            } 
        }
        else
        {
            foreach (var parameter in methodDefinition.Parameters)
            {
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Dup));
                
                // Pushing index
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldc_I4, parameter.Index));
                
                // Pushing value
                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Ldarg_S, parameter.Index));
                if (parameter.ParameterType.IsValueType)
                {
                    oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Box, parameter.ParameterType));
                }

                oldMethodProcessor.Append(oldMethodProcessor.Create(OpCodes.Stelem_Ref));
            }
        }

        // This will emit this code:
        // callvirt instance object class [MethodCracker]MethodCracker.HookCollection`1<class [System.Runtime]System.Action`3<object, int32, int32>>::Execute(object[])
        var executeMethodReference = Module.ImportReference(typeof(HookCollection<>).GetMethod("Execute"));
        executeMethodReference.GenericParameters.Add(new GenericParameter(delegateTypeReference));
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
        return true;

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
            if(!methodDefinition.IsIL)
            {
                return false;
            }

            foreach(var attribute in methodDefinition.CustomAttributes)
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
            return !IsProcessed & methodDefinition.CustomAttributes.Any(x => x.AttributeType.FullName
                        is "MethodCracker.Attributes.CrackableMethodAttribute");
        }
    }
}
