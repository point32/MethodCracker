using System.Reflection;
using System.Text;
using System.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ExampleApp1;

public class ObjectDumper
{
    public interface IValueDumper
    {
        bool CanDump(Type type);
        Task DumpValue(object value, TextWriter writer);
    }

    public interface ValueDumper<T> : IValueDumper
    {
        bool IValueDumper.CanDump(Type t) => typeof(T) == t;
        Task DumpValue(T value, TextWriter writer);
        Task IValueDumper.DumpValue(object value, TextWriter writer) => DumpValue((T)value, writer);
    }

    public class BoolDumper : ValueDumper<bool>
    {
        public bool CanDump(Type t) => typeof(bool) == t;
        async Task ValueDumper<bool>.DumpValue(bool value, TextWriter writer) => writer.Write(value ? "true" : "false");
    }

    public class StringDumper : ValueDumper<string>
    {
        public bool CanDump(Type t) => typeof(string) == t;
        async Task ValueDumper<string>.DumpValue(string value, TextWriter writer) => writer.Write($"\"{value}\"");
    }

    public class InstructionDumper : ValueDumper<Instruction>
    {
        async Task ValueDumper<Instruction>.DumpValue(Instruction inst, TextWriter writer)
        {
            var obj = new { OpCode = inst.OpCode.ToString(), Operand = inst.Operand };
            DumpObject(obj, writer);
        }
    }

    public class IntDumper : ValueDumper<ValueType>
    {
        public static string[] SupportedTyped { get; } =
        {
            "System.Int32",
            "System.Short",
            "System.Single",
            "System.Int128",
            "System.Int64",
            "System.Int16",
            "System.Double",
            "System.UInt16",
            "System.UInt32",
            "System.UInt64",
            "System.UInt128",
            "System.SByte",
            "System.Byte"
        };
        public bool CanDump(Type t) => SupportedTyped.Contains(t.FullName);
        async Task ValueDumper<ValueType>.DumpValue(ValueType value, TextWriter writer) => writer.Write(value.ToString());
    }

    public class EnumerableDumper : ValueDumper<IEnumerable>
    {
        public bool CanDump(Type t) => t.GetInterfaces().Any(x => x == typeof(IEnumerable));
        async Task ValueDumper<IEnumerable>.DumpValue(IEnumerable value, TextWriter writer)
        {
            var indent = new string('\t', Depth);
            writer.Write("[");

            bool appendComma = false;
            foreach (var item in value)
            {
                if (appendComma)
                {
                    writer.Write(",");
                }

                appendComma = true;
                writer.Write($"\n\t{indent}");
                await Task.Run(() => ObjectDumper.DumpJson(item, writer));
            }

            writer.Write($"\n{indent}]");
        }
    }

    public class ValueTypeDumper : ValueDumper<ValueType>
    {
        bool IValueDumper.CanDump(Type type) => type.BaseType == typeof(ValueType);
        async Task ValueDumper<ValueType>.DumpValue(ValueType value, TextWriter writer)
        {
            Type type = value.GetType();
            var indent = new string('\t', Depth);
            
            writer.WriteLine("{");
            writer.WriteLine($"{indent}\"Type\": \"{type.FullName}\",");
            writer.Write($"{indent}\"__IsValueType\": true");
            
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance))
            {
                object propertyValue;

                try
                {
                    propertyValue = await Task.Run(() => propertyInfo.GetValue(value));
                }
                catch (Exception e)
                {
                    propertyValue = $"Exception!{e.Message}";
                }

                writer.Write($",\n{indent}\"{propertyInfo.Name}\": ");
                await Task.Run(() => ObjectDumper.DumpJson(propertyValue, writer));
            }

            foreach (var fieldInfo in await Task.Run(() => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance)))
            {
                if (fieldInfo.IsSpecialName)
                {
                    continue;
                }

                writer.Write($",\n{indent}\"{fieldInfo.Name}\": ");
                await Task.Run(() => ObjectDumper.DumpJson(fieldInfo.GetValue(value), writer));
            }
        }
    }

    public class ClassDumper : ValueDumper<object>
    {
        bool IValueDumper.CanDump(Type type) => type.BaseType != typeof(ValueType);
        async Task ValueDumper<object>.DumpValue(object value, TextWriter writer)
        {
            Type type = value.GetType();
            var indent = new string('\t', Depth);
            writer.Write($"{"{"}\n{indent}\"Type\": \"{type.FullName}\",\n{indent}\"__IsValueType\": true,\n{indent}\"__ObjectId\": ${Dumped.Count - 1}");
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance))
            {
                object propertyValue;

                try
                {
                    propertyValue = await Task.Run(() => propertyInfo.GetValue(value));
                }
                catch (Exception e)
                {
                    propertyValue = $"Exception!{e.Message}";
                }

                writer.Write($",\n{indent}\"{propertyInfo.Name}\": "); ;
                await Task.Run(() => ObjectDumper.DumpJson(propertyValue, writer));
            }

            foreach (var fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance))
            {
                if (fieldInfo.IsSpecialName)
                {
                    continue;
                }

                writer.Write($",\n{indent}\"{fieldInfo.Name}\": ");
                await Task.Run(() => ObjectDumper.DumpJson(fieldInfo.GetValue(value), writer));
            }

            writer.Write("\n" + indent + "}");
        }
    }

    public static List<IValueDumper> Dumpers { get; } =
    [
        new InstructionDumper(),
        new BoolDumper(),
        new IntDumper(),
        new StringDumper(),
        new EnumerableDumper(),
        new ValueTypeDumper(),
        new ClassDumper()
    ];

    public static List<object> Dumped = new();
    public static int Depth = 0;
    public static void DumpObject(object o, TextWriter writer)
    {
        Dumped.Clear();
        DumpJson(o, writer).Wait();
    }

    public async static Task DumpJson(object? o, TextWriter writer)
    {
        if (Depth >= 5)
        {
            writer.Write("......");
            return;
        }

        if (o is null)
        {
            writer.Write("null");
            return;
        }

        var index = -1;
        for (int i = o is ValueType ? int.MaxValue : 0; i < Dumped.Count; i++)
        {
            if (object.ReferenceEquals(Dumped[i], o))
            {
                index = i;
            }
        }

        if (index >= 0 && o is not string)
        {
            writer.Write($"\"Value on Dumped List at " + index + "\"");
            return;
        }

        if (o is not ValueType)
        {
            Dumped.Add(o);
        }

        Depth++;
        foreach (var dumper in Dumpers)
        {
            if (dumper.CanDump(o.GetType()))
            {
                await dumper.DumpValue(o, writer);
                Depth--;
                return;
            }
        }

        Depth--;
        writer.Write("Not supported for type " + o.GetType().FullName);
    }
}
