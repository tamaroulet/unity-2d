// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using Game.Features.Boss;
using Game.Features.GameFlow;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorScripts
{
    public static class BossSceneBinder
    {
        [MenuItem("Tools/Bind Boss to MainGame Scene")]
        public static void BindBossToScene()
        {
            string dir = "Assets/Features/Boss/Instances";
            BossCatalogSO catalog = AssetDatabase.LoadAssetAtPath<BossCatalogSO>($"{dir}/BossCatalog.asset");
            AutoBattleResolverSO resolver = AssetDatabase.LoadAssetAtPath<AutoBattleResolverSO>($"{dir}/AutoBattleResolver.asset");

            if (catalog == null || resolver == null)
            {
                Debug.LogError("[BossSceneBinder] Boss assets not found. Run 'Tools/Generate Boss Assets' first.");
                return;
            }

            string scenePath = "Assets/Scenes/MainGame.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. GameFlowController へのバインド
            GameFlowController flowController = Object.FindFirstObjectByType<GameFlowController>();
            if (flowController != null)
            {
                SerializedObject so = new SerializedObject(flowController);
                so.FindProperty("_bossCatalog").objectReferenceValue = catalog;
                so.FindProperty("_autoBattleResolver").objectReferenceValue = resolver;

                // Act 1〜4 のボスバトル発生ターン（BossCatalog 内の BossId 1〜4 に対応する）
                int[] bossBattleTurns = { 6, 12, 18, 24 };
                SerializedProperty turnsProp = so.FindProperty("_bossBattleTurns");
                turnsProp.ClearArray();
                turnsProp.arraySize = bossBattleTurns.Length;
                for (int i = 0; i < bossBattleTurns.Length; i++)
                {
                    turnsProp.GetArrayElementAtIndex(i).intValue = bossBattleTurns[i];
                }

                so.ApplyModifiedProperties();
                Debug.Log("[BossSceneBinder] GameFlowController bound with Boss assets (Act 1-4, Turn 6/12/18/24).");
            }

            // 2. Canvas 配下に BossBattleDialogPanel を配置
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find("BossBattleDialogPanel");
                GameObject dialogGo = existing != null ? existing.gameObject : new GameObject("BossBattleDialogPanel", typeof(RectTransform));
                dialogGo.transform.SetParent(canvas.transform, false);

                RectTransform rect = dialogGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                // BossBattleDialogPanel に既存の View があれば破棄
                BossBattleDialogView oldView = dialogGo.GetComponent<BossBattleDialogView>();
                if (oldView != null)
                {
                    Object.DestroyImmediate(oldView);
                }

                // UIViews に BossBattleDialogView を配置
                Transform viewsTr = canvas.transform.Find("UIViews");
                GameObject viewsGo = viewsTr != null ? viewsTr.gameObject : new GameObject("UIViews", typeof(RectTransform));
                viewsGo.transform.SetParent(canvas.transform, false);
                viewsGo.SetActive(true);

                BossBattleDialogView dialogView = viewsGo.GetComponent<BossBattleDialogView>() ?? viewsGo.AddComponent<BossBattleDialogView>();

                // PanelRoot
                Transform rootTr = dialogGo.transform.Find("PanelRoot");
                GameObject rootGo = rootTr != null ? rootTr.gameObject : new GameObject("PanelRoot", typeof(RectTransform));
                rootGo.transform.SetParent(dialogGo.transform, false);

                // UI Components
                TextMeshProUGUI nameText = GetOrCreateText(rootGo, "BossNameText");
                TextMeshProUGUI hpText = GetOrCreateText(rootGo, "BossHpText");
                Slider hpSlider = GetOrCreateSlider(rootGo, "BossHpSlider");
                TextMeshProUGUI shieldText = GetOrCreateText(rootGo, "ShieldText");
                TextMeshProUGUI battleLogText = GetOrCreateText(rootGo, "BattleLogText");
                Button dismissButton = GetOrCreateButton(rootGo, "DismissButton");

                SerializedObject viewSo = new SerializedObject(dialogView);
                viewSo.FindProperty("_panelRoot").objectReferenceValue = dialogGo;
                viewSo.FindProperty("_bossNameText").objectReferenceValue = nameText;
                viewSo.FindProperty("_bossHpText").objectReferenceValue = hpText;
                viewSo.FindProperty("_bossHpSlider").objectReferenceValue = hpSlider;
                viewSo.FindProperty("_shieldText").objectReferenceValue = shieldText;
                viewSo.FindProperty("_battleLogText").objectReferenceValue = battleLogText;
                viewSo.FindProperty("_dismissButton").objectReferenceValue = dismissButton;
                viewSo.ApplyModifiedProperties();

                rootGo.SetActive(false);
                Debug.Log("[BossSceneBinder] BossBattleDialogPanel created and bound on Canvas.");
            }

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[BossSceneBinder] MainGame scene saved with Boss integration.");
        }

        private static TextMeshProUGUI GetOrCreateText(GameObject parent, string name)
        {
            Transform tr = parent.transform.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        }

        private static Slider GetOrCreateSlider(GameObject parent, string name)
        {
            Transform tr = parent.transform.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go.GetComponent<Slider>() ?? go.AddComponent<Slider>();
        }

        private static Button GetOrCreateButton(GameObject parent, string name)
        {
            Transform tr = parent.transform.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go.GetComponent<Button>() ?? go.AddComponent<Button>();
        }
    }
}
#endif
