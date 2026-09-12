// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Boss
{
    /// <summary>
    /// ボス1体の定義データ（ID、表示名、最大HP、攻撃力、行動パターン）。
    /// メタデータキーは _bossId ではなく name（アセット名。例: Boss_Act1_01）で管理し、
    /// _bossId は戦闘計算内での軽量な参照キーとして使う（GameDesignMaster.md 3.1章）。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss/Boss", fileName = "Boss")]
    public class BossSO : ScriptableObject
    {
        [SerializeField] private int _bossId;
        [SerializeField] private string _bossName;
        [SerializeField] private int _maxHp;
        [SerializeField] private int _attackPower;
        [SerializeField] private int _mentalPressurePower;
        [SerializeField] private int _guardShieldAmount;
        [SerializeField] private float _specialAttackMultiplier = 2f;
        [SerializeField] private List<BossActionKind> _actionPattern = new List<BossActionKind>();

        public int BossId => _bossId;

        public string BossName => _bossName;

        public int MaxHp => _maxHp;

        public int AttackPower => _attackPower;

        public int MentalPressurePower => _mentalPressurePower;

        /// <summary>Guard 行動1回で自身に付与する Shield 量。</summary>
        public int GuardShieldAmount => _guardShieldAmount;

        /// <summary>SpecialAttack のダメージ倍率（AttackPower に乗算）。0 以下は 1 として扱う。</summary>
        public float SpecialAttackMultiplier => _specialAttackMultiplier <= 0f ? 1f : _specialAttackMultiplier;

        public IReadOnlyList<BossActionKind> ActionPattern => _actionPattern;

        /// <summary>
        /// MaxHp を起点とした、戦闘開始時点の BossState を組み立てて返す。
        /// </summary>
        public BossState CreateInitialState()
        {
            return new BossState
            {
                BossId = _bossId,
                CurrentHp = _maxHp,
                MaxHp = _maxHp,
                Shield = 0,
                BattleTurn = 1
            };
        }

        /// <summary>
        /// 指定された戦闘ターン数（1 始まり）に対応する行動を、ActionPattern から周期的に取得する。
        /// ActionPattern が未設定または空の場合は Attack にフォールバックする。
        /// </summary>
        public BossActionKind GetActionForTurn(int battleTurn)
        {
            if (_actionPattern == null || _actionPattern.Count == 0)
            {
                return BossActionKind.Attack;
            }

            int index = (battleTurn - 1) % _actionPattern.Count;
            if (index < 0)
            {
                index += _actionPattern.Count;
            }

            return _actionPattern[index];
        }
    }
}
