using System;
using System.Linq;
using System.Reflection;

using Mono.Cecil;

namespace MethodCracker.Internals;

internal class AssemblyExtensions
{
    public static ModuleDefinition GetDefinition(this Assembly @this)
    {
        if(!File.Exists(@this.Location))
        {
            retuen null;
        }

        return ModuleDefinition.ReadModule(@this.Location);
    }
}
