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
        {
            TurnBonusPerTurn = turnBonusPerTurn;
            SkillBonusMultiplier = skillBonusMultiplier;
            BossDefeatedBonus = bossDefeatedBonus;
            GameClearBonus = gameClearBonus;
        }

        public int TurnBonusPerTurn     { get; }
        public int SkillBonusMultiplier { get; }
        public int BossDefeatedBonus    { get; }
        public int GameClearBonus       { get; }

        public int CalculateEarnedPoints(GameState finalState, bool isGameClear, int bossDefeatedCount)
        {
            if (finalState == null)
            {
                return 0;
            }

            int turnBonus = Math.Max(0, finalState.CurrentTurn) * TurnBonusPerTurn;
            int skillBonus = Math.Max(0, finalState.Skill) * SkillBonusMultiplier;
            int bossBonus = Math.Max(0, bossDefeatedCount) * BossDefeatedBonus;
            int clearBonus = isGameClear ? GameClearBonus : 0;

            return Math.Max(0, turnBonus + skillBonus + bossBonus + clearBonus);
        }

        public MetaProfileState ApplyRunResult(MetaProfileState currentProfile, int earnedPoints)
        {
            if (currentProfile == null)
            {
                return currentProfile;
            }

            int safeEarnedPoints = Math.Max(0, earnedPoints);

            return currentProfile with
            {
                AvailableMetaPoints = currentProfile.AvailableMetaPoints + safeEarnedPoints,
                TotalEarnedMetaPoints = currentProfile.TotalEarnedMetaPoints + safeEarnedPoints,
                TotalRunsCompleted = currentProfile.TotalRunsCompleted + 1
            };
        }
    }
}
