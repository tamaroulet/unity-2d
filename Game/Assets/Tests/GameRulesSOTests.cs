// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using Game.Features.Command;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GameRulesSOTests
    {
        [Test]
        public void ReturnsInitialStateMatchingConfiguredValues()
        {
            GameRulesSO rules = GameRulesSOFactory.Create(
                paramMin: 0, paramMax: 100,
                initialStamina: 100, initialSkill: 0, initialMental: 50,
                startTurn: 1, maxTurn: 24);

            try
            {
                GameState state = rules.CreateInitialState();

                Assert.AreEqual(rules.StartTurn, state.CurrentTurn);
                Assert.AreEqual(rules.InitialStamina, state.Stamina);
                Assert.AreEqual(rules.InitialSkill, state.Skill);
                Assert.AreEqual(rules.InitialMental, state.Mental);
            }
            finally
            {
                Object.DestroyImmediate(rules);
            }
        }
    }
}
