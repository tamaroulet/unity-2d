"""MS3 パイプライン（1 タスク実行ワーカー）。

    python tools/ms3_pipeline.py --unit tools/units/metapoint.json --selftest
    python tools/ms3_pipeline.py --unit tools/units/metapoint.json

人間を呼ばない。標準入力を読まない。終了コードは 0 / 1 / 2 のみ。

承認済みの 7 制約:
  1. 単位は --unit で外部から受け取る。ここに単位を書かない
  2. 標準入力を読まない（stdin=DEVNULL）
  3. 終了コードは 0=成功 / 1=門番 REJECT / 2=システム ABORT
  4. サブスク枠の残量を見ない。待つのは呼び出し側（MS4 スケジューラ）の責務
  5. 1 プロセス = 1 単位。グローバル状態を持ち越さない
  6. サンドボックス初期化は冪等
  7. TTL は設定から注入する
"""
import argparse
import json
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"

# Windows でサブプロセスがコンソールウィンドウを開かないようにする。
# 非 Windows では属性が無いので 0 になり、無害に無視される。
# 効くのは git / dotnet / gh / agy（コンソールアプリ）。Unity.exe は GUI
# サブシステムなので効かないが、-batchmode -nographics で元々出ない。
_NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)

# creationflags だけでは足りない。agy のように内部でさらに子を起こす
# プロセスは conhost.exe を一瞬立ち上げてフォーカスを奪う（実測）。
# STARTUPINFO で SW_HIDE を渡し、最初のウィンドウ表示自体を抑える。
_STARTUPINFO = None
if sys.platform == "win32":
    _STARTUPINFO = subprocess.STARTUPINFO()
    _STARTUPINFO.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    _STARTUPINFO.wShowWindow = 0  # SW_HIDE


# ============================================================ 基本

def run(args, cwd, ttl, label, env=None):
    """shell=False。stdin は塞ぐ（制約 2）。ウィンドウも出さない。"""
    try:
        r = subprocess.run(args, cwd=str(cwd), capture_output=True, text=True,
                           timeout=ttl, encoding="utf-8", errors="replace",
                           stdin=subprocess.DEVNULL, env=env,
                           creationflags=_NO_WINDOW, startupinfo=_STARTUPINFO)
        return r.returncode, r.stdout or "", r.stderr or ""
    except subprocess.TimeoutExpired:
        return 124, "", f"TTL超過 ({ttl}s): {label}"
    except FileNotFoundError as e:
        return 127, "", f"コマンドが見つかりません: {e}"


def resolve_cli(name):
    for cand in (name + ".cmd", name + ".exe", name):
        p = shutil.which(cand)
        if p:
            return [p]
    sys.exit(f"ABORT: {name} CLI が PATH にありません")


class Ctx:
    """1 単位分の文脈。グローバル状態を持たない（制約 5）。"""

    def __init__(self, cfg_path, unit_path):
        self.cfg = json.loads(Path(cfg_path).read_text(encoding="utf-8"))
        self.unit = json.loads(Path(unit_path).read_text(encoding="utf-8"))

        p = self.cfg["paths"]
        self.repo = Path(p["repo"])
        self.proj_sub = p["unity_project_subdir"]
        self.out = Path(p["out_dir"]) / "pipeline"
        self.sandbox = Path(p.get("sandbox", r"C:\src\.local\wt\ms3-sandbox"))
        self.stage = self.out / "golden"

        self.ttl = self.cfg["ttl_seconds"]
        self.ttl.update(self.unit.get("ttl_seconds", {}))  # 単位側で上書き可（制約 7）

        self.ctrl_fail = self.cfg["control_groups"]["must_fail"]
        self.ctrl_pass = self.cfg["control_groups"]["must_pass"]
        self.g = self.unit["golden"]
        self.out.mkdir(parents=True, exist_ok=True)

    # ---- パス
    def sb(self, rel):
        return self.sandbox / rel.replace("/", "\\")

    @property
    def unity_proj(self):
        return self.sandbox / self.proj_sub


# ============================================================ サンドボックス

def sandbox_reset(c):
    """冪等（制約 6）。中断が残っていても自力で復帰する。

    git clean -fd は ignored を消さないので Library は残る。消すと Unity の
    フルインポート（実測 11.9 分）が毎回走る。
    """
    if not (c.sandbox / ".git").exists():
        c.sandbox.parent.mkdir(parents=True, exist_ok=True)
        rc, _, err = run(["git", "worktree", "add", "--detach", str(c.sandbox), "HEAD"],
                         c.repo, c.ttl["git"], "worktree add")
        if rc != 0:
            sys.exit(f"ABORT: サンドボックスを作れません: {err}")

    # リポジトリの現在位置へ合わせる。"HEAD" を指定すると detached worktree は
    # 自分の古いコミットに留まり、リポジトリ側の修正が永遠に届かない。
    # MS1 の sandbox_reset に同じ教訓をコメントで書いたのに、MS3 で再演した。
    # 散文は再発を防がないので、下で機械検査する。
    rc, out, _ = run(["git", "rev-parse", "HEAD"], c.repo, c.ttl["git"], "repo head")
    target = out.strip()
    if rc != 0 or not target:
        sys.exit("ABORT: リポジトリの HEAD を取得できません")

    purge_holdout(c)
    run(["git", "reset", "--hard", target], c.sandbox, c.ttl["git"], "reset to repo head")
    run(["git", "clean", "-fd"], c.sandbox, c.ttl["git"], "clean")
    purge_holdout(c)

    # 同期できたことの検査。ここを散文ではなく検査にしないと、同じ間違いが
    # 次に書かれたとき「実装が悪い」という形で 3 回 REJECT されるだけで、
    # 原因に到達できない（実測でそうなった）。
    _, sb_head, _ = run(["git", "rev-parse", "HEAD"], c.sandbox, c.ttl["git"], "sandbox head")
    if sb_head.strip() != target:
        sys.exit(f"ABORT: サンドボックスがリポジトリに追従していません "
                 f"(sandbox={sb_head.strip()[:8]} repo={target[:8]})")

    if (c.sb(c.g["holdout_rel"])).exists():
        sys.exit("ABORT: ホールドアウトの残骸を消せませんでした")
    if not (c.sb(c.g["disclosed_rel"])).exists():
        sys.exit(f"ABORT: サンドボックスに開示ゴールデンがありません: {c.g['disclosed_rel']}")


def require_repo_clean(c):
    """起動時にリポジトリがクリーンであることを要求する。

    汚れたまま起動すると 2 つの害がある:
      1. 未コミットの修正はサンドボックスへ届かない（同期はコミット単位のため）
      2. 実行後の gate_repo_untouched が、その汚れを「AIの脱走」と誤報告する
    起動時の汚れ（運用ミス）と実行中の書き換え（脱走）を区別するため、
    ここで先に落とす。
    """
    dirty = gate_repo_untouched(c)
    if dirty:
        sys.exit("ABORT: リポジトリに未コミットの変更があります。"
                 "コミットするか破棄してから実行してください: " + ", ".join(dirty[:5]))


def purge_holdout(c):
    """ソースと出力の両方から消す。片方だけでは PreserveNewest で次のランに混入する。"""
    removed = []
    for p in [c.sb(c.g["holdout_rel"])] + \
             list(c.sandbox.glob("tests/**/bin/**/" + Path(c.g["holdout_rel"]).name)) + \
             list(c.stage.glob(Path(c.g["holdout_rel"]).name)):
        if p.exists():
            p.unlink()
            removed.append(str(p))
    return removed


# ============================================================ 実装AI

def call_implementer(c, feedback=""):
    # 相対パスで渡すと、実装AIが本体リポジトリを編集しうる（実測で発生した）。
    # サンドボックスの絶対パスに展開して曖昧さを消す。ただしこれは
    # 「間違えにくくする」だけで、脱走を防ぐ機構ではない。
    # 実際の防波堤は gate_repo_untouched（検出して ABORT）。
    prompt = c.unit["prompt"]
    for token, rel in (("{core_abs}", c.unit["core_impl"]),
                       ("{so_abs}", c.unit["so_impl"]),
                       ("{sandbox_abs}", None)):
        value = str(c.sandbox) if rel is None else str(c.sb(rel))
        prompt = prompt.replace(token, value)
    if feedback:
        prompt += "\n\n前回の失敗:\n" + feedback

    imp = c.cfg["implementer"]
    args = resolve_cli(imp["cli"]) + [imp["headless_flag"], prompt,
                                      imp["auto_approve_flag"],
                                      imp["model_flag"], imp["model_name"]]
    rc, out, err = run(args, c.sandbox, c.ttl["implementer"], "実装AI")
    if rc != 0:
        return False, f"実装AI が異常終了 (rc={rc}): {(err or out)[:400]}"
    return True, ""


# ============================================================ 静的門

def gate_repo_untouched(c):
    """本体リポジトリが触られていないことを確認する。

    サンドボックス（git worktree）は機構ではなく慣習である。同一ユーザー・
    同一権限で動く実装AIは本体を書き換えられるし、実際に書き換えた。
    防げないので、破れたことを検出する。検出したらリトライせず ABORT する
    （同じことを 3 回繰り返すだけなので）。
    """
    _, out, _ = run(["git", "status", "--porcelain"], c.repo, c.ttl["git"], "repo status")
    return [l[3:].strip() for l in out.splitlines() if l.strip()]


def gate_whitelist(c):
    _, out, _ = run(["git", "status", "--porcelain"], c.sandbox, c.ttl["git"], "status")
    allowed = set(c.unit["whitelist"])
    bad = []
    for line in out.splitlines():
        if not line.strip():
            continue
        path = line[3:].strip().strip('"')
        if " -> " in path:
            path = path.split(" -> ")[-1].strip().strip('"')
        path = path.replace("\\", "/")
        if path.endswith(".meta"):
            continue
        if path not in allowed:
            bad.append(path)
    return bad


def gate_static(c):
    core = c.sb(c.unit["core_impl"])
    so = c.sb(c.unit["so_impl"])
    if not core.exists():
        return f"{c.unit['core_impl']} が存在しません"
    if not so.exists():
        return f"{c.unit['so_impl']} が存在しません"

    core_t = core.read_text(encoding="utf-8", errors="replace")
    so_t = so.read_text(encoding="utf-8", errors="replace")

    # --- Game.Core 側を先に見る。
    # コンパイル可否に直結するもの（Mathf の残存）を、配線の問題（委譲）より先に
    # 報告する。逆順にすると、委譲が未配線の間は Mathf の門に到達できず、
    # 門が効いているかを確かめられない（自己検査で実測した）。
    missing = [s for s in c.unit["required_symbols"] if s not in core_t]
    if missing:
        return "シグネチャが壊れています: " + ", ".join(missing)

    if re.search(c.unit["forbidden_in_core_regex"], core_t):
        return "Game.Core 側に Mathf が残っています（noEngineReferences で通りません）"

    if c.unit["forbidden_leftover"] in core_t:
        return f"{c.unit['forbidden_leftover']} が残っています（{c.unit['core_impl']}）"

    # --- SO 側
    so_missing = [s for s in c.unit["so_required_symbols"] if s not in so_t]
    if so_missing:
        return "インスペクタ結合が壊れています: " + ", ".join(so_missing)

    if c.unit["so_required_delegation"] not in so_t:
        return f"委譲されていません（{c.unit['so_required_delegation']} が無い）"

    if c.unit["forbidden_leftover"] in so_t:
        return f"{c.unit['forbidden_leftover']} が残っています（{c.unit['so_impl']}）"

    for text in (core_t, so_t):
        m = re.search(c.unit["forbidden_skip_attribute_regex"], text)
        if m:
            return f"skip 属性の使用: [{m.group(1)}]"
    return None


def gate_diff_lines(c, verbose=True):
    total = 0
    for rel in c.unit["whitelist"]:
        _, out, _ = run(["git", "diff", "--numstat", "HEAD", "--", rel],
                        c.sandbox, c.ttl["git"], "numstat")
        for line in out.splitlines():
            parts = line.split("\t")
            if len(parts) < 3:
                continue
            add = 0 if parts[0] == "-" else int(parts[0])
            dele = 0 if parts[1] == "-" else int(parts[1])
            total += add + dele
    if verbose:
        print(f"    差分: {total} 行")
    if total > c.unit["max_impl_lines"]:
        return f"差分超過: {total} 行 > {c.unit['max_impl_lines']}"
    return None


# ============================================================ 高速検査（Pure C#）

def run_fast_tests(c, tag):
    trx = c.out / f"{tag}.trx"
    if trx.exists():
        trx.unlink()
    run(["dotnet", "test", c.unit["fast_test_project"],
         "--nologo", "--logger", f"trx;LogFileName={tag}.trx",
         "--results-directory", str(c.out)],
        c.sandbox, c.ttl["dotnet_test"], f"dotnet test ({tag})")
    if not trx.exists():
        return None, "検査系故障: TRX が生成されませんでした（ビルド失敗の可能性）"

    root = ET.parse(trx).getroot()
    results = {r.get("testName"): r.get("outcome")
               for r in root.iter(f"{TRX_NS}UnitTestResult")}
    return results, None


# ============================================================ Unity 受入

def run_unity_tests(c, tag):
    xml = c.out / f"{tag}.xml"
    log = c.out / f"{tag}.log"
    if xml.exists():
        xml.unlink()

    import os
    env = dict(os.environ)
    # 同居する全ランナーが同じステージング先を見る。片方しか設定しないと
    # 他方が「ファイルが無い」で落ちる。
    for var in c.cfg["golden_dir_env_vars"]:
        env[var] = str(c.stage)

    # -testFilter は付けない。名前空間を指定すると NUnit が [Explicit] を
    # 「明示的な選択」とみなして実行してしまい、Skip されるはずの 20 件が
    # 走って落ちる（実測で発生した）。無指定なら正しく Skip される。
    unity = c.cfg["unity_exe"]
    rc, _, err = run([unity, "-batchmode", "-nographics",
                      "-projectPath", str(c.unity_proj),
                      "-runTests", "-testPlatform", "EditMode",
                      "-testResults", str(xml), "-logFile", str(log)],
                     c.sandbox, c.ttl["unity"], f"unity ({tag})", env=env)
    if not xml.exists():
        return None, f"検査系故障: 結果 XML が生成されませんでした（rc={rc} {err[:200]} log={log}）"

    x = ET.parse(xml).getroot()
    results = {}
    for n in x.iter("test-case"):
        results[n.get("fullname")] = n.get("result")
    return results, None


# ============================================================ 判定

def names_with(results, needle):
    return [n for n in results if n and needle in n]


def outcome_of(results, needle):
    for n, o in results.items():
        if n and needle in n:
            return o
    return None


def real_failures(results, ctrl_fail, failed_word):
    return [n for n, o in results.items()
            if o == failed_word and ctrl_fail not in (n or "")]


def golden_count(c, rel_or_abs):
    p = Path(rel_or_abs)
    if not p.is_absolute():
        p = c.sb(str(p))
    return len(json.loads(p.read_text(encoding="utf-8"))["cases"])


def stage_golden(c, with_holdout):
    """開示ゴールデンを「全部」置く。

    この単位のゴールデンだけを置くと、同じ Unity スイートに同居する他の
    ゴールデンランナー（MS2 の GoldenMasterEquivalenceTests など）が
    「ファイルが無い」で落ち、実装が正しくても REJECT になる（実測で発生した）。
    ステージング先は 1 つで、そこを全ランナーが見る。
    """
    c.stage.mkdir(parents=True, exist_ok=True)
    for p in c.stage.glob("golden_*.json"):
        p.unlink()

    src_dir = c.sb("tests/golden")
    staged = []
    for p in sorted(src_dir.glob("golden_*.json")):
        shutil.copyfile(p, c.stage / p.name)
        staged.append(p.name)
    if not staged:
        sys.exit(f"ABORT: 開示ゴールデンが 1 本もありません: {src_dir}")

    if with_holdout:
        shutil.copyfile(c.g["holdout_src"], c.stage / Path(c.g["holdout_rel"]).name)
        shutil.copyfile(c.g["holdout_src"], c.sb(c.g["holdout_rel"]))
    return staged


def check_acceptance(c, fast, unity, expect_holdout):
    """終了コードではなく個別結果で判定する。"""
    ng = []
    d_tag, h_tag = c.g["disclosed_tag"], c.g["holdout_tag"]
    want_d = golden_count(c, c.g["disclosed_rel"])

    # --- Pure C# 側（純粋クラス直）
    hit = len(names_with(fast, d_tag + "_"))
    if hit < want_d:
        ng.append(f"高速検査で {d_tag} が {hit}/{want_d} 件しか実行されていない")
    f_fail = real_failures(fast, c.ctrl_fail, "Failed")
    if f_fail:
        ng.append(f"高速検査の不一致 {len(f_fail)} 件: " + ", ".join(f_fail[:3]))

    # --- Unity 側（SO 経由）
    if outcome_of(unity, c.ctrl_pass) != "Passed":
        return ["検査系故障: 必ず通る対照群が通らなかった"], True
    hit_u = len(names_with(unity, d_tag + "_"))
    if hit_u < want_d:
        return [f"検査系故障: Unity で {d_tag} が {hit_u}/{want_d} 件しか実行されていない"], True

    skipped = [n for n, o in unity.items() if o in ("Skipped", "Inconclusive")]
    for req in (d_tag, "MetaPointEquivalence"):
        if [n for n in skipped if req in n]:
            return [f"検査系故障: {req} が skip されている"], True
    if len(skipped) > c.cfg["unity_skip_baseline"]:
        ng.append(f"skip が既知の {c.cfg['unity_skip_baseline']} 件を超えた（{len(skipped)}）")

    leaked = names_with(unity, h_tag + "_")
    if not expect_holdout and leaked:
        return [f"検査系故障: 非開示が漏れ込んでいる（{len(leaked)} 件）"], True
    if expect_holdout:
        if not leaked:
            return ["検査系故障: 非開示が実行されていない"], True
        o = outcome_of(unity, c.ctrl_fail)
        if o is None:
            return ["検査系故障: 必ず落ちる対照群が実行されていない"], True
        if o != "Failed":
            return [f"検査系故障: 必ず落ちる対照群が落ちなかった（{o}）"], True

    u_fail = real_failures(unity, c.ctrl_fail, "Failed")
    if u_fail:
        ng.append(f"Unity の不一致 {len(u_fail)} 件: " + ", ".join(u_fail[:3]))

    return ng, False


# ============================================================ 1 周

def attempt(c, feedback):
    sandbox_reset(c)

    print("[1] 実装AI")
    ok, msg = call_implementer(c, feedback)
    if not ok:
        return "RETRY", msg

    escaped = gate_repo_untouched(c)
    if escaped:
        return "ABORT", ("実装AIがサンドボックス外（本体リポジトリ）を書き換えました: "
                         + ", ".join(escaped[:5]))

    print("[2] 静的機械判定")
    bad = gate_whitelist(c)
    if bad:
        return "RETRY", "許可外のファイル変更: " + ", ".join(bad[:5])
    ng = gate_static(c)
    if ng:
        return "RETRY", ng
    ng = gate_diff_lines(c)
    if ng:
        return "RETRY", ng

    print("[3] 高速検査（Pure C#）")
    fast, err = run_fast_tests(c, "impl_fast")
    if err:
        return "ABORT", err

    print("[4] Unity 受入（開示）")
    stage_golden(c, with_holdout=False)
    unity, err = run_unity_tests(c, "impl_disclosed")
    if err:
        return "ABORT", err
    ng, fatal = check_acceptance(c, fast, unity, expect_holdout=False)
    if fatal:
        return "ABORT", "; ".join(ng)
    if ng:
        return "RETRY", "; ".join(ng)

    print("[5] Unity 受入（非開示）")
    try:
        stage_golden(c, with_holdout=True)
        unity2, err = run_unity_tests(c, "impl_holdout")
        if err:
            return "ABORT", err
        ng, fatal = check_acceptance(c, fast, unity2, expect_holdout=True)
        if fatal:
            return "ABORT", "; ".join(ng)
        if ng:
            # 内容は渡さない（非開示由来）
            return "RETRY", "非開示の受入条件に不合格でした（詳細は開示されません）"
    finally:
        purge_holdout(c)

    print("[6] 持ち出し")
    for rel in c.unit["whitelist"]:
        src = c.sb(rel)
        dst = c.repo / rel.replace("/", "\\")
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
    run(["git", "add", "--"] + c.unit["whitelist"], c.repo, c.ttl["git"], "add")
    run(["git", "commit", "-m",
         f"feat(ms3): implement {c.unit['id']} via pipeline"],
        c.repo, c.ttl["git"], "commit")
    rc, out, err = run(["git", "push"], c.repo, c.ttl["git"], "push")
    if rc != 0:
        return "ABORT", f"push できません: {(err or out)[:300]}"

    print("[7] CI 完了検知")
    rc, out, err = run(["gh", "run", "watch", "--exit-status"],
                       c.repo, c.ttl["gh"], "gh run watch")
    if rc != 0:
        print("    CI が赤。自動で差し戻します。")
        run(["git", "revert", "--no-edit", "HEAD"], c.repo, c.ttl["git"], "revert")
        run(["git", "push"], c.repo, c.ttl["git"], "push revert")
        return "RETRY", "CI が赤でした（差し戻し済み）"

    return "SUCCESS", ""


# ============================================================ 自己検査

def selftest(c):
    """AI を呼ばずに、門が実際に赤を出すかを確認する。"""
    log = []

    def check(name, ok, detail=""):
        log.append(ok)
        print(f"  {'OK  ' if ok else 'NG  '} {name}" + (f"  -- {detail}" if detail else ""))

    print("=== 門の自己検査（実装AIは呼びません） ===")
    sandbox_reset(c)
    _, sb_head, _ = run(["git", "rev-parse", "HEAD"], c.sandbox, c.ttl["git"], "sandbox head")
    check("サンドボックスがリポジトリに追従している", True, sb_head.strip()[:8])

    print("[A] ベースライン（スタブのまま）")
    fast, err = run_fast_tests(c, "self_fast")
    if err:
        check("高速検査の実行", False, err)
        return 2
    d_tag = c.g["disclosed_tag"]
    want_d = golden_count(c, c.g["disclosed_rel"])
    hit = len(names_with(fast, d_tag + "_"))
    check("開示ゴールデンが実行されている", hit >= want_d, f"{hit}/{want_d}")
    check("スタブなので赤が出る", len(real_failures(fast, c.ctrl_fail, "Failed")) > 0,
          f"{len(real_failures(fast, c.ctrl_fail, 'Failed'))} 件 Failed")

    print("[B] ゲートの発火確認（わざと違反させます）")
    junk = c.sandbox / "junk_not_allowed.txt"
    junk.write_text("x", encoding="utf-8")
    check("ホワイトリストが許可外を弾く", len(gate_whitelist(c)) > 0)
    junk.unlink()

    core = c.sb(c.unit["core_impl"])
    orig = core.read_text(encoding="utf-8")
    core.write_text("namespace X { public class MetaPointRules { } }", encoding="utf-8")
    ng = gate_static(c)
    check("シグネチャ破壊を弾く", ng is not None and "シグネチャ" in ng, str(ng))

    core.write_text(orig.replace("throw new NotImplementedException()", "Mathf.Max(0, 0)"),
                    encoding="utf-8")
    ng = gate_static(c)
    check("Game.Core の Mathf を弾く", ng is not None and "Mathf" in ng, str(ng))

    core.write_text(orig + "\n" + "// filler\n" * (c.unit["max_impl_lines"] + 50),
                    encoding="utf-8")
    ng = gate_diff_lines(c, verbose=False)
    check("差分行数を弾く", ng is not None, str(ng))
    core.write_text(orig, encoding="utf-8")
    sandbox_reset(c)

    print("[C] 非開示の投入")
    try:
        stage_golden(c, with_holdout=True)
        unity, err = run_unity_tests(c, "self_holdout")
        if err:
            check("非開示の実行", False, err)
        else:
            h_tag = c.g["holdout_tag"]
            check("非開示が実行されている", len(names_with(unity, h_tag + "_")) > 0,
                  f"{len(names_with(unity, h_tag + '_'))} 件")
            check("必ず落ちる対照群が発見されている",
                  outcome_of(unity, c.ctrl_fail) is not None,
                  str(outcome_of(unity, c.ctrl_fail)))
    finally:
        purge_holdout(c)

    print("[D] 非開示の残留が無いこと")
    stage_golden(c, with_holdout=False)
    unity, err = run_unity_tests(c, "self_after_purge")
    if err:
        check("消去後の実行", False, err)
    else:
        leaked = names_with(unity, c.g["holdout_tag"] + "_")
        check("非開示が残留していない", len(leaked) == 0, f"{len(leaked)} 件")
    sandbox_reset(c)

    ng_count = sum(1 for ok in log if not ok)
    print()
    if ng_count:
        print(f"自己検査 NG: {ng_count} 件。門が効いていないので本番を回しません。")
        return 2
    print("自己検査 すべて OK。門は赤を出せる状態です。")
    return 0


# ============================================================ エントリ

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--unit", required=True, help="単位定義 JSON（制約 1）")
    ap.add_argument("--config", default=str(Path(__file__).with_name("ms3.config.json")))
    ap.add_argument("--selftest", action="store_true")
    ap.add_argument("--skip-selftest", action="store_true")
    args = ap.parse_args()

    c = Ctx(args.config, args.unit)
    require_repo_clean(c)

    if args.selftest:
        return selftest(c)

    if not args.skip_selftest:
        rc = selftest(c)
        if rc != 0:
            print("\n自己検査が通らないため本番を実行しません。")
            return rc
        print()

    history = []
    feedback = ""
    max_retry = c.cfg["gates"]["max_retry"]
    for i in range(max_retry + 1):
        print(f"=== 試行 {i + 1}/{max_retry + 1} ===")
        verdict, msg = attempt(c, feedback)
        history.append((verdict, msg))

        if verdict == "SUCCESS":
            print("\nMS3 完走。人間の出番はありません。")
            return 0
        if verdict == "ABORT":
            print(f"\nABORT（検査系の故障）: {msg}")
            return 2

        print(f"REJECT: {msg}")
        feedback = "\n".join(l for l in str(msg).splitlines()
                             if "holdout" not in l.lower() and c.ctrl_fail not in l)[:2000]

    print("\n" + "=" * 56)
    print(f"不合格。{max_retry + 1} 回とも通りませんでした。")
    print("=" * 56)
    for i, (v, m) in enumerate(history, 1):
        print(f"  試行{i}: {v}  {m}")
    sandbox_reset(c)
    return 1


if __name__ == "__main__":
    sys.exit(main())
