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
2. Build GmmlPatcher (`dotnet build GmmlPatcher -c Release -r win-x64`)

   *Note: if you're not using Visual Studio, you have to select the VS/VS Build Tools
MSBuild installation in your IDE settings*
### Install
Copy the contents of `<GmmlPatcher output path>/gmml-final` into the game's root

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
