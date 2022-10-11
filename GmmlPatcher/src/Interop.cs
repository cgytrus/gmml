using System.Reflection;
using System.Runtime.InteropServices;

using GmmlInteropGenerator;
using GmmlInteropGenerator.Types;

using JetBrains.Annotations;

namespace GmmlPatcher;

[PublicAPI]
public static class Interop {
    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void Function_Add(sbyte* name, GmlCall function, int argCount, bool unk);

    [UnmanagedCallersOnly]
    public static void InitGmlFunctions() {
        Console.WriteLine("Initializing interop functions");
        foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach(Type type in assembly.GetTypes())
                foreach(MethodInfo method in type.GetMethods()) {
                    AdvancedGmlInteropAttribute? attribute = method.GetCustomAttribute<AdvancedGmlInteropAttribute>();
                    if(attribute is null)
                        continue;

                    Console.WriteLine($"Initializing function {attribute.name}(argc={attribute.argumentCount})");
                    InitFunction(attribute.name, method.CreateDelegate<GmlCall>(), attribute.argumentCount);
                }
    }

    private static unsafe void InitFunction(string name, GmlCall function, int argumentCount) {
        sbyte* namePtr = (sbyte*)Marshal.StringToHGlobalAnsi(name);
        GCHandle.Alloc(function); // keep the function delegate alive forever
        Function_Add(namePtr, function, argumentCount, false);
    }
}
