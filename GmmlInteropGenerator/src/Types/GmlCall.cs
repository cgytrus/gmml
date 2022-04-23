using System.Runtime.InteropServices;

namespace GmmlInteropGenerator.Types;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void GmlCall(RValue* result, CInstance* selfInstance, CInstance* otherInstance, int argCount,
    RValue* args);
