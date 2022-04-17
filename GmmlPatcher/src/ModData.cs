namespace GmmlPatcher;

public readonly struct ModData {
    public ModMetadata metadata { get; init; }
    public string path { get; init; }
    public IEnumerable<ModMetadata> dependencies { get; init; }
    public Type type { get; init; }
}
