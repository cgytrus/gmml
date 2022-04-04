using System.Text.Json.Serialization;

using GmmlHooker;

using GmmlPatcher;

using UndertaleModLib;

namespace GmmlSampleMod;

// ReSharper disable once ClassNeverInstantiated.Global
public class SampleMod : IGameMakerMod {
    private struct Config {
        public int epilepsyWait { get; } = 10;

        [JsonConstructor]
        public Config(int epilepsyWait) => this.epilepsyWait = epilepsyWait;
    }

    public void Load(int audioGroup, UndertaleData data, ModMetadata currentMod,
        IReadOnlyList<ModMetadata> availableDependencies, IEnumerable<ModMetadata> queuedMods) {
        if(audioGroup != -1) return;
        Config config = GmmlConfig.Config.LoadPatcherConfig<Config>("sampleMod.json");

        Hooker.CreateScript(data, "scr_test_script", @"show_debug_message(""hi from test script"")
if argument1 == false {
    return false
}
show_debug_message(argument0)
return true", 2);

        // works only in Will You Snail
        Hooker.HookCode(data, "gml_Object_obj_epilepsy_warning_Create_0",
            @"#orig#()
txt_1 = ""eat my nuts""");

        Hooker.HookCode(data, "gml_Object_obj_epilepsy_warning_Create_0",
            @"#orig#()
var default_config = json_parse(""{ \""text\"": \""test\"" }"")
var config = gmml_config_load(""sample_mod_epilepsy_text.json"", default_config)
txt_2 = config.text");

        Hooker.HookCode(data, "gml_Object_obj_player_Step_0",
            @"#orig#()
if keyboard_check_pressed(vk_f2)
{
    room_restart()
    show_debug_message(""press"")
}
");

        // poor epilepsy warning, i'm hooking it for the third time already
        Hooker.HookAsm(data, "gml_Object_obj_epilepsy_warning_Create_0", (code, locals) => {
            AsmCursor cursor = new(data, code, locals);
            cursor.GotoNext("pushi.e 180");
            cursor.Replace($"pushi.e {config.epilepsyWait}");
        });

        Hooker.HookScript(data, "scr_move_like_a_snail",
            @"#orig#(argument0, argument1, argument2, argument3)
show_debug_message(""move like a snail"")
");
    }
}
