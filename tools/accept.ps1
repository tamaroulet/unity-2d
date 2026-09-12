# MS2 受入スクリプト（人間の判断を挟まない）
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File C:\src\unity-2d\tools\accept.ps1
#
# 判定は dotnet/Unity の終了コードではなく、結果 XML の個別結果で行う。
# 終了コードは「1件も実行しなくても 0」を返すため、それを根拠にすると
# 「緑だった」のか「見ていなかった」のかが区別できない（MS1 の既往事故）。
#
# 終了コード: 0 = 合格 / 1 = 不合格（等価性が壊れている） / 2 = 検査系の故障

$ErrorActionPreference = "Stop"

$Repo     = "C:\src\unity-2d"
$Proj     = Join-Path $Repo "Game"
$Out      = "C:\src\.local\out\ms2"
$Stage    = Join-Path $Out "golden"          # MS2_GOLDEN_DIR として渡す
$Disclosed = Join-Path $Repo "tests\golden\golden_disclosed.json"

# 非開示はリポジトリの外に置く。Public リポジトリに入れない。
$Holdout   = "C:\src\.local\oracles\golden_holdout.json"

$CtrlPass = "AlwaysPasses_ControlGroup"
$CtrlFail = "control_must_fail"
$Required = @("GameRulesSOTests", "GoldenMasterEquivalence")

# skip の扱い（案A）。
# 対象テストの skip は 0 件でなければならない。全体の skip は既知のベースラインを
# 上回ったら不合格にする。「skip を無視する」にすると MS1 の既往事故
# （skip が分母から外れて偽の緑になった）をそのまま再現するため、上限で縛る。
#
# ベースライン 20 件の内訳（MS2 とは無関係の既存要因。3 クラスに付いた属性）:
#   GameFlowControllerTests.cs:17       [Explicit("PlayMode 曳光弾で置換予定")] 13
#   GameFlowControllerRelicTests.cs:15  [Explicit("PlayMode 曳光弾で置換予定")]  4
#   GameMonteCarloSimulationTests.cs:19 [Explicit("PlayMode 曳光弾で置換予定")]  3
$SkipBaseline = 20

# ---------------------------------------------------------------- 前提

foreach ($p in @($Repo, $Proj, $Disclosed, $Holdout)) {
    if (-not (Test-Path $p)) { Write-Host "検査系故障: $p がありません" -ForegroundColor Red; exit 2 }
}
if (@(Get-Process Unity -ErrorAction SilentlyContinue).Count -gt 0) {
    Write-Host "検査系故障: Unity が起動中です。閉じてから再実行してください。" -ForegroundColor Red; exit 2
}

$verFile = Join-Path $Proj "ProjectSettings\ProjectVersion.txt"
$unityVer = ((Get-Content $verFile | Where-Object { $_ -match "^m_EditorVersion:" }) -split ":\s*")[1].Trim()
$UnityExe = @(
    "C:\Program Files\Unity\Hub\Editor\$unityVer\Editor\Unity.exe",
    "D:\Program Files\Unity\Hub\Editor\$unityVer\Editor\Unity.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $UnityExe) { Write-Host "検査系故障: Unity.exe が見つかりません ($unityVer)" -ForegroundColor Red; exit 2 }

New-Item -ItemType Directory -Force -Path $Stage | Out-Null

# ---------------------------------------------------------------- 実行と読み取り

function Invoke-UnityTests($tag) {
    $xml = Join-Path $Out "$tag.xml"
    $log = Join-Path $Out "$tag.log"
    if (Test-Path $xml) { Remove-Item $xml -Force }

    $env:MS2_GOLDEN_DIR = $Stage

    # Unity.exe は GUI サブシステムのバイナリ。呼び出し演算子（&）では終了を待たない。
    Start-Process -FilePath $UnityExe -ArgumentList @(
        "-batchmode", "-nographics",
        "-projectPath", $Proj,
        "-runTests", "-testPlatform", "EditMode",
        "-testResults", $xml,
        "-logFile", $log
    ) -Wait -PassThru -NoNewWindow | Out-Null

    if (-not (Test-Path $xml)) { return $null }
    return $xml
}

function Read-Results($xmlPath) {
    # Get-Content は行配列を返すため [xml] キャストが壊れる（NUnit の出力に < を含む
    # テキストがあると属性として解釈されて落ちる）。ファイルから直接読ませる。
    $x = New-Object System.Xml.XmlDocument
    try { $x.Load($xmlPath) }
    catch { Write-Host "検査系故障: 結果 XML を解析できません: $($_.Exception.Message)" -ForegroundColor Red; exit 2 }
    $map = @{}
    foreach ($n in $x.SelectNodes("//test-case")) {
        $map[$n.fullname] = $n.result
    }
    return $map
}

function Names($map, $needle) {
    return @($map.Keys | Where-Object { $_ -like "*$needle*" })
}

function Outcome($map, $needle) {
    $n = @($map.Keys | Where-Object { $_ -like "*$needle*" })
    if ($n.Count -eq 0) { return $null }
    return $map[$n[0]]
}

function RealFailures($map) {
    # Skipped を Failed と混ぜない。不一致だけを数える。
    return @($map.Keys | Where-Object { $map[$_] -eq "Failed" -and $_ -notlike "*$CtrlFail*" })
}

function SkippedNames($map) {
    return @($map.Keys | Where-Object { $map[$_] -eq "Skipped" -or $map[$_] -eq "Inconclusive" })
}

function Purge-Holdout {
    Get-ChildItem $Stage -Filter "golden_holdout*.json" -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-Item $_.FullName -Force }
}

# ---------------------------------------------------------------- [1] 開示のみ

Write-Host "[1] 開示ゴールデン 18 件" -ForegroundColor Cyan
Purge-Holdout
Copy-Item $Disclosed (Join-Path $Stage "golden_disclosed.json") -Force

$xml1 = Invoke-UnityTests "accept_disclosed"
if (-not $xml1) { Write-Host "検査系故障: 結果 XML が生成されませんでした（コンパイル失敗の可能性）" -ForegroundColor Red; exit 2 }
$r1 = Read-Results $xml1

$ng = @()
if ((Outcome $r1 $CtrlPass) -ne "Passed") { $ng += "必ず通る対照群が通らなかった" }
foreach ($req in $Required) {
    if ((Names $r1 $req).Count -eq 0) { $ng += "$req が結果に出ていない（実行されていない）" }
}
$disc = Names $r1 "golden_disclosed_"
if ($disc.Count -eq 0) { $ng += "開示ゴールデンが1件も実行されていない" }
$leak = Names $r1 "golden_holdout_"
if ($leak.Count -gt 0) { $ng += "非開示ゴールデンが漏れ込んでいる（$($leak.Count) 件）" }
$skipped = SkippedNames $r1
foreach ($req in $Required) {
    $reqSkipped = @($skipped | Where-Object { $_ -like "*$req*" })
    if ($reqSkipped.Count -gt 0) { $ng += "$req が skip されている（$($reqSkipped.Count) 件）" }
}
if ($skipped.Count -gt $SkipBaseline) {
    $ng += "skip が既知の $SkipBaseline 件を超えた（$($skipped.Count) 件）。新たに無効化されたテストがある"
}

if ($ng.Count -gt 0) {
    Write-Host "検査系故障:" -ForegroundColor Red
    $ng | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 2
}

$fail1 = RealFailures $r1
Write-Host ("    実行 {0} 件 / 開示ゴールデン {1} 件 / 不一致 {2} 件" -f $r1.Count, $disc.Count, $fail1.Count)
if ($fail1.Count -gt 0) {
    Write-Host "不合格: 開示ゴールデンと一致しません" -ForegroundColor Red
    $fail1 | Select-Object -First 10 | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Purge-Holdout
    exit 1
}

# ---------------------------------------------------------------- [2] 非開示を投入

Write-Host "[2] 非開示ゴールデン 11 件（対照群を含む）" -ForegroundColor Cyan
try {
    Copy-Item $Holdout (Join-Path $Stage "golden_holdout.json") -Force

    $xml2 = Invoke-UnityTests "accept_holdout"
    if (-not $xml2) { Write-Host "検査系故障: 結果 XML が生成されませんでした" -ForegroundColor Red; exit 2 }
    $r2 = Read-Results $xml2

    $hold = Names $r2 "golden_holdout_"
    if ($hold.Count -eq 0) { Write-Host "検査系故障: 非開示ゴールデンが実行されていない" -ForegroundColor Red; exit 2 }

    $o = Outcome $r2 $CtrlFail
    if ($null -eq $o) { Write-Host "検査系故障: 必ず落ちる対照群が実行されていない" -ForegroundColor Red; exit 2 }
    if ($o -ne "Failed") { Write-Host "検査系故障: 必ず落ちる対照群が落ちなかった（$o）" -ForegroundColor Red; exit 2 }

    $fail2 = RealFailures $r2
    Write-Host ("    実行 {0} 件 / 非開示ゴールデン {1} 件 / 不一致 {2} 件" -f $r2.Count, $hold.Count, $fail2.Count)
    if ($fail2.Count -gt 0) {
        Write-Host "不合格: 非開示ゴールデンと一致しません" -ForegroundColor Red
        $fail2 | Select-Object -First 10 | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        exit 1
    }
}
finally {
    # 非開示は必ず消す。中断しても残さない。
    Purge-Holdout
}

Write-Host ""
Write-Host "合格: GameRulesSO のラッパー化前後で挙動が一致しています。" -ForegroundColor Green
exit 0
