# tools

自律開発の道具（スケジューラ・実装パイプライン・分解役・監査役）は、2026-09-13 に
**[tamaroulet/game-harness](https://github.com/tamaroulet/game-harness)** へ移した。
ゲームを何本作っても道具が 1 本で済むようにするため。

| 移設前（ここ） | 移設先（game-harness） |
|:--|:--|
| `ms4_scheduler.py` | `harness/scheduler.py` |
| `ms3_pipeline.py` | `harness/pipeline.py` |
| `decompose.py` / `audit.py` / `exitcode.py` | `harness/` |
| `test_ms4_scheduler.py` | `tests/test_scheduler.py` |
| `ms3.config.json` | `projects/unity-2d/pipeline.json` |
| `decompose.config.json` / `audit.config.json` / `ms4.config.json` | `config/` |

このゲームの設定は `projects/unity-2d/` にある。

```
python C:\src\game-harness\harness\scheduler.py --project unity-2d --dry-run
```

## ここに残したもの

このゲーム固有のもので、道具ではない。

- `units/` — 単位定義。分解役の出力で、実装の根拠としてコミット履歴に残す
- `accept.ps1` — このリポジトリの Unity 受入
- `ms3_capture_golden.ps1` — MS3 のゴールデン採取。設定は game-harness の `projects/unity-2d/` から読む
