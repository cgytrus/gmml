using JetBrains.Annotations;

namespace GmmlInteropGenerator.Types;

[PublicAPI]
public unsafe struct RefThing<T> where T : unmanaged {
    public T* thing;
    public int refCount;
    public int size;
}
