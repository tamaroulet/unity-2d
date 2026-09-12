// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;

namespace Game.Features.Boss
{
    /// <summary>
    /// 全自動交戦（ResolveFullBattle）の最終結果と全ターン履歴を保持する不変レコード。
    /// </summary>
    public sealed record FullBattleResult
    {
        public BattleOutcomeKind Outcome { get; init; }
        public GameState FinalPlayerState { get; init; }
        public BossState FinalBossState { get; init; }
        public IReadOnlyList<BattleTurnResult> TurnHistory { get; init; }

        public FullBattleResult(
            BattleOutcomeKind outcome,
            GameState finalPlayerState,
            BossState finalBossState,
            IReadOnlyList<BattleTurnResult> turnHistory)
        {
            Outcome = outcome;
            FinalPlayerState = finalPlayerState;
            FinalBossState = finalBossState;
            TurnHistory = turnHistory;
        }
    }
}
