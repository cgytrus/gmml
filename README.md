# GameMaker Mod Loader
A mod loader for some GameMaker Studio 2 games ***not*** using YYC,
based on [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool)

## Installation
1. Install [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.3-windows-x64-installer)
2. Download latest release (there are none yet lol, [compile it yourself](#Compilation))
3. Unpack the downloaded archive into the game's folder

## Usage
1. Put your mods in `<game root>/gmml/mods`
2. Put mods' IDs (you can get them from the mods' `manifest.json` file) or paths (prefixed with your system's directory separator)
   in `mods/blacklist.txt` to ignore them
3. Put mods' IDs in `mods/whitelist.txt` to enable the whitelist and only load those mods

## Compilation
### Prerequisites
- Visual Studio 2022 with MSVC v143 and .NET 6 SDK

**or**
- Any C# IDE that supports using VS Build Tools (both Rider and VSCode should work, though I didn't test VSC)
- Visual Studio Build Tools 2022 with MSVC v143 and .NET 6 SDK
### Compile
1. Clone GMML recursively (`git clone https://github.com/cgytrus/gmml.git --recursive`)
2. Restore GmmlPatcher (`dotnet restore GmmlPatcher`)
3. Build GmmlPatcher (`msbuild GmmlPatcher -p:Configuration=Release` from the Native Tools Command Prompt)

   *Note: if you're not using Visual Studio, you have to select the VS/VS Build Tools
MSBuild installation in your IDE settings*
#### Compile for different platform
If you want to compile for a platform different than your current, use the command below in the Native Tools Command Prompt
(currently supported platforms are `win-x64` and `win-x86`)

```
dotnet restore GmmlPatcher
msbuild GmmlPatcher -p:Configuration=Release -p:NativeRuntimeIdentifier=<platform>
```
### Install
Copy the contents of GmmlPatcher output into the game's root
**or**
1. Create `_set_game_dir.bat` in `scripts/bat` and paste the following contents into it,
replacing `<game dir>` with the path to your game:
```
set GAME_DIR=<game dir>
```

2. Run the `patcher.bat` and `mods.bat` or `mods_no_example.bat` scripts,
they will symlink `GmmlPatcher`'s and the other projects' build outputs respectively
to the correct locations in your game folder to allow for easier debugging

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
