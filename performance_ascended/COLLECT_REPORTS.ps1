param([string]$BepInExDll)
$ErrorActionPreference = 'Stop'
$stage = $null
try {
    if (Get-Process -Name valheim -ErrorAction SilentlyContinue) { throw 'Close Valheim after your captures, then collect reports so the log is complete.' }
    if ([string]::IsNullOrWhiteSpace($BepInExDll)) {
        Add-Type -AssemblyName System.Windows.Forms
        $picker = New-Object System.Windows.Forms.OpenFileDialog
        $picker.Title = 'Select BepInEx.dll from the SAME active profile used for installation'
        $picker.Filter = 'BepInEx.dll|BepInEx.dll'
        $picker.Multiselect = $false
        try {
            if ($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { exit 1 }
            $BepInExDll = $picker.FileName
        }
        finally { $picker.Dispose() }
    }
    if (-not (Test-Path -LiteralPath $BepInExDll -PathType Leaf)) { throw 'Selected BepInEx.dll does not exist.' }
    $bep = Split-Path -Parent (Split-Path -Parent ([System.IO.Path]::GetFullPath($BepInExDll)))
    $reports = Join-Path $bep 'AethelbornPerformanceReports'
    if (-not (Test-Path -LiteralPath $reports)) { throw 'No performance report folder found. Record a capture in a world first.' }
    $files = @(Get-ChildItem -LiteralPath $reports -File | Where-Object { $_.Extension -eq '.txt' -or $_.Extension -eq '.csv' })
    if ($files.Count -eq 0) { throw 'No reports found. Record a capture first.' }
    $stage = Join-Path ([System.IO.Path]::GetTempPath()) ('AethelbornReports_' + [System.Guid]::NewGuid().ToString('N'))
    $captureFolder = Join-Path $stage 'captures'
    [void][System.IO.Directory]::CreateDirectory($captureFolder)
    foreach ($file in $files) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $captureFolder $file.Name) }
    foreach ($relative in @('LogOutput.log', 'config\albedo.aethelborn.performance.cfg')) {
        $path = Join-Path $bep $relative
        if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $stage ([System.IO.Path]::GetFileName($path))) }
    }
    @('Performance reports and BepInEx log collected for manual sharing.', 'Includes in-game position samples, hardware, mod names, config and game log.', 'No saves or plugin binaries are collected. Nothing is uploaded automatically.') | Set-Content -LiteralPath (Join-Path $stage 'CONTENTS.txt') -Encoding UTF8
    $outDir = Join-Path $PSScriptRoot 'SupportReports'
    [void][System.IO.Directory]::CreateDirectory($outDir)
    $output = Join-Path $outDir ('AethelbornPerformanceReports_' + [DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss_fff') + '.zip')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $output)
    Write-Host "Reports collected: $output" -ForegroundColor Green
    Write-Host 'Send this ZIP back with a short description of each marker/action. Nothing was uploaded automatically.'
}
catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }
finally { if ($stage -and (Test-Path -LiteralPath $stage)) { Remove-Item -LiteralPath $stage -Recurse -Force } }
