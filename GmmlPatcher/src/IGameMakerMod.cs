using GmmlInteropGenerator;

using UndertaleModLib;
// ReSharper disable UnusedParameter.Global

namespace GmmlPatcher;

[EnableSimpleGmlInterop]
public interface IGameMakerMod {
    // audioGroup 0 = game data
    public void EarlyLoad(int audioGroup, UndertaleData data, ModData currentMod) { }
    public void Load(int audioGroup, UndertaleData data, ModData currentMod);
    public void LateLoad(int audioGroup, UndertaleData data, ModData currentMod) { }
}
