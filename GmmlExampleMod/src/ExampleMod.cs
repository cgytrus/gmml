using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using GmmlHooker;

using GmmlPatcher;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlExampleMod;

// see https://github.com/cgytrus/WysApi/WysExampleMod for more examples
// ReSharper disable once UnusedType.Global
public class ExampleMod : IGameMakerMod {
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    private class Config {
        public string logText { get; private set; } = "";
    }

    public void Load(int audioGroup, UndertaleData data, ModData currentMod) {
        string soundPath = Path.Combine(currentMod.path, "sou_example_sound.wav");
        if(File.Exists(soundPath))
            data.AddSound(audioGroup, 1, soundPath);

        if(audioGroup != 0) return;
        Config config = GmmlConfig.Config.LoadPatcherConfig<Config>(audioGroup, "gmmlExampleMod.json");

        data.CreateFunction("scr_test_func", @$"show_debug_message(""hi from test func"")
if argument1 == false {{
    return false
}}
show_debug_message(argument0)
show_debug_message({new UndertaleString(config.logText)})
return true", 2);
    }
}
