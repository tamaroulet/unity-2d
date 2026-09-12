// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.Boss
{
    /// <summary>
    /// ボス戦闘の勝敗判定結果。AutoBattleResolverSO.ResolveTurn() の戻り値に含まれる。
    /// </summary>
    public enum BattleOutcomeKind
    {
        /// <summary>決着していない。戦闘継続。</summary>
        InProgress,

        /// <summary>ボスの CurrentHp が 0 以下になった（プレイヤーの勝利）。</summary>
        Victory,

        /// <summary>プレイヤーの Stamina が 0 以下になった（プレイヤーの敗北）。</summary>
        Defeat
    }
}
