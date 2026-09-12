// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Event;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// テストから GameEventSO / GameEventCatalogSO の private フィールドへ値を
    /// 注入するためのヘルパー。.asset を作らずに任意の値を持つインスタンスを組み立てる。
    /// </summary>
    internal static class GameEventSOFactory
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static GameEventSO CreateEvent(
            int eventId,
            EventTriggerKind triggerKind,
            int triggerTurn,
            TrackedParameter targetParameter,
            int threshold,
            int priority,
            int staminaDelta = 0,
            int skillDelta = 0,
            int mentalDelta = 0,
            string displayName = "",
            string body = "")
        {
            GameEventSO gameEvent = ScriptableObject.CreateInstance<GameEventSO>();

            SetField(gameEvent, "_eventId", eventId);
            SetField(gameEvent, "_displayName", displayName);
            SetField(gameEvent, "_body", body);
            SetField(gameEvent, "_triggerKind", triggerKind);
            SetField(gameEvent, "_triggerTurn", triggerTurn);
            SetField(gameEvent, "_targetParameter", targetParameter);
            SetField(gameEvent, "_threshold", threshold);
            SetField(gameEvent, "_priority", priority);
            SetField(gameEvent, "_staminaDelta", staminaDelta);
            SetField(gameEvent, "_skillDelta", skillDelta);
            SetField(gameEvent, "_mentalDelta", mentalDelta);

            return gameEvent;
        }

        public static GameEventCatalogSO CreateCatalog(int paramMin, int paramMax, params GameEventSO[] events)
        {
            GameEventCatalogSO catalog = ScriptableObject.CreateInstance<GameEventCatalogSO>();

            SetField(catalog, "_paramMin", paramMin);
            SetField(catalog, "_paramMax", paramMax);
            SetField(catalog, "_events", new List<GameEventSO>(events));

            return catalog;
        }

        private static void SetField(ScriptableObject target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。" +
                    "フィールド名がリネームされていないか、GameEventSOFactory を確認してください。");
            }

            field.SetValue(target, value);
        }
    }
}
