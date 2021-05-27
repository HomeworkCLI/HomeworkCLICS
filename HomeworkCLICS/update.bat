@echo off
echo "Copying update files..."
choice /t 3 /d y /n >nul
xcopy .\updateFiles . /s /y
del .\updateFiles
start "" "HomeworkCLI.exe"