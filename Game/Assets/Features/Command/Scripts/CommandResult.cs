// SPDX-AI-Disclosure: ai-generated
using Game.Core;

namespace Game.Features.Command
{
    /// <summary>
    /// CommandResolverSO.Resolve() の戻り値。適用後の状態、実行可否、終了種別を保持する。
    /// </summary>
    public record CommandResult
    {
        public GameState State { get; init; }

        public bool IsExecutable { get; init; }

        public TerminationKind Termination { get; init; }
    }
}
