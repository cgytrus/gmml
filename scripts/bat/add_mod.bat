@echo off
call .\_set_game_dir.bat
set /p rootPath=<..\..\bin\current.txt

call .\remove_mod.bat %~1
mkdir "%GAME_DIR%\gmml\mods\%~1"
mklink /h "%GAME_DIR%\gmml\mods\%~1\%~1.dll" "%rootPath%gmml\mods\%~1\%~1.dll"
mklink /h "%GAME_DIR%\gmml\mods\%~1\%~1.pdb" "%rootPath%gmml\mods\%~1\%~1.pdb"
mklink /h "%GAME_DIR%\gmml\mods\%~1\metadata.json" "%rootPath%gmml\mods\%~1\metadata.json"
