// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.Boss
{
    /// <summary>
    /// ボス戦闘中の、シリアライズしない実行時の不変状態。
    /// 状態変化は with 式による新しいインスタンスの生成で表現する（GameState と同様の設計）。
    /// </summary>
    public record BossState
    {
        /// <summary>対応する BossSO.BossId。</summary>
        public int BossId { get; init; }

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        /// <summary>Guard 行動で蓄積するシールド値。被ダメージ時に HP より先に減算される。</summary>
        public int Shield { get; init; }

        /// <summary>この戦闘内での経過ターン数（1 始まり）。ActionPattern の周期選択に使用する。</summary>
        public int BattleTurn { get; init; }

        public bool IsDefeated => CurrentHp <= 0;
    }
}
