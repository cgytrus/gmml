using System.Reflection;
using System.Text;

using GmmlInteropGenerator.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GmmlInteropGenerator;

[Generator]
public class InteropSourceGenerator : ISourceGenerator {
    public static void Workaround() {
        Console.WriteLine(@"
This message is a workaround to the CLR not wanting to load my dependencies properly...
   I have this urge
   I have this urge to kill
   I have this urge to kill and show that this is dumb
   I'm getting sick from .NET CLR
   From projects with dependencies
   That they don't want to load, what the fuck
");
    }

    private class GmlInteropMethodsSyntaxReceiver : ISyntaxReceiver {
        public List<(string, string, ClassDeclarationSyntax, List<(MethodDeclarationSyntax, string)>)> types { get; } =
            new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if(syntaxNode is not ClassDeclarationSyntax { Parent: BaseNamespaceDeclarationSyntax typeNamespace } type)
                return;

            List<(MethodDeclarationSyntax, string)> methods = new();
            foreach(SyntaxNode child in type.ChildNodes()) {
                if(child is not MethodDeclarationSyntax method ||
                    !ContainsAttribute(method.AttributeLists, nameof(GmlInteropAttribute),
                        out AttributeSyntax? attribute))
                    continue;

                methods.Add((method, attribute?.ArgumentList?.Arguments.ToString() ?? ""));
            }

            if(methods.Count <= 0)
                return;

            StringBuilder usingsBuilder = new();
            foreach(SyntaxNode child in typeNamespace.Parent?.ChildNodes() ?? Array.Empty<SyntaxNode>()) {
                if(child is not UsingDirectiveSyntax usingDirective)
                    continue;
                usingsBuilder.AppendLine(usingDirective.ToString());
            }
            string fileUsings = usingsBuilder.ToString();
            types.Add((fileUsings, typeNamespace.Name.ToString(), type, methods));
        }

        private static bool ContainsAttribute(IEnumerable<AttributeListSyntax> attributes, string name,
            out AttributeSyntax? attribute) {
            AttributeSyntax? attributeSyntax = null;
            bool hasAttribute = attributes.Any(attributesSyntax => {
                attributeSyntax = attributesSyntax.Attributes.FirstOrDefault(syntax => {
                    string usedName = syntax.Name.ToString();
                    string shortName = name.Substring(0, name.Length - "Attribute".Length);
                    return usedName == name || usedName == shortName;
                });
                return attributeSyntax is not null;
            });
            attribute = attributeSyntax;
            return hasAttribute;
        }
    }

    private const string IndentLevel = "    ";
    private const string Indent = $"{IndentLevel}{IndentLevel}";

    public void Initialize(GeneratorInitializationContext context) {
        context.RegisterForSyntaxNotifications(() => new GmlInteropMethodsSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context) {
        GmlInteropMethodsSyntaxReceiver? syntaxReceiver = (GmlInteropMethodsSyntaxReceiver?)context.SyntaxReceiver;
        MethodInfo? gmlCallInfo = typeof(GmlCall).GetMethod(nameof(GmlCall.Invoke));
        if(syntaxReceiver is null || gmlCallInfo is null)
            return;

        List<(string, string, ClassDeclarationSyntax, List<(MethodDeclarationSyntax, string)>)> interopTypes =
            syntaxReceiver.types;

        StringBuilder builder = new();

        const string dllImport = "[DllImport(\"version.dll\", CallingConvention = CallingConvention.Cdecl)]";
        const string inline = "[MethodImpl(MethodImplOptions.AggressiveInlining)]";
        const string assignKind = $"gmlValue->{nameof(RValue.kind)} = {nameof(RVKind)}.";
        string gmlCallParams = string.Join(", ",
            gmlCallInfo.GetParameters().Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}"));

        foreach((string usings, string typeNamespace, ClassDeclarationSyntax type,
                List<(MethodDeclarationSyntax method, string attributeArgs)> methods) in interopTypes) {
            #region Class definition and a lot of pre-generated methods
            builder.AppendLine($@"// Auto-generated code
// ignore because the using might've already been included
// but i'm too lazy to make it actually check if the using has already been there or not
#pragma warning disable CS0105 // The using directive appeared previously in this namespace
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
{usings}
#pragma warning restore CS0105 // The using directive appeared previously in this namespace

namespace {typeNamespace};

public partial class {type.Identifier.ValueText} {{
    #region DLL imports

    {dllImport}
    private static extern unsafe void* mmAlloc(ulong size, sbyte* why, int unk2, bool unk3);

    {dllImport}
    private static extern unsafe bool YYError(sbyte* error);

    {dllImport}
    private static extern unsafe bool YYGetBool({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe float YYGetFloat({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe int YYGetInt32({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe long YYGetInt64({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe IntPtr YYGetPtrOrInt({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe double YYGetReal({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe sbyte* YYGetString({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe uint YYGetUint32({nameof(RValue)}* args, int argIndex);

    {dllImport}
    private static extern unsafe void YYCreateString({nameof(RValue)}* value, sbyte* str);

    {dllImport}
    private static extern unsafe {nameof(RefDynamicArrayOfRValue)}* ARRAY_RefAlloc();

    {dllImport}
    private static extern unsafe void SET_RValue_Array({nameof(RValue)}* arr, {nameof(RValue)}* value,
        {nameof(YYObjectBase)}* unk, int index);

    {dllImport}
    private static extern unsafe bool GET_RValue({nameof(RValue)}* arr, {nameof(RValue)}* value,
        {nameof(YYObjectBase)}* unk, int index, bool unk1, bool unk2);

    #endregion

    #region Argument parsing methods

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, bool value) {{
        {assignKind}{nameof(RVKind.Bool)};
        gmlValue->{nameof(RValue.valueBool)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, float value) {{
        {assignKind}{nameof(RVKind.Real)};
        gmlValue->{nameof(RValue.valueFloat)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, double value) {{
        {assignKind}{nameof(RVKind.Real)};
        gmlValue->{nameof(RValue.valueReal)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, int value) {{
        {assignKind}{nameof(RVKind.Int32)};
        gmlValue->{nameof(RValue.valueInt32)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, uint value) {{
        {assignKind}{nameof(RVKind.Int32)};
        gmlValue->{nameof(RValue.valueUint32)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, long value) {{
        {assignKind}{nameof(RVKind.Int64)};
        gmlValue->{nameof(RValue.valueInt64)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, IntPtr value) {{
        {assignKind}{nameof(RVKind.Ptr)};
        gmlValue->{nameof(RValue.ptr)} = (void*)value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, UIntPtr value) {{
        {assignKind}{nameof(RVKind.Ptr)};
        gmlValue->{nameof(RValue.ptr)} = (void*)value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, sbyte* value) => YYCreateString(gmlValue, value);

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, byte* value) =>
        YYCreateString(gmlValue, (sbyte*)value);

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, string value) =>
        GmlSetValue(gmlValue, (sbyte*)Marshal.StringToHGlobalAnsi(value));

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, {nameof(YYObjectBase)}* value) {{
        {assignKind}{nameof(RVKind.Object)};
        gmlValue->{nameof(RValue.obj)} = value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, {nameof(YYObjectBase)} value) =>
        GmlSetValue(gmlValue, &value);

    // not sure if this will work
    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, {nameof(CInstance)}* value) {{
        {assignKind}{nameof(RVKind.Object)};
        gmlValue->{nameof(RValue.obj)} = ({nameof(YYObjectBase)}*)value;
    }}

    {inline}
    private static unsafe void GmlSetValue({nameof(RValue)}* gmlValue, {nameof(CInstance)} value) =>
        GmlSetValue(gmlValue, &value);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out bool value) =>
        value = YYGetBool(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out float value) =>
        value = YYGetFloat(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out double value) =>
        value = YYGetReal(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out int value) =>
        value = YYGetInt32(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out uint value) =>
        value = YYGetUint32(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out long value) =>
        value = YYGetInt64(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out IntPtr value) =>
        value = YYGetPtrOrInt(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out UIntPtr value) =>
        value = (UIntPtr)(void*)YYGetPtrOrInt(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out sbyte* value) =>
        value = YYGetString(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out byte* value) =>
        value = (byte*)YYGetString(args, argIndex);

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out string value) =>
        value = Marshal.PtrToStringAnsi(({nameof(IntPtr)})YYGetString(args, argIndex));

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out {nameof(YYObjectBase)}* value) =>
        value = args[argIndex].{nameof(RValue.obj)};

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out {nameof(YYObjectBase)} value) =>
        value = *args[argIndex].{nameof(RValue.obj)};

    // not sure if this will work
    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out {nameof(CInstance)}* value) =>
        value = ({nameof(CInstance)}*)args[argIndex].{nameof(RValue.obj)};

    {inline}
    private static unsafe void GmlGetValue({nameof(RValue)}* args, int argIndex, out {nameof(CInstance)} value) =>
        value = *({nameof(CInstance)}*)args[argIndex].{nameof(RValue.obj)};

    #endregion");
            #endregion

            foreach((MethodDeclarationSyntax? method, string? attributeArgs) in methods)
                GenerateMethod(builder, gmlCallParams, method, attributeArgs);

            builder.AppendLine("}");

            context.AddSource($"{type.Identifier.ValueText}.g.cs", builder.ToString());
        }
    }

    private static void GenerateMethod(StringBuilder builder, string gmlCallParams,
        MethodDeclarationSyntax method, string attributeArgs) {
        SeparatedSyntaxList<ParameterSyntax> allMethodParams = method.ParameterList.Parameters;
        List<ParameterSyntax> methodParams = new(allMethodParams.Count - 2);
        for(int i = 2; i < allMethodParams.Count; i++)
            methodParams.Add(allMethodParams[i]);

        builder.Append($@"
    [{nameof(AdvancedGmlInteropAttribute)}({attributeArgs}, {methodParams.Count.ToString()})]
    public static unsafe void {method.Identifier.ValueText}_Advanced({gmlCallParams}) {{
");

        for(int i = 0; i < methodParams.Count; i++) {
            ParameterSyntax parameter = methodParams[i];
            GenerateGmlToManaged(builder, parameter.Type?.ToString() ?? "", parameter.Identifier.ValueText,
                "args", i.ToString());
            builder.AppendLine();
        }

        string methodArgs = string.Join(", ",
            methodParams.Select(parameter => GetManagedArgName(parameter.Identifier.ValueText)));

        string returnType = method.ReturnType.ToString();
        bool hasReturn = returnType != "void";
        string returnStatement = hasReturn ? $"{returnType} methodResult = " : "";
        string firstArgs = methodParams.Count > 0 ?
            "ref *selfInstance, ref *otherInstance, " :
            "ref *selfInstance, ref *otherInstance";
        builder.AppendLine($"{Indent}{returnStatement}{method.Identifier.ValueText}({firstArgs}{methodArgs});");
        if(hasReturn)
            GenerateManagedToGml(builder, returnType, "result", "methodResult");

        builder.AppendLine("    }");
    }

    private static string GetManagedArgName(string name) => $"{name}Managed";

    // ReSharper disable once CognitiveComplexity
    private static void GenerateGmlToManaged(StringBuilder builder, string typeStr, string name,
        string argArray, string index, string indent = Indent) {
        builder.AppendLine($"{indent}// {argArray}[{index}]: {typeStr} {name}");
        string managedArgName = GetManagedArgName(name);
        string assign = $"{indent}{typeStr} {managedArgName} = ";

        if(IsArray(typeStr)) {
            string arrayPtrName = $"{managedArgName}Ptr";
            string indexName = $"{managedArgName}Index";
            string elementName = $"{managedArgName}Element";
            string arrayTypeStr = GetArrayType(typeStr);

            builder.AppendLine($@"{indent}{nameof(RefDynamicArrayOfRValue)}* {arrayPtrName} = {argArray}[{index}].refArray;
{assign}new {arrayTypeStr}[{arrayPtrName}->length];
{indent}for(int {indexName} = 0; {indexName} < {managedArgName}.Length; {indexName}++) {{");

            GenerateGmlToManaged(builder, arrayTypeStr, elementName, $"{arrayPtrName}->array", indexName,
                $"{indent}{IndentLevel}");

            builder.AppendLine($"{indent}{IndentLevel}{managedArgName}[{indexName}] = {GetManagedArgName(elementName)};");
            builder.AppendLine($"{indent}}}");
        }
        else if(IsUnknownPointer(typeStr)) {
            builder.AppendLine($"{indent}GmlGetValue({argArray}, {index}, out void* {managedArgName}Intermediary);");
            builder.AppendLine($"{indent}{typeStr} {managedArgName} = {typeStr}{managedArgName}Intermediary;");
        }
        else
            builder.AppendLine($"{indent}GmlGetValue({argArray}, {index}, out {typeStr} {managedArgName});");
    }

    // ReSharper disable once CognitiveComplexity
    private static void GenerateManagedToGml(StringBuilder builder, string typeStr, string resultName,
        string valueName, string indent = Indent) {

        if(IsArray(typeStr)) {
            string indexName = $"{valueName}Index";
            string elementName = $"{resultName}Element";
            string elementPtrName = $"{elementName}Ptr";
            string arrayTypeStr = GetArrayType(typeStr);

            builder.AppendLine($@"{indent}{resultName}->{nameof(RValue.kind)} = {nameof(RVKind)}.{nameof(RVKind.Array)};
{indent}{resultName}->{nameof(RValue.refArray)} = ARRAY_RefAlloc();
{indent}for(int {indexName} = 0; {indexName} < {valueName}.Length; {indexName}++) {{
{indent}{IndentLevel}{nameof(RValue)} {elementName} = new();
{indent}{IndentLevel}{nameof(RValue)}* {elementPtrName} = &{elementName}");

            GenerateManagedToGml(builder, arrayTypeStr, elementPtrName, $"{valueName}[{indexName}]",
                $"{indent}{IndentLevel}");

            builder.AppendLine($"{indent}{IndentLevel}SET_RValue_Array({resultName}, {elementPtrName}, ({nameof(YYObjectBase)}*)0, {indexName});");
            builder.AppendLine($"{indent}}}");
        }
        else if(IsUnknownPointer(typeStr))
            builder.AppendLine($"{indent}GmlSetValue({resultName}, (void*){valueName});");
        else
            builder.AppendLine($"{indent}GmlSetValue({resultName}, {valueName});");
    }

    private static bool IsUnknownPointer(string typeStr) =>
        typeStr != $"{nameof(YYObjectBase)}*" &&
        typeStr != $"{nameof(CInstance)}*" &&
        typeStr != "sbyte*" &&
        typeStr.EndsWith("*", StringComparison.Ordinal);

    private static bool IsArray(string typeStr) => typeStr.EndsWith("[]", StringComparison.Ordinal);

    private static string GetArrayType(string typeStr) => typeStr.Substring(0, typeStr.Length - "[]".Length);
}
