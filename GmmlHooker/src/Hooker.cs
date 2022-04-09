using GmmlPatcher;

using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

// rider, this is a GOD DAMN PUBLIC API
// CAN YOU SHUT UP ALREADY PLEASE thanks
// ReSharper disable MemberCanBePrivate.Global MemberCanBeInternal UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global OutParameterValueIsAlwaysDiscarded.Global

namespace GmmlHooker;

// ReSharper disable once ClassNeverInstantiated.Global
public class Hooker : IGameMakerMod {
    private static readonly Dictionary<string, UndertaleCode> originalCodes = new();

    public void Load(int audioGroup, ModData currentMod) {
        if(audioGroup != 0) return;
        // TODO: define hooks in JSON? maybe?

        CreateLegacyScript("gmml_read_all_text", @"
var file_buffer = buffer_load(argument0);
var text = buffer_read(file_buffer, buffer_string);
buffer_delete(file_buffer);
return text
", 1);
    }

    public static void ReplaceGmlSafe(UndertaleCode code, string gmlCode) =>
        ReplaceGmlSafe(code, gmlCode, Patcher.data);

    public static void ReplaceGmlSafe(UndertaleCode code, string gmlCode, UndertaleData data) {
        try { code.ReplaceGML(gmlCode, data); }
        // UndertaleModLib is trying to write profile cache but fails, we don't care
        catch(Exception ex) {
            if(ex.Message.StartsWith("Error during writing of GML code to profile", StringComparison.InvariantCulture))
                return;
            throw;
        }
    }

    public static void AppendGmlSafe(UndertaleCode code, string gmlCode) =>
        AppendGmlSafe(code, gmlCode, Patcher.data);

    public static void AppendGmlSafe(UndertaleCode code, string gmlCode, UndertaleData data) {
        try { code.AppendGML(gmlCode, data); }
        // UndertaleModLib is trying to write profile cache but fails, we don't care
        catch(Exception ex) {
            if(ex.Message.StartsWith("Error during writing of GML code to profile", StringComparison.InvariantCulture))
                return;
            throw;
        }
    }

    public static void CreateCode(UndertaleString name, out UndertaleCodeLocals locals) =>
        CreateCode(Patcher.data, name, out locals);

    public static UndertaleCode CreateCode(UndertaleData data, UndertaleString name, out UndertaleCodeLocals locals) {
        locals = new UndertaleCodeLocals {
            Name = name
        };
        locals.Locals.Add(new UndertaleCodeLocals.LocalVar {
            Name = data.Strings.MakeString("arguments"),
            Index = 2
        });
        data.CodeLocals.Add(locals);

        UndertaleCode mainCode = new() {
            Name = name,
            LocalsCount = 1,
            ArgumentsCount = 0
        };
        data.Code.Add(mainCode);

        return mainCode;
    }

    public static UndertaleScript CreateLegacyScript(string name, string code, ushort argCount) =>
        CreateLegacyScript(Patcher.data, name, code, argCount);

    public static UndertaleScript CreateLegacyScript(UndertaleData data, string name, string code, ushort argCount) {
        UndertaleString mainName = data.Strings.MakeString(name, out int nameIndex);
        UndertaleCode mainCode = CreateCode(data, mainName, out _);
        mainCode.ArgumentsCount = argCount;

        ReplaceGmlSafe(mainCode, code, data);

        UndertaleScript script = new() {
            Name = mainName,
            Code = mainCode
        };
        data.Scripts.Add(script);

        UndertaleFunction function = new() {
            Name = mainName,
            NameStringID = nameIndex
        };
        data.Functions.Add(function);

        return script;
    }

    public static UndertaleScript CreateFunction(string name, string code, ushort argCount) =>
        CreateFunction(Patcher.data, name, code, argCount);
    public static UndertaleScript CreateFunction(UndertaleData data, string name, string code, ushort argCount) {
        UndertaleScript globalScript = CreateGlobalScript(data, name, "", 0, out UndertaleCodeLocals locals);
        CreateInlineFunction(data, true, globalScript.Code, locals, name, code, argCount);
        return globalScript;
    }

    public static UndertaleScript CreateGlobalScript(string name, string code, ushort argCount,
        out UndertaleCodeLocals locals) =>
        CreateGlobalScript(Patcher.data, name, code, argCount, out locals);
    public static UndertaleScript CreateGlobalScript(UndertaleData data, string name, string code, ushort argCount,
        out UndertaleCodeLocals locals) {
        UndertaleString scriptName = data.Strings.MakeString(name);
        UndertaleString codeName = data.Strings.MakeString($"gml_GlobalScript_{name}");

        UndertaleCode globalCode = CreateCode(data, codeName, out locals);
        globalCode.ArgumentsCount = argCount;

        ReplaceGmlSafe(globalCode, code, data);

        UndertaleScript script = new() {
            Name = scriptName,
            Code = globalCode
        };
        data.Scripts.Add(script);

        data.GlobalInitScripts.Add(new UndertaleGlobalInit {
            Code = globalCode
        });

        return script;
    }

    public static UndertaleScript CreateInlineFunction(UndertaleGlobalInit parent, string name,
        string code, ushort argCount) => CreateInlineFunction(Patcher.data, parent, name, code, argCount);
    public static UndertaleScript CreateInlineFunction(UndertaleData data, UndertaleGlobalInit parent, string name,
        string code, ushort argCount) => CreateInlineFunction(data, true, parent.Code,
        data.CodeLocals.ByName(parent.Code.Name.Content), name, code, argCount);

    public static UndertaleScript CreateInlineFunction(UndertaleGlobalInit parent, UndertaleCodeLocals locals,
        string name, string code, ushort argCount) =>
            CreateInlineFunction(Patcher.data, parent, locals, name, code, argCount);
    public static UndertaleScript CreateInlineFunction(UndertaleData data,
        UndertaleGlobalInit parent, UndertaleCodeLocals locals, string name, string code, ushort argCount) =>
            CreateInlineFunction(data, true, parent.Code, locals, name, code, argCount);

    public static UndertaleScript CreateInlineFunction(string parent, string name,
        string code, ushort argCount) => CreateInlineFunction(Patcher.data, parent, name, code, argCount);
    public static UndertaleScript CreateInlineFunction(UndertaleData data, string parent, string name,
        string code, ushort argCount) => CreateInlineFunction(data, data.Code.ByName(parent),
        data.CodeLocals.ByName(parent), name, code, argCount);

    public static UndertaleScript CreateInlineFunction(UndertaleCode parent, UndertaleCodeLocals parentLocals,
        string name, string code, ushort argCount) =>
            CreateInlineFunction(Patcher.data, parent, parentLocals, name, code, argCount);
    public static UndertaleScript CreateInlineFunction(UndertaleData data,
        UndertaleCode parent, UndertaleCodeLocals parentLocals,
        string name, string code, ushort argCount) =>
            CreateInlineFunction(data, false, parent, parentLocals, name, code, argCount);

    private static UndertaleScript CreateInlineFunction(UndertaleData data, bool global,
        UndertaleCode parent, UndertaleCodeLocals parentLocals, string name, string code, ushort argCount) {
        UndertaleScript functionScript = CreateFunctionDefinition(data, global, parent, name, argCount);
        PrependFunctionCode(data, name, code, parent, parentLocals, functionScript.Name.Content);
        return functionScript;
    }

    private static UndertaleScript CreateFunctionDefinition(UndertaleData data, bool global, UndertaleCode parent,
        string name, ushort argCount) {
        UndertaleString scriptName = data.Strings.MakeString(
            global ? $"gml_Script_{name}" : $"gml_Script_{name}_{parent.Name.Content}", out int scriptNameIndex);

        data.Variables.EnsureDefined(name, UndertaleInstruction.InstanceType.Self, false, data.Strings, data);

        UndertaleFunction scriptFunction = new() {
            Name = scriptName,
            NameStringID = scriptNameIndex,
            Autogenerated = true
        };
        data.Functions.Add(scriptFunction);

        UndertaleCode scriptCode = new() {
            Name = scriptName,
            LocalsCount = 0,
            ArgumentsCount = argCount,
            ParentEntry = parent
        };
        parent.ChildEntries.Add(scriptCode);
        data.Code.Insert(data.Code.IndexOf(parent) + 1, scriptCode);

        UndertaleScript functionScript = new() {
            Name = scriptName,
            Code = scriptCode
        };
        data.Scripts.Add(functionScript);

        return functionScript;
    }

    public static void PrependFunctionCode(string name, string gmlCode, UndertaleCode code,
        UndertaleCodeLocals locals, string intermediaryName) =>
        PrependFunctionCode(Patcher.data, name, gmlCode, code, locals, intermediaryName);

    private static void PrependFunctionCode(UndertaleData data, string name, string gmlCode,
        UndertaleCode code, UndertaleCodeLocals locals, string functionName) {
        UndertaleInstruction?[] childStarts = new UndertaleInstruction?[code.ChildEntries.Count];
        for(int i = 0; i < code.ChildEntries.Count; i++) {
            UndertaleCode child = code.ChildEntries[i];
            childStarts[i] = child.Offset == 0 ? null : code.GetInstructionFromAddress(child.Offset / 4);
        }

        List<UndertaleInstruction> oldCode = new(code.Instructions);
        ReplaceGmlSafe(code, gmlCode, data);
        code.Replace(Assembler.Assemble(@$"
b [func_def]

{code.Disassemble(data.Variables, locals).Replace("\n:[end]", "")}

exit.i

:[func_def]
push.i {functionName}
conv.i.v
pushi.e -1
conv.i.v
call.i method(argc=2)
dup.v 0
pushi.e -1
pop.v.v [stacktop]self.{name}
popz.v

:[end]", data));
        code.Append(oldCode);

        for(int i = 0; i < code.ChildEntries.Count; i++) {
            UndertaleInstruction? childStart = childStarts[i];
            if(childStart is null) continue;
            code.ChildEntries[i].Offset = childStart.Address * 4;
        }
    }

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

    public static void HookCode(string code, string hook) => HookCode(Patcher.data, code, hook);
    public static void HookCode(UndertaleData data, string code, string hook) =>
        HookCode(data, data.Code.ByName(code), data.CodeLocals.ByName(code), hook);

    public static void HookCode(UndertaleCode code, UndertaleCodeLocals locals, string hook) =>
        HookCode(Patcher.data, code, locals, hook);

    public static void HookCode(UndertaleData data, UndertaleCode code, UndertaleCodeLocals locals, string hook) {
        string originalName = $"gmml_{code.Name.Content}_orig_{Guid.NewGuid().ToString().Replace('-', '_')}";
        originalCodes.TryAdd(code.Name.Content, MoveCodeForHook(data, originalName, code, locals));
        ReplaceGmlSafe(code, hook.Replace("#orig#", $"{originalName}"), data);
    }

    public static void HookFunction(string function, string hook) => HookFunction(Patcher.data, function, hook);
    public static void HookFunction(UndertaleData data, string function, string hook) {
        string hookedFunctionName = $"gml_Script_{function}";
        UndertaleCode hookedFunctionCode = data.Code.ByName(hookedFunctionName);
        UndertaleCode hookedCode = hookedFunctionCode.ParentEntry;
        UndertaleCodeLocals hookedCodeLocals = data.CodeLocals.ByName(hookedCode.Name.Content);

        string originalName = $"gmml_{hookedFunctionName}_orig_{Guid.NewGuid().ToString().Replace('-', '_')}";

        UndertaleScript originalFunctionScript =
            CreateFunctionDefinition(data, true, hookedCode, originalName, hookedFunctionCode.ArgumentsCount);

        originalFunctionScript.Code.Offset = hookedFunctionCode.Offset;
        hookedFunctionCode.Offset = 0;

        PrependFunctionCode(data, function, hook.Replace("#orig#", $"{originalFunctionScript.Name.Content}"),
            hookedCode, hookedCodeLocals, hookedFunctionName);

        HookAsm(hookedCode, hookedCodeLocals, (code, locals) => {
            AsmCursor cursor = new(data, code, locals);
            cursor.GotoNext(instruction => instruction.Address == originalFunctionScript.Code.Offset / 4);
            cursor.GotoNext($"push.i {hookedFunctionName}");
            cursor.Replace($"push.i {originalFunctionScript.Name.Content}");
            cursor.index += 6;
            cursor.Replace($"pop.v.v [stacktop]self.{originalName}");
        });
    }

    public delegate void AsmHook(UndertaleCode code, UndertaleCodeLocals locals);

    public static void HookAsm(string name, AsmHook hook) => HookAsm(Patcher.data, name, hook);
    public static void HookAsm(UndertaleData data, string name, AsmHook hook) {
        if(originalCodes.TryGetValue(name, out UndertaleCode? code))
            HookAsm(code, data.CodeLocals.ByName(code.Name.Content), hook);
        else
            HookAsm(data.Code.ByName(name), data.CodeLocals.ByName(name), hook);
    }

    public static void HookAsm(UndertaleCode code, UndertaleCodeLocals locals, AsmHook hook) {
        hook(code, locals);
        code.UpdateAddresses();
    }

    public static Dictionary<string, UndertaleVariable> GetLocalVars(UndertaleCodeLocals locals) =>
        GetLocalVars(Patcher.data, locals);
    public static Dictionary<string, UndertaleVariable> GetLocalVars(UndertaleData data, UndertaleCodeLocals locals) =>
        locals.Locals.ToDictionary(local => local.Name.Content, local => data.Variables[(int)local.Index]);

    private static readonly UndertaleSound blankSound = new();
    public static UndertaleSound AddSound(int currentAudioGroup, int audioGroup, string file, bool embed = true,
        bool decodeOnLoad = true) => AddSound(Patcher.data, currentAudioGroup, audioGroup, file, embed, decodeOnLoad);
    public static UndertaleSound AddSound(UndertaleData data, int currentAudioGroup, int audioGroup, string file,
        bool embed = true, bool decodeOnLoad = true) {
        if(currentAudioGroup == audioGroup)
            data.EmbeddedAudio.Add(new UndertaleEmbeddedAudio {
                Data = File.ReadAllBytes(file)
            });

        if(currentAudioGroup != 0)
            return blankSound; // we don't care what data is in there, it's not gonna be used anyway

        if(!TryGetAudioEntryFlags(file, embed, decodeOnLoad, out UndertaleSound.AudioEntryFlags flags))
            Console.WriteLine($"Warning! Unsupported audio format ({file})");

        return AddSound(data, audioGroup, Path.GetFileNameWithoutExtension(file), Path.GetFileName(file), flags);
    }

    private static bool TryGetAudioEntryFlags(string file, bool embed, bool decodeOnLoad,
        out UndertaleSound.AudioEntryFlags flags) {
        flags = UndertaleSound.AudioEntryFlags.Regular;
        string extension = Path.GetExtension(file);
        switch(extension) {
            case ".ogg": {
                if(embed) {
                    flags |= UndertaleSound.AudioEntryFlags.IsCompressed;
                    if(decodeOnLoad)
                        flags |= UndertaleSound.AudioEntryFlags.IsEmbedded;
                }
                break;
            }
            case ".wav":
                flags |= UndertaleSound.AudioEntryFlags.IsEmbedded;
                break;
            default:
                return false;
        }

        return true;
    }

    private static UndertaleSound AddSound(UndertaleData data, int audioGroup, string name, string file,
        UndertaleSound.AudioEntryFlags flags) {
        // if the only flag is Regular, that means it's an external sound which doesn't have an ID
        int audioId = flags == UndertaleSound.AudioEntryFlags.Regular ? -1 : GetAudioId(data, audioGroup);
        UndertaleSound sound = new() {
            Name = data.Strings.MakeString(name),
            Flags = flags,
            File = data.Strings.MakeString(file),
            AudioID = audioId,
            AudioGroup = data.AudioGroups[audioGroup],
            GroupID = audioGroup
        };
        data.Sounds.Add(sound);
        return sound;
    }

    private static int GetAudioId(UndertaleData data, int audioGroup) {
        int audioId = -1;
        foreach(UndertaleSound sound in data.Sounds)
            if(sound.GroupID == audioGroup && sound.AudioID > audioId)
                audioId = sound.AudioID;

        return audioId + 1;
    }
}
