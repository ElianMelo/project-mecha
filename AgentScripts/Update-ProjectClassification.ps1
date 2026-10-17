param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))

$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$gitRoot = $ProjectRoot.Replace('\', '/')
$paths = @(& git -c "safe.directory=$gitRoot" --no-optional-locks -C $ProjectRoot ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate maintained project files.' }

# These are the only images with documented AI provenance. Sprite assets derived
# from them retain that provenance; neither filenames nor commit authors prove AI use.
$aiArt = @('CyberpunkCity.png', 'IndustrialPlatforms.png', 'CyberpunkFarSky.png',
    'CyberpunkMiddleSkyline.png', 'CyberpunkNearRooftops.png', 'CityBackdrop.asset',
    'FloorPlatform.asset', 'FloatingPlatform.asset', 'CyberpunkFarSky.asset',
    'CyberpunkMiddleSkyline.asset', 'CyberpunkNearRooftops.asset')
$manifestPath = 'Docs/ProjectFileClassification.csv'
if ($paths -notcontains $manifestPath) { $paths += $manifestPath }

$rows = foreach ($path in ($paths | Sort-Object -Unique)) {
    if ($path -ne $manifestPath -and !(Test-Path -LiteralPath (Join-Path $ProjectRoot $path) -PathType Leaf)) { continue }
    $basePath = $path -replace '\.meta$', ''
    $category = 'Others'; $origin = 'Unverified'; $status = 'Support'
    $kind = 'Project support'; $evidence = 'No reliable authorship record; do not infer AI origin.'
    if ($basePath -like 'Assets/_Project/Art/Environment/*' -and
        $aiArt -contains ($basePath.Split('/')[-1])) {
        $category = 'AI Generated'; $origin = 'AI Generated'; $status = 'Placeholder'
        $kind = 'Environment image or derived native Sprite'
        $evidence = 'Docs/ScenarioArtPrompts.md; Docs/ThreeLayerParallaxPrompts.md'
    } elseif ($basePath -match '^Assets/(_Project/Audio|8 Sci fi Tracks|AlpineAudio|Gamemaster Audio - Pro Sound Collection|Sci Fi Ambiances|Sci-Fi Alarm SFX|UI - Mechanical)/.+\.(wav|ogg|mp3|aiff?)$') {
        $category = 'Placeholder'; $status = 'Placeholder'; $origin = 'Supplied / imported; upstream AI origin unverified'
        $kind = 'Imported audio artwork'
        $evidence = 'Latest supplied audio packages; Docs/AudioClipOrigins.csv records selected clip moves with retained GUIDs. No final audio approval.'
    } elseif ($basePath -match '^Assets/_Project/(Audio|Scripts/Audio)(/|$)' -and
        (Test-Path -LiteralPath (Join-Path $ProjectRoot $basePath) -PathType Container)) {
        $category = 'Others'; $status = 'Support'; $kind = 'Audio organization folder'
    } elseif ($basePath -match '^Assets/(8 Sci fi Tracks|AlpineAudio|Gamemaster Audio - Pro Sound Collection|Sci Fi Ambiances|Sci-Fi Alarm SFX|UI - Mechanical)(/|$)') {
        $origin = 'Supplied / imported; upstream AI origin unverified'; $kind = 'Imported audio package support or folder'
    } elseif ($basePath -match '^Assets/(FishNet|Plugins)(/|$)') {
        $origin = 'Third-party dependency'; $kind = 'Vendor code, asset, demo or license'
        $evidence = 'Imported vendor tree; retain upstream notices. AI provenance not audited.'
    } elseif ($basePath -match '^Assets/(TextMesh Pro|TutorialInfo)(/|$)' -or $basePath -eq 'Assets/Readme.asset') {
        $origin = 'Unity package or template'; $kind = 'Package resource, sample or template'
        $evidence = 'Unity/TMP template resources; retain upstream notices.'
    } elseif ($basePath -match '^Assets/_Project/Animations/Interface/Aim(Scan|Capture|Tracked)\.anim$') {
        $category = 'Placeholder'; $status = 'Placeholder'; $origin = 'Unverified'
        $kind = 'Native authored reticle animation'
        $evidence = 'Docs/GameAdjustments.md; authored through Unity animation APIs, not an imported clip or final-product approval.'
    } elseif ($basePath -match '^Assets/_Project/(Models/|Animations/.+\.anim$)' -or
        $basePath -eq 'Assets/_Project/VFX/Textures/SpeedWind.png') {
        $category = 'Placeholder'; $status = 'Placeholder'; $origin = 'Supplied / imported; upstream AI origin unverified'
        $kind = 'Character, animation, weapon or supplied effect image'
        $evidence = 'Docs/PresentationAndCamera.md; supplied asset lineage, not a final-product approval.'
    } elseif ($basePath -match '^Assets/Editor(/|$)' -or $basePath -match '^AgentScripts(/|$)') {
        $kind = 'Editor / maintenance tooling'
    } elseif ($basePath -match '^Assets/Tests(/|$)') {
        $kind = 'Existing automated tests (explicit request required to run or modify)'
    } elseif ($basePath -match '^Assets/_Project/NewSkills(/|$)' -or $basePath -match '^\.agents(/|$)') {
        $origin = 'Imported tooling'; $kind = 'Agent skill / reference'
        $evidence = 'Tooling instructions; not game content or proof of AI-authored code.'
    } elseif ($basePath -match '^Assets/_Project/Scripts/.+\.cs$') {
        $category = 'Code'; $status = 'Provisional'; $kind = 'Gameplay implementation'
        $evidence = 'User classification correction 2026-10-16: Placeholder describes unfinished art, not source code.'
    } elseif ($basePath -match '^Assets/(_Project|Scenes|Settings)/.+\.prefab$') {
        $category = 'Prefabs'; $status = 'Provisional'; $kind = 'Reusable game assembly'
        $evidence = 'Contains references to provisional art; prefab configuration is not itself placeholder artwork.'
    } elseif ($basePath -match '^Assets/(_Project|Scenes|Settings)/.+\.unity$') {
        $category = 'Scenes'; $status = 'Provisional'; $kind = 'Scene composition'
    } elseif ($basePath -match '^Assets/(_Project|Scenes|Settings)/.+\.(asset|mixer|inputactions|controller|overrideController|physicsMaterial|physicMaterial|renderTexture|shadersubgraph)$') {
        $category = 'Authored Data'; $status = 'Provisional'; $kind = 'Settings, definition or native configuration'
        $evidence = 'User classification correction 2026-10-16: authored configuration is separate from unfinished art.'
    } elseif ($basePath -match '^Assets/Sci Fi Gun Heavy(/|$)') {
        $category = 'Placeholder'; $status = 'Placeholder'; $origin = 'Supplied / imported; upstream AI origin unverified'
        $kind = 'Imported weapon art'; $evidence = 'Existing Sci Fi Gun Heavy package supplied in workspace; no final art approval.'
        if ($basePath -like '*.prefab') { $category = 'Prefabs'; $status = 'Provisional'; $kind = 'Imported weapon prefab' }
        if ($basePath -like '*.unity') { $category = 'Scenes'; $status = 'Provisional'; $kind = 'Imported weapon demonstration scene' }
        if ($basePath -match '\.(txt|pdf)$') { $category = 'Others'; $status = 'Support'; $kind = 'Imported package documentation' }
    } elseif ($basePath -match '^Assets/_Project/Settings(/|$)' -and
        (Test-Path -LiteralPath (Join-Path $ProjectRoot $basePath) -PathType Container)) {
        $category = 'Others'; $status = 'Support'; $kind = 'Authored settings category folder'
        $evidence = 'Docs/SettingsCategories.md; folder organization is not unfinished artwork.'
    } elseif ($basePath -match '^Assets/(_Project|Scenes|Settings)(/|$)') {
        $category = 'Placeholder'; $status = 'Placeholder'; $kind = 'Unfinished visual content'
        $evidence = 'User decision 2026-10-16: current game content exists for testing; no final product.'
    } elseif ($basePath -match '^Assets/DefaultPrefabObjects\.asset$') {
        $origin = 'Unity / FishNet generated metadata'; $kind = 'Generated network prefab registry'
        $evidence = 'Engine/tool-generated does not mean AI-generated.'
    } elseif ($basePath -match '^SourceArchive(/|$)') {
        $status = 'Archive'; $kind = 'Preserved source archive'
        $origin = 'Supplied / imported; upstream AI origin unverified'
        $evidence = 'Docs/PresentationAndCamera.md; recoverable unused supplied model files.'
    } elseif ($basePath -match '^Docs(/|$)' -or $basePath -in @('README.md', 'AGENTS.md')) {
        $kind = 'Documentation / reference capture'
        $evidence = 'Project process or captured reference; AI authorship not inferred from rendered game art.'
    } elseif ($basePath -match '^Packages(/|$)') {
        $origin = 'Unity package tooling'; $kind = 'Dependency manifest / lockfile'
        $evidence = 'Dependency declarations; resolved package caches are excluded.'
    } elseif ($basePath -match '^ProjectSettings(/|$)') {
        $origin = 'Unity configuration'; $kind = 'Project settings'
        $evidence = 'Engine-serialized configuration; not AI provenance.'
    } elseif ($basePath -match '^\.codex(/|$)' -or $basePath -in @('.gitignore', '.vsconfig')) {
        $kind = 'Workspace / tool configuration'
    }
    $labels = if ($category -eq 'AI Generated') { 'AI Generated;Placeholder' } else { $category }
    [pscustomobject]@{
        Path = $path; Category = $category; Status = $status; Origin = $origin
        Kind = if ($path.EndsWith('.meta')) { "Unity metadata for: $kind" } else { $kind }
        Labels = $labels; Evidence = $evidence
    }
}
$rows | Export-Csv -LiteralPath (Join-Path $ProjectRoot $manifestPath) -NoTypeInformation -Encoding utf8
$rows | Group-Object Category, Status | Select-Object Count, Name | Format-Table -AutoSize
Write-Output "Classified $($rows.Count) maintained files. Regenerate after file additions or deletions."
