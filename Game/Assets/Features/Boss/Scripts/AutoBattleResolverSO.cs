// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Command;
using Game.Features.Relic;
using UnityEngine;

namespace Game.Features.Boss
{
    /// <summary>
    /// プレイヤーの戦闘コマンドとボスの行動パターンを1ターン分自動で計算し、
    /// 新しい GameState / BossState と勝敗判定を返す純粋関数としての ScriptableObject。
    /// static フィールド、Time、Random、DateTime、Debug は参照しない。
    ///
    /// 育成パラメータの戦闘計算への写像（GameDesignMaster.md 4章）:
    /// ・スタミナ＝AP　　　　… Attack のコストであり、0 になるとプレイヤーの敗北条件になる
    /// ・スキル＝ダメージ　　… Attack で与えるダメージ量に直接反映される
    /// ・メンタル＝シールド維持 … ボスの Attack / SpecialAttack による被ダメージを軽減する
    ///
    /// 各行動の消費・回復量は本 SO のフィールド（調整値）に持たせ、C# 側にハードコードしない。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss/AutoBattleResolver", fileName = "AutoBattleResolver")]
    public class AutoBattleResolverSO : ScriptableObject
    {
        [SerializeField] private int _attackStaminaCost = 15;
        [SerializeField] private int _defendStaminaRecoveryAmount = 20;
        [SerializeField] private int _mentalFocusRecoveryAmount = 20;
        [SerializeField] private float _skillToDamageMultiplier = 1f;

        [Tooltip("この値以上の Mental を持つ場合、ボスの Attack / SpecialAttack のスタミナダメージを100%軽減する。")]
        [SerializeField] private int _mentalShieldFullValue = 100;

        public int AttackStaminaCost => _attackStaminaCost;

        public int DefendStaminaRecoveryAmount => _defendStaminaRecoveryAmount;

        public int MentalFocusRecoveryAmount => _mentalFocusRecoveryAmount;

        public float SkillToDamageMultiplier => _skillToDamageMultiplier;

        public int MentalShieldFullValue => _mentalShieldFullValue;

        /// <summary>
        /// プレイヤーのアクションとボスの行動を1ターン分計算し、戦闘結果を返す。
        ///
        /// 処理順序:
        /// 1. 所持レリック（OnCommandExecuted）の効果をプレイヤーの行動に合成する
        /// 2. プレイヤーのアクションを適用し、ボスへのダメージ量を算出する
        /// 3. ボスへダメージを適用する（Shield から先に減算）
        /// 4. ボスを撃破していれば、その時点で Victory として即座に返す（ボスは行動しない）
        /// 5. プレイヤーが自滅（Stamina 0 以下）していれば、その時点で Defeat として即座に返す
        /// 6. ボスの行動（ActionPattern を BattleTurn で周期参照）を適用する
        /// 7. BattleTurn を進め、最終的な勝敗を判定する
        /// </summary>
        public BattleTurnResult ResolveTurn(
            GameState playerState,
            BossState bossState,
            BattlePlayerActionKind playerAction,
            BossSO bossData,
            IReadOnlyList<RelicSO> activeRelics,
            GameRulesSO rules)
        {
            if (bossState.IsDefeated || playerState.Stamina <= 0)
            {
                return new BattleTurnResult
                {
                    PlayerState = playerState,
                    BossState = bossState,
                    BossActionTaken = null,
                    DamageDealtToBoss = 0,
                    StaminaDamageTaken = 0,
                    MentalDamageTaken = 0,
                    Outcome = bossState.IsDefeated ? BattleOutcomeKind.Victory : BattleOutcomeKind.Defeat
                };
            }

            RelicEffect relicEffect = CombineCommandExecutedRelicEffects(activeRelics);

            (GameState stateAfterPlayer, int damageDealtToBoss) =
                ApplyPlayerAction(playerState, playerAction, relicEffect, rules);

            BossState bossAfterPlayerDamage = ApplyDamageToBoss(bossState, damageDealtToBoss);

            if (bossAfterPlayerDamage.IsDefeated)
            {
                return new BattleTurnResult
                {
                    PlayerState = stateAfterPlayer,
                    BossState = bossAfterPlayerDamage,
                    BossActionTaken = null,
                    DamageDealtToBoss = damageDealtToBoss,
                    StaminaDamageTaken = 0,
                    MentalDamageTaken = 0,
                    Outcome = BattleOutcomeKind.Victory
                };
            }

            if (stateAfterPlayer.Stamina <= 0)
            {
                return new BattleTurnResult
                {
                    PlayerState = stateAfterPlayer,
                    BossState = bossAfterPlayerDamage,
                    BossActionTaken = null,
                    DamageDealtToBoss = damageDealtToBoss,
                    StaminaDamageTaken = 0,
                    MentalDamageTaken = 0,
                    Outcome = BattleOutcomeKind.Defeat
                };
            }

            BossActionKind bossAction = bossData.GetActionForTurn(bossAfterPlayerDamage.BattleTurn);
            (GameState stateAfterBoss, BossState bossAfterAction, int staminaDamage, int mentalDamage) =
                ApplyBossAction(stateAfterPlayer, bossAfterPlayerDamage, bossAction, bossData, rules);

            BossState advancedBossState = bossAfterAction with { BattleTurn = bossAfterAction.BattleTurn + 1 };

            return new BattleTurnResult
            {
                PlayerState = stateAfterBoss,
                BossState = advancedBossState,
                BossActionTaken = bossAction,
                DamageDealtToBoss = damageDealtToBoss,
                StaminaDamageTaken = staminaDamage,
                MentalDamageTaken = mentalDamage,
                Outcome = DetermineOutcome(stateAfterBoss, advancedBossState)
            };
        }

        /// <summary>
        /// OnCommandExecuted 発動のレリック効果を合成する。ボス戦のプレイヤー行動を
        /// 通常フェーズのコマンド実行とみなして再利用する（RelicResolverSO.ModifyCommandEffect と同じ方針）。
        /// </summary>
        private RelicEffect CombineCommandExecutedRelicEffects(IReadOnlyList<RelicSO> activeRelics)
        {
            if (activeRelics == null || activeRelics.Count == 0)
            {
                return new RelicEffect(0, 0, 0, 1f);
            }

            int staminaBonus = 0;
            int skillBonus = 0;
            int mentalBonus = 0;
            float costMultiplier = 1f;

            for (int i = 0; i < activeRelics.Count; i++)
            {
                RelicSO relic = activeRelics[i];
                if (relic != null && relic.TriggerKind == RelicTriggerKind.OnCommandExecuted)
                {
                    staminaBonus += relic.Effect.StaminaDeltaBonus;
                    skillBonus += relic.Effect.SkillDeltaBonus;
                    mentalBonus += relic.Effect.MentalDeltaBonus;
                    costMultiplier *= relic.Effect.StaminaCostMultiplier;
                }
            }

            return new RelicEffect(staminaBonus, skillBonus, mentalBonus, costMultiplier);
        }

        /// <summary>
        /// プレイヤーの行動を適用した GameState と、ボスへ与えるダメージ量を返す。
        /// </summary>
        private (GameState state, int damageDealtToBoss) ApplyPlayerAction(
            GameState playerState,
            BattlePlayerActionKind action,
            RelicEffect relicEffect,
            GameRulesSO rules)
        {
            switch (action)
            {
                case BattlePlayerActionKind.Attack:
                {
                    int staminaCost = Mathf.Max(0, Mathf.RoundToInt(_attackStaminaCost * relicEffect.StaminaCostMultiplier));
                    int effectiveSkill = Mathf.Max(0, playerState.Skill + relicEffect.SkillDeltaBonus);
                    int damage = Mathf.Max(0, Mathf.RoundToInt(effectiveSkill * _skillToDamageMultiplier));

                    int stamina = TurnRules.Clamp(
                        playerState.Stamina - staminaCost + relicEffect.StaminaDeltaBonus, rules.ParamMin, rules.ParamMax);
                    int mental = TurnRules.Clamp(playerState.Mental + relicEffect.MentalDeltaBonus, rules.ParamMin, rules.ParamMax);

                    return (playerState with { Stamina = stamina, Mental = mental }, damage);
                }

                case BattlePlayerActionKind.Defend:
                {
                    int stamina = TurnRules.Clamp(
                        playerState.Stamina + _defendStaminaRecoveryAmount + relicEffect.StaminaDeltaBonus,
                        rules.ParamMin, rules.ParamMax);
                    int mental = TurnRules.Clamp(playerState.Mental + relicEffect.MentalDeltaBonus, rules.ParamMin, rules.ParamMax);

                    return (playerState with { Stamina = stamina, Mental = mental }, 0);
                }

                case BattlePlayerActionKind.MentalFocus:
                {
                    int mental = TurnRules.Clamp(
                        playerState.Mental + _mentalFocusRecoveryAmount + relicEffect.MentalDeltaBonus,
                        rules.ParamMin, rules.ParamMax);
                    int stamina = TurnRules.Clamp(playerState.Stamina + relicEffect.StaminaDeltaBonus, rules.ParamMin, rules.ParamMax);

                    return (playerState with { Stamina = stamina, Mental = mental }, 0);
                }

                default:
                    return (playerState, 0);
            }
        }

        /// <summary>
        /// ボスへダメージを適用する。Shield が残っている場合は Shield から先に減算する。
        /// </summary>
        private BossState ApplyDamageToBoss(BossState bossState, int damage)
        {
            if (damage <= 0)
            {
                return bossState;
            }

            int remainingDamage = damage;
            int shield = bossState.Shield;

            if (shield > 0)
            {
                int absorbed = Mathf.Min(shield, remainingDamage);
                shield -= absorbed;
                remainingDamage -= absorbed;
            }

            int hp = Mathf.Max(0, bossState.CurrentHp - remainingDamage);

            return bossState with { CurrentHp = hp, Shield = shield };
        }

        /// <summary>
        /// ボスの行動を適用した GameState / BossState と、プレイヤーが受けたダメージ内訳を返す。
        /// </summary>
        private (GameState playerState, BossState bossState, int staminaDamage, int mentalDamage) ApplyBossAction(
            GameState playerState,
            BossState bossState,
            BossActionKind action,
            BossSO bossData,
            GameRulesSO rules)
        {
            switch (action)
            {
                case BossActionKind.Attack:
                {
                    int mitigated = MitigateByMentalShield(bossData.AttackPower, playerState.Mental);
                    int stamina = TurnRules.Clamp(playerState.Stamina - mitigated, rules.ParamMin, rules.ParamMax);

                    return (playerState with { Stamina = stamina }, bossState, mitigated, 0);
                }

                case BossActionKind.MentalPressure:
                {
                    int mental = TurnRules.Clamp(
                        playerState.Mental - bossData.MentalPressurePower, rules.ParamMin, rules.ParamMax);

                    return (playerState with { Mental = mental }, bossState, 0, bossData.MentalPressurePower);
                }

                case BossActionKind.Guard:
                {
                    BossState guardedBoss = bossState with { Shield = bossState.Shield + bossData.GuardShieldAmount };

                    return (playerState, guardedBoss, 0, 0);
                }

                case BossActionKind.SpecialAttack:
                {
                    int rawDamage = Mathf.RoundToInt(bossData.AttackPower * bossData.SpecialAttackMultiplier);
                    int mitigated = MitigateByMentalShield(rawDamage, playerState.Mental);
                    int stamina = TurnRules.Clamp(playerState.Stamina - mitigated, rules.ParamMin, rules.ParamMax);

                    return (playerState with { Stamina = stamina }, bossState, mitigated, 0);
                }

                default:
                    return (playerState, bossState, 0, 0);
            }
        }

        /// <summary>
        /// Mental の残量に応じて、スタミナダメージを軽減する。
        /// Mental が MentalShieldFullValue 以上であれば軽減率100%（ノーダメージ）になる。
        /// </summary>
        private int MitigateByMentalShield(int rawDamage, int mental)
        {
            if (_mentalShieldFullValue <= 0)
            {
                return Mathf.Max(0, rawDamage);
            }

            float mitigationRatio = Mathf.Clamp01((float)mental / _mentalShieldFullValue);
            return Mathf.Max(0, Mathf.RoundToInt(rawDamage * (1f - mitigationRatio)));
        }

        private BattleOutcomeKind DetermineOutcome(GameState playerState, BossState bossState)
        {
            if (bossState.IsDefeated)
            {
                return BattleOutcomeKind.Victory;
            }

            if (playerState.Stamina <= 0)
            {
                return BattleOutcomeKind.Defeat;
            }

            return BattleOutcomeKind.InProgress;
        }

        /// <summary>
        /// オートバトラーにおけるプレイヤーの最適な自動行動を決定する。
        /// 1. スタミナが Attack コスト未満の場合: Defend（スタミナ回復）
        /// 2. メンタルが危険領域でボスの強攻撃が来る場合: MentalFocus
        /// 3. それ以外: Attack（攻撃）
        /// </summary>
        public BattlePlayerActionKind DecidePlayerAction(GameState playerState, BossState bossState, BossSO bossData)
        {
            if (playerState.Stamina < _attackStaminaCost)
            {
                return BattlePlayerActionKind.Defend;
            }

            BossActionKind nextBossAction = bossData.GetActionForTurn(bossState.BattleTurn);
            if ((nextBossAction == BossActionKind.Attack || nextBossAction == BossActionKind.SpecialAttack)
                && playerState.Mental < (_mentalShieldFullValue / 2)
                && playerState.Stamina >= _attackStaminaCost * 2)
            {
                return BattlePlayerActionKind.MentalFocus;
            }

            return BattlePlayerActionKind.Attack;
        }

        /// <summary>
        /// 戦闘開始から決着（Victory または Defeat）まで全自動で交戦をシミュレートし、ターン履歴と最終結果を返す。
        /// </summary>
        public FullBattleResult ResolveFullBattle(
            GameState initialPlayerState,
            BossSO bossData,
            IReadOnlyList<RelicSO> activeRelics,
            GameRulesSO rules,
            int maxBattleTurns = 50)
        {
            BossState currentBoss = bossData.CreateInitialState();
            GameState currentPlayer = initialPlayerState;
            List<BattleTurnResult> turnHistory = new List<BattleTurnResult>();

            for (int t = 1; t <= maxBattleTurns; t++)
            {
                BattlePlayerActionKind action = DecidePlayerAction(currentPlayer, currentBoss, bossData);
                BattleTurnResult result = ResolveTurn(currentPlayer, currentBoss, action, bossData, activeRelics, rules);
                turnHistory.Add(result);

                currentPlayer = result.PlayerState;
                currentBoss = result.BossState;

                if (result.Outcome != BattleOutcomeKind.InProgress)
                {
                    return new FullBattleResult(result.Outcome, currentPlayer, currentBoss, turnHistory);
                }
            }

            // 上限ターン到達時はスタミナ切れ扱い（Defeat）
            return new FullBattleResult(BattleOutcomeKind.Defeat, currentPlayer, currentBoss, turnHistory);
        }
    }
}
