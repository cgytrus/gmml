using GmmlHooker;

using GmmlPatcher;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlSampleMod;

// ReSharper disable once UnusedType.Global
public class SampleMod : IGameMakerMod {
    public void Load(int audioGroup, UndertaleData data, ModMetadata currentMod,
        IReadOnlyList<ModMetadata> availableDependencies, IEnumerable<ModMetadata> queuedMods) {
        if(audioGroup != -1) return;
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
txt_2 = ""making sure it actually works""");

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
            cursor.Replace("pushi.e 10");
            cursor.Finish();
        });

        Hooker.HookScript(data, "scr_move_like_a_snail",
            @"#orig#(argument0, argument1, argument2, argument3)
show_debug_message(""move like a snail"")
");
    }
}
