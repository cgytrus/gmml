using UndertaleModLib;

namespace GmmlPatcher;

public interface IGameMakerMod {
    public void Load(UndertaleData data, IReadOnlyList<ModMetadata> availableDependencies,
        IEnumerable<ModMetadata> queuedMods);
}
