@echo off
call .\_set_game_dir.bat
rmdir "%GAME_DIR%\gmml\cache" /s /q
