using System.Diagnostics.CodeAnalysis;

using GmmlHooker;

using GmmlInteropGenerator;
using GmmlInteropGenerator.Types;

using GmmlPatcher;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlExampleMod;

// see https://github.com/cgytrus/WysApi/WysExampleMod for more examples
// ReSharper disable once UnusedType.Global
[EnableSimpleGmlInterop]
public partial class ExampleMod : IGameMakerMod {
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

show_debug_message(interop_test(""hello from c#!!""))

var arr = array_create(2)
array_set(arr, 0, ""hello from c# 2: electric boogaloo"")
array_set(arr, 1, ""hello from c# 3!!"")
interop_test_1(arr)
return true", 2);
    }

    [GmlInterop("interop_test")]
    public static string InteropTest(ref CInstance self, ref CInstance other, string text) {
        Console.WriteLine(text);
        return "hello from gml!!";
    }

    [GmlInterop("interop_test_1")]
    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    public static void InteropTest1(ref CInstance self, ref CInstance other, string[] texts) {
        foreach(string text in texts)
            Console.WriteLine(text);
    }
}
