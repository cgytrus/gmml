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
return true", 2);

        data.CreateGlobalScript("scr_test_global", @$"show_debug_message(""hi from test global"")
show_debug_message({new UndertaleString(config.logText)})

show_debug_message(interop_test(""hello from c#!!""))

var arr = array_create(2)
array_set(arr, 0, ""hello from c# 2: electric boogaloo"")
array_set(arr, 1, ""hello from c# 3!!"")
interop_test_1(arr)

var someStruct = json_parse(""{{\""number\"":69,\""text\"":\""some test text\""}}"")
show_debug_message(""before:"")
show_debug_message(someStruct)

show_debug_message(interop_test_2(someStruct))
show_debug_message(someStruct)
", 0, out _);
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

    [GmlInterop("interop_test_2")]
    public static unsafe YYObjectBase* InteropTest2(ref CInstance self, ref CInstance other, YYObjectBase* arg) {
        RValue numberValue;
        arg->GetStructValue("number", &numberValue);

        RValue textValue;
        arg->GetStructValue("text", &textValue);

        GmlGetValue(&numberValue, 0, out int number);
        GmlGetValue(&textValue, 0, out string text);
        Console.WriteLine($"{number}, {text}");

        RValue newNumberValue = new();
        GmlSetValue(&newNumberValue, 420);

        RValue newTextValue = new();
        GmlSetValue(&newTextValue, "trolling");

        RValue anotherNewNumberValue = new();
        GmlSetValue(&anotherNewNumberValue, 69420);

        arg->SetStructValue("number", &newNumberValue);
        arg->SetStructValue("text", &newTextValue);
        arg->SetStructValue("number2", &anotherNewNumberValue);

        Console.WriteLine("after:");
        return arg;
    }
}
