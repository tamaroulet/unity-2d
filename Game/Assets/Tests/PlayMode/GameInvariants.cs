// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Features.GameFlow;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// 指示書 26: 6 つの不変条件 (INV-1〜6) を検査するための PlayMode ヘルパー。
    /// プロダクションコードを一切汚染せず、外部観測とアサーションのみを行う。
    /// </summary>
    public static class GameInvariants
    {
        public static string GetHierarchyPath(Transform t)
        {
            if (t == null) return string.Empty;
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        /// <summary>
        /// INV-1: どのフェーズも WaitingInput または終端に到達せずに 10 秒以上留まらない。
        /// </summary>
        public static void AssertPhaseProgress(
            GameFlowController flow,
            ref GamePhase lastPhase,
            ref float phaseEnterTime,
            string lastAction,
            float timeoutSeconds = 10f)
        {
            GamePhase current = flow.CurrentPhase;
            if (current != lastPhase)
            {
                lastPhase = current;
                phaseEnterTime = Time.realtimeSinceStartup;
            }
            else
            {
                if (current != GamePhase.WaitingInput && current != GamePhase.GameOver && current != GamePhase.GameClear)
                {
                    float elapsed = Time.realtimeSinceStartup - phaseEnterTime;
                    if (elapsed > timeoutSeconds)
                    {
                        Assert.Fail($"[INV-1] Phase '{current}' did not advance for {elapsed:F2}s (timeout: {timeoutSeconds:F2}s). Last action: {lastAction}");
                    }
                }
            }
        }

        /// <summary>
        /// INV-2: 終端（GameOver または GameClear）到達時に EndingDecidedChannelSO が 1 回以上 Raise されていること。
        /// </summary>
        public static void AssertEndingDecided(int endingRaisedCount, GamePhase finalPhase)
        {
            Assert.IsTrue(
                finalPhase == GamePhase.GameOver || finalPhase == GamePhase.GameClear,
                $"[INV-2] Game has not reached a terminal phase. Current: {finalPhase}");
            Assert.GreaterOrEqual(
                endingRaisedCount, 1,
                $"[INV-2] Terminal phase '{finalPhase}' reached, but EndingDecidedChannel was not raised.");
        }

        /// <summary>
        /// INV-3: WaitingInput の間、Canvas 配下に raycastTarget==true かつ Canvas の 90% 以上を覆う Graphic が 1 つも無い。
        /// </summary>
        public static void AssertNoFullscreenBlockerInWaitingInput(Canvas canvas, float coverageThreshold = 0.9f)
        {
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            float canvasWidth = Mathf.Abs(canvasCorners[2].x - canvasCorners[0].x);
            float canvasHeight = Mathf.Abs(canvasCorners[2].y - canvasCorners[0].y);
            float canvasArea = canvasWidth * canvasHeight;
            if (canvasArea <= 0.001f) return;

            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(false);
            List<string> blockers = new List<string>();

            foreach (Graphic g in graphics)
            {
                if (!g.raycastTarget) continue;

                RectTransform gRect = g.rectTransform;
                Vector3[] gCorners = new Vector3[4];
                gRect.GetWorldCorners(gCorners);
                float gWidth = Mathf.Abs(gCorners[2].x - gCorners[0].x);
                float gHeight = Mathf.Abs(gCorners[2].y - gCorners[0].y);
                float gArea = gWidth * gHeight;

                if (gArea / canvasArea >= coverageThreshold)
                {
                    blockers.Add($"'{GetHierarchyPath(g.transform)}' (Type: {g.GetType().Name}, AreaRatio: {gArea / canvasArea:P1})");
                }
            }

            if (blockers.Count > 0)
            {
                Assert.Fail($"[INV-3] Fullscreen raycast blocker(s) found during WaitingInput:\n" + string.Join("\n", blockers));
            }
        }

        /// <summary>
        /// INV-4: 表示中の全 TextMeshProUGUI について、割り当てフォントに全文字のグリフが存在する。
        /// </summary>
        public static void AssertAllTextGlyphsPresent()
        {
            TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            List<string> missingList = new List<string>();
            foreach (TextMeshProUGUI tmp in texts)
            {
                if (!tmp.isActiveAndEnabled || string.IsNullOrEmpty(tmp.text)) continue;

                TMP_FontAsset font = tmp.font;
                if (font == null) continue;

                if (!font.HasCharacters(tmp.text, out List<char> missingChars))
                {
                    if (missingChars != null && missingChars.Count > 0)
                    {
                        string chars = string.Join(", ", missingChars.Select(c => $"'{c}'(U+{(int)c:X4})"));
                        missingList.Add($"'{GetHierarchyPath(tmp.transform)}' (Text: \"{tmp.text}\") missing glyphs: {chars}");
                    }
                }
            }

            if (missingList.Count > 0)
            {
                Assert.Fail($"[INV-4] Missing font glyphs detected:\n" + string.Join("\n", missingList));
            }
        }

        /// <summary>
        /// INV-5: 表示中かつ interactable==true の全 Button について、中心への Raycast の最初のヒットが自分または子孫。
        /// </summary>
        public static void AssertInteractableButtonsReachable()
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (Button btn in buttons)
            {
                if (!btn.isActiveAndEnabled || !btn.interactable) continue;

                AssertRaycastReachesButton(btn, btn.gameObject.name);
            }
        }

        public static void AssertRaycastReachesButton(Button button, string buttonName)
        {
            Assert.IsTrue(button != null, $"{buttonName} is null.");
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"{buttonName} is not active in hierarchy.");

            Canvas canvas = button.GetComponentInParent<Canvas>();
            Assert.IsTrue(canvas != null, $"Canvas not found for {buttonName}.");
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            Assert.IsTrue(raycaster != null, $"GraphicRaycaster not found on Canvas for {buttonName}.");

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, button.transform.position);

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPoint
            };

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            Assert.IsTrue(results.Count > 0, $"[INV-5] Raycast hit nothing at {buttonName} screen position {screenPoint}.");

            GameObject topHit = results[0].gameObject;
            bool isTargetOrChild = topHit == button.gameObject || topHit.transform.IsChildOf(button.transform);
            Assert.IsTrue(
                isTargetOrChild,
                $"[INV-5] Raycast to '{buttonName}' was blocked by '{topHit.name}' (Hierarchy: {GetHierarchyPath(topHit.transform)}). Target: {button.gameObject.name}");
        }

        /// <summary>
        /// INV-6: 1 ランを通した後、イベント・ボス・ドラフトの各発火回数がすべて 1 以上であること。
        /// </summary>
        public static void AssertSubsystemsFired(int eventCount, int bossCount, int draftCount)
        {
            List<string> failures = new List<string>();
            if (eventCount < 1) failures.Add($"GameEventFiredChannelSO fired {eventCount} times (< 1)");
            if (bossCount < 1) failures.Add($"Boss battle occurred {bossCount} times (< 1)");
            if (draftCount < 1) failures.Add($"Relic draft requested {draftCount} times (< 1)");

            if (failures.Count > 0)
            {
                Assert.Fail($"[INV-6] Subsystem activation invariant violated:\n  - " + string.Join("\n  - ", failures));
            }
        }
    }
}
