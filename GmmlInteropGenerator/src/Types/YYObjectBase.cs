using System.Runtime.InteropServices;

namespace GmmlInteropGenerator.Types;

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
    public CHashMap<int, RValue>* yyVarsMap;
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

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Code_Variable_Find_Slot_From_Name(YYObjectBase* obj, sbyte* name);

    public RValue* GetStructValue(string name) {
        fixed(YYObjectBase* thisPtr = &this) {
            sbyte* namePtr = (sbyte*)Marshal.StringToHGlobalAnsi(name);
            int slot = Code_Variable_Find_Slot_From_Name(thisPtr, namePtr);
            uint hash = CHashMap<int, IntPtr>.CalculateHash(slot);
            if(!yyVarsMap->FindElement(hash, out RValue* value))
                return (RValue*)0;
            return value;
        }
    }
}
