using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlHooker;

// ReSharper disable MemberCanBePrivate.Global MemberCanBeInternal UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global OutParameterValueIsAlwaysDiscarded.Global

public static class HookExtensions {
    private static readonly Dictionary<string, UndertaleCode> originalCodes = new();

    private static UndertaleCode MoveCodeForHook(UndertaleData data, string cloneName, UndertaleCode cloning,
        UndertaleCodeLocals cloningLocals) {
        UndertaleCode codeClone = new() {
            Name = data.Strings.MakeString(cloneName),
            LocalsCount = cloning.LocalsCount,
            ArgumentsCount = cloning.ArgumentsCount,
            WeirdLocalsFlag = cloning.WeirdLocalsFlag,
            WeirdLocalFlag = cloning.WeirdLocalFlag
        };
        codeClone.Replace(cloning.Instructions);
        data.Code.Insert(data.Code.IndexOf(cloning) + 1, codeClone);
        data.Scripts.Add(new UndertaleScript {
            Name = codeClone.Name,
            Code = codeClone
        });

        foreach(UndertaleCode childEntry in cloning.ChildEntries) {
            childEntry.ParentEntry = codeClone;
            codeClone.ChildEntries.Add(childEntry);
        }

        cloning.ChildEntries.Clear();
        cloning.Instructions.Clear();
        cloning.UpdateAddresses();

        UndertaleCodeLocals localsClone = new() {
            Name = codeClone.Name
        };
        foreach(UndertaleCodeLocals.LocalVar localVar in cloningLocals.Locals)
            localsClone.Locals.Add(new UndertaleCodeLocals.LocalVar {
                Name = localVar.Name,
                Index = localVar.Index
            });
        cloningLocals.Locals.Clear();
        data.CodeLocals.Add(localsClone);

        return codeClone;
    }

    public static void HookCode(this UndertaleData data, string code, string hook) =>
        data.Code.ByName(code).Hook(data, data.CodeLocals.ByName(code), hook);

    public static void Hook(this UndertaleCode code, UndertaleData data, UndertaleCodeLocals locals, string hook) {
        string originalName = $"gmml_{code.Name.Content}_orig_{Guid.NewGuid().ToString().Replace('-', '_')}";
        originalCodes.TryAdd(code.Name.Content, MoveCodeForHook(data, originalName, code, locals));
        code.ReplaceGmlSafe(hook.Replace("#orig#", $"{originalName}"), data);
    }

    public static void HookFunction(this UndertaleData data, string function, string hook) {
        string hookedFunctionName = $"gml_Script_{function}";
        UndertaleCode hookedFunctionCode = data.Code.ByName(hookedFunctionName);
        UndertaleCode hookedCode = hookedFunctionCode.ParentEntry;
        UndertaleCodeLocals hookedCodeLocals = data.CodeLocals.ByName(hookedCode.Name.Content);

        string originalName = $"gmml_{hookedFunctionName}_orig_{Guid.NewGuid().ToString().Replace('-', '_')}";

        UndertaleScript originalFunctionScript =
            hookedCode.CreateFunctionDefinition(data, true, originalName, hookedFunctionCode.ArgumentsCount);

        originalFunctionScript.Code.Offset = hookedFunctionCode.Offset;
        hookedFunctionCode.Offset = 0;

        hookedCode.PrependFunctionCode(data, function, hook.Replace("#orig#", $"{originalFunctionScript.Name.Content}"),
            hookedCodeLocals, hookedFunctionName);

        hookedCode.Hook(hookedCodeLocals, (code, locals) => {
            AsmCursor cursor = new(data, code, locals);
            cursor.GotoNext(instruction => instruction.Address == originalFunctionScript.Code.Offset / 4);
            cursor.GotoNext($"push.i {hookedFunctionName}");
            cursor.Replace($"push.i {originalFunctionScript.Name.Content}");
            cursor.index += 7;
            cursor.Replace($"pop.v.v [stacktop]self.{originalName}");
        });
    }

    public delegate void AsmHook(UndertaleCode code, UndertaleCodeLocals locals);

    public static void HookAsm(this UndertaleData data, string name, AsmHook hook) {
        if(!originalCodes.TryGetValue(name, out UndertaleCode? code))
            code = data.Code.ByName(name);
        code.Hook(data.CodeLocals.ByName(code.Name.Content), hook);
    }

    public static void Hook(this UndertaleCode code, UndertaleCodeLocals locals, AsmHook hook) {
        hook(code, locals);
        code.UpdateAddresses();
    }

    public static Dictionary<string, UndertaleVariable> GetLocalVars(this UndertaleCodeLocals locals,
        UndertaleData data) => locals.Locals.ToDictionary(local => local.Name.Content, local =>
        data.Variables.First(variable => variable.VarID == (int)local.Index));
}
