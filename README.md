# GameMaker Mod Loader
A mod loader for some GameMaker Studio 2 games ***not*** using YYC

## Installation
1. Install [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.3-windows-x64-installer)
2. Download latest release (there are none yet lol, [compile it yourself](#Compilation))
3. Unpack the downloaded archive into the game's folder

## Compilation
### Prerequisites
- Visual Studio 2022 (IDE or Build Tools, MSVC v143 and .NET 6 SDK)
### Compile
1. Clone UndertaleModTool (`git clone https://github.com/krzys-h/UndertaleModTool.git`)
2. Publish UndertaleModLib (`dotnet publish UndertaleModLib -c Release -r win-x64 --self-contained`)
3. Add the NuGet package to NuGet sources (`dotnet nuget add source <path to folder with the .nupkg file>`)

   *Note for Visual Studio users: you can right click on the solution in the Solution Explorer tab >
Manage NuGet Packages for Solution > Settings (gear icon next to the "Package source" dropdown) and add a new feed*

   *Note for Rider users: you can open the NuGet tab > Sources > Feeds and add a new feed*
4. Clone GMML recursively (`git clone https://github.com/cgytrus/gmml.git --recursive`)
5. Build GmmlPatcher (`dotnet build GmmlPatcher -c Release -r win-x64 --self-contained`)
### Install
Copy the contents of `<GmmlPatcher build output>/gmml-*-*-final` into the game's root
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

## Usage
1. Put your mods in `<game root>/gmml/mods`
2. Put mods' IDs (you can get them from the mods' `manifest.json` file) or paths (prefixed with your system's directory separator)
   in `mods/blacklist.txt` to ignore them
3. Put mods' IDs in `mods/whitelist.txt` to enable the whitelist and only load those mods
