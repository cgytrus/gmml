namespace GmmlPatcher;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GmlInteropAttribute : Attribute {
    public const int AutoGenerateFunction = -2;
    public const int VariableArgCount = -1;

    public string name { get; set; }
    public int argumentCount { get; set; } = AutoGenerateFunction;

    public GmlInteropAttribute(string name) => this.name = name;
}
