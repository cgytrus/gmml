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
