using System.Text.Json.Serialization;

using GmmlHooker;

using GmmlPatcher;

namespace GmmlExampleMod;

// see https://github.com/cgytrus/WysApi/WysExampleMod for more examples
// ReSharper disable once ClassNeverInstantiated.Global
public class ExampleMod : IGameMakerMod {
    private struct Config {
        public string logText { get; }

        [JsonConstructor]
        public Config(string logText) => this.logText = logText;
    }

    public void Load(int audioGroup, ModMetadata currentMod, IEnumerable<ModMetadata> dependencies) {
        if(audioGroup != -1) return;
        Config config = GmmlConfig.Config.LoadPatcherConfig<Config>("gmmlExampleMod.json");

        Hooker.CreateScript("scr_test_script", @$"show_debug_message(""hi from test script"")
if argument1 == false {{
    return false
}}
show_debug_message(argument0)
show_debug_message({config.logText})
return true", 2);
    }
}
