# MS3 ゴールデンマスタ採取（Phase 2）
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File C:\src\unity-2d\tools\ms3_capture_golden.ps1
#
# 現行の MetaPointResolverSO の挙動をゴールデンマスタとして機械採取する。
# 人間は期待値を書かない。
#
# 順序は固定（フェイルセーフ）:
#   ① 採取テストを配置 → ② Unity 実行 → ③ 同語反復 6 条件の検査
#   → ④ 二分と対照群の付与 → ⑤ 配置 → ⑥ 配置先の検証
#   → ⑦ 検証が通った場合のみ採取テストを削除
# ⑥を通さずに⑦へ進むと採取データごと消える。
#
# 終了コード: 0 = 成功 / 2 = 失敗（採取テストは残す）

$ErrorActionPreference = "Stop"

# 設定は game-harness に移設した（2026-09-13）。ここに写しを置くと片方だけ直して食い違う。
#   projects\unity-2d\project.json   リポジトリの位置
#   projects\unity-2d\pipeline.json  採取・パイプラインの設定（旧 tools\ms3.config.json）
$HarnessDir = if ($env:GAME_HARNESS_DIR) { $env:GAME_HARNESS_DIR } else { "C:\src\game-harness" }
$ProjectDir = Join-Path $HarnessDir "projects\unity-2d"
$ConfigPath  = Join-Path $ProjectDir "pipeline.json"
$ProjectPath = Join-Path $ProjectDir "project.json"
foreach ($p in @($ConfigPath, $ProjectPath)) {
    if (-not (Test-Path $p)) { throw "設定が見つかりません: $p" }
}
$CFG     = Get-Content $ConfigPath  -Raw -Encoding UTF8 | ConvertFrom-Json
$PROJECT = Get-Content $ProjectPath -Raw -Encoding UTF8 | ConvertFrom-Json

$Repo     = $PROJECT.repo_dir
$Proj     = Join-Path $Repo $PROJECT.unity_project_subdir
$Staging  = $CFG.paths.out_dir
$Oracles  = $CFG.paths.oracles_dir
$GC       = $CFG.golden_capture
$TA       = $CFG.tautology
$CtrlFail = $CFG.control_groups.must_fail
$CaptureCs   = Join-Path $Repo ($CFG.rel.capture_test -replace "/", "\")
$DisclosedDst = Join-Path $Repo ($CFG.rel.disclosed_golden -replace "/", "\")
$HoldoutDst   = Join-Path $Oracles $CFG.oracle_filenames.holdout

function Write-Utf8($Path, $Text) {
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding $false))
}

function Fail($msg) {
    Write-Host ""
    Write-Host "採取に失敗しました: $msg" -ForegroundColor Red
    Write-Host "  採取テストは残してあります: $CaptureCs"
    exit 2
}

# ---------------------------------------------------------------- 0. 前提
Write-Host "[0] 前提の確認" -ForegroundColor Cyan
if (@(Get-Process Unity -ErrorAction SilentlyContinue).Count -gt 0) {
    Fail "Unity が起動中です。閉じてから再実行してください。"
}
$verFile = Join-Path $Proj "ProjectSettings\ProjectVersion.txt"
$unityVer = ((Get-Content $verFile | Where-Object { $_ -match "^m_EditorVersion:" }) -split ":\s*")[1].Trim()
$UnityExe = @(
    "C:\Program Files\Unity\Hub\Editor\$unityVer\Editor\Unity.exe",
    "D:\Program Files\Unity\Hub\Editor\$unityVer\Editor\Unity.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $UnityExe) { Fail "Unity.exe が見つかりません ($unityVer)" }
Write-Host "  Unity $unityVer"

# ---------------------------------------------------------------- 1. 採取テストの配置
Write-Host "[1] 採取テストを配置" -ForegroundColor Cyan

$template = @'
// SPDX-AI-Disclosure: ai-generated
// 使い捨て。ms3_capture_golden.ps1 が生成し、採取の検証が通ったら削除する。
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Core;
using Game.Features.MetaProgression;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class MetaPointCaptureTests
    {
        private static string N(int v) { return v.ToString(CultureInfo.InvariantCulture); }
        private static string B(bool v) { return v ? "true" : "false"; }

        private static string Ids(IReadOnlyList<int> v)
        {
            if (v == null) { return "[]"; }
            var parts = new List<string>();
            for (int i = 0; i < v.Count; i++) { parts.Add(N(v[i])); }
            return "[" + string.Join(",", parts.ToArray()) + "]";
        }

        private struct Coeff { public int Turn, Skill, Boss, Clear; }

        private static Coeff BaseCoeff()
        {
            return new Coeff { Turn = __C_TURN__, Skill = __C_SKILL__, Boss = __C_BOSS__, Clear = __C_CLEAR__ };
        }

        private static string CoeffJson(Coeff c)
        {
            return "{\"turnBonusPerTurn\":" + N(c.Turn)
                 + ",\"skillBonusMultiplier\":" + N(c.Skill)
                 + ",\"bossDefeatedBonus\":" + N(c.Boss)
                 + ",\"gameClearBonus\":" + N(c.Clear) + "}";
        }

        private static string CalcCase(string id, Coeff c, bool stateNull,
                                       int turn, int sta, int skl, int men,
                                       bool clear, int boss)
        {
            MetaPointResolverSO so = MetaPointResolverSOFactory.Create(c.Turn, c.Skill, c.Boss, c.Clear);
            try
            {
                GameState st = stateNull ? null : new GameState
                {
                    CurrentTurn = turn, Stamina = sta, Skill = skl, Mental = men
                };
                int points = so.CalculateEarnedPoints(st, clear, boss);

                return "{\"id\":\"" + id + "\",\"control\":\"\",\"method\":\"calc\","
                     + "\"coeff\":" + CoeffJson(c) + ","
                     + "\"in\":{\"stateNull\":" + B(stateNull)
                     + ",\"currentTurn\":" + N(turn) + ",\"stamina\":" + N(sta)
                     + ",\"skill\":" + N(skl) + ",\"mental\":" + N(men)
                     + ",\"isGameClear\":" + B(clear) + ",\"bossDefeatedCount\":" + N(boss) + "},"
                     + "\"out\":{\"points\":" + N(points) + "}}";
            }
            finally { Object.DestroyImmediate(so); }
        }

        private static string ApplyCase(string id, Coeff c, bool profileNull,
                                        int avail, int totalEarned, int runs,
                                        int[] unlocked, int earned)
        {
            MetaPointResolverSO so = MetaPointResolverSOFactory.Create(c.Turn, c.Skill, c.Boss, c.Clear);
            try
            {
                MetaProfileState p = profileNull ? null : new MetaProfileState
                {
                    AvailableMetaPoints = avail,
                    TotalEarnedMetaPoints = totalEarned,
                    TotalRunsCompleted = runs,
                    UnlockedIds = unlocked
                };
                MetaProfileState r = so.ApplyRunResult(p, earned);

                string outJson = (r == null)
                    ? "{\"resultNull\":true}"
                    : "{\"resultNull\":false,\"availableMetaPoints\":" + N(r.AvailableMetaPoints)
                      + ",\"totalEarnedMetaPoints\":" + N(r.TotalEarnedMetaPoints)
                      + ",\"totalRunsCompleted\":" + N(r.TotalRunsCompleted)
                      + ",\"unlockedIds\":" + Ids(r.UnlockedIds) + "}";

                return "{\"id\":\"" + id + "\",\"control\":\"\",\"method\":\"apply\","
                     + "\"coeff\":" + CoeffJson(c) + ","
                     + "\"in\":{\"profileNull\":" + B(profileNull)
                     + ",\"availableMetaPoints\":" + N(avail)
                     + ",\"totalEarnedMetaPoints\":" + N(totalEarned)
                     + ",\"totalRunsCompleted\":" + N(runs)
                     + ",\"unlockedIds\":" + Ids(unlocked)
                     + ",\"earnedPoints\":" + N(earned) + "},"
                     + "\"out\":" + outJson + "}";
            }
            finally { Object.DestroyImmediate(so); }
        }

        [Test]
        public void CaptureGoldenMaster()
        {
            string outDir = System.Environment.GetEnvironmentVariable("MS3_GOLDEN_OUT");
            Assert.IsFalse(string.IsNullOrEmpty(outDir), "MS3_GOLDEN_OUT が設定されていません");
            Directory.CreateDirectory(outDir);

            int[] probe = { __PROBE__ };
            Coeff bc = BaseCoeff();

            const int bTurn = __B_TURN__, bSta = __B_STA__, bSkl = __B_SKL__, bMen = __B_MEN__, bBoss = __B_BOSS__;
            const bool bClear = __B_CLEAR__;
            const int aAvail = __A_AVAIL__, aTotal = __A_TOTAL__, aRuns = __A_RUNS__, aEarned = __A_EARNED__;

            var cases = new List<string>();

            // --- CalculateEarnedPoints ---
            cases.Add(CalcCase("calc_base", bc, false, bTurn, bSta, bSkl, bMen, bClear, bBoss));
            cases.Add(CalcCase("calc_null_state", bc, true, bTurn, bSta, bSkl, bMen, bClear, bBoss));
            cases.Add(CalcCase("calc_not_clear", bc, false, bTurn, bSta, bSkl, bMen, false, bBoss));

            // 基準から1軸ずつ動かすだけでは全項が同時に 0 にならず、最終の Max(0, 合計) に
            // 負値経路から到達できない。全入力を非正にしたケースを1件だけ明示的に置く。
            cases.Add(CalcCase("calc_all_nonpositive", bc, false, -1, bSta, -1, bMen, false, -1));

            for (int i = 0; i < probe.Length; i++)
            {
                int v = probe[i];
                cases.Add(CalcCase("calc_turn_" + i, bc, false, v, bSta, bSkl, bMen, bClear, bBoss));
                cases.Add(CalcCase("calc_skill_" + i, bc, false, bTurn, bSta, v, bMen, bClear, bBoss));
                cases.Add(CalcCase("calc_boss_" + i, bc, false, bTurn, bSta, bSkl, bMen, bClear, v));
            }

            // 係数感度: 基準から 1 本だけ変える。出力が変わらなければその係数は使われていない。
            cases.Add(CalcCase("calc_coeff_turn", new Coeff { Turn = __V_TURN__, Skill = bc.Skill, Boss = bc.Boss, Clear = bc.Clear },
                               false, bTurn, bSta, bSkl, bMen, bClear, bBoss));
            cases.Add(CalcCase("calc_coeff_skill", new Coeff { Turn = bc.Turn, Skill = __V_SKILL__, Boss = bc.Boss, Clear = bc.Clear },
                               false, bTurn, bSta, bSkl, bMen, bClear, bBoss));
            cases.Add(CalcCase("calc_coeff_boss", new Coeff { Turn = bc.Turn, Skill = bc.Skill, Boss = __V_BOSS__, Clear = bc.Clear },
                               false, bTurn, bSta, bSkl, bMen, bClear, bBoss));
            cases.Add(CalcCase("calc_coeff_clear", new Coeff { Turn = bc.Turn, Skill = bc.Skill, Boss = bc.Boss, Clear = __V_CLEAR__ },
                               false, bTurn, bSta, bSkl, bMen, bClear, bBoss));

            int[] coeffEdges = { __C_EDGES__ };
            for (int i = 0; i < coeffEdges.Length; i++)
            {
                int e = coeffEdges[i];
                cases.Add(CalcCase("calc_coeff_edge_" + i, new Coeff { Turn = e, Skill = e, Boss = e, Clear = e },
                                   false, bTurn, bSta, bSkl, bMen, bClear, bBoss));
            }

            // --- ApplyRunResult ---
            int[] baseUnlocked = { __A_UNLOCKED__ };
            cases.Add(ApplyCase("apply_base", bc, false, aAvail, aTotal, aRuns, baseUnlocked, aEarned));
            cases.Add(ApplyCase("apply_null_profile", bc, true, aAvail, aTotal, aRuns, baseUnlocked, aEarned));

            for (int i = 0; i < probe.Length; i++)
            {
                int v = probe[i];
                cases.Add(ApplyCase("apply_avail_" + i, bc, false, v, aTotal, aRuns, baseUnlocked, aEarned));
                cases.Add(ApplyCase("apply_total_" + i, bc, false, aAvail, v, aRuns, baseUnlocked, aEarned));
                cases.Add(ApplyCase("apply_runs_" + i, bc, false, aAvail, aTotal, v, baseUnlocked, aEarned));
                cases.Add(ApplyCase("apply_earned_" + i, bc, false, aAvail, aTotal, aRuns, baseUnlocked, v));
            }

            cases.Add(ApplyCase("apply_unlocked_empty", bc, false, aAvail, aTotal, aRuns, new int[0], aEarned));
            cases.Add(ApplyCase("apply_unlocked_one", bc, false, aAvail, aTotal, aRuns, new int[] { 7 }, aEarned));
            cases.Add(ApplyCase("apply_unlocked_dup", bc, false, aAvail, aTotal, aRuns, new int[] { 7, 7 }, aEarned));

            var sb = new StringBuilder();
            sb.Append("{\n  \"source\": \"MetaPointResolverSO\",\n  \"cases\": [\n    ");
            sb.Append(string.Join(",\n    ", cases.ToArray()));
            sb.Append("\n  ]\n}\n");

            File.WriteAllText(Path.Combine(outDir, "golden_metapoint_all.json"),
                              sb.ToString(), new UTF8Encoding(false));

            Debug.Log("[MS3] captured " + cases.Count + " cases -> " + outDir);
        }
    }
}
'@

$b  = $GC.calc_base
$a  = $GC.apply_base
$bc = $GC.base_coeff
$vc = $GC.coeff_variants

$cs = $template.
    Replace('__PROBE__',      [string]::Join(', ', $GC.probe_values)).
    Replace('__C_TURN__',     [string]$bc.turnBonusPerTurn).
    Replace('__C_SKILL__',    [string]$bc.skillBonusMultiplier).
    Replace('__C_BOSS__',     [string]$bc.bossDefeatedBonus).
    Replace('__C_CLEAR__',    [string]$bc.gameClearBonus).
    Replace('__V_TURN__',     [string]$vc.turnBonusPerTurn).
    Replace('__V_SKILL__',    [string]$vc.skillBonusMultiplier).
    Replace('__V_BOSS__',     [string]$vc.bossDefeatedBonus).
    Replace('__V_CLEAR__',    [string]$vc.gameClearBonus).
    Replace('__C_EDGES__',    [string]::Join(', ', $GC.coeff_edge_values)).
    Replace('__B_TURN__',     [string]$b.currentTurn).
    Replace('__B_STA__',      [string]$b.stamina).
    Replace('__B_SKL__',      [string]$b.skill).
    Replace('__B_MEN__',      [string]$b.mental).
    Replace('__B_BOSS__',     [string]$b.bossDefeatedCount).
    Replace('__B_CLEAR__',    $(if ($b.isGameClear) { "true" } else { "false" })).
    Replace('__A_AVAIL__',    [string]$a.availableMetaPoints).
    Replace('__A_TOTAL__',    [string]$a.totalEarnedMetaPoints).
    Replace('__A_RUNS__',     [string]$a.totalRunsCompleted).
    Replace('__A_EARNED__',   [string]$a.earnedPoints).
    Replace('__A_UNLOCKED__', [string]::Join(', ', $a.unlockedIds))

if ($cs -match '__[A-Z_]+__') { Fail "採取テストに未置換のトークンが残っています: $($Matches[0])" }
Write-Utf8 $CaptureCs $cs
Write-Host "  $CaptureCs"

# ---------------------------------------------------------------- 2. Unity 実行
Write-Host "[2] Unity をバッチモードで実行" -ForegroundColor Cyan
if (Test-Path $Staging) { Remove-Item $Staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Staging | Out-Null
$env:MS3_GOLDEN_OUT = $Staging

$logPath = Join-Path $Staging "unity.log"
$xmlPath = Join-Path $Staging "results.xml"
$started = Get-Date

# Unity.exe は GUI サブシステム。& では終了を待たない。
$proc = Start-Process -FilePath $UnityExe -ArgumentList @(
    "-batchmode", "-nographics",
    "-projectPath", $Proj,
    "-runTests", "-testPlatform", "EditMode",
    "-testFilter", "Game.Tests.EditMode.MetaPointCaptureTests",
    "-testResults", $xmlPath,
    "-logFile", $logPath
) -Wait -PassThru -NoNewWindow
Write-Host ("    終了コード {0} / 所要 {1} 分" -f $proc.ExitCode, [math]::Round(((Get-Date) - $started).TotalMinutes, 1))

$allPath = Join-Path $Staging "golden_metapoint_all.json"
if (-not (Test-Path $allPath)) { Fail "採取結果が生成されていません。ログ: $logPath" }

$all = Get-Content $allPath -Raw | ConvertFrom-Json
$cases = @($all.cases)
Write-Host ("    採取 {0} 件" -f $cases.Count)

if ($cases.Count -lt $GC.case_count_min -or $cases.Count -gt $GC.case_count_max) {
    Fail ("ケース総数 {0} が {1}〜{2} の範囲外です" -f $cases.Count, $GC.case_count_min, $GC.case_count_max)
}

# ---------------------------------------------------------------- 3. 同語反復 6 条件
Write-Host "[3] 同語反復の検査（荷物が軽いので、通ったが何も測っていない状態を弾く）" -ForegroundColor Cyan

$calc  = @($cases | Where-Object { $_.method -eq "calc" })
$apply = @($cases | Where-Object { $_.method -eq "apply" })

function OutVal($c) {
    if ($c.method -eq "calc") { return [string]$c.out.points }
    if ($c.out.resultNull) { return "null" }
    return "{0}/{1}/{2}/{3}" -f $c.out.availableMetaPoints, $c.out.totalEarnedMetaPoints, $c.out.totalRunsCompleted, ($c.out.unlockedIds -join "-")
}

$ng = @()

# 条件1: 係数感度
$baseCalc = $calc | Where-Object { $_.id -eq "calc_base" } | Select-Object -First 1
$sensitive = 0
foreach ($name in @("calc_coeff_turn", "calc_coeff_skill", "calc_coeff_boss", "calc_coeff_clear")) {
    $v = $calc | Where-Object { $_.id -eq $name } | Select-Object -First 1
    if ($null -ne $v -and $v.out.points -ne $baseCalc.out.points) { $sensitive++ }
    else { Write-Host "    係数が出力に効いていない: $name" -ForegroundColor Yellow }
}
if ($sensitive -lt $TA.min_coeff_sensitive) { $ng += "係数感度 $sensitive/$($TA.min_coeff_sensitive)" }

# 条件2: 出力が入力の単純な写しであるケースの割合
$copy = 0
foreach ($c in $calc) {
    $ins = @($c.in.currentTurn, $c.in.skill, $c.in.bossDefeatedCount, $c.in.stamina, $c.in.mental)
    if ($ins -contains $c.out.points) { $copy++ }
}
$copyRatio = if ($calc.Count -gt 0) { $copy / $calc.Count } else { 1 }
if ($copyRatio -gt $TA.max_copy_ratio) { $ng += ("単純な写しが {0:P0}（上限 {1:P0}）" -f $copyRatio, $TA.max_copy_ratio) }

# 条件3: 各項の Max(0,･) が効いていること。
# 「出力が 0」で判定するのは誤り。全項が同時に 0 でなければ成立せず、
# 他の項が正である限り負値をいくら入れても 0 にならない（実測で確認）。
# 正しい証明は f(負値) == f(0) の一致。3 軸すべてで確認する。
$clampAxes = 0
$axisMap = [ordered]@{ turn = "currentTurn"; skill = "skill"; boss = "bossDefeatedCount" }
foreach ($ax in $axisMap.Keys) {
    $prop = $axisMap[$ax]
    $rows = @($calc | Where-Object { $_.id -like "calc_${ax}_*" })
    $zero = $rows | Where-Object { $_.in.$prop -eq 0 } | Select-Object -First 1
    $negs = @($rows | Where-Object { $_.in.$prop -lt 0 })
    if ($null -ne $zero -and $negs.Count -gt 0 -and
        @($negs | Where-Object { $_.out.points -ne $zero.out.points }).Count -eq 0) {
        $clampAxes++
    } else {
        Write-Host "    クランプを確認できない軸: $ax" -ForegroundColor Yellow
    }
}
if ($clampAxes -lt $TA.min_clamp_axes) { $ng += "クランプ確認できた軸 $clampAxes/$($TA.min_clamp_axes)" }

# 最終の Max(0, 合計) に負値経路から到達していること
$allNonPos = @($calc | Where-Object {
    -not $_.in.stateNull -and -not $_.in.isGameClear -and
    $_.in.currentTurn -le 0 -and $_.in.skill -le 0 -and $_.in.bossDefeatedCount -le 0
})
$allNonPosZero = @($allNonPos | Where-Object { $_.out.points -eq 0 }).Count
if ($TA.require_all_nonpositive_zero -and $allNonPosZero -lt 1) {
    $ng += "全入力非正で 0 になるケース未到達"
}

# 条件4: 桁あふれして 0（入力が全て非負なのに 0 になる = 乗算が回り込んだ）
$overflow = @($calc | Where-Object {
    -not $_.in.stateNull -and $_.out.points -eq 0 -and
    $_.in.currentTurn -ge 0 -and $_.in.skill -ge 0 -and $_.in.bossDefeatedCount -ge 0 -and
    $_.coeff.turnBonusPerTurn -gt 0
}).Count
if ($overflow -lt $TA.min_overflow_to_zero) { $ng += "桁あふれ到達 $overflow" }

# 条件5: null ガード
$nullCalc  = @($calc  | Where-Object { $_.in.stateNull }).Count
$nullApply = @($apply | Where-Object { $_.in.profileNull }).Count
if ($nullCalc  -lt $TA.min_null_guard_per_method) { $ng += "calc の null ガード未到達" }
if ($nullApply -lt $TA.min_null_guard_per_method) { $ng += "apply の null ガード未到達" }

# 条件6: 出力の相異なる個数
$distinct = @($cases | ForEach-Object { OutVal $_ } | Sort-Object -Unique).Count
$distinctRatio = $distinct / $cases.Count
if ($distinctRatio -lt $TA.min_distinct_ratio) {
    $ng += ("相異なる出力 {0}/{1} = {2:P0}（下限 {3:P0}）" -f $distinct, $cases.Count, $distinctRatio, $TA.min_distinct_ratio)
}

Write-Host ("    係数感度 {0}/4 / 写し {1:P0} / クランプ軸 {2}/3 / 非正で0 {3} / 桁あふれ {4} / null {5}+{6} / 相異なり {7:P0}" -f `
    $sensitive, $copyRatio, $clampAxes, $allNonPosZero, $overflow, $nullCalc, $nullApply, $distinctRatio)

if ($ng.Count -gt 0) {
    Write-Host "  同語反復の検査に不合格:" -ForegroundColor Red
    $ng | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
    Fail "採取データが挙動を測れていません"
}
Write-Host "  OK: 6 条件すべて通過" -ForegroundColor Green

# ---------------------------------------------------------------- 4. 二分と対照群
Write-Host "[4] 機械二分と対照群" -ForegroundColor Cyan
$nth = $GC.holdout_every_nth
$disclosed = @(); $holdout = @()
for ($i = 0; $i -lt $cases.Count; $i++) {
    if ($i % $nth -eq 0) { $holdout += $cases[$i] } else { $disclosed += $cases[$i] }
}

# 必ず落ちる対照群。期待値を 1 だけずらす。
$ctrlSrc = $calc | Where-Object { $_.id -eq "calc_base" } | Select-Object -First 1
$ctrl = $ctrlSrc | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$ctrl.id = $CtrlFail
$ctrl.control = "must_fail"
$ctrl.out.points = $ctrlSrc.out.points + 1
$holdout += $ctrl

Write-Host ("    開示 {0} / 非開示 {1}（対照群を含む）" -f $disclosed.Count, $holdout.Count)

# ---------------------------------------------------------------- 5. 配置
Write-Host "[5] 配置" -ForegroundColor Cyan
# 1 ケース 1 行で書く。整形出力にすると、入れ子を正規表現で切り出す読み手
# （Unity 側テスト。Newtonsoft が無く JsonUtility は "in" を扱えない）が壊れる。
function Write-Golden($Path, $Source, $Cases) {
    $lines = @($Cases | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress })
    $text = "{`n  `"source`": `"$Source`",`n  `"cases`": [`n    " + ($lines -join ",`n    ") + "`n  ]`n}`n"
    Write-Utf8 $Path $text
}

Write-Golden $DisclosedDst $all.source $disclosed
Write-Golden $HoldoutDst   $all.source $holdout

# ---------------------------------------------------------------- 6. 配置先の検証（削除の前）
Write-Host "[6] 配置先の検証" -ForegroundColor Cyan
$vD = (Get-Content $DisclosedDst -Raw | ConvertFrom-Json).cases.Count
$vH = (Get-Content $HoldoutDst   -Raw | ConvertFrom-Json).cases.Count
if ($vD -ne $disclosed.Count) { Fail "開示分の配置に失敗 ($DisclosedDst)" }
if ($vH -ne $holdout.Count)   { Fail "非開示分の配置に失敗 ($HoldoutDst)" }
if ((Get-Content $HoldoutDst -Raw) -notmatch $CtrlFail) { Fail "非開示分に対照群が含まれていません" }
Write-Host ("  OK: 開示 {0} / 非開示 {1}" -f $vD, $vH) -ForegroundColor Green

# ---------------------------------------------------------------- 7. 整流化（検証が通った場合のみ）
Write-Host "[7] 採取テストを削除" -ForegroundColor Cyan
Remove-Item $CaptureCs -Force
$metaPath = "$CaptureCs.meta"
if (Test-Path $metaPath) { Remove-Item $metaPath -Force }
Write-Host "  削除しました: $CaptureCs"

Write-Host ""
Write-Host "採取が完了しました。" -ForegroundColor Green
exit 0
