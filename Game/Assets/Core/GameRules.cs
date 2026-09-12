using System;
using Game.Core;

namespace Game.Features.Command
{
    /// <summary>
    /// GameRulesSO から UnityEngine 依存を外した純粋版。
    /// 引数順は GameRulesSOFactory.Create と同一。
    /// </summary>
    public class GameRules
    {
        public GameRules(
            int paramMin, int paramMax,
            int initialStamina, int initialSkill, int initialMental,
            int startTurn, int maxTurn)
        {
            ParamMin = paramMin;
            ParamMax = paramMax;
            InitialStamina = initialStamina;
            InitialSkill = initialSkill;
            InitialMental = initialMental;
            StartTurn = startTurn;
            MaxTurn = maxTurn;
        }

        public int ParamMin { get; }
        public int ParamMax { get; }
        public int InitialStamina { get; }
        public int InitialSkill { get; }
        public int InitialMental { get; }
        public int StartTurn { get; }
        public int MaxTurn { get; }

        public GameState CreateInitialState()
        {
            return new GameState
            {
                CurrentTurn = StartTurn,
                Stamina = InitialStamina,
                Skill = InitialSkill,
                Mental = InitialMental
            };
        }
    }
}