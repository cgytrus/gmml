namespace GmmlInteropGenerator;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GmlInteropAttribute : Attribute {
    public string name { get; }
    public GmlInteropAttribute(string name) => this.name = name;
}
