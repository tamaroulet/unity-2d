// SPDX-AI-Disclosure: ai-generated
using System;
using System.Reflection;
using Game.Core;
using Game.Features.Ending;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// テストから EndingRulesSO の private フィールドへ値を注入するためのヘルパー。
    /// .asset を作らずに任意の値を持つ EndingRulesSO インスタンスを組み立てる。
    /// </summary>
    internal static class EndingRulesSOFactory
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static EndingRulesSO Create(
            int skillWeight,
            int mentalWeight,
            int staminaWeight,
            int threshold,
            TrackedParameter[] priorityOrder = null)
        {
            EndingRulesSO rules = ScriptableObject.CreateInstance<EndingRulesSO>();

            SetField(rules, "_skillWeight", skillWeight);
            SetField(rules, "_mentalWeight", mentalWeight);
            SetField(rules, "_staminaWeight", staminaWeight);
            SetField(rules, "_threshold", threshold);

            if (priorityOrder != null)
            {
                SetField(rules, "_priorityOrder", priorityOrder);
            }

            return rules;
        }

        private static void SetField(EndingRulesSO target, string fieldName, object value)
        {
            FieldInfo field = typeof(EndingRulesSO).GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"EndingRulesSO にフィールド '{fieldName}' が見つかりません。" +
                    "フィールド名がリネームされていないか、EndingRulesSOFactory を確認してください。");
            }

            field.SetValue(target, value);
        }
    }
}
