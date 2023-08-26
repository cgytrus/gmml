# ⚠️ THIS PROJECT IS NO LONGER MAINTAINED ⚠️
## Please, use [gs2ml](https://github.com/rgc-exists/gs2ml) instead

# GameMaker Mod Loader
A mod loader for some GameMaker Studio 2 games ***not*** using YYC,
based on [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool)

## Installation
1. Install [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.3-windows-x64-installer)
2. Download latest release (currently only [development releases](https://github.com/cgytrus/gmml/releases/tag/Development) are available)
3. Unpack the downloaded archive into the game's folder

## Usage
### Players
1. Put your mods in `<game root>/gmml/mods`
2. Put mods' IDs (you can get them from the mods' `manifest.json` file) or paths (prefixed with your system's directory separator)
   in `mods/blacklist.txt` to ignore them
3. Put mods' IDs in `mods/whitelist.txt` to enable the whitelist and only load those mods
### Modders
1. Create a mod using a [template](https://github.com/cgytrus/GMML.Templates)
2. See [GMML](./GmmlExampleMod) and [WYS](https://github.com/cgytrus/WysApi/tree/main/WysExampleMod) example mods for examples
#### If you have set the game path
- Run `scripts/bat/libs.bat` to setup libraries
- Run `scripts/bat/mods.bat` to install the mod (needs to be ran after build)

## Compilation
### Prerequisites
- Visual Studio 2022 with MSVC v143 and .NET 6 SDK

**or**
- Any C# IDE that supports using VS Build Tools (both Rider and VSCode should work, though I didn't test VSC)
- Visual Studio Build Tools 2022 with MSVC v143 and .NET 6 SDK
### Compile
1. Clone GMML recursively (`git clone https://github.com/cgytrus/gmml.git --recursive`, `cd gmml`)
2. Restore solution (`dotnet restore`)
3. Build solution (`msbuild -p:Configuration=Release -p:Platform=x64` from the Native Tools Command Prompt)

   *Note: if you're not using Visual Studio, you have to select the VS/VS Build Tools
MSBuild installation in your IDE settings*

### Install
Copy the contents of GmmlPatcher output into the game's root
**or**
1. Create `_set_game_dir.bat` in `scripts/bat` and paste the following contents into it,
replacing `<game dir>` with the path to your game:
```
set GAME_DIR=<game dir>
```

2. Run the `patcher.bat` and `mods.bat` or `mods_no_example.bat` scripts,
they will hardlink `GmmlPatcher`'s and the other projects' build outputs respectively
to the correct locations in your game folder to allow for easier debugging
(not symlink because that requires special permissions.. for... some reason)

The final structure would look something like this:
```
<game root>
+---...
+---gmml
|   +---patcher
|   |   +---...
|   |   +---GmmlPatcher.dll
|   |   \---...
|   \---mods
|       +---*your mods here*
|       \---...
+---gmml.cfg
+---nethost.dll
+---version.dll
\---...
```
