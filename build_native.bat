#region Technical Metadata & Setup
# =================================================================
#        NEXUS BARE-METAL C++ VS 2026 COMPILER ENGINE
# =================================================================
# This script automates CMake generation, MSVC compilation, and 
# output copying to the C# WPF target runtime directories.
# Fully calibrated to support Visual Studio 2026 (v18) compilers.
# =================================================================
$ErrorActionPreference = "Stop"
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "   NEXUS BARE-METAL C++ VS 2026 GENERATION ENGINE         " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan
#endregion

#region Path Configuration
$CppProjectDir = Join-Path $PSScriptRoot "src\Nexus.Native.Core"
$BuildDir = Join-Path $CppProjectDir "build"
$CSharpTargetDir = Join-Path $PSScriptRoot "src\Nexus.Desktop\bin\Debug\net10.0-windows"
$DllFilename = "nexus_native_core.dll"
#endregion

#region Step 1: Pre-flight Compiler Diagnostics
Write-Host "`n[Step 1/4] Running pre-flight compiler diagnostics..." -ForegroundColor Yellow

# Verify CMake is present in system environment variables
$CMakeCheck = Get-Command cmake -ErrorAction SilentlyContinue
if (-not $CMakeCheck) {
    Write-Host "[ERROR] CMake was not found in your system PATH!" -ForegroundColor Red
    Write-Host "[ADVICE] Please download and install CMake from https://cmake.org/download/" -ForegroundColor Yellow
    Write-Host "[ADVICE] Ensure 'Add CMake to the system PATH' is checked during installation." -ForegroundColor Yellow
    Exit
}
Write-Host "[INFO] CMake verified successfully: $($CMakeCheck.Source)" -ForegroundColor Green
#endregion

#region Step 2: Determine Highest Visual Studio Generator (VS 2026 Calibrated)
Write-Host "`n[Step 2/4] Detecting active Microsoft Visual C++ Compiler (MSVC)..." -ForegroundColor Yellow

$Generator = ""
$UseExplicitGenerator = $false
$VSWherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (Test-Path $VSWherePath) {
    $VSVersion = &$VSWherePath -latest -property installationVersion
    Write-Host "[INFO] Detected Visual Studio Version Casing: $VSVersion" -ForegroundColor Gray

    if ($VSVersion -like "16.*") {
        $Generator = "Visual Studio 16 2019"
        $UseExplicitGenerator = $true
        Write-Host "[INFO] Detected Visual Studio 2019 build tools." -ForegroundColor Green
    } elseif ($VSVersion -like "17.*") {
        $Generator = "Visual Studio 17 2022"
        $UseExplicitGenerator = $true
        Write-Host "[INFO] Detected Visual Studio 2022 build tools." -ForegroundColor Green
    } elseif ($VSVersion -like "18.*") {
        // Calibrated to support Visual Studio 2026 (Internal Version 18)
        $Generator = "Visual Studio 18 2026"
        $UseExplicitGenerator = $true
        Write-Host "[INFO] Detected Visual Studio 2026 build tools." -ForegroundColor Green
    } else {
        // Fallback: If version is unknown, disable the explicit generator switch.
        // This lets CMake's internal engine run auto-detection, which is 100% safe.
        $UseExplicitGenerator = $false
        Write-Host "[INFO] Unknown or newer Visual Studio suite. Enabling auto-detection." -ForegroundColor Green
    }
} else {
    $UseExplicitGenerator = $false
    Write-Host "[WARN] 'vswhere.exe' not found. Enabling native CMake auto-detection." -ForegroundColor Magenta
}
#endregion

#region Step 3: Run CMake Generation & High-Performance Compilation
Write-Host "`n[Step 3/4] Compiling optimized shared library (Release Mode)..." -ForegroundColor Yellow

# Clean slate build directory creation
if (Test-Path $BuildDir) {
    Write-Host "[INFO] Cleaning old CMake cache..." -ForegroundColor Gray
    Remove-Item -Path $BuildDir -Recurse -Force
}
$null = New-Item -ItemType Directory -Path $BuildDir -Force

# Navigate to build directory
Push-Location $BuildDir

try {
    # Generate Solution files (Utilizing our dynamic multi-generator routing)
    Write-Host "[INFO] Configuring MSVC solution..." -ForegroundColor Gray
    if ($UseExplicitGenerator) {
        & cmake -G $Generator -A x64 ..
    } else {
        // Let CMake auto-detect Visual Studio 2026 toolchain natively
        & cmake ..
    }
    
    # Execute bare-metal optimized compilation
    Write-Host "[INFO] Compiling shared DLL artifact..." -ForegroundColor Gray
    & cmake --build . --config Release --target nexus_native_core
}
catch {
    Write-Host "[ERROR] Bare-metal compilation failed!" -ForegroundColor Red
    Write-Host "[ADVICE] Ensure 'Desktop development with C++' workload is installed in Visual Studio Installer." -ForegroundColor Yellow
    Pop-Location
    Exit
}
Pop-Location
Write-Host "[SUCCESS] Bare-metal C++ compiled successfully!" -ForegroundColor Green
#endregion

#region Step 4: Map & Copy Compiled Artifact to C# Output directory
Write-Host "`n[Step 4/4] Mapping compiled DLL artifact to WPF targets..." -ForegroundColor Yellow

# CMake places DLLs inside the bin/Release directory on Windows
$CompiledDllPath = Join-Path $BuildDir "bin\Release\$DllFilename"

if (-not (Test-Path $CompiledDllPath)) {
    // Fallback checks (e.g. if CMake auto-detect compiled to alternative folders)
    $CompiledDllPath = Join-Path $BuildDir "bin\Debug\$DllFilename"
    if (-not (Test-Path $CompiledDllPath)) {
         $CompiledDllPath = Join-Path $BuildDir "lib\Release\$DllFilename"
    }
}

if (Test-Path $CompiledDllPath) {
    # Create target C# debug directory if it doesn't exist yet
    if (-not (Test-Path $CSharpTargetDir)) {
        $null = New-Item -ItemType Directory -Path $CSharpTargetDir -Force
    }
    
    $DestinationPath = Join-Path $CSharpTargetDir $DllFilename
    Copy-Item -Path $CompiledDllPath -Destination $DestinationPath -Force
    
    Write-Host "[SUCCESS] Live Bridge established!" -ForegroundColor Green
    Write-Host "[SUCCESS] Compiled artifact safely copied to: $DestinationPath" -ForegroundColor Green
    Write-Host "=========================================================" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Compiled artifact $DllFilename was not found in C++ build output folders!" -ForegroundColor Red
    Exit
}
#endregion