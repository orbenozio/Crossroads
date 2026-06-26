<#
.SYNOPSIS
    Scaffold a layered Unity project skeleton (folders + asmdef dependency graph +
    Packages/manifest.json + ProjectSettings/ProjectVersion.txt) from a declarative spec.

.DESCRIPTION
    This is the reusable core of a "build Unity project skeleton" skill. It is intentionally
    content-agnostic: it lays down the structural boilerplate that is fiddly to get right by
    hand (assembly-definition graph, package manifest, editor-version pin, folder tree). The
    actual C# and game content are written on top of the skeleton separately.

    The skeleton it produces honours a one-directional assembly dependency graph: each entry in
    -Assemblies declares its own name, folder, and the assemblies it is allowed to reference.
    Because Unity enforces asmdef references at compile time, the dependency rules become a
    compiler-enforced contract rather than a convention.

    Re-running is safe: existing files are overwritten with freshly generated content, and the
    generator never touches files outside the paths it manages.

.PARAMETER ProjectRoot
    Absolute path to the Unity project root (the folder that contains Assets/, Packages/,
    ProjectSettings/).

.PARAMETER UnityVersion
    Editor version string written to ProjectSettings/ProjectVersion.txt (e.g. 6000.3.7f1).

.PARAMETER UnityChangeset
    Editor changeset hash for the m_EditorVersionWithRevision line (e.g. 696ec25a53d1).

.EXAMPLE
    pwsh ./tools/New-UnitySkeleton.ps1 -ProjectRoot 'C:\Dev\UnityProjects\Crossroads'

.NOTES
    Files are written as UTF-8 without BOM (Unity prefers no BOM for asmdef/json).
    To package this as a skill, parameterise $Packages / $Assemblies / $Folders below; they are
    kept inline here as the Crossroads default so the script is a runnable, self-documenting template.
#>
[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityVersion = '6000.3.7f1',
    [string]$UnityChangeset = '696ec25a53d1'
)

$ErrorActionPreference = 'Stop'

# --- declarative spec (parameterise these for the generic skill) ----------------------------

# UPM packages. Module packages (com.unity.modules.*) are always version 1.0.0.
$Packages = [ordered]@{
    'com.unity.inputsystem'             = '1.14.0'
    'com.unity.nuget.newtonsoft-json'   = '3.2.1'
    'com.unity.test-framework'          = '1.6.0'
    'com.unity.ugui'                    = '2.0.0'
    'com.unity.modules.imgui'           = '1.0.0'
    'com.unity.modules.jsonserialize'   = '1.0.0'
    'com.unity.modules.ui'              = '1.0.0'
    'com.unity.modules.uielements'      = '1.0.0'
    'com.unity.modules.unitywebrequest' = '1.0.0'
}

# The layered assembly graph. Path is relative to Assets/. References are by assembly name.
# Engine has zero references back to UI/Games => the golden rule is compiler-enforced.
$Assemblies = @(
    [pscustomobject]@{
        Name = 'Crossroads.Engine'; Path = 'Engine/Runtime'; RootNamespace = 'Crossroads.Engine'
        References = @(); IncludePlatforms = @(); PrecompiledReferences = @()
        OverrideReferences = $false; AutoReferenced = $true; DefineConstraints = @(); NoEngineReferences = $false
    }
    [pscustomobject]@{
        Name = 'Crossroads.UI'; Path = 'Engine/UI'; RootNamespace = 'Crossroads.UI'
        References = @('Crossroads.Engine', 'Unity.InputSystem'); IncludePlatforms = @(); PrecompiledReferences = @()
        OverrideReferences = $false; AutoReferenced = $true; DefineConstraints = @(); NoEngineReferences = $false
    }
    [pscustomobject]@{
        Name = 'Crossroads.Engine.Tests'; Path = 'Engine/Tests'; RootNamespace = 'Crossroads.Engine.Tests'
        References = @('Crossroads.Engine', 'UnityEngine.TestRunner', 'UnityEditor.TestRunner')
        IncludePlatforms = @('Editor'); PrecompiledReferences = @('nunit.framework.dll')
        OverrideReferences = $true; AutoReferenced = $false; DefineConstraints = @('UNITY_INCLUDE_TESTS'); NoEngineReferences = $false
    }
    [pscustomobject]@{
        Name = 'Crossroads.Game.Template'; Path = 'Games/_Template'; RootNamespace = 'Crossroads.Game.Template'
        References = @('Crossroads.Engine', 'Crossroads.UI'); IncludePlatforms = @(); PrecompiledReferences = @()
        OverrideReferences = $false; AutoReferenced = $true; DefineConstraints = @(); NoEngineReferences = $false
    }
)

# Extra (otherwise empty) folders that are part of the structure but get their content later.
$Folders = @(
    'Assets/Engine/UI/Prefabs'
    'Assets/Games/_Template/Scenes'
    'Assets/Games/_Template/Content'
    'Assets/Games/_Template/Theme'
    'Assets/Games/_Template/Art'
    'Assets/Games/NewbornKing'
    'Assets/Games/RefugeeRoad'
    'Assets/Shared'
)

# --- helpers --------------------------------------------------------------------------------

function New-Dir([string]$Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Path $Path -Force | Out-Null }
}

function Write-TextFile([string]$Path, [string]$Content) {
    New-Dir (Split-Path -Parent $Path)
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Format-JsonStringArray($Items) {
    if (-not $Items -or $Items.Count -eq 0) { return '[]' }
    $quoted = $Items | ForEach-Object { '"' + $_ + '"' }
    return '[' + ($quoted -join ', ') + ']'
}

function New-Asmdef($Asm) {
    $lines = @(
        '{'
        '    "name": "' + $Asm.Name + '",'
        '    "rootNamespace": "' + $Asm.RootNamespace + '",'
        '    "references": ' + (Format-JsonStringArray $Asm.References) + ','
        '    "includePlatforms": ' + (Format-JsonStringArray $Asm.IncludePlatforms) + ','
        '    "excludePlatforms": [],'
        '    "allowUnsafeCode": false,'
        '    "overrideReferences": ' + $Asm.OverrideReferences.ToString().ToLower() + ','
        '    "precompiledReferences": ' + (Format-JsonStringArray $Asm.PrecompiledReferences) + ','
        '    "autoReferenced": ' + $Asm.AutoReferenced.ToString().ToLower() + ','
        '    "defineConstraints": ' + (Format-JsonStringArray $Asm.DefineConstraints) + ','
        '    "versionDefines": [],'
        '    "noEngineReferences": ' + $Asm.NoEngineReferences.ToString().ToLower()
        '}'
    )
    return ($lines -join "`n") + "`n"
}

# --- execute --------------------------------------------------------------------------------

Write-Host "Scaffolding Unity skeleton at: $ProjectRoot"

# ProjectVersion.txt
$projectVersion = "m_EditorVersion: $UnityVersion`nm_EditorVersionWithRevision: $UnityVersion ($UnityChangeset)`n"
Write-TextFile (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') $projectVersion
Write-Host "  + ProjectSettings/ProjectVersion.txt ($UnityVersion)"

# Packages/manifest.json
$depLines = @()
foreach ($k in $Packages.Keys) { $depLines += '    "' + $k + '": "' + $Packages[$k] + '"' }
$manifest = "{`n  `"dependencies`": {`n" + ($depLines -join ",`n") + "`n  }`n}`n"
Write-TextFile (Join-Path $ProjectRoot 'Packages/manifest.json') $manifest
Write-Host "  + Packages/manifest.json ($($Packages.Count) packages)"

# Assemblies (folders + asmdef)
foreach ($asm in $Assemblies) {
    $asmDir = Join-Path $ProjectRoot (Join-Path 'Assets' $asm.Path)
    New-Dir $asmDir
    $asmPath = Join-Path $asmDir ($asm.Name + '.asmdef')
    Write-TextFile $asmPath (New-Asmdef $asm)
    $refTxt = if ($asm.References.Count) { $asm.References -join ', ' } else { '(none)' }
    Write-Host "  + $($asm.Path)/$($asm.Name).asmdef  ->  $refTxt"
}

# Structural folders (kept in git via .gitkeep; Unity will generate .meta on import)
foreach ($f in $Folders) {
    $full = Join-Path $ProjectRoot $f
    New-Dir $full
    $keep = Join-Path $full '.gitkeep'
    if (-not (Get-ChildItem $full -Force -ErrorAction SilentlyContinue)) {
        Write-TextFile $keep ''
    }
}
Write-Host "  + $($Folders.Count) structural folders"

Write-Host "Done."
