﻿using System.Text.Json;

using GmmlHooker;

using GmmlPatcher;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlConfig;

// ReSharper disable once ClassNeverInstantiated.Global
public class Config : IGameMakerMod {
    public void Load(int audioGroup, UndertaleData data, ModMetadata currentMod,
        IReadOnlyList<ModMetadata> availableDependencies, IEnumerable<ModMetadata> queuedMods) {
        if(audioGroup != -1) return;
        UndertaleString path = new(Patcher.configPath.Replace('\\', '/'));

        Hooker.CreateSimpleScript(data, "gmml_config_get_path", @$"
var directory = {path} + ""/""
if !directory_exists(directory) {{
    directory_create(directory);
}}
return directory
", 0);

        Hooker.CreateSimpleScript(data, "gmml_config_open_write", @"
return file_text_open_write(gmml_config_get_path() + argument0)
", 1);

        Hooker.CreateSimpleScript(data, "gmml_config_open_read", @"
var path = gmml_config_get_path() + argument0
if !file_exists(path) {{
    gmml_config_save(argument0, argument1)
}}
return file_text_open_read(path)
", 2);

        Hooker.CreateSimpleScript(data, "gmml_config_save", @"
var config_file = gmml_config_open_write(argument0)
file_text_write_string(config_file, json_stringify(argument1))
file_text_close(config_file)
", 2);

        Hooker.CreateSimpleScript(data, "gmml_config_load", @"
var config_file = gmml_config_open_read(argument0, argument1)
var config = json_parse(gmml_read_all_text(config_file))
file_text_close(config_file)
return config
", 2);
    }

    public static string GetPatcherConfigPath(string name) {
        Directory.CreateDirectory(Patcher.configPath);
        string path = Path.Combine(Patcher.configPath, Path.GetFileName(name));
        return path;
    }

    public static T LoadPatcherConfig<T>(string name) where T : new() {
        string path = GetPatcherConfigPath(name);
        T config = new();
        if(File.Exists(path)) config = JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? config;
        else File.WriteAllText(path, JsonSerializer.Serialize(config));
        return config;
    }
}