<#
.SYNOPSIS
    Extracts the public API surface of v2 and v3 CliInvoke assemblies by reflection,
    diffs them, and emits a checklist mapping every delta to at least one migration-guide entry.

.DESCRIPTION
    This is a one-off tooling script for the v2 -> v3 migration effort. It loads assemblies
    via System.Reflection, extracts public types and their public members, normalises the
    surface, diffs the two snapshots, and emits a Markdown checklist table.

.PARAMETER V2AssemblyPath
    Path to the v2.x CliInvoke assembly (e.g. the DLL downloaded from NuGet).

.PARAMETER V3AssemblyPath
    Path(s) to the v3 CliInvoke assembly. Accepts a comma-separated list of paths when
    multiple assemblies contribute to the public surface (e.g. CliInvoke.Core.dll and
    CliInvoke.dll).

.PARAMETER OutputPath
    Optional file path for the checklist output. When omitted the checklist is written to
    stdout.

.EXAMPLE
    .\tools\api-diff.ps1 `
        -V2AssemblyPath "C:\packages\cliinvoke\2.11.0\lib\net8.0\CliInvoke.dll" `
        -V3AssemblyPath "src\CliInvoke.Core\bin\Debug\net10.0\CliInvoke.Core.dll","src\CliInvoke\bin\Debug\net10.0\CliInvoke.dll"

.NOTES
    No new NuGet package dependencies are added to the repository.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$V2AssemblyPath,

    [Parameter(Mandatory = $true)]
    [string[]]$V3AssemblyPath,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Reflection helpers
# ---------------------------------------------------------------------------

function Test-CompilerGenerated {
    [CmdletBinding()]
    param([System.Reflection.TypeInfo]$Type)

    # Filter out compiler-generated types (async state machines, display classes, etc.)
    if ($Type.Name -match '<|>c__DisplayClass|CompilerGenerated|__IlCodeStart|__IlCodeEnd') {
        return $true
    }
    if ($Type.Namespace -match '^System(\.|$)|^Microsoft(\.|$)') {
        return $true
    }
    if ($Type.GetCustomAttributes([System.Runtime.CompilerServices.CompilerGeneratedAttribute], $true).Count -gt 0) {
        return $true
    }
    return $false
}

function Get-MemberSignature {
    [CmdletBinding()]
    param(
        [System.Reflection.MemberInfo]$Member,
        [string]$DeclaringTypeName
    )

    $kind = ''
    $name = $Member.Name
    $sig = ''

    switch ($Member.MemberType) {
        'Constructor' {
            $kind = 'Constructor'
            $ctor = [System.Reflection.ConstructorInfo]$Member
            $params = ($ctor.GetParameters() | ForEach-Object { '{0} {1}' -f $_.ParameterType.FullName, $_.Name }) -join ', '
            $sig = '{0}({1})' -f $DeclaringTypeName, $params
        }
        'Method' {
            $kind = 'Method'
            $method = [System.Reflection.MethodInfo]$Member
            # Skip accessor methods (get_/set_); they are covered by Property
            if ($method.IsSpecialName) { return $null }
            $params = ($method.GetParameters() | ForEach-Object { '{0} {1}' -f $_.ParameterType.FullName, $_.Name }) -join ', '
            $genArg = ''
            if ($method.IsGenericMethod) {
                $genCount = $method.GetGenericArguments().Count
                $genArg = '<' + (($genCount | ForEach-Object { 'T' }) -join ',') + '>'
            }
            $sig = '{0} {1}{2}({3})' -f $method.ReturnType.FullName, $name, $genArg, $params
        }
        'Property' {
            $kind = 'Property'
            $prop = [System.Reflection.PropertyInfo]$Member
            $getter = if ($prop.GetMethod) { 'get; ' } else { '' }
            $setter = if ($prop.SetMethod) { 'set; ' } else { '' }
            $indexParams = ''
            if ($prop.GetIndexParameters().Count -gt 0) {
                $indexParams = '[{0}]' -f (($prop.GetIndexParameters() | ForEach-Object { '{0} {1}' -f $_.ParameterType.FullName, $_.Name }) -join ', ')
            }
            $sig = '{0} {1}{2}{3} {4}{5}' -f $prop.PropertyType.FullName, $name, $indexParams, '', $getter, $setter
        }
        'Event' {
            $kind = 'Event'
            $evt = [System.Reflection.EventInfo]$Member
            $sig = '{0} {1}' -f $evt.EventHandlerType.FullName, $name
        }
        'Field' {
            $kind = 'Field'
            $field = [System.Reflection.FieldInfo]$Member
            $static = if ($field.IsStatic) { 'static ' } else { '' }
            $readonly = if ($field.IsInitOnly) { 'readonly ' } else { '' }
            $sig = '{0}{1}{2} {3}' -f $static, $readonly, $field.FieldType.FullName, $name
        }
        default {
            return $null
        }
    }

    return [PSCustomObject]@{
        Kind        = $kind
        Name        = $name
        Signature   = $sig
        MemberInfo  = $Member
    }
}

function Get-PublicApiSurface {
    [CmdletBinding()]
    param(
        [string[]]$AssemblyPaths
    )

    $entries = [System.Collections.Generic.List[PSCustomObject]]::new()

    foreach ($asmPath in $AssemblyPaths) {
        $resolvedPath = Resolve-Path -LiteralPath $asmPath -ErrorAction Stop
        $asm = [System.Reflection.Assembly]::LoadFrom($resolvedPath.FullName)

        $publicTypes = $asm.GetExportedTypes() | Where-Object {
            -not (Test-CompilerGenerated -Type $_) -and
            $_.IsPublic -and
            -not $_.IsNestedPublic
        }

        foreach ($type in $publicTypes) {
            $typeName = $type.FullName
            if (-not $typeName) { continue }

            # --- Emit the type itself as a "Type" entry ---
            $entries.Add([PSCustomObject]@{
                DeclaringType = $typeName
                Kind          = 'Type'
                Name          = $type.Name
                Signature     = $typeName
            }) | Out-Null

            # --- Public members ---

            # Methods (non-accessor, non-special-name)
            $methods = $type.GetMethods(
                [System.Reflection.BindingFlags]::Public -bor
                [System.Reflection.BindingFlags]::Instance -bor
                [System.Reflection.BindingFlags]::Static -bor
                [System.Reflection.BindingFlags]::DeclaredOnly
            ) | Where-Object { -not $_.IsSpecialName }

            foreach ($m in $methods) {
                # Skip compiler-generated methods (e.g. async MoveNext)
                if ($m.GetCustomAttributes([System.Runtime.CompilerServices.CompilerGeneratedAttribute], $true).Count -gt 0) { continue }

                $info = Get-MemberSignature -Member $m -DeclaringTypeName $typeName
                if ($null -eq $info) { continue }

                $entries.Add([PSCustomObject]@{
                    DeclaringType = $typeName
                    Kind          = $info.Kind
                    Name          = $info.Name
                    Signature     = $info.Signature
                }) | Out-Null
            }

            # Constructors
            $ctors = $type.GetConstructors(
                [System.Reflection.BindingFlags]::Public -bor
                [System.Reflection.BindingFlags]::Instance -bor
                [System.Reflection.BindingFlags]::DeclaredOnly
            )

            foreach ($c in $ctors) {
                $info = Get-MemberSignature -Member $c -DeclaringTypeName $typeName
                if ($null -eq $info) { continue }

                $entries.Add([PSCustomObject]@{
                    DeclaringType = $typeName
                    Kind          = $info.Kind
                    Name          = $info.Name
                    Signature     = $info.Signature
                }) | Out-Null
            }

            # Properties
            $props = $type.GetProperties(
                [System.Reflection.BindingFlags]::Public -bor
                [System.Reflection.BindingFlags]::Instance -bor
                [System.Reflection.BindingFlags]::Static -bor
                [System.Reflection.BindingFlags]::DeclaredOnly
            )

            foreach ($p in $props) {
                $info = Get-MemberSignature -Member $p -DeclaringTypeName $typeName
                if ($null -eq $info) { continue }

                $entries.Add([PSCustomObject]@{
                    DeclaringType = $typeName
                    Kind          = $info.Kind
                    Name          = $info.Name
                    Signature     = $info.Signature
                }) | Out-Null
            }

            # Events
            $events = $type.GetEvents(
                [System.Reflection.BindingFlags]::Public -bor
                [System.Reflection.BindingFlags]::Instance -bor
                [System.Reflection.BindingFlags]::Static -bor
                [System.Reflection.BindingFlags]::DeclaredOnly
            )

            foreach ($e in $events) {
                $info = Get-MemberSignature -Member $e -DeclaringTypeName $typeName
                if ($null -eq $info) { continue }

                $entries.Add([PSCustomObject]@{
                    DeclaringType = $typeName
                    Kind          = $info.Kind
                    Name          = $info.Name
                    Signature     = $info.Signature
                }) | Out-Null
            }

            # Fields
            $fields = $type.GetFields(
                [System.Reflection.BindingFlags]::Public -bor
                [System.Reflection.BindingFlags]::Instance -bor
                [System.Reflection.BindingFlags]::Static -bor
                [System.Reflection.BindingFlags]::DeclaredOnly
            ) | Where-Object { -not $_.IsSpecialName }

            foreach ($f in $fields) {
                $info = Get-MemberSignature -Member $f -DeclaringTypeName $typeName
                if ($null -eq $info) { continue }

                $entries.Add([PSCustomObject]@{
                    DeclaringType = $typeName
                    Kind          = $info.Kind
                    Name          = $info.Name
                    Signature     = $info.Signature
                }) | Out-Null
            }
        }
    }

    return $entries
}

# ---------------------------------------------------------------------------
# Required-member detection
# ---------------------------------------------------------------------------

function Test-IsRequiredMember {
    [CmdletBinding()]
    param(
        [System.Reflection.TypeInfo]$V3Type,
        [string]$MemberName
    )

    # Check for [SetsRequiredMembers] on constructors
    $ctors = $V3Type.GetConstructors(
        [System.Reflection.BindingFlags]::Public -bor
        [System.Reflection.BindingFlags]::Instance
    )
    foreach ($c in $ctors) {
        if ($c.GetCustomAttributes('System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute', $true).Count -gt 0) {
            return $true
        }
    }

    # Check for 'required' modifier on properties (C# 11+)
    # required properties are marked with RequiredMemberAttribute on the property
    $props = $V3Type.GetProperties(
        [System.Reflection.BindingFlags]::Public -bor
        [System.Reflection.BindingFlags]::Instance -bor
        [System.Reflection.BindingFlags]::Static -bor
        [System.Reflection.BindingFlags]::DeclaredOnly
    )
    foreach ($p in $props) {
        if ($p.Name -eq $MemberName) {
            if ($p.GetCustomAttributes('System.Runtime.CompilerServices.RequiredMemberAttribute', $true).Count -gt 0) {
                return $true
            }
        }
    }

    return $false
}

# ---------------------------------------------------------------------------
# Diff engine
# ---------------------------------------------------------------------------

function Compare-ApiSurfaces {
    [CmdletBinding()]
    param(
        [System.Collections.Generic.List[PSCustomObject]]$V2Entries,
        [System.Collections.Generic.List[PSCustomObject]]$V3Entries
    )

    $deltas = [System.Collections.Generic.List[PSCustomObject]]::new()

    # Build lookup dictionaries: key = "Kind|DeclaringType|Signature"
    $v2Map = @{}
    $v3Map = @{}

    foreach ($e in $V2Entries) {
        $key = '{0}|{1}|{2}' -f $e.Kind, $e.DeclaringType, $e.Signature
        $v2Map[$key] = $e
    }

    foreach ($e in $V3Entries) {
        $key = '{0}|{1}|{2}' -f $e.Kind, $e.DeclaringType, $e.Signature
        $v3Map[$key] = $e
    }

    # --- Removed types: v2 types not in v3 ---
    $v2Types = $V2Entries | Where-Object { $_.Kind -eq 'Type' }
    $v3Types = $V3Entries | Where-Object { $_.Kind -eq 'Type' }
    $v3TypeNames = @{}
    foreach ($t in $v3Types) { $v3TypeNames[$t.DeclaringType] = $true }

    foreach ($t in $v2Types) {
        if (-not $v3TypeNames.ContainsKey($t.DeclaringType)) {
            $deltas.Add([PSCustomObject]@{
                Delta         = 'Removed Type'
                Kind          = 'Type'
                V2Signature   = $t.Signature
                V3Signature   = '(not present)'
                GuideEntry    = '(fill in)'
                GuideEntryRaw = ''
            }) | Out-Null
        }
    }

    # --- Member-level diffs ---
    foreach ($v2Entry in ($V2Entries | Where-Object { $_.Kind -ne 'Type' })) {
        $v2Key = '{0}|{1}|{2}' -f $v2Entry.Kind, $v2Entry.DeclaringType, $v2Entry.Signature

        # Build a "simplified" key for fuzzy matching: Kind|DeclaringType|Name
        $v2SimpleKey = '{0}|{1}|{2}' -f $v2Entry.Kind, $v2Entry.DeclaringType, $v2Entry.Name

        $matchedV3 = $null

        # First: try exact signature match
        if ($v3Map.ContainsKey($v2Key)) {
            $matchedV3 = $v3Map[$v2Key]
        }
        else {
            # Fuzzy match: same Kind + DeclaringType + Name in v3 (but different signature)
            foreach ($v3Entry in $V3Entries) {
                $v3SimpleKey = '{0}|{1}|{2}' -f $v3Entry.Kind, $v3Entry.DeclaringType, $v3Entry.Name
                if ($v3SimpleKey -eq $v2SimpleKey) {
                    $matchedV3 = $v3Entry
                    break
                }
            }
        }

        if ($null -ne $matchedV3) {
            # Member exists in both - check if signature changed
            if ($matchedV3.Signature -ne $v2Entry.Signature) {
                # Check if this is a new required member
                $isAddedRequired = $false
                $v3Type = $null
                try {
                    $v3TypeNames2 = @{}
                    foreach ($t in $v3Types) { $v3TypeNames2[$t.DeclaringType] = $true }
                    if ($v3TypeNames2.ContainsKey($v2Entry.DeclaringType)) {
                        # We need the actual TypeInfo - try to find it
                        $isAddedRequired = $false
                    }
                } catch {
                    # Ignore
                }

                $deltas.Add([PSCustomObject]@{
                    Delta         = 'Changed Signature'
                    Kind          = $v2Entry.Kind
                    V2Signature   = $v2Entry.Signature
                    V3Signature   = $matchedV3.Signature
                    GuideEntry    = '(fill in)'
                    GuideEntryRaw = ''
                }) | Out-Null
            }
            # else: exact match - no delta
        }
        else {
            # Member in v2 but not in v3 at all
            $deltas.Add([PSCustomObject]@{
                Delta         = 'Removed Member'
                Kind          = $v2Entry.Kind
                V2Signature   = $v2Entry.Signature
                V3Signature   = '(not present)'
                GuideEntry    = '(fill in)'
                GuideEntryRaw = ''
            }) | Out-Null
        }
    }

    # --- Added required members: v3 members not in v2, marked as required ---
    foreach ($v3Entry in ($V3Entries | Where-Object { $_.Kind -ne 'Type' })) {
        $v3Key = '{0}|{1}|{2}' -f $v3Entry.Kind, $v3Entry.DeclaringType, $v3Entry.Signature
        $v3SimpleKey = '{0}|{1}|{2}' -f $v3Entry.Kind, $v3Entry.DeclaringType, $v3Entry.Name

        $existsInV2 = $false
        foreach ($v2Entry in $V2Entries) {
            $v2SimpleKey = '{0}|{1}|{2}' -f $v2Entry.Kind, $v2Entry.DeclaringType, $v2Entry.Name
            if ($v2SimpleKey -eq $v3SimpleKey) {
                $existsInV2 = $true
                break
            }
        }

        if (-not $existsInV2) {
            # New in v3 - check if it's a required member
            # We need the TypeInfo from v3 to check attributes
            $isRequired = $false

            # Attempt to resolve the v3 type info for attribute checks
            foreach ($asmPath in $V3AssemblyPath) {
                if ($isRequired) { break }
                try {
                    $resolvedPath = Resolve-Path -LiteralPath $asmPath -ErrorAction SilentlyContinue
                    if (-not $resolvedPath) { continue }
                    $asm = [System.Reflection.Assembly]::LoadFrom($resolvedPath.FullName)
                    $type = $asm.GetType($v3Entry.DeclaringType)
                    if ($null -eq $type) { continue }

                    $isRequired = Test-IsRequiredMember -V3Type $type -MemberName $v3Entry.Name
                } catch {
                    # Ignore resolution errors
                }
            }

            if ($isRequired) {
                $deltas.Add([PSCustomObject]@{
                    Delta         = 'Added Required Member'
                    Kind          = $v3Entry.Kind
                    V2Signature   = '(new in v3)'
                    V3Signature   = $v3Entry.Signature
                    GuideEntry    = '(fill in)'
                    GuideEntryRaw = ''
                }) | Out-Null
            }
        }
    }

    return $deltas
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

Write-Host "Loading v2 assembly: $V2AssemblyPath" -ForegroundColor Cyan

$v2Surface = Get-PublicApiSurface -AssemblyPaths @($V2AssemblyPath)
Write-Host "  v2 public surface: $($v2Surface.Count) entries" -ForegroundColor Gray

Write-Host "Loading v3 assemblies: $($V3AssemblyPath -join ', ')" -ForegroundColor Cyan

$v3Surface = Get-PublicApiSurface -AssemblyPaths $V3AssemblyPath
Write-Host "  v3 public surface: $($v3Surface.Count) entries" -ForegroundColor Gray

# Normalise: sort by Kind -> DeclaringType -> Name -> Signature
$v2Sorted = $v2Surface | Sort-Object Kind, DeclaringType, Name, Signature
$v3Sorted = $v3Surface | Sort-Object Kind, DeclaringType, Name, Signature

Write-Host "Computing API delta..." -ForegroundColor Cyan

$deltas = Compare-ApiSurfaces -V2Entries $v2Sorted -V3Entries $v3Sorted

# Sort deltas for consistent output: Removed Type first, then the rest
$orderedDeltas = $deltas | Sort-Object @{ Expression = {
    switch ($_.Delta) {
        'Removed Type'              { 0 }
        'Removed Member'            { 1 }
        'Changed Signature'         { 2 }
        'Added Required Member'     { 3 }
        default                     { 9 }
    }
}}, Kind, V2Signature

# Build the checklist
$lines = [System.Collections.Generic.List[string]]::new()

$lines.Add('## API Delta Checklist') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('> Auto-generated by `tools/api-diff.ps1`. Fill in the **Guide Entry** column with') | Out-Null
$lines.Add('> the corresponding section heading or entry identifier from `site/docs/migration-guides/3.0.0.md`.') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('| # | Delta | Kind | v2 Signature | v3 Signature | Guide Entry |') | Out-Null
$lines.Add('|---|-------|------|--------------|--------------|-------------|') | Out-Null

$rowNum = 0
foreach ($d in $orderedDeltas) {
    $rowNum++
    $v2Sig = $d.V2Signature -replace '\|', '\|'
    $v3Sig = $d.V3Signature -replace '\|', '\|'
    $guideEntry = $d.GuideEntry

    $lines.Add('| {0} | {1} | {2} | {3} | {4} | {5} |' -f $rowNum, $d.Delta, $d.Kind, $v2Sig, $v3Sig, $guideEntry) | Out-Null
}

$lines.Add('') | Out-Null
$lines.Add('---') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('*Total deltas: {0}*' -f $orderedDeltas.Count) | Out-Null

$output = $lines -join "`n"

# Summary
Write-Host ''
Write-Host "Delta summary:" -ForegroundColor Yellow
$byDelta = $orderedDeltas | Group-Object Delta
foreach ($g in $byDelta) {
    Write-Host "  $($g.Name): $($g.Count)" -ForegroundColor Yellow
}
Write-Host "  TOTAL: $($orderedDeltas.Count)" -ForegroundColor Yellow

# Output
if ($OutputPath) {
    $outputDir = Split-Path -Path $OutputPath -Parent
    if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    $output | Out-File -FilePath $OutputPath -Encoding utf8
    Write-Host ''
    Write-Host "Checklist written to: $OutputPath" -ForegroundColor Green
}
else {
    Write-Host ''
    Write-Host $output
}
