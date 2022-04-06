namespace GmmlPatcher;

public interface IGameMakerMod {
    // audioGroup -1 = game data
    public void Load(int audioGroup, ModMetadata currentMod, IEnumerable<ModMetadata> dependencies);
}
