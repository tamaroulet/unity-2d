// SPDX-AI-Disclosure: ai-generated
namespace Game.Core
{
    /// <summary>
    /// ターン制育成シミュレーションの、シリアライズしない実行時の不変状態。
    /// 状態変化は with 式による新しいインスタンスの生成で表現する。
    /// </summary>
    public record GameState
    {
        public int CurrentTurn { get; init; }

        public int Stamina { get; init; }

        public int Skill { get; init; }

        public int Mental { get; init; }

        /// <summary>
        /// 発火済みイベントのビットマスク。イベント ID n が発火済みのとき、ビット n が立つ。
        /// ulong（64ビット）とすることで、イベント ID 0〜63 まで扱える。
        /// </summary>
        public ulong FiredEventMask { get; init; }

        /// <summary>
        /// プレイヤーが現在所持しているレリックの ID 一覧。
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<int> AcquiredRelicIds { get; init; } =
            System.Array.Empty<int>();
    }
}
