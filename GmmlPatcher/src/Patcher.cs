﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Semver;

using UndertaleModLib;

namespace GmmlPatcher;

// ReSharper disable once UnusedType.Global
public static class Patcher {
    private static readonly string patcherPath = Path.Combine("gmml", "patcher");
    private static readonly string modsPath = Path.Combine("gmml", "mods");
    private static readonly string cachePath = Path.Combine("gmml", "cache");
    private static readonly string hashesFilePath = Path.Combine(cachePath, "hashes.json");

    private static bool _errored;
    private static Dictionary<string, string>? _hashes;

    private static List<(ModMetadata metadata, Assembly assembly, IReadOnlyList<ModMetadata> availableDependencies)>?
        _queuedMods;

    [DllImport("version.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void* mmAlloc(ulong size, sbyte* why, int unk2, bool unk3);

    // ReSharper disable once UnusedMember.Global
    [UnmanagedCallersOnly]
    public static unsafe byte* ModifyData(int audioGroup, byte* original, int* size) {
        if(_errored) return original;
        try {
            if(TryLoadCache(audioGroup, original, size, out byte* modified, out string fileName, out MD5 hash))
                return modified;
            _hashes ??= new Dictionary<string, string>(1);
            SaveCache(fileName, hash, _hashes);
            modified = LoadMods(audioGroup, original, size);
            return modified;
        }
        catch(Exception ex) {
            _errored = true;
            Console.WriteLine($"Error while loading mods! Loading vanilla\n{ex}");
        }
        finally {
            Console.WriteLine("Running full garbage collection");
            GC.Collect();
        }
        return original;
    }

    private static unsafe bool TryLoadCache(int audioGroup, byte* original, int* size, out byte* modified,
        out string fileName, out MD5 hash) {
        modified = original;
        fileName = GetFileNameFromAudioGroup(audioGroup);

        byte[] data = new byte[*size];
        Marshal.Copy((IntPtr)original, data, 0, data.Length);
        hash = HashCurrentSetup(audioGroup, data);

        if(hash.Hash is null) {
            Console.WriteLine($"Warning! Failed to {fileName} hash");
            return false;
        }

        if(!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);

        if(!File.Exists(hashesFilePath)) {
            Console.WriteLine("No cache found");
            return false;
        }

        _hashes ??= JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(hashesFilePath));

        if(_hashes is null) {
            Console.WriteLine("Warning! Failed to read cache hashes");
            File.Delete(hashesFilePath);
            return false;
        }

        try {
            foreach((string? hashFileName, string? hashText) in _hashes) {
                if(hashFileName != fileName) continue;
                return TryLoadCache(ref modified, size, fileName, hash, hashText);
            }
        }
        catch(Exception ex) {
            Console.WriteLine($"Warning! Failed to read {fileName} cache hash\n{ex}");
            _hashes.Remove(fileName);
            return false;
        }

        Console.WriteLine($"No cached {fileName} found");
        return false;
    }

    private static unsafe bool TryLoadCache(ref byte* modified, int* size, string fileName, HashAlgorithm hash,
        string hashText) {
        byte[] cacheHash = Convert.FromHexString(hashText);

        if(hash.Hash!.Length == 0 || cacheHash.Length == 0 || hash.Hash.Length != cacheHash.Length) {
            Console.WriteLine($"Outdated {fileName} cache");
            return false;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        for(int i = 0; i < hash.Hash.Length; i++) {
            if(hash.Hash[i] == cacheHash[i])
                continue;
            Console.WriteLine($"Outdated {fileName} cache");
            return false;
        }

        Console.WriteLine($"Loading cached {fileName}");
        byte[] fileBytes = File.ReadAllBytes(Path.Combine(cachePath, fileName));
        *size = fileBytes.Length;
        modified = (byte*)mmAlloc((ulong)*size, (sbyte*)0, 0x124, false);
        Marshal.Copy(fileBytes, 0, (IntPtr)modified, fileBytes.Length);
        return true;
    }

    private static void SaveCache(string fileName, HashAlgorithm hash, Dictionary<string, string> hashes) {
        try {
            Console.WriteLine($"Saving {fileName} cache");

            // already warned in TryLoadCache so we can just quietly return
            if(hash.Hash is null)
                return;

            string hashString = BitConverter.ToString(hash.Hash).Replace("-", "").ToLower();
            hashes[fileName] = hashString;
            File.WriteAllText(hashesFilePath, JsonSerializer.Serialize(hashes));
        }
        catch(Exception ex) {
            Console.WriteLine($"Warning! Failed to save {fileName} cache\n{ex}");
        }
    }

    private static MD5 HashCurrentSetup(int audioGroup, byte[] data) {
        MD5 hash = MD5.Create();

        AppendDirectoryToHash(hash, patcherPath);
        AppendDirectoryToHash(hash, modsPath);

        AppendFileToHash(hash, "version.dll");

        byte[] audioGroupBytes = BitConverter.GetBytes(audioGroup);
        hash.TransformBlock(audioGroupBytes, 0, audioGroupBytes.Length, audioGroupBytes, 0);

        hash.TransformFinalBlock(data, 0, data.Length);

        return hash;
    }

    // https://stackoverflow.com/a/15683147/10484146
    private static void AppendDirectoryToHash(ICryptoTransform hash, string path) {
        foreach(string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories).OrderBy(p => p)) {
            // hash path
            byte[] pathBytes = Encoding.UTF8.GetBytes(path.ToLower());
            hash.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

            // hash contents
            AppendFileToHash(hash, file);
        }
    }

    private static void AppendFileToHash(ICryptoTransform hash, string path) {
        byte[] contentBytes = File.ReadAllBytes(path);
        hash.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
    }

    private static unsafe byte* LoadMods(int audioGroup, byte* original, int* size) {
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
        string whitelistPath = Path.Combine(modsPath, "whitelist.txt");
        string blacklistPath = Path.Combine(modsPath, "blacklist.txt");

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

        SearchForMods(modsPath);

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

        string relativePath = $"{Path.PathSeparator}{Path.GetRelativePath(modsPath, path)}";
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

    private static string GetFileNameFromAudioGroup(int audioGroup) =>
        audioGroup < 0 ? "data.win" : $"audiogroup{audioGroup}.dat";

    private static unsafe byte* UndertaleDataToBytes(UndertaleData data, int* size) {
        using MemoryStream stream = new(*size);
        UndertaleIO.Write(stream, data);
        *size = (int)stream.Length;
        byte* bytesPtr = (byte*)mmAlloc((ulong)*size, (sbyte*)0, 0x124, false);
        Marshal.Copy(stream.GetBuffer(), 0, (IntPtr)bytesPtr, *size);
        return bytesPtr;
    }
}
