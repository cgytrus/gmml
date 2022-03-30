using UndertaleModLib;

namespace GmmlPatcher;

public interface IGameMakerMod {
    public void Load(UndertaleData data, IEnumerable<ModMetadata> queuedMods);
}
