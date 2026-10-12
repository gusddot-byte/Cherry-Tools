@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
    echo Falta el SDK .NET 8. Instala Visual Studio con Desarrollo de escritorio .NET.
    pause
    exit /b 1
)
echo Compilando Cherrybox - Euler SS Tools v2.0 (Windows x64)...
dotnet publish EulerSSTools.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o publish
if errorlevel 1 (
    echo Error de compilacion. Copia el mensaje y compartelo para corregirlo.
    pause
    exit /b 1
)
echo.
echo EXE: "%~dp0publish\EulerSSTools.exe"
echo El ejecutable pedira permisos UAC al iniciarse. No esta firmado digitalmente.
pause
