using JetBrains.Annotations;

namespace GmmlInteropGenerator.Types;

[PublicAPI]
public unsafe struct CInstanceBase {
    public void* vtable;
    public RValue* yyVars;
}
