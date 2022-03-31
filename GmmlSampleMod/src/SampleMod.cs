using GmmlHooker;

using GmmlPatcher;

using UndertaleModLib;

namespace GmmlSampleMod;

// ReSharper disable once UnusedType.Global
public class SampleMod : IGameMakerMod {
    public void Load(int audioGroup, UndertaleData data, IReadOnlyList<ModMetadata> availableDependencies,
        IEnumerable<ModMetadata> queuedMods) {
        if(audioGroup != -1) return;
        try {
            // works only in Will You Snail
            Hooker.SimpleHook(data, "gml_Object_obj_epilepsy_warning_Create_0",
                "gml_Object_obj_epilepsy_warning_Create_0_orig()\ntxt_1 = \"eat my nuts\"\ntxt_2=\"making sure it actually works\"");
        }
        // UndertaleModLib is trying to write profile cache but fails, we don't care
        catch(Exception) { /* ignored */ }
    }
}
