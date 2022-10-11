using JetBrains.Annotations;

namespace GmmlInteropGenerator.Types;

// ReSharper disable once InconsistentNaming
[PublicAPI]
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
