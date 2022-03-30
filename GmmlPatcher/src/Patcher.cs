using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

using Semver;

using UndertaleModLib;

namespace GmmlPatcher;

// ReSharper disable once UnusedType.Global
public static unsafe class Patcher {
    private const string ModsPath = "mods";

    private static List<(ModMetadata metadata, Assembly assembly, IReadOnlyList<ModMetadata> availableDependencies)>?
        _queuedMods;

    // ReSharper disable once UnusedMember.Global
    [UnmanagedCallersOnly]
    public static byte* ModifyData(int audioGroup, byte* original, int* size) {
        Console.WriteLine(audioGroup < 0 ? "Modifying game data" : $"Modifying audio group {audioGroup}");

        Console.WriteLine("Deserializing data");

        using UnmanagedMemoryStream originalStream = new(original, *size);
        UndertaleData data = UndertaleIO.Read(originalStream);

        Console.WriteLine("Loading mods");

        _queuedMods ??= QueueMods();

        AppDomain.CurrentDomain.AssemblyResolve += TempResolveModAssemblies;

        IEnumerable<ModMetadata> exposedQueuedMods = _queuedMods.Select(mod => mod.metadata);
        foreach((ModMetadata metadata, Assembly? assembly, IReadOnlyList<ModMetadata> availableDependencies) in
            _queuedMods) {
            if(!TryLoadMod(metadata, assembly, audioGroup, data, availableDependencies, exposedQueuedMods))
                continue;
            Console.WriteLine($"Loaded mod {metadata.id}");
        }

        AppDomain.CurrentDomain.AssemblyResolve -= TempResolveModAssemblies;

        Console.WriteLine("Serializing data");
        return UndertaleDataToBytes(data, size);
    }

    private static List<(ModMetadata metadata, Assembly assembly, IReadOnlyList<ModMetadata> availableDependencies)>
        QueueMods() {
        string whitelistPath = Path.Combine(ModsPath, "whitelist.txt");
        string blacklistPath = Path.Combine(ModsPath, "blacklist.txt");

        ImmutableHashSet<string> whitelist = File.Exists(whitelistPath) ?
            File.ReadAllLines(whitelistPath).ToImmutableHashSet() : ImmutableHashSet<string>.Empty;
        ImmutableHashSet<string> blacklist = File.Exists(blacklistPath) ?
            File.ReadAllLines(blacklistPath).ToImmutableHashSet() : ImmutableHashSet<string>.Empty;

        List<(string, ModMetadata)> availableMods = new();

        void SearchForMods(string directory) {
            foreach(string path in Directory.EnumerateDirectories(directory)) {
                if(TryGetModMetadata(path, blacklist, whitelist, out ModMetadata metadata))
                    availableMods.Add((path, metadata));
                SearchForMods(path);
            }
        }

        SearchForMods(ModsPath);

        List<(ModMetadata metadata, Assembly assembly, IReadOnlyList<ModMetadata> availableDependencies)> queuedMods =
            new();
        foreach((string path, ModMetadata metadata) in availableMods) {
            if(!TryQueueMod(path, metadata, availableMods, out Assembly? assembly,
                out IReadOnlyList<ModMetadata> availableDependencies))
                continue;
            queuedMods.Add((metadata, assembly, availableDependencies));
        }
        return queuedMods;
    }

    private static bool TryGetModMetadata(string path, ImmutableHashSet<string> blacklist,
        ImmutableHashSet<string> whitelist, out ModMetadata metadata) {
        string metadataPath = Path.Combine(path, "metadata.json");
        if(!File.Exists(metadataPath)) {
            metadata = default(ModMetadata);
            return false;
        }

        string name = Path.GetFileName(path);

        try { metadata = GetModMetadata(metadataPath, name); }
        catch(Exception ex) {
            LogModLoadError(name, ex);
            metadata = default(ModMetadata);
            return false;
        }

        if(!VerifyMetadata(metadata.id, metadata.version, "metadata"))
            return false;

        if(metadata.dependencies.Any(dependency => !VerifyMetadata(dependency.id, dependency.version, "dependency")))
            return false;

        string relativePath = $"{Path.PathSeparator}{Path.GetRelativePath(ModsPath, path)}";
        bool blacklisted = blacklist.Contains(metadata.id) || blacklist.Contains(relativePath);
        bool whitelisted = whitelist.IsEmpty || whitelist.Contains(metadata.id) || whitelist.Contains(relativePath);
        if(blacklisted || !whitelisted) {
            Console.WriteLine($"Ignoring mod {metadata.id} ({(whitelisted ? "blacklisted" : "not whitelisted")})");
            return false;
        }

        // ReSharper disable once InvertIf
        if(metadata.mainAssembly is null) {
            LogModLoadError(metadata.id, "wtf????");
            return false;
        }

        return true;
    }

    private static ModMetadata GetModMetadata(string path, string internalName) {
        string jsonMetadata = File.ReadAllText(path);
        ModMetadata metadata = JsonSerializer.Deserialize<ModMetadata>(jsonMetadata);
        metadata.mainAssembly ??= $"{internalName}.dll";

        return metadata;
    }

    private static bool VerifyMetadata(string id, string version, string metadataText) {
        if(!VerifyId(id)) {
            const string packageFormatLink = "https://docs.oracle.com/javase/tutorial/java/package/namingpkgs.html";
            LogModLoadError(id, $"invalid {metadataText} ID, expected java-like package format: {packageFormatLink}");
            return false;
        }

        // ReSharper disable once InvertIf
        if(!VerifyVersion(version)) {
            LogModLoadError(id, $"invalid {metadataText} version, expected SemVer 2.0.0: https://semver.org");
            return false;
        }

        return true;
    }

    private static bool VerifyId(string id) {
        if(string.IsNullOrWhiteSpace(id)) return false;
        string[] parts = id.Split('.');
        return parts.Length > 1 && parts.All(part => !string.IsNullOrWhiteSpace(part)) && id.All(character =>
            char.IsLetter(character) && char.IsLower(character) || char.IsDigit(character) || character is '.' or '_');
    }

    private static bool VerifyVersion(string version) => SemVersion.TryParse(version, SemVersionStyles.Strict, out _);

    // ReSharper disable once CognitiveComplexity
    private static bool TryQueueMod(string path, ModMetadata metadata, IEnumerable<(string, ModMetadata)> availableMods,
        [NotNullWhen(true)] out Assembly? assembly, out IReadOnlyList<ModMetadata> availableDependencies) {
        assembly = null;

        availableDependencies = GetAvailableDependencies(availableMods, metadata.dependencies, out bool allAvailable);
        if(allAvailable)
            return TryLoadModAssembly(path, metadata, out assembly);

        LogModLoadError(metadata.id, "missing dependencies");
        return false;
    }

    private static IReadOnlyList<ModMetadata> GetAvailableDependencies(
        IEnumerable<(string, ModMetadata metadata)> availableMods, IEnumerable<ModMetadata.ModDependency> dependencies,
        out bool allAvailable) {
        List<ModMetadata> availableDependencies = new();
        allAvailable = true;
        foreach(ModMetadata.ModDependency dependency in dependencies) {
            bool currentAvailable = false;
            foreach((string _, ModMetadata metadata) in availableMods) {
                if(metadata.id != dependency.id || !VersionsCompatible(
                    SemVersion.Parse(metadata.version, SemVersionStyles.Strict),
                    SemVersion.Parse(dependency.version, SemVersionStyles.Strict)))
                    continue;
                availableDependencies.Add(metadata);
                currentAvailable = true;
                break;
            }
            if(!dependency.optional) allAvailable &= currentAvailable;
        }
        return availableDependencies;
    }

    private static bool VersionsCompatible(SemVersion left, SemVersion right) =>
        left == right || left.Major != 0 && left.Major == right.Major;

    private static bool TryLoadModAssembly(string path, ModMetadata metadata,
        [NotNullWhen(true)] out Assembly? assembly) {
        assembly = null;

        if(metadata.mainAssembly is null)
            return false;

        try { assembly = Assembly.LoadFrom(Path.Combine(path, metadata.mainAssembly)); }
        catch(Exception ex) {
            LogModLoadError(metadata.id, ex);
            return false;
        }

        return true;
    }

    private static bool TryLoadMod(ModMetadata metadata, Assembly assembly, int audioGroup,
        UndertaleData data, IReadOnlyList<ModMetadata> availableDependencies, IEnumerable<ModMetadata> queuedMods) {
        try {
            Type? type = assembly.GetTypes()
                .FirstOrDefault(modType => modType.GetInterfaces().Contains(typeof(IGameMakerMod)));
            if(type is null) {
                LogModLoadError(metadata.id, "mod type not found");
                return false;
            }

            (Activator.CreateInstance(type) as IGameMakerMod)?
                .Load(audioGroup, data, availableDependencies, queuedMods);
        }
        catch(Exception ex) {
            LogModLoadError(metadata.id, ex);
            return false;
        }

        return true;
    }

    private static Assembly? TempResolveModAssemblies(object? _, ResolveEventArgs args) => AppDomain
        .CurrentDomain.GetAssemblies()
        .FirstOrDefault(assembly => assembly.FullName == args.Name);

    private static void LogModLoadError(string id, string error) =>
        Console.WriteLine($"Error! Failed when loading mod {id} ({error})");

    private static void LogModLoadError(string id, Exception exception) =>
        Console.WriteLine($"Error! Failed when loading mod {id}:\n{exception}");

    private static byte* UndertaleDataToBytes(UndertaleData data, int* size) {
        using MemoryStream stream = new(*size);
        UndertaleIO.Write(stream, data);
        byte[] bytes = stream.GetBuffer();
        *size = (int)stream.Length;
        byte* bytesPtr = (byte*)Marshal.AllocHGlobal(*size);
        for(int i = 0; i < *size; i++) *(bytesPtr + i) = bytes[i];
        return bytesPtr;
    }
}
