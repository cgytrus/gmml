using System.Reflection;
using System.Runtime.InteropServices;
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
        public List<(string, ClassDeclarationSyntax)> types { get; } = new();
        public List<(string, string, ClassDeclarationSyntax, MethodDeclarationSyntax, string)> methods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if(syntaxNode is not ClassDeclarationSyntax { Parent: BaseNamespaceDeclarationSyntax typeNamespace } type ||
                !ContainsAttribute(type.AttributeLists, nameof(EnableSimpleGmlInteropAttribute), out _))
                return;

            types.Add((typeNamespace.Name.ToString(), type));

            StringBuilder usingsBuilder = new();
            foreach(SyntaxNode child in typeNamespace.Parent?.ChildNodes() ?? Array.Empty<SyntaxNode>()) {
                if(child is not UsingDirectiveSyntax usingDirective)
                    continue;
                usingsBuilder.AppendLine(usingDirective.ToString());
            }
            string fileUsings = usingsBuilder.ToString();

            foreach(SyntaxNode child in type.ChildNodes()) {
                if(child is not MethodDeclarationSyntax method ||
                    !ContainsAttribute(method.AttributeLists, nameof(GmlInteropAttribute),
                        out AttributeSyntax? attribute))
                    continue;

                methods.Add((fileUsings, typeNamespace.Name.ToString(), type, method,
                    attribute?.ArgumentList?.Arguments.ToString() ?? ""));
            }
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

    public unsafe void Execute(GeneratorExecutionContext context) {
        GmlInteropMethodsSyntaxReceiver? syntaxReceiver = (GmlInteropMethodsSyntaxReceiver?)context.SyntaxReceiver;
        MethodInfo? gmlCallInfo = typeof(GmlCall).GetMethod(nameof(GmlCall.Invoke));
        if(syntaxReceiver is null || gmlCallInfo is null)
            return;

        List<(string, ClassDeclarationSyntax)> interopTypes = syntaxReceiver.types;
        List<(string, string, ClassDeclarationSyntax, MethodDeclarationSyntax, string)> interopMethods =
            syntaxReceiver.methods;

        const string dllImport = "[DllImport(\"version.dll\", CallingConvention = CallingConvention.Cdecl)]";

        foreach((string typeNamespace, ClassDeclarationSyntax type) in interopTypes)
            context.AddSource($"{type.Identifier.ValueText}.g.cs", $@"// Auto-generated code
using System;
using System.Runtime.InteropServices;
using {nameof(GmmlInteropGenerator)}.{nameof(GmmlInteropGenerator.Types)};
namespace {typeNamespace};

public partial class {type.Identifier.ValueText} {{
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
    private static extern unsafe void* YYGetPtr({nameof(RValue)}* args, int argIndex);

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
}}
");

        StringBuilder builder = new();

        string gmlCallParams = string.Join(", ",
            gmlCallInfo.GetParameters().Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}"));

        foreach((string usings, string typeNamespace, ClassDeclarationSyntax? type, MethodDeclarationSyntax? method,
                string attributeArgs) in interopMethods) {
            SeparatedSyntaxList<ParameterSyntax> allMethodParams = method.ParameterList.Parameters;
            List<ParameterSyntax> methodParams = new(allMethodParams.Count - 2);
            for(int i = 2; i < allMethodParams.Count; i++)
                methodParams.Add(allMethodParams[i]);

            builder.Clear();
            builder.Append($@"// Auto-generated code
using System.Runtime.InteropServices;
{usings}
namespace {typeNamespace};

public partial class {type.Identifier.ValueText} {{
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

            builder.Append("    }\n}\n");

            context.AddSource($"{type.Identifier.ValueText}.{method.Identifier.ValueText}.g.cs", builder.ToString());
        }
    }

    private static string GetManagedArgName(string name) => $"{name}Managed";

    // ReSharper disable once CognitiveComplexity
    private static unsafe void GenerateGmlToManaged(StringBuilder builder, string typeStr, string name,
        string argArray, string index, string indent = Indent) {
        builder.AppendLine($"{indent}// {argArray}[{index}]: {typeStr} {name}");
        string managedArgName = GetManagedArgName(name);
        string assign = $"{indent}{typeStr} {managedArgName} = ";
        Type? type = GetTypeFromString(typeStr);

        string argName = $"{name}Arg";
        builder.AppendLine($"{indent}{nameof(RValue)} {argName} = {argArray}[{index}];");
        if(type == typeof(bool))
            builder.AppendLine($"{assign}YYGetBool({argArray}, {index});");
        else if(type == typeof(float))
            builder.AppendLine($"{assign}YYGetFloat({argArray}, {index});");
        else if(type == typeof(double))
            builder.AppendLine($"{assign}YYGetReal({argArray}, {index});");
        else if(type == typeof(int))
            builder.AppendLine($"{assign}YYGetInt32({argArray}, {index});");
        else if(type == typeof(uint))
            builder.AppendLine($"{assign}YYGetUint32({argArray}, {index});");
        else if(type == typeof(long))
            builder.AppendLine($"{assign}YYGetInt64({argArray}, {index});");
        else if(type == typeof(YYObjectBase*))
            builder.AppendLine($"{assign}{argName}.{nameof(RValue.obj)};");
        else if(type == typeof(IntPtr))
            builder.AppendLine($"{assign}({nameof(IntPtr)})YYGetPtr({argArray}, {index});");
        else if(type == typeof(sbyte*))
            builder.AppendLine($"{assign}YYGetString({argArray}, {index});");
        else if(type?.IsPointer ?? false)
            builder.AppendLine($"{assign}({typeStr})YYGetPtr({argArray}, {index});");
        else if(type == typeof(string)) {
            const string ptrToStringAnsi = $"{nameof(Marshal)}.{nameof(Marshal.PtrToStringAnsi)}";
            builder.AppendLine($"{assign}{ptrToStringAnsi}(({nameof(IntPtr)})YYGetString({argArray}, {index}));");
        }
        else if(type?.IsArray ?? false) {
            string arrayPtrName = $"{managedArgName}Ptr";
            string indexName = $"{managedArgName}Index";
            string elementName = $"{managedArgName}Element";
            string arrayTypeStr = type.GetElementType()?.FullName ?? "";

            builder.AppendLine($@"{indent}{nameof(RefDynamicArrayOfRValue)}* {arrayPtrName} = {argName}.refArray;
{assign}new {arrayTypeStr}[{arrayPtrName}->length];
{indent}for(int {indexName} = 0; {indexName} < {managedArgName}.Length; {indexName}++) {{");

            GenerateGmlToManaged(builder, arrayTypeStr, elementName, $"{arrayPtrName}->array", indexName,
                $"{indent}{IndentLevel}");

            builder.AppendLine($"{indent}{IndentLevel}{managedArgName}[{indexName}] = {GetManagedArgName(elementName)};");
            builder.AppendLine($"{indent}}}");
        }
        else {
            builder.AppendLine($"{indent}// Conversion failed");
            builder.AppendLine($"{assign}default;");
        }
    }

    // ReSharper disable once CognitiveComplexity
    private static unsafe void GenerateManagedToGml(StringBuilder builder, string typeStr, string resultName,
        string valueName, string indent = Indent) {
        Type? type = GetTypeFromString(typeStr);

        string assignKind = $"{indent}{resultName}->{nameof(RValue.kind)} = {nameof(RVKind)}.";

        if(type == typeof(bool)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Bool)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.valueBool)} = {valueName};");
        }
        else if(type == typeof(float)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Real)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.valueFloat)} = {valueName};");
        }
        else if(type == typeof(double)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Real)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.valueReal)} = {valueName};");
        }
        else if(type == typeof(int)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Int32)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.valueInt32)} = {valueName};");
        }
        else if(type == typeof(uint)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Int32)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.valueUint32)} = {valueName};");
        }
        else if(type == typeof(long)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Int64)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.valueInt64)} = {valueName};");
        }
        else if(type == typeof(IntPtr)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Ptr)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.ptr)} = (void*){valueName};");
        }
        else if(type == typeof(sbyte*))
            builder.AppendLine($"{indent}YYCreateString({resultName}, {valueName});");
        else if(type == typeof(YYObjectBase*)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Object)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.obj)} = {valueName};");
        }
        else if(type == typeof(YYObjectBase)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Object)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.obj)} = &{valueName};");
        }
        // not sure if this will work
        else if(type == typeof(CInstance*)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Object)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.obj)} = ({nameof(YYObjectBase)}*){valueName};");
        }
        else if(type == typeof(CInstance)) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Object)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.obj)} = ({nameof(YYObjectBase)}*)&{valueName};");
        }
        else if(type?.IsPointer ?? false) {
            builder.AppendLine($"{assignKind}{nameof(RVKind.Ptr)};");
            builder.AppendLine($"{indent}{resultName}->{nameof(RValue.ptr)} = (void*)&{valueName};");
        }
        else if(type == typeof(string)) {
            const string stringToHGlobalAnsi = $"{nameof(Marshal)}.{nameof(Marshal.StringToHGlobalAnsi)}";
            builder.AppendLine($"{indent}YYCreateString({resultName}, (sbyte*){stringToHGlobalAnsi}({valueName}));");
        }
        else if(type?.IsArray ?? false) {
            string indexName = $"{valueName}Index";
            string elementName = $"{resultName}Element";
            string elementPtrName = $"{elementName}Ptr";
            string arrayTypeStr = type.GetElementType()?.FullName ?? "";

            builder.AppendLine($@"{assignKind}{nameof(RVKind.Array)};
{indent}{resultName}->{nameof(RValue.refArray)} = ARRAY_RefAlloc();
{indent}for(int {indexName} = 0; {indexName} < {valueName}.Length; {indexName}++) {{
{indent}{IndentLevel}{nameof(RValue)} {elementName} = new();
{indent}{IndentLevel}{nameof(RValue)}* {elementPtrName} = &{elementName}");

            GenerateManagedToGml(builder, arrayTypeStr, elementPtrName, $"{valueName}[{indexName}]",
                $"{indent}{IndentLevel}");

            builder.AppendLine($"{indent}{IndentLevel}SET_RValue_Array({resultName}, {elementPtrName}, ({nameof(YYObjectBase)}*)0), {indexName}");
            builder.AppendLine($"{indent}}}");
        }
        else
            builder.AppendLine($@"{indent}// Conversion failed
{indent}*{resultName} = new {nameof(RValue)}() {{
{indent}{IndentLevel}{nameof(RValue.kind)} = {nameof(RVKind)}.{nameof(RVKind.Unset)}
{indent}}};
");
    }

    private static Type? GetTypeFromString(string typeStr) => Type.GetType(typeStr) ?? typeStr switch {
        "bool" => typeof(bool),
        "float" => typeof(float),
        "double" => typeof(double),
        "int" => typeof(int),
        "uint" => typeof(uint),
        "long" => typeof(long),
        $"{nameof(YYObjectBase)}*" => typeof(YYObjectBase*),
        nameof(IntPtr) => typeof(IntPtr),
        "sbyte*" => typeof(sbyte*),
        _ when typeStr.EndsWith("*", StringComparison.Ordinal) => typeof(void*),
        "string" => typeof(string),
        _ when typeStr.EndsWith("[]", StringComparison.Ordinal) => GetTypeFromString(
            typeStr.Substring(0, typeStr.Length - "[]".Length))?.MakeArrayType(),
        _ => null
    };
}
