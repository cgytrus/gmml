using JetBrains.Annotations;

namespace GmmlInteropGenerator;

[AttributeUsage(AttributeTargets.Method, Inherited = false), PublicAPI]
public class AdvancedGmlInteropAttribute : Attribute {
    public string name { get; }
    public int argumentCount { get; }

    public AdvancedGmlInteropAttribute(string name, int argumentCount) {
        this.name = name;
        this.argumentCount = argumentCount;
    }
}
