// SPDX-AI-Disclosure: ai-generated
using Game.Core;

namespace Game.Features.Event
{
    /// <summary>
    /// EventResolverSO.Resolve() の戻り値。適用後の状態、発火したイベント、発火の有無を保持する。
    /// </summary>
    public record EventResult
    {
        public GameState State { get; init; }

        public GameEventSO FiredEvent { get; init; }

        public bool HasFired { get; init; }
    }
}
