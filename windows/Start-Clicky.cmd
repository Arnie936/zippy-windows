@echo off
setlocal

if not exist "%~dp0Clicky.Windows.exe" (
  call "%~dp0Build-Clicky.cmd"
  if errorlevel 1 exit /b %errorlevel%
)

start "" "%~dp0Clicky.Windows.exe"
exit /b 0
