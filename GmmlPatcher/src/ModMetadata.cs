﻿using System.Text.Json.Serialization;

using Semver;

namespace GmmlPatcher;

public struct ModMetadata {
    public struct ModDependency {
        internal string id { get; }
        internal string version { get; }

        [JsonConstructor]
        public ModDependency(string id, string version) {
            this.id = id;
            this.version = version;
        }
    }

    public string id { get; }
    public string name { get; }
    public string version { get; }
    public string[] authors { get; }
    public string description { get; }
    public ModDependency[] dependencies { get; }
    public string? mainAssembly { get; internal set; }

    [JsonConstructor]
    public ModMetadata(string id, string name, string version, string[] authors, string? description = null,
        ModDependency[]? dependencies = null, string? mainAssembly = null) {
        this.id = id;
        this.name = name;
        this.version = version;
        this.authors = authors;
        this.description = description ?? "";
        this.dependencies = dependencies ?? Array.Empty<ModDependency>();
        this.mainAssembly = mainAssembly;
    }
}
