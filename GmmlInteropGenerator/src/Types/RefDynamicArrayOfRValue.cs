namespace GmmlInteropGenerator.Types;

public unsafe struct RefDynamicArrayOfRValue {
    public YYObjectBase objectBase;
    public int refCount;
    public int flags;
    public RValue* array;
    public long owner;
    public int visited;
    public int length;
}
