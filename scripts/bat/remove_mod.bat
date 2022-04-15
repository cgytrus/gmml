@echo off
call .\_set_game_dir.bat
rmdir /s /q "%GAME_DIR%\gmml\mods\%~1"
