$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "IMMORTAL HEROES v0.22.2 - HUD AND ALTAR FIXES" -ForegroundColor Cyan
Write-Host "Protected build: all 8 DLLs compile in staging before the live profile is touched." -ForegroundColor Gray
Write-Host ""

Add-Type -AssemblyName System.Windows.Forms

function Pick-File([string]$title, [string]$filter) {
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Title = $title
    $dlg.Filter = $filter
    $dlg.Multiselect = $false

    if ($dlg.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        throw "No file selected."
    }

    return $dlg.FileName
}

function Need-File([string]$path, [string]$label) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "$label not found:`n$path"
    }
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
    [string]$sourcePath,
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

    $source = [System.IO.File]::ReadAllText($sourcePath)

    for ($attempt = 1; $attempt -le 12; $attempt++) {
        try {
            Add-Type `
                -TypeDefinition $source `
                -Language CSharp `
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

    throw "Could not compile $sourcePath after dependency retries."
}

try {
    Write-Host "1) Select BepInEx.dll from your ACTIVE profile's BepInEx\core folder." -ForegroundColor Yellow
    $bepInExDll = Pick-File "Select BepInEx.dll" "BepInEx.dll|BepInEx.dll"

    $coreDir = Split-Path -Parent $bepInExDll
    $bepInExDir = Split-Path -Parent $coreDir
    $pluginsDir = Join-Path $bepInExDir "plugins"

    Write-Host ""
    Write-Host "Looking for Jotunn.dll..." -ForegroundColor Yellow

    $jotunn = Get-ChildItem -LiteralPath $pluginsDir -Recurse -File -Filter "Jotunn.dll" -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($jotunn) {
        $jotunnDll = $jotunn.FullName
    }
    else {
        $jotunnDll = Pick-File "Select Jotunn.dll" "Jotunn.dll|Jotunn.dll"
    }

    Need-File $jotunnDll "Jotunn.dll"

    Write-Host ""
    Write-Host "2) Select UnityEngine.CoreModule.dll from Valheim\valheim_Data\Managed." -ForegroundColor Yellow
    $unityCore = Pick-File "Select UnityEngine.CoreModule.dll" "UnityEngine.CoreModule.dll|UnityEngine.CoreModule.dll"

    $managedDir = Split-Path -Parent $unityCore

    $assemblyValheim = Join-Path $managedDir "assembly_valheim.dll"
    $netstandard = Join-Path $managedDir "netstandard.dll"
    $softRefs = Join-Path $managedDir "SoftReferenceableAssets.dll"
    $textMeshPro = Join-Path $managedDir "Unity.TextMeshPro.dll"
    $harmony = Join-Path $coreDir "0Harmony.dll"

    Need-File $assemblyValheim "assembly_valheim.dll"
    Need-File $netstandard "netstandard.dll"
    Need-File $harmony "0Harmony.dll"

    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $uiAssetSource = Join-Path $scriptDir "ImmortalHeroesAssets\Cleric_Paladin_Reference.png"
    Need-File $uiAssetSource "Immortal Heroes shared Cleric/Paladin UI asset"
    Need-File (Join-Path $scriptDir "ImmortalHeroesAssets\Cleric_Priest_Artwork.png") "Immortal Heroes Priest UI asset"

    Need-File (Join-Path $scriptDir "ImmortalHeroesAssets\Altar_Background.png") "Altar background"
    Need-File (Join-Path $scriptDir "ImmortalHeroesAssets\Altar_ClassCards.png") "Altar class cards"
    Need-File (Join-Path $scriptDir "ImmortalHeroesAssets\Confirm_Plaque.png") "Altar button plaque"

    $coreSource = Join-Path $scriptDir "AlbedosCustomClasses.Core.cs"
    $runtimeSource = Join-Path $scriptDir "DragonsAltar.CombatRuntime.cs"
    $skillsSource = Join-Path $scriptDir "AlbedosCustomClasses.Skills.cs"
    $passivesSource = Join-Path $scriptDir "AlbedosCustomClasses.Passives.cs"
    $guardianSource = Join-Path $scriptDir "AlbedosCustomClasses.GuardianAngel.cs"
    $advancedSource = Join-Path $scriptDir "AlbedosCustomClasses.Advanced.cs"
    $sorcererSource = Join-Path $scriptDir "DragonsAltar.Sorcerer.cs"
    $devToolsSource = Join-Path $scriptDir "DragonsAltar.DevTools.cs"

    Need-File $coreSource "Core source"
    Need-File $runtimeSource "Combat Runtime source"
    Need-File $skillsSource "Skills source"
    Need-File $passivesSource "Passives source"
    Need-File $guardianSource "Guardian source"
    Need-File $advancedSource "Advanced source"
    Need-File $sorcererSource "Sorcerer source"
    Need-File $devToolsSource "Developer Tools source"

    if (-not (Test-Path -LiteralPath $pluginsDir)) {
        New-Item -ItemType Directory -Path $pluginsDir | Out-Null
    }

    $liveCoreOut = Join-Path $pluginsDir "AlbedosCustomClasses.dll"
    $liveRuntimeOut = Join-Path $pluginsDir "AlbedosCustomClasses.CombatRuntime.dll"
    $liveSkillsOut = Join-Path $pluginsDir "AlbedosCustomClasses.Skills.dll"
    $livePassivesOut = Join-Path $pluginsDir "AlbedosCustomClasses.Passives.dll"
    $liveGuardianOut = Join-Path $pluginsDir "AlbedosCustomClasses.GuardianAngel.dll"
    $liveAdvancedOut = Join-Path $pluginsDir "AlbedosCustomClasses.Advanced.dll"
    $liveSorcererOut = Join-Path $pluginsDir "DragonsAltar.Sorcerer.dll"
    $liveDevToolsOut = Join-Path $pluginsDir "DragonsAltar.DevTools.dll"

    $stageDir = Join-Path ([System.IO.Path]::GetTempPath()) ("AlbedosCustomClasses-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $stageDir | Out-Null
    $stageAssetDir = Join-Path $stageDir "ImmortalHeroesAssets"
    New-Item -ItemType Directory -Path $stageAssetDir | Out-Null
    # v0.15.0: stage every UI asset (backdrop, Tier buttons, Confirm plaque, selection ring).
    $uiAssetDir = Split-Path -Parent $uiAssetSource
    $uiAssets = Get-ChildItem -LiteralPath $uiAssetDir -File -Filter "*.png"
    foreach ($asset in $uiAssets) {
        Copy-Item -LiteralPath $asset.FullName -Destination (Join-Path $stageAssetDir $asset.Name) -Force
    }

    $coreOut = Join-Path $stageDir "AlbedosCustomClasses.dll"
    $runtimeOut = Join-Path $stageDir "AlbedosCustomClasses.CombatRuntime.dll"
    $skillsOut = Join-Path $stageDir "AlbedosCustomClasses.Skills.dll"
    $passivesOut = Join-Path $stageDir "AlbedosCustomClasses.Passives.dll"
    $guardianOut = Join-Path $stageDir "AlbedosCustomClasses.GuardianAngel.dll"
    $advancedOut = Join-Path $stageDir "AlbedosCustomClasses.Advanced.dll"
    $sorcererOut = Join-Path $stageDir "DragonsAltar.Sorcerer.dll"
    $devToolsOut = Join-Path $stageDir "DragonsAltar.DevTools.dll"

    $common = New-Object System.Collections.ArrayList
    [void](Add-UniqueRef $common $bepInExDll)
    [void](Add-UniqueRef $common $assemblyValheim)
    [void](Add-UniqueRef $common $netstandard)
    [void](Add-UniqueRef $common $softRefs)
    [void](Add-UniqueRef $common $textMeshPro)

    Get-ChildItem -LiteralPath $managedDir -File -Filter "UnityEngine*.dll" |
        ForEach-Object { [void](Add-UniqueRef $common $_.FullName) }

    Write-Host ""
    Write-Host "[1/8] Compiling shrine core to staging..." -ForegroundColor Yellow
    $coreRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$coreRefs.Add($r) }
    [void](Add-UniqueRef $coreRefs $jotunnDll)
    Compile-WithDependencies $coreSource $coreOut $coreRefs $managedDir $coreDir
    Write-Host "      Core compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[2/8] Compiling Combat Runtime to staging..." -ForegroundColor Yellow
    $runtimeRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$runtimeRefs.Add($r) }
    [void](Add-UniqueRef $runtimeRefs $harmony)
    Compile-WithDependencies $runtimeSource $runtimeOut $runtimeRefs $managedDir $coreDir
    Write-Host "      Combat Runtime compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[3/8] Compiling starter skills to staging..." -ForegroundColor Yellow
    $skillsRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$skillsRefs.Add($r) }
    [void](Add-UniqueRef $skillsRefs $harmony)
    [void](Add-UniqueRef $skillsRefs $runtimeOut)
    Compile-WithDependencies $skillsSource $skillsOut $skillsRefs $managedDir $coreDir
    Write-Host "      Starter skills compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[4/8] Compiling base-blessings compatibility DLL to staging..." -ForegroundColor Yellow
    $passivesRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$passivesRefs.Add($r) }
    [void](Add-UniqueRef $passivesRefs $runtimeOut)
    Compile-WithDependencies $passivesSource $passivesOut $passivesRefs $managedDir $coreDir
    Write-Host "      Base-blessings compatibility compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[5/8] Compiling Grand Sigil survival DLL to staging..." -ForegroundColor Yellow
    $guardianRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$guardianRefs.Add($r) }
    [void](Add-UniqueRef $guardianRefs $harmony)
    [void](Add-UniqueRef $guardianRefs $runtimeOut)
    Compile-WithDependencies $guardianSource $guardianOut $guardianRefs $managedDir $coreDir
    Write-Host "      Grand Sigil survival compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[6/8] Compiling advancements + Skillbook to staging..." -ForegroundColor Yellow
    $advancedRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$advancedRefs.Add($r) }
    [void](Add-UniqueRef $advancedRefs $harmony)
    [void](Add-UniqueRef $advancedRefs $coreOut)
    [void](Add-UniqueRef $advancedRefs $runtimeOut)
    [void](Add-UniqueRef $advancedRefs $skillsOut)
    Compile-WithDependencies $advancedSource $advancedOut $advancedRefs $managedDir $coreDir
    Write-Host "      Advancements compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[7/8] Compiling Sorcerer advancements to staging..." -ForegroundColor Yellow
    $sorcererRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$sorcererRefs.Add($r) }
    [void](Add-UniqueRef $sorcererRefs $harmony)
    [void](Add-UniqueRef $sorcererRefs $coreOut)
    [void](Add-UniqueRef $sorcererRefs $runtimeOut)
    [void](Add-UniqueRef $sorcererRefs $skillsOut)
    Compile-WithDependencies $sorcererSource $sorcererOut $sorcererRefs $managedDir $coreDir
    Write-Host "      Sorcerer advancements compile passed." -ForegroundColor Green

    Write-Host ""
    Write-Host "[8/8] Compiling Developer Tools to staging..." -ForegroundColor Yellow
    $devToolsRefs = New-Object System.Collections.ArrayList
    foreach ($r in $common) { [void]$devToolsRefs.Add($r) }
    Compile-WithDependencies $devToolsSource $devToolsOut $devToolsRefs $managedDir $coreDir
    Write-Host "      Developer Tools compile passed." -ForegroundColor Green

    Write-Host "All 8 DLLs compiled successfully. Installing staged DLL set..." -ForegroundColor Cyan

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"

    $liveDlls = @(
        $liveCoreOut,
        $liveRuntimeOut,
        $liveSkillsOut,
        $livePassivesOut,
        $liveGuardianOut,
        $liveAdvancedOut,
        $liveSorcererOut,
        $liveDevToolsOut
    )

    foreach ($oldDll in $liveDlls) {
        if (Test-Path -LiteralPath $oldDll) {
            Copy-Item -LiteralPath $oldDll -Destination ($oldDll + ".backup-" + $stamp) -Force
        }
    }

    Copy-Item -LiteralPath $coreOut -Destination $liveCoreOut -Force
    Copy-Item -LiteralPath $runtimeOut -Destination $liveRuntimeOut -Force
    Copy-Item -LiteralPath $skillsOut -Destination $liveSkillsOut -Force
    Copy-Item -LiteralPath $passivesOut -Destination $livePassivesOut -Force
    Copy-Item -LiteralPath $guardianOut -Destination $liveGuardianOut -Force
    Copy-Item -LiteralPath $advancedOut -Destination $liveAdvancedOut -Force
    Copy-Item -LiteralPath $sorcererOut -Destination $liveSorcererOut -Force
    Copy-Item -LiteralPath $devToolsOut -Destination $liveDevToolsOut -Force

    $liveAssetDir = Join-Path $pluginsDir "ImmortalHeroesAssets"
    if (-not (Test-Path -LiteralPath $liveAssetDir)) {
        New-Item -ItemType Directory -Path $liveAssetDir | Out-Null
    }
    foreach ($asset in $uiAssets) {
        $liveUiAsset = Join-Path $liveAssetDir $asset.Name
        if (Test-Path -LiteralPath $liveUiAsset) {
            Copy-Item -LiteralPath $liveUiAsset -Destination ($liveUiAsset + ".backup-" + $stamp) -Force
        }
        Copy-Item -LiteralPath (Join-Path $stageAssetDir $asset.Name) -Destination $liveUiAsset -Force
    }

    Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host ""
    Write-Host "SUCCESS" -ForegroundColor Green
    Write-Host ""
    Write-Host "Installed 8 isolated DLLs:" -ForegroundColor White
    Write-Host "  AlbedosCustomClasses.dll                  (altar/core)" -ForegroundColor White
    Write-Host "  AlbedosCustomClasses.CombatRuntime.dll    (cast lock / debuffs / buffs / mastery runtime)" -ForegroundColor White
    Write-Host "  AlbedosCustomClasses.Skills.dll           (starter skills + VFX)" -ForegroundColor White
    Write-Host "  AlbedosCustomClasses.Passives.dll         (compatibility shell; legacy passives disabled)" -ForegroundColor White
    Write-Host "  AlbedosCustomClasses.GuardianAngel.dll    (Priest Grand Sigil survival)" -ForegroundColor White
    Write-Host "  AlbedosCustomClasses.Advanced.dll         (Warrior/Cleric advancements + clean dynamic HUD)" -ForegroundColor White
    Write-Host "  DragonsAltar.Sorcerer.dll                 (Wizard/Spellcaster advancements + clean dynamic HUD)" -ForegroundColor White
    Write-Host "  DragonsAltar.DevTools.dll                 (developer skill tuning + world range/radius preview)" -ForegroundColor White
    Write-Host "  ImmortalHeroesAssets\*.png (Skill Tree art, Tier buttons, Confirm plaque, selection ring)" -ForegroundColor White
    Write-Host ""
    Write-Host "If a risky combat DLL fails, the staged installer will not replace your current live set." -ForegroundColor Cyan
    Write-Host ""
}
catch {
    if ($stageDir -and (Test-Path -LiteralPath $stageDir)) {
        Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host ""
    Write-Host "FAILED:" -ForegroundColor Red
    Write-Host ($_ | Out-String) -ForegroundColor Red
    Write-Host ""
    Write-Host "A compile failure before the install stage leaves the previous live DLL set untouched." -ForegroundColor Cyan
    Write-Host "Send this exact screen to ChatGPT." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
