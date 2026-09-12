// SPDX-AI-Disclosure: ai-generated
using System;
using Game.Core;

namespace Game.Features.MetaProgression
{
    /// <summary>
    /// MetaPointResolverSO から UnityEngine 依存を外した純粋版。
    /// コンストラクタの引数順は MetaPointResolverSO の [SerializeField] 宣言順と同一。
    ///
    /// 実装時の注意: Game.Core は noEngineReferences: true のため UnityEngine.Mathf を
    /// 参照できない。System.Math.Max を使うこと（int 同士なら挙動は同一。
    /// 桁あふれの回り込みも同じ）。
    /// </summary>
    public class MetaPointRules
    {
        public MetaPointRules(
            int turnBonusPerTurn, int skillBonusMultiplier,
            int bossDefeatedBonus, int gameClearBonus)
            => throw new NotImplementedException();

        public int TurnBonusPerTurn     => throw new NotImplementedException();
        public int SkillBonusMultiplier => throw new NotImplementedException();
        public int BossDefeatedBonus    => throw new NotImplementedException();
        public int GameClearBonus       => throw new NotImplementedException();

        public int CalculateEarnedPoints(GameState finalState, bool isGameClear, int bossDefeatedCount)
            => throw new NotImplementedException();

        public MetaProfileState ApplyRunResult(MetaProfileState currentProfile, int earnedPoints)
            => throw new NotImplementedException();
    }
}
