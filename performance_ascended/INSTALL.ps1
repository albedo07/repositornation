param([string]$BepInExDll, [string]$UnityCoreDll)
$ErrorActionPreference = "Stop"
$stage = $null
Write-Host ""
Write-Host "PERFORMANCE ASCENDED v0.6.8 - INSTALL" -ForegroundColor Cyan
Write-Host "AA-style installer: select your active profile and game references." -ForegroundColor Gray
Write-Host "Compile in staging before replacing this plugin's DLL." -ForegroundColor Gray
Write-Host ""

Add-Type -AssemblyName System.Windows.Forms
function Pick-File([string]$title, [string]$filter) {
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Title = $title
    $dlg.Filter = $filter
    $dlg.Multiselect = $false
    try {
        if ($dlg.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { throw "No file selected." }
        return $dlg.FileName
    }
    finally { $dlg.Dispose() }
}
function Need-File([string]$path, [string]$label) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "$label not found:`n$path" }
}
function Add-UniqueRef([System.Collections.ArrayList]$list, [string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return $false }
    if (-not (Test-Path -LiteralPath $path)) { return $false }

    $full = [System.IO.Path]::GetFullPath($path)

    foreach ($existing in $list) {
        if ([string]::Equals($existing, $full, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }

    [void]$list.Add($full)
    return $true
}

function Compile-WithDependencies(
    [string[]]$sourcePaths,
    [string]$outputPath,
    [System.Collections.ArrayList]$baseRefs,
    [string]$managedDir,
    [string]$coreDir
) {
    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Force
    }

    $refs = New-Object System.Collections.ArrayList
    foreach ($r in $baseRefs) {
        [void](Add-UniqueRef $refs $r)
    }


    for ($attempt = 1; $attempt -le 12; $attempt++) {
        if (Test-Path -LiteralPath $outputPath) { Remove-Item -LiteralPath $outputPath -Force }
        try {
            Add-Type `
                -LiteralPath $sourcePaths `
                -ReferencedAssemblies ([string[]]$refs.ToArray()) `
                -OutputAssembly $outputPath `
                -OutputType Library `
                -IgnoreWarnings `
                -ErrorAction Stop

            if (-not (Test-Path -LiteralPath $outputPath)) {
                throw "Compiler returned without producing: $outputPath"
            }

            return
        }
        catch {
            $details = ($_ | Out-String)
            $matches = [regex]::Matches($details, "assembly '([^,']+)")
            $added = $false

            foreach ($match in $matches) {
                $name = $match.Groups[1].Value
                if ([string]::IsNullOrWhiteSpace($name)) { continue }

                # Never add framework assemblies that Windows PowerShell's C# compiler already
                # imports implicitly. Adding Valheim's copy of mscorlib/System can create a
                # duplicate assembly-identity failure and hide the real compiler diagnostic.
                if ($name -ieq "mscorlib" -or $name -ieq "System" -or $name -ieq "System.Core") {
                    continue
                }

                $candidateManaged = Join-Path $managedDir ($name + ".dll")
                $candidateCore = Join-Path $coreDir ($name + ".dll")

                if (Add-UniqueRef $refs $candidateManaged) {
                    Write-Host "  Auto-added dependency: $name.dll" -ForegroundColor DarkCyan
                    $added = $true
                    continue
                }

                if (Add-UniqueRef $refs $candidateCore) {
                    Write-Host "  Auto-added dependency: $name.dll" -ForegroundColor DarkCyan
                    $added = $true
                }
            }

            if (-not $added) {
                throw
            }
        }
    }

    throw "Could not compile the plugin after dependency retries."
}

try {
    if ($PSVersionTable.PSEdition -eq 'Core') { throw 'Use Windows PowerShell 5.1 by running INSTALL.bat.' }
    if (Get-Process -Name valheim -ErrorAction SilentlyContinue) { throw 'Close Valheim before installing.' }
    Write-Host "1) Select BepInEx.dll from your ACTIVE profile's BepInEx\core folder." -ForegroundColor Yellow
    Write-Host 'You can paste its full path in the File name box.' -ForegroundColor Gray
    if ([string]::IsNullOrWhiteSpace($BepInExDll)) { $BepInExDll = Pick-File 'Select BepInEx.dll' 'BepInEx.dll|BepInEx.dll' }
    Need-File $BepInExDll 'BepInEx.dll'
    $coreDir = Split-Path -Parent ([System.IO.Path]::GetFullPath($BepInExDll))
    $bepInExDir = Split-Path -Parent $coreDir
    $pluginsDir = Join-Path $bepInExDir 'plugins'
    Write-Host ""
    Write-Host '2) Select UnityEngine.CoreModule.dll from Valheim\valheim_Data\Managed.' -ForegroundColor Yellow
    if ([string]::IsNullOrWhiteSpace($UnityCoreDll)) { $UnityCoreDll = Pick-File 'Select UnityEngine.CoreModule.dll' 'UnityEngine.CoreModule.dll|UnityEngine.CoreModule.dll' }
    Need-File $UnityCoreDll 'UnityEngine.CoreModule.dll'
    $managedDir = Split-Path -Parent ([System.IO.Path]::GetFullPath($UnityCoreDll))
    Need-File (Join-Path $managedDir 'assembly_valheim.dll') 'assembly_valheim.dll'
    Need-File (Join-Path $coreDir '0Harmony.dll') '0Harmony.dll'
    $sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName })
    # 0.4.1: check the exact source names (a bare count broke when UnloadBudget.cs was added)
    $required = @('DiagnosticData.cs','FrameRecorder.cs','PerformancePlugin.cs','PerformancePolicy.cs','PerformanceSettings.cs','RuntimeDiagnostics.cs','UiTracer.cs','UnloadBudget.cs','ViewCulling.cs','WorldTweaks.cs')
    foreach ($name in $required) { if (-not (Test-Path -LiteralPath (Join-Path (Join-Path $PSScriptRoot 'src') $name))) { throw ('Missing src\' + $name + '. Extract the complete archive first.') } }
    if ($sources.Count -ne $required.Count) { throw ('Expected ' + $required.Count + ' source files in src, found ' + $sources.Count + '. Extract the complete archive into an empty folder.') }
    $refs = New-Object System.Collections.ArrayList
    [void](Add-UniqueRef $refs $BepInExDll)
    [void](Add-UniqueRef $refs (Join-Path $coreDir '0Harmony.dll'))
    foreach ($name in @('assembly_valheim.dll', 'assembly_utils.dll', 'netstandard.dll', 'SoftReferenceableAssets.dll')) {
        [void](Add-UniqueRef $refs (Join-Path $managedDir $name))
    }
    # Framework references come from Windows, never the game's mscorlib/System copies.
    $framework = [System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
    [void](Add-UniqueRef $refs (Join-Path $framework 'System.dll'))
    [void](Add-UniqueRef $refs (Join-Path $framework 'System.Core.dll'))
    Get-ChildItem -LiteralPath $managedDir -File -Filter 'UnityEngine*.dll' |
        Where-Object { $_.Name -ne 'UnityEngine.UI.dll' } |
        ForEach-Object { [void](Add-UniqueRef $refs $_.FullName) }
    $stage = Join-Path ([System.IO.Path]::GetTempPath()) ('AethelbornPerformance_' + [System.Guid]::NewGuid().ToString('N'))
    [void][System.IO.Directory]::CreateDirectory($stage)
    $output = Join-Path $stage 'AethelbornPerformance.dll'
    Write-Host ""
    Write-Host '[1/2] Compiling performance plugin to staging...' -ForegroundColor Yellow
    Compile-WithDependencies $sources $output $refs $managedDir $coreDir
    Write-Host '      Compile passed.' -ForegroundColor Green
    $targetFolder = Join-Path $pluginsDir 'AethelbornPerformance'
    $target = Join-Path $targetFolder 'AethelbornPerformance.dll'
    $backup = $null
    if (Test-Path -LiteralPath $target) {
        $backupFolder = Join-Path $bepInExDir ('AethelbornPerformanceBackups\' + [DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss_fff'))
        [void][System.IO.Directory]::CreateDirectory($backupFolder)
        $backup = Join-Path $backupFolder 'AethelbornPerformance.dll'
        Copy-Item -LiteralPath $target -Destination $backup
    }
    Write-Host '[2/2] Installing to the selected active profile...' -ForegroundColor Yellow
    [void][System.IO.Directory]::CreateDirectory($targetFolder)
    try { Copy-Item -LiteralPath $output -Destination $target -Force }
    catch { if ($backup) { Copy-Item -LiteralPath $backup -Destination $target -Force }; throw }
    Write-Host "Installed: $target" -ForegroundColor Green
    Write-Host 'Done. AA files and other plugins were not changed.' -ForegroundColor Green
}
catch { Write-Host ($_ | Out-String) -ForegroundColor Red; exit 1 }
finally { if ($stage -and (Test-Path -LiteralPath $stage)) { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue } }
