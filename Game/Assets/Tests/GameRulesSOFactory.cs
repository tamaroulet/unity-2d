// SPDX-AI-Disclosure: ai-generated
using System;
using System.Reflection;
using Game.Features.Command;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// テストから GameRulesSO の private フィールドへ値を注入するためのヘルパー。
    /// .asset を作らずに任意の値を持つ GameRulesSO インスタンスを組み立てる。
    /// </summary>
    internal static class GameRulesSOFactory
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static GameRulesSO Create(
            int paramMin, int paramMax,
            int initialStamina, int initialSkill, int initialMental,
            int startTurn, int maxTurn)
        {
            GameRulesSO rules = ScriptableObject.CreateInstance<GameRulesSO>();

            SetField(rules, "_paramMin", paramMin);
            SetField(rules, "_paramMax", paramMax);
            SetField(rules, "_initialStamina", initialStamina);
            SetField(rules, "_initialSkill", initialSkill);
            SetField(rules, "_initialMental", initialMental);
            SetField(rules, "_startTurn", startTurn);
            SetField(rules, "_maxTurn", maxTurn);

            return rules;
        }

        private static void SetField(GameRulesSO target, string fieldName, int value)
        {
            FieldInfo field = typeof(GameRulesSO).GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"GameRulesSO にフィールド '{fieldName}' が見つかりません。" +
                    "フィールド名がリネームされていないか、GameRulesSOFactory を確認してください。");
            }

            field.SetValue(target, value);
        }
    }
}
