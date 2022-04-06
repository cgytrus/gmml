using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using GmmlHooker;

using GmmlPatcher;

using UndertaleModLib.Models;

namespace GmmlExampleMod;

// see https://github.com/cgytrus/WysApi/WysExampleMod for more examples
// ReSharper disable once UnusedType.Global
public class ExampleMod : IGameMakerMod {
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    private class Config {
        public string logText { get; private set; } = "";
    }

    public void Load(int audioGroup, ModMetadata currentMod, IEnumerable<ModMetadata> dependencies) {
        if(audioGroup != 0) return;
        Config config = GmmlConfig.Config.LoadPatcherConfig<Config>("gmmlExampleMod.json");

        Hooker.CreateScript("scr_test_script", @$"show_debug_message(""hi from test script"")
if argument1 == false {{
    return false
}}
show_debug_message(argument0)
show_debug_message({new UndertaleString(config.logText)})
return true", 2);
    }
}
