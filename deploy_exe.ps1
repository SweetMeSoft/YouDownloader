# YouDownloader Standalone Setup EXE Generator
# Generates a single self-extracting .exe installer

$ErrorActionPreference = "Stop"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "   Generating Standalone Setup EXE (.NET 10)  " -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

$zipFile = ".\YouDownloader_Release_win-x64.zip"
$sfxBuilderDir = ".\SfxBuilder"
$outputExe = ".\YouDownloader_Setup.exe"

# 1. Ensure the zip release payload exists
if (-not (Test-Path $zipFile)) {
    Write-Host "Zip release payload not found. Running deploy.ps1 to generate it..." -ForegroundColor Yellow
    powershell -ExecutionPolicy Bypass -File .\deploy.ps1
}

# 2. Setup SfxBuilder workspace
if (Test-Path $sfxBuilderDir) {
    Remove-Item -Recurse -Force $sfxBuilderDir
}
New-Item -ItemType Directory -Path $sfxBuilderDir | Out-Null

# Copy the zip package to the builder directory as payload.zip
Copy-Item $zipFile -Destination "$sfxBuilderDir\payload.zip"

# Create SfxBuilder.csproj
$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ApplicationIcon>..\YouDownloader\icon.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="payload.zip" Link="payload.zip" />
  </ItemGroup>

</Project>
"@
Set-Content -Path "$sfxBuilderDir\SfxBuilder.csproj" -Value $csprojContent

# Create Program.cs
$programContent = @"
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace YouDownloader.Sfx
{
    static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_OK = 0x00000000;
        private const uint MB_ICONERROR = 0x00000010;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string tempRoot = Path.GetTempPath();
                string appDirName = "YouDownloader_Installed";
                string targetDir = Path.Combine(tempRoot, appDirName);

                if (Directory.Exists(targetDir))
                {
                    try
                    {
                        Directory.Delete(targetDir, true);
                    }
                    catch 
                    {
                        // Ignore lock errors and proceed to overwrite
                    }
                }
                
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var assembly = Assembly.GetExecutingAssembly();
                using Stream? resourceStream = assembly.GetManifestResourceStream("payload.zip") 
                                            ?? assembly.GetManifestResourceStream("SfxBuilder.payload.zip");
                
                if (resourceStream == null)
                {
                    throw new Exception("Embedded payload.zip not found in executable resources.");
                }

                using (ZipArchive archive = new ZipArchive(resourceStream))
                {
                    archive.ExtractToDirectory(targetDir, overwriteFiles: true);
                }

                string exePath = Path.Combine(targetDir, "YouDownloader.exe");
                if (!File.Exists(exePath))
                {
                    throw new FileNotFoundException("YouDownloader.exe was not found in the extracted package.");
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                };

                if (args.Length > 0)
                {
                    psi.Arguments = string.Join(" ", args);
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox(IntPtr.Zero, $"Failed to extract and launch YouDownloader:\n{ex.Message}", "Launcher Error", MB_OK | MB_ICONERROR);
            }
        }
    }
}
"@
Set-Content -Path "$sfxBuilderDir\Program.cs" -Value $programContent

# 3. Publish the Setup Executable
Write-Host "Compiling setup bootstrapper..." -ForegroundColor Yellow
$publishOut = "$sfxBuilderDir\publish_out"

dotnet publish "$sfxBuilderDir\SfxBuilder.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -o $publishOut

# Move output exe to root workspace
if (Test-Path "$publishOut\SfxBuilder.exe") {
    if (Test-Path $outputExe) {
        Remove-Item -Force $outputExe
    }
    Move-Item -Path "$publishOut\SfxBuilder.exe" -Destination $outputExe
    Write-Host "Cleanup temporary builder workspace..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $sfxBuilderDir
} else {
    Write-Error "SFX Compilation failed!"
    exit 1
}

Write-Host "==============================================" -ForegroundColor Green
Write-Host "   Standalone Setup EXE successfully generated!  " -ForegroundColor Green
Write-Host "   File: $outputExe" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
