// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Command;
using Game.Features.Relic;
using UnityEngine;

namespace Game.Features.MetaProgression
{
    /// <summary>
    /// 周回終了時の MetaPoints 計算、プロフィールへの反映、アンロック購入、
    /// および購入済みアンロックの効果解決を行う純粋関数 Resolver。
    /// 内部状態を持たず、引数のみから計算を行う（static フィールド、Time、Random、
    /// DateTime、Debug は参照しない）。
    ///
    /// 指示書（07_meta_progression_system.md）に対する設計上の補強点:
    /// 1. CalculateEarnedPoints は不正な負値入力（Skill 等）を 0 未満に落とし込まないよう
    ///    各項を個別にクランプする。乱数もイベントも介在しないはずの値だが、将来
    ///    レリック効果等でマイナス補正が入っても MetaPoints がマイナスにならない保証とする。
    /// 2. ApplyUnlock は「二重購入」を明示的に禁止する。指示書の型定義だけでは
    ///    UnlockedIds に同一 ID が重複登録される経路を防げず、ApplyUnlockedStatBonuses が
    ///    ボーナスを人数分二重加算してしまう時限爆弾になり得るため、ここで一次防御する。
    /// 3. MetaUnlockSO.Cost が負値のデータ（登録ミス）は、購入のたびに MetaPoints を
    ///    無限増殖させる経路になるため ApplyUnlock で明示的に拒否する。
    /// 4. 指示書は「ポイントの計算」「アンロックの購入」までしか定義しておらず、
    ///    購入した効果を実際のゲームプレイに反映する経路が存在しなかった（機能が
    ///    存在するのに何も起きないサイレントホール）。ApplyRunResult /
    ///    ApplyUnlockedStatBonuses / ResolveUnlockedRelics を追加してこれを閉じる。
    /// 5. ApplyUnlockedStatBonuses / ResolveUnlockedRelics は UnlockedIds 内の重複 ID を
    ///    デデュープしてから集計する（保存データの手動改変や将来の不具合に対する保険）。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/MetaProgression/MetaPointResolver", fileName = "MetaPointResolver")]
    public class MetaPointResolverSO : ScriptableObject
    {
        [SerializeField] private int _turnBonusPerTurn = 5;
        [SerializeField] private int _skillBonusMultiplier = 2;
        [SerializeField] private int _bossDefeatedBonus = 50;
        [SerializeField] private int _gameClearBonus = 100;

        public int TurnBonusPerTurn => _turnBonusPerTurn;

        public int SkillBonusMultiplier => _skillBonusMultiplier;

        public int BossDefeatedBonus => _bossDefeatedBonus;

        public int GameClearBonus => _gameClearBonus;

        /// <summary>
        /// 周回終了時の最終状態から、獲得する MetaPoints を計算する。
        /// 計算式: CurrentTurn * TurnBonusPerTurn + Skill * SkillBonusMultiplier
        ///        + BossDefeatedCount * BossDefeatedBonus + (IsGameClear ? GameClearBonus : 0)
        /// 各項は 0 未満をとらないよう個別にクランプしてから合算し、結果全体も 0 未満にはならない。
        /// </summary>
        public int CalculateEarnedPoints(GameState finalState, bool isGameClear, int bossDefeatedCount)
        {
            if (finalState == null)
            {
                return 0;
            }

            int turnBonus = Mathf.Max(0, finalState.CurrentTurn) * _turnBonusPerTurn;
            int skillBonus = Mathf.Max(0, finalState.Skill) * _skillBonusMultiplier;
            int bossBonus = Mathf.Max(0, bossDefeatedCount) * _bossDefeatedBonus;
            int clearBonus = isGameClear ? _gameClearBonus : 0;

            return Mathf.Max(0, turnBonus + skillBonus + bossBonus + clearBonus);
        }

        /// <summary>
        /// 周回1回分の結果（CalculateEarnedPoints の戻り値）をプロフィールへ反映する。
        /// AvailableMetaPoints / TotalEarnedMetaPoints へ加算し、TotalRunsCompleted を+1する。
        /// 指示書のデータ定義には存在するが計算経路が無かった、獲得ポイントの反映処理を担う。
        /// </summary>
        public MetaProfileState ApplyRunResult(MetaProfileState currentProfile, int earnedPoints)
        {
            if (currentProfile == null)
            {
                return currentProfile;
            }

            int safeEarnedPoints = Mathf.Max(0, earnedPoints);

            return currentProfile with
            {
                AvailableMetaPoints = currentProfile.AvailableMetaPoints + safeEarnedPoints,
                TotalEarnedMetaPoints = currentProfile.TotalEarnedMetaPoints + safeEarnedPoints,
                TotalRunsCompleted = currentProfile.TotalRunsCompleted + 1
            };
        }

        /// <summary>
        /// アンロックの購入を試みる。以下のいずれかに該当する場合は購入せず success = false を返す。
        /// ・currentProfile / unlock が null
        /// ・unlock.Cost が負値（データ不整合）
        /// ・unlock が既に購入済み（UnlockedIds に含まれる。二重購入・二重加算の防止）
        /// ・AvailableMetaPoints が unlock.Cost に満たない
        /// </summary>
        public (MetaProfileState newProfile, bool success) ApplyUnlock(MetaProfileState currentProfile, MetaUnlockSO unlock)
        {
            if (currentProfile == null || unlock == null || unlock.Cost < 0)
            {
                return (currentProfile, false);
            }

            if (IsAlreadyUnlocked(currentProfile, unlock.UnlockId))
            {
                return (currentProfile, false);
            }

            if (currentProfile.AvailableMetaPoints < unlock.Cost)
            {
                return (currentProfile, false);
            }

            List<int> newUnlockedIds = new List<int>(currentProfile.UnlockedIds) { unlock.UnlockId };

            MetaProfileState newProfile = currentProfile with
            {
                AvailableMetaPoints = currentProfile.AvailableMetaPoints - unlock.Cost,
                UnlockedIds = newUnlockedIds
            };

            return (newProfile, true);
        }

        /// <summary>
        /// 購入済みアンロック（InitialStaminaBonus / InitialSkillBonus / InitialMentalBonus）の
        /// BonusValue を合算し、baseState に加算・クランプした新しい GameState を返す。
        /// 新規周回開始時の初期状態底上げに使用する。
        /// </summary>
        public GameState ApplyUnlockedStatBonuses(
            GameState baseState,
            MetaProfileState profile,
            MetaUnlockCatalogSO catalog,
            GameRulesSO rules)
        {
            if (baseState == null || profile == null || catalog == null || rules == null)
            {
                return baseState;
            }

            int staminaBonus = 0;
            int skillBonus = 0;
            int mentalBonus = 0;
            HashSet<int> processedIds = new HashSet<int>();

            for (int i = 0; i < profile.UnlockedIds.Count; i++)
            {
                int unlockId = profile.UnlockedIds[i];
                if (!processedIds.Add(unlockId))
                {
                    continue; // 重複 ID の二重加算を防ぐ
                }

                MetaUnlockSO unlock = catalog.FindById(unlockId);
                if (unlock == null)
                {
                    continue; // カタログから外れたアンロック ID は無視する
                }

                switch (unlock.Kind)
                {
                    case MetaUnlockKind.InitialStaminaBonus:
                        staminaBonus += unlock.BonusValue;
                        break;
                    case MetaUnlockKind.InitialSkillBonus:
                        skillBonus += unlock.BonusValue;
                        break;
                    case MetaUnlockKind.InitialMentalBonus:
                        mentalBonus += unlock.BonusValue;
                        break;
                    default:
                        break; // UnlockRelic はここでは扱わない（ResolveUnlockedRelics の責務）
                }
            }

            return baseState with
            {
                Stamina = TurnRules.Clamp(baseState.Stamina + staminaBonus, rules.ParamMin, rules.ParamMax),
                Skill = TurnRules.Clamp(baseState.Skill + skillBonus, rules.ParamMin, rules.ParamMax),
                Mental = TurnRules.Clamp(baseState.Mental + mentalBonus, rules.ParamMin, rules.ParamMax)
            };
        }

        /// <summary>
        /// 購入済みアンロック（UnlockRelic）の TargetRelicId を relicCatalog から解決し、
        /// ドラフトプールに追加すべき RelicSO の一覧を返す。存在しない ID・重複 ID は無視する。
        /// </summary>
        public IReadOnlyList<RelicSO> ResolveUnlockedRelics(
            MetaProfileState profile,
            MetaUnlockCatalogSO catalog,
            RelicCatalogSO relicCatalog)
        {
            if (profile == null || catalog == null || relicCatalog == null)
            {
                return System.Array.Empty<RelicSO>();
            }

            List<RelicSO> resolvedRelics = new List<RelicSO>();
            HashSet<int> processedRelicIds = new HashSet<int>();

            for (int i = 0; i < profile.UnlockedIds.Count; i++)
            {
                MetaUnlockSO unlock = catalog.FindById(profile.UnlockedIds[i]);
                if (unlock == null || unlock.Kind != MetaUnlockKind.UnlockRelic)
                {
                    continue;
                }

                if (!processedRelicIds.Add(unlock.TargetRelicId))
                {
                    continue; // 同一レリックを解放するアンロックが複数あっても1件のみ返す
                }

                RelicSO relic = relicCatalog.FindById(unlock.TargetRelicId);
                if (relic != null)
                {
                    resolvedRelics.Add(relic);
                }
            }

            return resolvedRelics;
        }

        private static bool IsAlreadyUnlocked(MetaProfileState profile, int unlockId)
        {
            IReadOnlyList<int> unlockedIds = profile.UnlockedIds;
            for (int i = 0; i < unlockedIds.Count; i++)
            {
                if (unlockedIds[i] == unlockId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
