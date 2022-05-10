using System.Collections.ObjectModel;

using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace GmmlHooker;

// rider, this is a GOD DAMN PUBLIC API
// CAN YOU SHUT UP ALREADY PLEASE thanks
// ReSharper disable MemberCanBePrivate.Global MemberCanBeInternal UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global OutParameterValueIsAlwaysDiscarded.Global

public static class CreateExtensions {
    public static UndertaleCode CreateCode(this UndertaleData data, UndertaleString name, out UndertaleCodeLocals locals) {
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

    public static UndertaleScript CreateLegacyScript(this UndertaleData data, string name, string code, ushort argCount) {
        UndertaleString mainName = data.Strings.MakeString(name, out int nameIndex);
        UndertaleCode mainCode = CreateCode(data, mainName, out _);
        mainCode.ArgumentsCount = argCount;

        mainCode.ReplaceGmlSafe(code, data);

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

    public static UndertaleScript CreateFunction(this UndertaleData data, string name, string code, ushort argCount) {
        UndertaleScript globalScript = CreateGlobalScript(data, name, "", 0, out UndertaleCodeLocals locals);
        globalScript.Code.CreateInlineFunction(data, true, locals, name, code, argCount);
        return globalScript;
    }

    public static UndertaleScript CreateGlobalScript(this UndertaleData data, string name, string code, ushort argCount,
        out UndertaleCodeLocals locals) {
        UndertaleString scriptName = data.Strings.MakeString(name);
        UndertaleString codeName = data.Strings.MakeString($"gml_GlobalScript_{name}");

        UndertaleCode globalCode = CreateCode(data, codeName, out locals);
        globalCode.ArgumentsCount = argCount;

        globalCode.ReplaceGmlSafe(code, data);

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

    public static UndertaleScript CreateInlineFunction(this UndertaleGlobalInit parent, UndertaleData data, string name,
        string code, ushort argCount) => parent.Code.CreateInlineFunction(data, true,
            data.CodeLocals.ByName(parent.Code.Name.Content), name, code, argCount);

    public static UndertaleScript CreateInlineFunction(this UndertaleGlobalInit parent, UndertaleData data,
        UndertaleCodeLocals locals, string name, string code, ushort argCount) =>
            parent.Code.CreateInlineFunction(data, true, locals, name, code, argCount);

    public static UndertaleScript CreateInlineFunction(this UndertaleData data, string parent, string name,
        string code, ushort argCount) => data.Code.ByName(parent).CreateInlineFunction(data,
            data.CodeLocals.ByName(parent), name, code, argCount);

    public static UndertaleScript CreateInlineFunction( this UndertaleCode parent, UndertaleData data,
        UndertaleCodeLocals parentLocals, string name, string code, ushort argCount) =>
            parent.CreateInlineFunction(data, false, parentLocals, name, code, argCount);

    private static UndertaleScript CreateInlineFunction(this UndertaleCode parent, UndertaleData data, bool global,
        UndertaleCodeLocals parentLocals, string name, string code, ushort argCount) {
        UndertaleScript functionScript = parent.CreateFunctionDefinition(data, global, name, argCount);
        parent.PrependFunctionCode(data, name, code, parentLocals, functionScript.Name.Content);
        return functionScript;
    }

    // TODO: make these two public and put massive warnings in the docs
    internal static UndertaleScript CreateFunctionDefinition(this UndertaleCode parent, UndertaleData data, bool global,
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

    internal static void PrependFunctionCode(this UndertaleCode code, UndertaleData data, string name, string gmlCode,
        UndertaleCodeLocals locals, string functionName) {
        UndertaleInstruction?[] childStarts = new UndertaleInstruction?[code.ChildEntries.Count];
        for(int i = 0; i < code.ChildEntries.Count; i++) {
            UndertaleCode child = code.ChildEntries[i];
            childStarts[i] = child.Offset == 0 ? null : code.GetInstructionFromAddress(child.Offset / 4);
        }

        List<UndertaleInstruction> oldCode = new(code.Instructions);
        ObservableCollection<UndertaleCodeLocals.LocalVar> oldLocals = new(locals.Locals);
        code.ReplaceGmlSafe(gmlCode, data);
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
        foreach(UndertaleCodeLocals.LocalVar oldLocal in oldLocals) {
            if(locals.Locals.Any(local => local.Index == oldLocal.Index))
                continue;
            locals.Locals.Add(oldLocal);
        }

        for(int i = 0; i < code.ChildEntries.Count; i++) {
            UndertaleInstruction? childStart = childStarts[i];
            if(childStart is null) continue;
            code.ChildEntries[i].Offset = childStart.Address * 4;
        }
    }

    private static readonly UndertaleSound blankSound = new();
    public static UndertaleSound AddSound(this UndertaleData data, int currentAudioGroup, int audioGroup, string file,
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
