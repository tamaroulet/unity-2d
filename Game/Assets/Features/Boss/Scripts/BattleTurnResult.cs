// SPDX-AI-Disclosure: ai-generated
using Game.Core;

namespace Game.Features.Boss
{
    /// <summary>
    /// AutoBattleResolverSO.ResolveTurn() の戻り値。
    /// 適用後のプレイヤー・ボス双方の状態と、その内訳、勝敗判定結果を保持する。
    /// </summary>
    public record BattleTurnResult
    {
        public GameState PlayerState { get; init; }

        public BossState BossState { get; init; }

        /// <summary>
        /// このターンにボスが実際に取った行動。プレイヤーの行動だけで決着した場合
        /// （Victory または自滅による Defeat）はボスは行動していないため null。
        /// </summary>
        public BossActionKind? BossActionTaken { get; init; }

        /// <summary>プレイヤーの行動によりボスへ与えたダメージ（Shield 吸収前の総量）。</summary>
        public int DamageDealtToBoss { get; init; }

        /// <summary>ボスの行動によりプレイヤーが受けたスタミナダメージ（メンタルシールド軽減後）。</summary>
        public int StaminaDamageTaken { get; init; }

        /// <summary>ボスの行動によりプレイヤーが受けたメンタルダメージ。</summary>
        public int MentalDamageTaken { get; init; }

        public BattleOutcomeKind Outcome { get; init; }
    }
}
