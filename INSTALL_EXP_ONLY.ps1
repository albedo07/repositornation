$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -AssemblyName System.Windows.Forms
function ChooseDll($title, $filename) {
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Title = $title
    $dlg.Filter = "$filename|$filename"
    if ($dlg.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { throw "No file selected." }
    return $dlg.FileName
}
Write-Host "1. Select BepInEx.dll FROM YOUR ACTIVE GALE PROFILE (not the game installation)." -ForegroundColor Yellow
Write-Host "   The folder will contain BepInEx\core\BepInEx.dll."
$bepDll = ChooseDll "Active Gale profile: BepInEx\\core\\BepInEx.dll" "BepInEx.dll"
$core = Split-Path -Parent $bepDll
if ((Split-Path -Leaf $core) -ne "core") { throw "Select the DLL inside BepInEx\\core." }
$bep = Split-Path -Parent $core
$plugins = Join-Path $bep "plugins"
if (!(Test-Path (Join-Path $plugins "AlbedosCustomClasses.Advanced.dll"))) {
    throw "WRONG PROFILE: no existing Aethelborn Advancements DLL found at $plugins"
}
Write-Host "2. Select UnityEngine.CoreModule.dll from Valheim\\valheim_Data\\Managed." -ForegroundColor Yellow
$unityDll = ChooseDll "Valheim game: valheim_Data\\Managed\\UnityEngine.CoreModule.dll" "UnityEngine.CoreModule.dll"
$managed = Split-Path -Parent $unityDll
$src = Join-Path $here "Aethelborn.Experience.cs"
$out = Join-Path $plugins "Aethelborn.Experience.dll"
foreach ($file in @($src, (Join-Path $managed "assembly_valheim.dll"), (Join-Path $core "0Harmony.dll"))) {
    if (!(Test-Path $file)) { throw "Missing dependency: $file" }
}
$refs = New-Object System.Collections.ArrayList
foreach ($name in @("assembly_valheim.dll", "netstandard.dll", "Unity.TextMeshPro.dll")) {
    $file = Join-Path $managed $name; if (Test-Path $file) { [void]$refs.Add($file) }
}
foreach ($name in @("BepInEx.dll", "0Harmony.dll")) { [void]$refs.Add((Join-Path $core $name)) }
Get-ChildItem $managed -File -Filter "UnityEngine*.dll" | ForEach-Object { [void]$refs.Add($_.FullName) }
$temp = Join-Path $env:TEMP ("aethelborn-exp-" + [guid]::NewGuid().ToString("N") + ".dll")
try {
    Add-Type -TypeDefinition ([IO.File]::ReadAllText($src)) -Language CSharp -ReferencedAssemblies ([string[]]$refs.ToArray()) -OutputAssembly $temp -OutputType Library -IgnoreWarnings -ErrorAction Stop
    if (!(Test-Path $temp)) { throw "EXP DLL compile produced no output." }
    if (Test-Path $out) { Copy-Item $out ($out + ".backup-" + (Get-Date -Format "yyyyMMdd-HHmmss")) -Force }
    Copy-Item $temp $out -Force
    Write-Host "EXP installed to ACTIVE profile: $out" -ForegroundColor Green
    if (!(Test-Path $out)) { throw "EXP plugin not copied." }
} finally { if (Test-Path $temp) { Remove-Item $temp -Force } }
Write-Host "On next launch verify: Loading [Aethelborn Ascended - Experience ...]" -ForegroundColor Cyan
Read-Host "Press Enter to close"
