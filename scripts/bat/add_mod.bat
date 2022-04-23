@echo off
call .\_set_game_dir.bat
set /p rootPath=<..\..\%~1\bin\current.txt
set rootPath=..\..\%~1\%rootPath%

call .\remove_mod.bat %~1
mkdir "%GAME_DIR%\gmml\mods\%~1"
mklink /h "%GAME_DIR%\gmml\mods\%~1\%~1.dll" "%rootPath%%~1.dll"
mklink /h "%GAME_DIR%\gmml\mods\%~1\%~1.pdb" "%rootPath%%~1.pdb"
mklink /h "%GAME_DIR%\gmml\mods\%~1\metadata.json" "%rootPath%metadata.json"
