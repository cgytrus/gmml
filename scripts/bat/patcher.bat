@echo off
call .\_set_game_dir.bat
set /p rootPath=<..\..\bin\current.txt

call :symlink_file version.dll
call :symlink_file version.pdb
call :symlink_file nethost.dll
call :symlink_file gmml.cfg
rmdir "%GAME_DIR%\gmml\patcher"
mklink /j "%GAME_DIR%\gmml\patcher" "%rootPath%gmml\patcher"
exit /b 0

:symlink_file
del "%GAME_DIR%\%~1"
mklink /h "%GAME_DIR%\%~1" "%rootPath%%~1"
exit /b 0
