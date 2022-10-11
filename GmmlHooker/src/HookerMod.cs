using GmmlPatcher;

using JetBrains.Annotations;

using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace GmmlHooker;

[PublicAPI]
public class HookerMod : IGameMakerMod {
    public void Load(int audioGroup, UndertaleData data, ModData currentMod) {
        if(audioGroup != 0) return;
        data.CreateLegacyScript("gmml_read_all_text", @"
var file_buffer = buffer_load(argument0);
var text = buffer_read(file_buffer, buffer_string);
buffer_delete(file_buffer);
return text
", 1);
    }
}
