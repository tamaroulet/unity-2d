// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.GameFlow
{
    /// <summary>
    /// GameFlowController が管理するゲーム進行の状態を表す。
    /// </summary>
    public enum GamePhase
    {
        Initializing,
        TurnStart,
        ShowingEvent,
        ShowingRelicDraft,
        BossBattle,
        WaitingInput,
        ExecutingCommand,
        TurnEnd,
        GameOver,
        GameClear
    }
}
