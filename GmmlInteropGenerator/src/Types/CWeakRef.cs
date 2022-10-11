using JetBrains.Annotations;

namespace GmmlInteropGenerator.Types;

[PublicAPI]
public unsafe struct CWeakRef {
    public YYObjectBase objectBase;
    public YYObjectBase* weakRef;
}
