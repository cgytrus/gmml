namespace GmmlPatcher;

public interface IGameMakerMod {
    // audioGroup 0 = game data
    public void Load(int audioGroup, ModMetadata currentMod, IEnumerable<ModMetadata> dependencies);
    public void LateLoad(int audioGroup, ModMetadata currentMod, IEnumerable<ModMetadata> dependencies) { }
}
