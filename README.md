# GameMaker Mod Loader
A mod loader for some GameMaker Studio 2 games ***not*** using YYC

## Installation
1. Install [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.3-windows-x64-installer)
2. Download latest release (there are none yet lol, [compile it yourself](#Compilation))
3. Unpack the downloaded archive into the game's folder

## Compilation
### Prerequisites
- Visual Studio Build Tools 2022 (MSVC v143)
- .NET 6 SDK
### Compile
1. Clone UndertaleModTool `git clone https://github.com/krzys-h/UndertaleModTool.git`
2. Publish UndertaleModLib `dotnet publish UndertaleModLib -c Release -r win-x64 --self-contained`
3. Add the NuGet package to NuGet sources `dotnet nuget add source <path to folder with the .nupkg file>`

   *Note: if you're using Rider, you can open the NuGet tab > Sources > Feeds and add a new feed*
4. Clone GMML recursively `git clone https://github.com/cgytrus/gmml.git --recursive`
5. Go to `<dotnet folder (usually C:/Program Files/dotnet)>/packs/Microsoft.NETCore.App.Hosy.win-x64/<latest 6.x.x>/runtimes/win-x64/native`
6. Copy the files `coreclr_delegates.h`, `hostfxr.h` and `nethost.h` to `gmml/lib/nethost`
7. Copy the files `nethost.dll` and `nethost.lib` to `gmml/lib/nethost/x64`

   (yes, i was too lazy to make it find them automatically)

   ((P.S. i don't even think that's possible without it being a c# project lol))
8. If you need an x86 build, do the same but replace `x64` with `x86` everywhere
9. Build gmml `msbuild ./gmml/gmml.vcxproj` (in x64 Native Tools Command Prompt)
10. Publish GmmlPatcher `dotnet publish GmmlPatcher -c Release -r win-x64 --self-contained`
### Install
1. Copy the `version.dll` and `nethost.dll` from gmml build output to your game's root
2. Copy the publish folder from GmmlPatcher publish output to `<game root>/gmml` and rename it to `patcher`
3. Create a `gmml.cfg` file in game's root
4. Optional: add line `debug` to gmml.cfg to enable some debug messages and `console` to display a console
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
