// SPDX-AI-Disclosure: ai-generated
namespace Game.Core
{
    /// <summary>
    /// ターン加算・終了判定・クランプを行う純粋関数群。
    /// static フィールドや外部状態（Time、Random、DateTime、Debug 等）は参照しない。
    /// </summary>
    public static class TurnRules
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        public static GameState AdvanceTurn(GameState state)
        {
            return state with { CurrentTurn = state.CurrentTurn + 1 };
        }

        public static TerminationKind EvaluateTermination(GameState state, int maxTurn)
        {
            if (state.Mental <= 0)
            {
                return TerminationKind.GameOver;
            }

            if (state.CurrentTurn > maxTurn)
            {
                return TerminationKind.NormalEnd;
            }

            return TerminationKind.Continue;
        }
    }
}
