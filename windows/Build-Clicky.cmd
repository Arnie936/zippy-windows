@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "%CSC%" (
  echo C# compiler not found.
  exit /b 1
)

%CSC% /nologo /target:winexe ^
 /out:"%~dp0Clicky.Windows.exe" ^
 /reference:System.Windows.Forms.dll ^
 /reference:System.Drawing.dll ^
 /reference:System.Net.Http.dll ^
 /reference:System.Web.Extensions.dll ^
 /reference:Microsoft.CSharp.dll ^
 "%~dp0Clicky.Windows.cs"

exit /b %errorlevel%

