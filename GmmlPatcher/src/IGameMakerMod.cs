using UndertaleModLib;

namespace GmmlPatcher;

public interface IGameMakerMod {
    // audioGroup -1 = game data
    public void Load(int audioGroup, UndertaleData data, IReadOnlyList<ModMetadata> availableDependencies,
        IEnumerable<ModMetadata> queuedMods);
}
