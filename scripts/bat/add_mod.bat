@echo off
call .\_set_game_dir.bat

call .\remove_mod.bat %~1
mkdir "%GAME_DIR%\gmml\mods\%~1"
mklink /h "%GAME_DIR%\gmml\mods\%~1\%~1.dll" "..\..\%~1\bin\Current\%~1.dll"
mklink /h "%GAME_DIR%\gmml\mods\%~1\%~1.pdb" "..\..\%~1\bin\Current\%~1.pdb"
mklink /h "%GAME_DIR%\gmml\mods\%~1\metadata.json" "..\..\%~1\bin\Current\metadata.json"
