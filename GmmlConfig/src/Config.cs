using System.Text.Json;

using GmmlHooker;

using GmmlPatcher;

using JetBrains.Annotations;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlConfig;

[PublicAPI]
public class Config : IGameMakerMod {
    private static readonly string configPath = Path.Combine("gmml", "config");

    public void Load(int audioGroup, UndertaleData data, ModData currentMod) {
        if(audioGroup != 0) return;
        UndertaleString configPathString = new(configPath.Replace('\\', '/'));

        data.CreateLegacyScript("gmml_config_get_path", @$"
var directory = {configPathString} + ""/""
if !directory_exists(directory) {{
    directory_create(directory);
}}
return directory
", 0);

        data.CreateLegacyScript("gmml_config_open_write", @"
return file_text_open_write(gmml_config_get_path() + argument0)
", 1);

        data.CreateLegacyScript("gmml_config_open_read", @"
var path = gmml_config_get_path() + argument0
if !file_exists(path) {{
    gmml_config_save(argument0, argument1)
}}
return file_text_open_read(path)
", 2);

        data.CreateLegacyScript("gmml_config_save", @"
var config_file = gmml_config_open_write(argument0)
file_text_write_string(config_file, json_stringify(argument1))
file_text_close(config_file)
", 2);

        data.CreateLegacyScript("gmml_config_load", @"
var config_file = gmml_config_open_read(argument0, argument1)
var config = json_parse(file_text_read_string(config_file))
file_text_close(config_file)
return config
", 2);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static string GetPatcherConfigPath(string name) {
        Directory.CreateDirectory(configPath);
        string path = Path.Combine(configPath, Path.GetFileName(name));
        return path;
    }

    public static T LoadPatcherConfig<T>(int audioGroup, string name) where T : new() {
        string path = GetPatcherConfigPath(name);
        T config = new();
        if(File.Exists(path)) config = JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? config;
        else File.WriteAllText(path, JsonSerializer.Serialize(config));
        Patcher.AddFileToCache(audioGroup, path);
        return config;
    }
}
