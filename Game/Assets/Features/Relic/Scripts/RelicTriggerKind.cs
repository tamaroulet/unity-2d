// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.Relic
{
    /// <summary>
    /// レリックのパッシブ効果が発動するタイミング。
    /// </summary>
    public enum RelicTriggerKind
    {
        /// <summary>
        /// ターン開始時に発動（毎ターンのパラメータ変動）。
        /// </summary>
        OnTurnStart = 0,

        /// <summary>
        /// コマンド実行直後（実行効果に加算・乗算）。
        /// </summary>
        OnCommandExecuted = 1,

        /// <summary>
        /// ターン終了時に発動。
        /// </summary>
        OnTurnEnd = 2
    }
}
