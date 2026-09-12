// SPDX-AI-Disclosure: ai-generated
using System;
using System.Reflection;
using Game.Features.MetaProgression;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// テストから MetaPointResolverSO の private フィールドへ値を注入するためのヘルパー。
    /// .asset を作らずに任意の係数を持つ MetaPointResolverSO インスタンスを組み立てる。
    /// 引数順は [SerializeField] の宣言順と同一にしてある。
    /// </summary>
    internal static class MetaPointResolverSOFactory
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static MetaPointResolverSO Create(
            int turnBonusPerTurn, int skillBonusMultiplier,
            int bossDefeatedBonus, int gameClearBonus)
        {
            MetaPointResolverSO resolver = ScriptableObject.CreateInstance<MetaPointResolverSO>();

            SetField(resolver, "_turnBonusPerTurn", turnBonusPerTurn);
            SetField(resolver, "_skillBonusMultiplier", skillBonusMultiplier);
            SetField(resolver, "_bossDefeatedBonus", bossDefeatedBonus);
            SetField(resolver, "_gameClearBonus", gameClearBonus);

            return resolver;
        }

        private static void SetField(MetaPointResolverSO target, string fieldName, int value)
        {
            FieldInfo field = typeof(MetaPointResolverSO).GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"MetaPointResolverSO にフィールド '{fieldName}' が見つかりません。" +
                    "フィールド名がリネームされていないか、MetaPointResolverSOFactory を確認してください。");
            }

            field.SetValue(target, value);
        }
    }
}
