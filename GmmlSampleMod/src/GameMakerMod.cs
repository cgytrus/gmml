using GmmlPatcher;

using UndertaleModLib;

namespace GmmlSampleMod;

// ReSharper disable once UnusedType.Global
public static class GameMakerMod {
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedParameter.Global
    public static void Load(UndertaleData data, IEnumerable<ModMetadata> queuedMods) {
        try {
            // works only in Will You Snail
            data.Code.First(code => code.Name.Content == "gml_Object_obj_epilepsy_warning_Create_0")
                .AppendGML("txt_1 = \"eat my nuts\"", data);
        }
        // UndertaleModLib is trying to write profile cache but fails, we don't care
        catch(Exception) { /* ignored */ }
    }
}
