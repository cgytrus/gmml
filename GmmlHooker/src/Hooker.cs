using GmmlPatcher;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlHooker;

// ReSharper disable once ClassNeverInstantiated.Global
public class Hooker : IGameMakerMod {
    public void Load(int audioGroup, UndertaleData data, IReadOnlyList<ModMetadata> availableDependencies,
        IEnumerable<ModMetadata> queuedMods) { /* TODO: define hooks in JSON? maybe? */ }

    public static void SimpleHook(UndertaleData data, string code, string hook) {
        UndertaleCode hookedCode = data.Code.ByName(code);

        UndertaleCode originalCodeCopy = new() {
            Name = data.Strings.MakeString($"{hookedCode.Name.Content}_orig"),
            LocalsCount = hookedCode.LocalsCount,
            ArgumentsCount = hookedCode.ArgumentsCount,
            WeirdLocalsFlag = hookedCode.WeirdLocalsFlag,
            WeirdLocalFlag = hookedCode.WeirdLocalFlag
        };
        originalCodeCopy.Replace(hookedCode.Instructions);
        data.Code.Add(originalCodeCopy);
        data.Scripts.Add(new UndertaleScript {
            Name = originalCodeCopy.Name,
            Code = originalCodeCopy
        });

        hookedCode.ReplaceGML(hook, data);
    }
}
