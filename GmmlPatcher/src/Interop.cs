using System.Reflection;
using System.Runtime.InteropServices;
// ReSharper disable MemberCanBeInternal MemberCanBePrivate.Global UnassignedField.Global FieldCanBeMadeReadOnly.Global

namespace GmmlPatcher;

public static class Interop {
    // ReSharper disable once InconsistentNaming
    public enum RVKind : uint {
        Real = 0, // Real value
        String = 1, // String value
        Array = 2, // Array value
        Object = 6, // YYObjectBase* value
        Int32 = 7, // Int32 value
        Undefined = 5, // Undefined value
        Ptr = 3, // Ptr value
        Vec3 = 4, // Deprecated : unused : Vec3 (x,y,z) value (within the RValue)
        Vec4 = 8, // Deprecated : unused :Vec4 (x,y,z,w) value (allocated from pool)
        Vec44 = 9, // Deprecated : unused :Vec44 (matrix) value (allocated from pool)
        Int64 = 10, // Int64 value
        Accessor = 11, // Actually an accessor
        Null = 12, // JS Null
        Bool = 13, // Bool value
        Iterator = 14, // JS For-in Iterator
        Ref = 15, // Reference value (uses the ptr to point at a RefBase structure)
        Unset = 0xffffff
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
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

    public unsafe struct RefThing<T> where T : unmanaged {
        public T* thing;
        public int refCount;
        public int size;
    }

    public unsafe struct CInstanceBase {
        public void* vtable;
        public RValue* yyVars;
    }

    // ReSharper disable once InconsistentNaming
    public unsafe struct YYObjectBase {
        public CInstanceBase instanceBase;
        public YYObjectBase* pNextObject;
        public YYObjectBase* pPrevObject;
        public YYObjectBase* prototype;
        public sbyte* classStr;
        public void* getOwnProperty;
        public void* deleteProperty;
        public void* defineOwnProperty;
        public void* yyVarsMap; // CHashMap<int, RValue*>
        public CWeakRef** weakRefs;
        public uint numWeakRefs;
        public uint nVars;
        public uint flags;
        public uint capacity;
        public uint visited;
        public uint visitedGc;
        public int gcGen;
        public int creationFrame;
        public int slot;
        public int kind;
        public int rvalueInitType;
        public int curSlot;
    }

    public unsafe struct CWeakRef {
        public YYObjectBase objectBase;
        public YYObjectBase* weakRef;
    }

    public unsafe struct RefDynamicArrayOfRValue {
        public YYObjectBase objectBase;
        public int refCount;
        public int flags;
        public RValue* array;
        public long owner;
        public int visited;
        public int length;
    }

    public unsafe struct CInstance {
        public YYObjectBase objectBase;
        public long createCounter;
        public void* pObject;
        public void* pPhysicsObject;
        public void* pSkeletonAnimation;
        public void* pControllingSeqInst;
        public uint instanceFlags;
        public int id;
        public int objectIndex;
        public int spriteIndex;
        public float sequencePos;
        public float lastSequencePos;
        public float sequenceDir;
        public float imageIndex;
        public float imageSpeed;
        public float imageScaleX;
        public float imageScaleY;
        public float imageAngle;
        public float imageAlpha;
        public uint imageBlend;
        public float x;
        public float y;
        public float xStart;
        public float yStart;
        public float xPrevious;
        public float yPrevious;
        public float direction;
        public float speed;
        public float friction;
        public float gravityDir;
        public float gravity;
        public float hSpeed;
        public float vSpeed;
        public int* bBox; // array, length = 4
        public int* timer; // array, length = 12
        public void* pPathAndTimeline;
        public void* initCode; // CCode
        public void* preCreateCode; // CCode
        public void* pOldObject;
        public int nLayerId;
        public int maskIndex;
        public short nMouseOver;
        public CInstance* pNext;
        public CInstance* pPrev;
        public void** collisionLink; // SLink array, length = 3
        public void** dirtyLink; // SLink array, length = 3
        public void** withLink; // SLink array, length = 3
        public float depth;
        public float currentDepth;
        public float lastImageNumber;
        public uint collisionTestNumber;
    }

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void* mmAlloc(ulong size, sbyte* why, int unk2, bool unk3);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void GmlCall(RValue* result, CInstance* selfInstance, CInstance* otherInstance, int argCount,
        RValue* args);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void Function_Add(sbyte* name, GmlCall function, int argCount, bool unk);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe bool YYError(sbyte* error);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe bool YYGetBool(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe float YYGetFloat(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe int YYGetInt32(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe long YYGetInt64(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void* YYGetPtr(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe double YYGetReal(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe sbyte* YYGetString(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe uint YYGetUint32(RValue* args, int argIndex);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void YYCreateString(RValue* value, sbyte* str);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe RefDynamicArrayOfRValue* ARRAY_RefAlloc();

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void SET_RValue_Array(RValue* arr, RValue* value, YYObjectBase* unk, int index);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe bool GET_RValue(RValue* arr, RValue* value, YYObjectBase* unk, int index, bool unk1,
        bool unk2);

    [UnmanagedCallersOnly]
    // ReSharper disable once UnusedMember.Global
    public static void InitGmlFunctions() {
        // Assume all mods are loaded at the point when we're initializing functions
        foreach(ModData mod in Patcher.mods)
            foreach(MethodInfo method in mod.type.GetMethods()) {
                GmlInteropAttribute? attribute = method.GetCustomAttribute<GmlInteropAttribute>();
                if(attribute is null)
                    continue;

                if(attribute.argumentCount == GmlInteropAttribute.AutoGenerateFunction)
                    InitFunction(attribute.name, method);
                else
                    InitFunction(attribute.name, method.CreateDelegate<GmlCall>(), attribute.argumentCount);
            }
    }

    private static unsafe void InitFunction(string name, MethodInfo method) {
        ParameterInfo[] parameters = method.GetParameters()[2..];
        InitFunction(name, (result, selfInstance, otherInstance, argumentCount, args) => {
            object?[] methodParameters = RValuesToObjectArray(args, argumentCount, parameters);
            methodParameters[0] = *selfInstance;
            methodParameters[1] = *otherInstance;

            object? methodResult = method.Invoke(null, methodParameters);

            // we know they're not null because the method arguments are structs
            *selfInstance = (CInstance)methodParameters[0]!;
            *otherInstance = (CInstance)methodParameters[1]!;

            if(method.ReturnType != typeof(void))
                *result = ObjectToRValue(methodResult);
        }, parameters.Length);
    }

    private static unsafe void InitFunction(string name, GmlCall function, int argumentCount) {
        sbyte* namePtr = (sbyte*)Marshal.StringToHGlobalAnsi(name);
        GCHandle.Alloc(function); // keep the function delegate alive forever
        Function_Add(namePtr, function, argumentCount, false);
    }

    private static unsafe object?[] RValuesToObjectArray(RValue* values, int count,
        IReadOnlyList<ParameterInfo> parameters) {
        if(count != parameters.Count) {
            string error = $"Error! Argument count ({count}) doesn't match with C# method ({parameters})";
            sbyte* errorPtr = (sbyte*)Marshal.StringToHGlobalAnsi(error);
            YYError(errorPtr);
            return Array.Empty<object?>();
        }

        object?[] result = new object?[count + 2];
        for(int i = 0; i < count; i++)
            result[i + 2] = RValueToObject(values, i, parameters[i].ParameterType);

        return result;
    }

    private static unsafe object? RValueToObject(RValue* values, int valueIndex, Type? helpType) {
        RValue value = values[valueIndex];
        switch(value.kind) {
            case RVKind.Bool:
                return YYGetBool(values, valueIndex);
            case RVKind.Real when helpType == typeof(float):
                return YYGetFloat(values, valueIndex);
            case RVKind.Real:
                return YYGetReal(values, valueIndex);
            case RVKind.Int32 when helpType == typeof(uint):
                return YYGetUint32(values, valueIndex);
            case RVKind.Int32:
                return YYGetInt32(values, valueIndex);
            case RVKind.Int64:
                return YYGetInt64(values, valueIndex);
            case RVKind.Ptr when helpType == typeof(IntPtr):
                return (IntPtr)YYGetPtr(values, valueIndex);
            case RVKind.Ptr when helpType is not null:
                return Pointer.Box(YYGetPtr(values, valueIndex), helpType);
            case RVKind.Object:
                return Pointer.Box(value.obj, typeof(YYObjectBase*));
            case RVKind.String:
                return Marshal.PtrToStringAnsi((IntPtr)YYGetString(values, valueIndex));
            case RVKind.Array: {
                RefDynamicArrayOfRValue* array = value.refArray;
                object?[] result = new object?[array->length];
                for(int i = 0; i < result.Length; i++) {
                    RValue arrValue;
                    GET_RValue(array->array, &arrValue, (YYObjectBase*)0, i, false, false);
                    helpType = arrValue.kind switch {
                        // not sure if i wanna use void* or IntPtr here
                        RVKind.Ptr => typeof(void*),
                        RVKind.Object => typeof(YYObjectBase*),
                        _ => null
                    };
                    result[i] = RValueToObject(&arrValue, 0, helpType);
                }
                return result;
            }
        }
        return null;
    }

    // ReSharper disable once CognitiveComplexity
    private static unsafe RValue ObjectToRValue(object? obj) {
        RValue value = new() {
            kind = RVKind.Unset
        };

        if(obj is null)
            return value;

        Type type = obj.GetType();

        if(type == typeof(bool)) {
            value.kind = RVKind.Bool;
            value.valueBool = (bool)obj;
        }
        else if(type == typeof(float)) {
            value.kind = RVKind.Real;
            value.valueFloat = (float)obj;
        }
        else if(type == typeof(double)) {
            value.kind = RVKind.Real;
            value.valueReal = (double)obj;
        }
        else if(type == typeof(int)) {
            value.kind = RVKind.Int32;
            value.valueInt32 = (int)obj;
        }
        else if(type == typeof(uint)) {
            value.kind = RVKind.Int32;
            value.valueUint32 = (uint)obj;
        }
        else if(type == typeof(long)) {
            value.kind = RVKind.Int64;
            value.valueInt64 = (long)obj;
        }
        else if(type == typeof(IntPtr)) {
            value.kind = RVKind.Ptr;
            value.ptr = (void*)(IntPtr)obj;
        }
        else if(type == typeof(YYObjectBase*)) {
            value.kind = RVKind.Object;
            value.obj = (YYObjectBase*)Pointer.Unbox(obj);
        }
        else if(type.IsPointer) {
            value.kind = RVKind.Ptr;
            value.ptr = Pointer.Unbox(obj);
        }
        else if(type == typeof(string))
            YYCreateString(&value, (sbyte*)Marshal.StringToHGlobalAnsi((string)obj));
        else if(type.IsArray) {
            value.kind = RVKind.Array;
            value.refArray = ARRAY_RefAlloc();
            Array array = (Array)obj;
            for(int i = 0; i < array.Length; i++) {
                RValue element = ObjectToRValue(array.GetValue(i));
                SET_RValue_Array(&value, &element, (YYObjectBase*)0, i);
            }
        }
        else
            Console.WriteLine("Warning! Tried to return unsupported type. Returning unset");

        return value;
    }
}
