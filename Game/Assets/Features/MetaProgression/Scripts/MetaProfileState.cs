// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.MetaProgression
{
    /// <summary>
    /// 周回を跨いで永続化される、シリアライズしない実行時の不変状態。
    /// PlayerPrefs への保存・復元は上位層（GameFlow 等）の責務とし、本 record 自体は
    /// GameState / BossState と同様に with 式による新しいインスタンスの生成で状態変化を表現する。
    /// </summary>
    public sealed record MetaProfileState
    {
        /// <summary>現在使用可能な（未消費の）MetaPoints。アンロック購入で減算される。</summary>
        public int AvailableMetaPoints { get; init; }

        /// <summary>過去の全周回で獲得した MetaPoints の累計。消費しても減らない実績値。</summary>
        public int TotalEarnedMetaPoints { get; init; }

        /// <summary>これまでに完了（クリアまたはゲームオーバーで終了）した周回数。</summary>
        public int TotalRunsCompleted { get; init; }

        /// <summary>購入済み MetaUnlockSO.UnlockId の一覧。重複は含まない。</summary>
        public System.Collections.Generic.IReadOnlyList<int> UnlockedIds { get; init; } =
            System.Array.Empty<int>();
    }
}
