using System.Runtime.InteropServices;

using JetBrains.Annotations;

namespace GmmlInteropGenerator.Types;

[UnmanagedFunctionPointer(CallingConvention.Cdecl), PublicAPI]
public unsafe delegate void GmlCall(RValue* result, CInstance* selfInstance, CInstance* otherInstance, int argCount,
    RValue* args);
