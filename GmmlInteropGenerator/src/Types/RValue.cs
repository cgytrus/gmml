using System.Runtime.InteropServices;

using JetBrains.Annotations;

namespace GmmlInteropGenerator.Types;

[StructLayout(LayoutKind.Explicit, Size = 16), PublicAPI]
public unsafe struct RValue {
    [FieldOffset(0)] public bool valueBool;
    [FieldOffset(0)] public int valueInt32;
    [FieldOffset(0)] public long valueInt64;
    [FieldOffset(0)] public float valueFloat;
    [FieldOffset(0)] public double valueReal;
    [FieldOffset(0)] public uint valueUint32;
    [FieldOffset(0)] public RefThing<sbyte>* refString;
    [FieldOffset(0)] public RefDynamicArrayOfRValue* refArray;
    [FieldOffset(0)] public YYObjectBase* obj;
    [FieldOffset(0)] public void* ptr;
    [FieldOffset(8)] public uint flags;
    [FieldOffset(12)] public RVKind kind;
}
