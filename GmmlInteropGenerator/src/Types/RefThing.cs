namespace GmmlInteropGenerator.Types;

public unsafe struct RefThing<T> where T : unmanaged {
    public T* thing;
    public int refCount;
    public int size;
}
