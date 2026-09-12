// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Features.Event;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 指示書 31 (§1-B): 到達不能なイベント発火設定を検出するテスト。
    /// GameEventCatalogSO に登録された全イベントについて、
    /// TriggerKind == TurnReached のものの TriggerTurn が、ボス戦ターン {6, 12, 18, 24} と衝突しないことを検証する。
    /// </summary>
    public class GameEventTurnConflictTests
    {
        private static readonly int[] BossBattleTurns = { 6, 12, 18, 24 };

        [Test]
        public void EventCatalog_TurnEvents_DoNotConflictWithBossBattleTurns()
        {
            GameEventCatalogSO catalog = Resources.Load<GameEventCatalogSO>("GameEventCatalog");
            if (catalog == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:GameEventCatalogSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    catalog = AssetDatabase.LoadAssetAtPath<GameEventCatalogSO>(path);
                }
            }
            Assert.IsNotNull(catalog, "Failed to load GameEventCatalogSO via Resources.Load or AssetDatabase.FindAssets");

            List<string> conflicts = new List<string>();
            foreach (GameEventSO evt in catalog.Events)
            {
                if (evt == null) continue;
                if (evt.TriggerKind == EventTriggerKind.TurnReached)
                {
                    foreach (int bossTurn in BossBattleTurns)
                    {
                        if (evt.TriggerTurn == bossTurn)
                        {
                            conflicts.Add($"Event '{evt.DisplayName}' (id={evt.EventId}, asset={evt.name}) triggers at turn {evt.TriggerTurn}, which conflicts with boss battle turn {bossTurn}");
                        }
                    }
                }
            }

            if (conflicts.Count > 0)
            {
                Assert.Fail($"Turn conflict detected in GameEventCatalog:\n  - {string.Join("\n  - ", conflicts)}");
            }
        }
    }
}
#endif
