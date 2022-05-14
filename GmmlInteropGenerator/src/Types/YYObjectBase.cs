using System.Runtime.InteropServices;

namespace GmmlInteropGenerator.Types;

// ReSharper disable once InconsistentNaming
public unsafe struct YYObjectBase {
    public CInstanceBase instanceBase;
    public YYObjectBase* pNextObject;
    public YYObjectBase* pPrevObject;
    public YYObjectBase* prototype;
    private sbyte* _classStr; // DO NOT USE: unreliable
    private void* _getOwnProperty; // DO NOT USE: unreliable
    private void* _deleteProperty; // DO NOT USE: unreliable
    private void* _defineOwnProperty; // DO NOT USE: unreliable
    private void* _yyVarsMap; // CHashMap<int, RValue*> // DO NOT USE: unreliable
    private CWeakRef** _weakRefs; // DO NOT USE: unreliable
    private uint _numWeakRefs; // DO NOT USE: unreliable
    private uint _nVars; // DO NOT USE: unreliable
    private uint _flags; // DO NOT USE: unreliable
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

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool Variable_GetValue_Direct(YYObjectBase* obj, int slot, int alwaysMinInt32, RValue* value,
        bool unk1, bool unk2);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Code_Variable_FindAlloc_Slot_From_Name(YYObjectBase* obj, sbyte* name);

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool Variable_SetValue_Direct(YYObjectBase* obj, int slot, int alwaysMinInt32, RValue* value);

    public bool GetStructValue(string name, RValue* value) {
        fixed(YYObjectBase* thisPtr = &this) {
            sbyte* namePtr = (sbyte*)Marshal.StringToHGlobalAnsi(name);
            int slot = Code_Variable_Find_Slot_From_Name(thisPtr, namePtr);
            return Variable_GetValue_Direct(thisPtr, slot, int.MinValue, value, false, false);
        }
    }

    public void SetStructValue(string name, RValue* value) {
        fixed(YYObjectBase* thisPtr = &this) {
            sbyte* namePtr = (sbyte*)Marshal.StringToHGlobalAnsi(name);
            int slot = Code_Variable_FindAlloc_Slot_From_Name(thisPtr, namePtr);
            Variable_SetValue_Direct(thisPtr, slot, int.MinValue, value);
        }
    }
}
