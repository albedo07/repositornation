@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0COLLECT_REPORTS.ps1"
pause
