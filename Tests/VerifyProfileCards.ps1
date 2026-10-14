param(
    [string] $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-Condition([bool] $Condition, [string] $Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function Get-UnityYamlDocuments([string] $Path) {
    $source = Get-Content -LiteralPath $Path -Raw
    $pattern = '(?ms)^--- !u!(?<class>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n(?<body>.*?)(?=^--- !u!|\z)'
    foreach ($match in [regex]::Matches($source, $pattern)) {
        [pscustomobject]@{
            Class = [int] $match.Groups['class'].Value
            Id = $match.Groups['id'].Value
            Body = $match.Groups['body'].Value
        }
    }
}

function Get-YamlScalar([string] $Body, [string] $Field) {
    $fieldPattern = [regex]::Escape($Field)
    $match = [regex]::Match($Body, "(?m)^\s*${fieldPattern}:\s*(?<value>[^\r\n]+)")
    if (-not $match.Success) { return $null }
    return $match.Groups['value'].Value.Trim()
}

function Get-YamlReferenceId([string] $Body, [string] $Field) {
    $value = Get-YamlScalar $Body $Field
    if ($value -and $value -match '\{fileID:\s*(-?\d+)') { return $Matches[1] }
    return $null
}

function Get-Vector([string] $Body, [string] $Field, [int] $Dimensions) {
    $value = Get-YamlScalar $Body $Field
    if (-not $value) { return $null }
    $number = '[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?'
    if ($Dimensions -eq 3) {
        $pattern = "\{x:\s*(?<x>$number),\s*y:\s*(?<y>$number),\s*z:\s*(?<z>$number)\}"
    } else {
        $pattern = "\{x:\s*(?<x>$number),\s*y:\s*(?<y>$number)\}"
    }
    $match = [regex]::Match($value, $pattern)
    if (-not $match.Success) { return $null }
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    $result = @{ x = [double]::Parse($match.Groups['x'].Value, $culture); y = [double]::Parse($match.Groups['y'].Value, $culture) }
    if ($Dimensions -eq 3) { $result.z = [double]::Parse($match.Groups['z'].Value, $culture) }
    return $result
}

function Get-AssetGuid([string] $Path) {
    $metaPath = "$Path.meta"
    Assert-Condition (Test-Path -LiteralPath $metaPath) "Missing Unity meta file: $metaPath"
    $match = [regex]::Match((Get-Content -LiteralPath $metaPath -Raw), '(?m)^guid:\s*(?<guid>[0-9a-f]{32})\s*$')
    Assert-Condition $match.Success "No GUID found in $metaPath"
    return $match.Groups['guid'].Value
}

function Get-RootRenderer([object[]] $Documents, [string] $PrefabName) {
    $root = $Documents | Where-Object { $_.Class -eq 1 -and (Get-YamlScalar $_.Body 'm_Name') -eq $PrefabName } | Select-Object -First 1
    Assert-Condition ($null -ne $root) "Could not find root GameObject '$PrefabName'."
    $renderers = @($Documents | Where-Object { $_.Class -eq 23 -and (Get-YamlReferenceId $_.Body 'm_GameObject') -eq $root.Id })
    Assert-Condition ($renderers.Count -gt 0) "'$PrefabName' has no root MeshRenderer."
    return @{ Root = $root; Renderer = $renderers[0] }
}

function Assert-WhiteUrpMaterial([string] $PrefabPath, [string] $PrefabName, [string] $UrpShaderGuid, [string] $WhiteMaterialGuid) {
    $docs = @(Get-UnityYamlDocuments $PrefabPath)
    $parts = Get-RootRenderer $docs $PrefabName
    $materialReference = [regex]::Match($parts.Renderer.Body, '(?m)^\s*-\s*\{fileID:\s*\d+,\s*guid:\s*(?<guid>[0-9a-f]{32}),\s*type:\s*\d+\}')
    Assert-Condition $materialReference.Success "'$PrefabName' root renderer has no serialized material asset reference."
    $materialGuid = $materialReference.Groups['guid'].Value
    Assert-Condition ($materialGuid -ne '00000000000000000000000000000000') "'$PrefabName' still uses a built-in material."

    $materialPath = Join-Path $ProjectRoot 'Assets/White_URP.mat'
    if ($materialGuid -eq $WhiteMaterialGuid) {
        $materialPath = Join-Path $ProjectRoot 'Assets/White_URP.mat'
    } else {
        $candidate = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File -Filter '*.mat' |
            Where-Object { (Get-AssetGuid $_.FullName) -eq $materialGuid } | Select-Object -First 1
        Assert-Condition ($null -ne $candidate) "'$PrefabName' material GUID $materialGuid does not resolve to a project .mat asset."
        $materialPath = $candidate.FullName
    }

    $material = Get-Content -LiteralPath $materialPath -Raw
    $shader = [regex]::Match($material, '(?m)^\s*m_Shader:\s*\{fileID:\s*\d+,\s*guid:\s*(?<guid>[0-9a-f]{32}),')
    Assert-Condition $shader.Success "No shader reference found in $materialPath"
    Assert-Condition ($shader.Groups['guid'].Value -eq $UrpShaderGuid) "'$PrefabName' material does not use the project's URP shader."

    $baseColor = [regex]::Match($material, '(?m)^\s*-\s*_BaseColor:\s*\{r:\s*(?<r>[^,]+),\s*g:\s*(?<g>[^,]+),\s*b:\s*(?<b>[^,]+),\s*a:\s*(?<a>[^}]+)\}')
    if ($baseColor.Success) {
        $culture = [System.Globalization.CultureInfo]::InvariantCulture
        $white = @('r', 'g', 'b', 'a') | ForEach-Object { [double]::Parse($baseColor.Groups[$_].Value, $culture) }
        Assert-Condition (@($white | Where-Object { $_ -lt 0.98 }).Count -eq 0) "'$PrefabName' material base color is not white."
    }

    Write-Output "PASS: $PrefabName root material resolves to URP with a white base color."
}

function Assert-TextFitsQuad([string] $PrefabPath, [string] $PrefabName) {
    $docs = @(Get-UnityYamlDocuments $PrefabPath)
    $parts = Get-RootRenderer $docs $PrefabName
    $rootTransform = $docs | Where-Object { $_.Class -in @(4, 224) -and (Get-YamlReferenceId $_.Body 'm_GameObject') -eq $parts.Root.Id } | Select-Object -First 1
    Assert-Condition ($null -ne $rootTransform) "'$PrefabName' has no root Transform."
    $rootTransformId = $rootTransform.Id
    $gameObjects = @{}
    foreach ($doc in $docs | Where-Object { $_.Class -eq 1 }) {
        $gameObjects[$doc.Id] = Get-YamlScalar $doc.Body 'm_Name'
    }

    foreach ($textName in @('NameText', 'DescriptionText')) {
        $textObject = $docs | Where-Object { $_.Class -eq 1 -and (Get-YamlScalar $_.Body 'm_Name') -eq $textName } | Select-Object -First 1
        Assert-Condition ($null -ne $textObject) "'$PrefabName' is missing $textName."
        $textId = $textObject.Id
        $hasTmp = $docs | Where-Object { $_.Class -eq 114 -and (Get-YamlReferenceId $_.Body 'm_GameObject') -eq $textId -and $_.Body -match 'Unity\.TextMeshPro::TMPro\.TextMeshPro' } | Select-Object -First 1
        Assert-Condition ($null -ne $hasTmp) "'$PrefabName/$textName' has no world-space TMP component."
        $rect = $docs | Where-Object { $_.Class -eq 224 -and (Get-YamlReferenceId $_.Body 'm_GameObject') -eq $textId } | Select-Object -First 1
        Assert-Condition ($null -ne $rect) "'$PrefabName/$textName' has no RectTransform."
        Assert-Condition ((Get-YamlReferenceId $rect.Body 'm_Father') -eq $rootTransformId) "'$PrefabName/$textName' must remain a direct child of the card quad for bounds validation."

        $anchorMin = Get-Vector $rect.Body 'm_AnchorMin' 2
        $anchorMax = Get-Vector $rect.Body 'm_AnchorMax' 2
        $anchored = Get-Vector $rect.Body 'm_AnchoredPosition' 2
        $sizeDelta = Get-Vector $rect.Body 'm_SizeDelta' 2
        $pivot = Get-Vector $rect.Body 'm_Pivot' 2
        $localPosition = Get-Vector $rect.Body 'm_LocalPosition' 3
        Assert-Condition (($null -ne $anchorMin) -and ($null -ne $anchorMax) -and ($null -ne $anchored) -and ($null -ne $sizeDelta) -and ($null -ne $pivot) -and ($null -ne $localPosition)) "'$PrefabName/$textName' has incomplete RectTransform data."

        # The built-in Quad is one unit square in its local XY plane. Resolve
        # RectTransform anchors against that square, then verify the whole text
        # rectangle stays inside its edges.
        $sizeX = ($anchorMax.x - $anchorMin.x) + $sizeDelta.x
        $sizeY = ($anchorMax.y - $anchorMin.y) + $sizeDelta.y
        $pivotX = -0.5 + $anchorMin.x + ($anchorMax.x - $anchorMin.x) * $pivot.x + $anchored.x
        $pivotY = -0.5 + $anchorMin.y + ($anchorMax.y - $anchorMin.y) * $pivot.y + $anchored.y
        $minX = $pivotX - $sizeX * $pivot.x
        $maxX = $minX + $sizeX
        $minY = $pivotY - $sizeY * $pivot.y
        $maxY = $minY + $sizeY
        $epsilon = 0.005
        Assert-Condition (($sizeX -gt 0) -and ($sizeY -gt 0) -and ($minX -ge (-0.5 - $epsilon)) -and ($maxX -le (0.5 + $epsilon)) -and ($minY -ge (-0.5 - $epsilon)) -and ($maxY -le (0.5 + $epsilon))) "'$PrefabName/$textName' RectTransform exceeds the 1×1 card quad bounds."
        Assert-Condition ($localPosition.z -lt -0.001) "'$PrefabName/$textName' is not offset in front of the card quad toward -Z."
    }

    Write-Output "PASS: $PrefabName name and description fit inside the quad and sit in front of it."
}

$profileCardPath = Join-Path $ProjectRoot 'Assets/Prefabs/ProfileCard_TEMP.prefab'
$profilePickupPath = Join-Path $ProjectRoot 'Assets/Prefabs/ProfileWorldPickup_TEMP.prefab'
$whiteMaterialPath = Join-Path $ProjectRoot 'Assets/White_URP.mat'
foreach ($path in @($profileCardPath, $profilePickupPath, $whiteMaterialPath)) {
    Assert-Condition (Test-Path -LiteralPath $path) "Required asset is missing: $path"
}

$whiteMaterialGuid = Get-AssetGuid $whiteMaterialPath
$whiteMaterial = Get-Content -LiteralPath $whiteMaterialPath -Raw
$whiteShader = [regex]::Match($whiteMaterial, '(?m)^\s*m_Shader:\s*\{fileID:\s*\d+,\s*guid:\s*(?<guid>[0-9a-f]{32}),')
Assert-Condition $whiteShader.Success "Could not read URP shader from Assets/White_URP.mat."
$urpShaderGuid = $whiteShader.Groups['guid'].Value

foreach ($asset in @(@{ Name = 'ProfileCard_TEMP'; Path = $profileCardPath }, @{ Name = 'ProfileWorldPickup_TEMP'; Path = $profilePickupPath })) {
    Assert-WhiteUrpMaterial $asset.Path $asset.Name $urpShaderGuid $whiteMaterialGuid
    Assert-TextFitsQuad $asset.Path $asset.Name
}

$scenePath = Join-Path $ProjectRoot 'Assets/Scenes/Office_Level1.unity'
$scene = Get-Content -LiteralPath $scenePath -Raw
$cardGuid = Get-AssetGuid $profileCardPath
$pickupGuid = Get-AssetGuid $profilePickupPath
Assert-Condition ($scene -match "profileCardPrefab: \{fileID: \d+, guid: $cardGuid,") 'Office_Level1 sidequest is no longer wired to the validated ProfileCard_TEMP prefab.'
Assert-Condition ($scene -match "profilePaperPrefab: \{fileID: \d+, guid: $pickupGuid,") 'Office_Level1 collected-profile panel is no longer wired to the validated ProfileWorldPickup_TEMP prefab.'
Write-Output 'PASS: Office_Level1 uses the validated prefabs for sidequest and collected-profile cards.'
