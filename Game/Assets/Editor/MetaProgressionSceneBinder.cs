// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorScripts
{
    public static class MetaProgressionSceneBinder
    {
        [MenuItem("Tools/Bind Meta Progression to MainGame Scene")]
        public static void BindToScene()
        {
            string dir = "Assets/Features/MetaProgression/Instances";
            MetaPointResolverSO resolver = AssetDatabase.LoadAssetAtPath<MetaPointResolverSO>($"{dir}/MetaPointResolver.asset");
            MetaUnlockCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MetaUnlockCatalogSO>($"{dir}/MetaUnlockCatalog.asset");

            if (resolver == null || catalog == null)
            {
                Debug.LogError("[MetaProgressionSceneBinder] Assets not found. Run 'Tools/Generate Meta Progression Assets' first.");
                return;
            }

            string scenePath = "Assets/Scenes/MainGame.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. GameFlowController へのバインド
            GameFlowController flowController = Object.FindFirstObjectByType<GameFlowController>();
            if (flowController != null)
            {
                SerializedObject so = new SerializedObject(flowController);
                so.FindProperty("_metaPointResolver").objectReferenceValue = resolver;
                so.FindProperty("_metaUnlockCatalog").objectReferenceValue = catalog;
                so.ApplyModifiedProperties();
                Debug.Log("[MetaProgressionSceneBinder] GameFlowController bound with MetaProgression assets.");
            }

            // 2. Canvas 配下に MetaShopDialogPanel を配置
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find("MetaShopDialogPanel");
                GameObject dialogGo = existing != null ? existing.gameObject : new GameObject("MetaShopDialogPanel", typeof(RectTransform));
                dialogGo.transform.SetParent(canvas.transform, false);

                RectTransform rect = dialogGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                MetaShopDialogView dialogView = dialogGo.GetComponent<MetaShopDialogView>() ?? dialogGo.AddComponent<MetaShopDialogView>();

                // PanelRoot
                Transform rootTr = dialogGo.transform.Find("PanelRoot");
                GameObject rootGo = rootTr != null ? rootTr.gameObject : new GameObject("PanelRoot", typeof(RectTransform));
                rootGo.transform.SetParent(dialogGo.transform, false);

                // UI Components
                TextMeshProUGUI pointsText = GetOrCreateText(rootGo, "AvailablePointsText");
                TextMeshProUGUI runsText = GetOrCreateText(rootGo, "TotalRunsText");
                Button closeButton = GetOrCreateButton(rootGo, "CloseButton");

                SerializedObject viewSo = new SerializedObject(dialogView);
                viewSo.FindProperty("_panelRoot").objectReferenceValue = rootGo;
                viewSo.FindProperty("_availablePointsText").objectReferenceValue = pointsText;
                viewSo.FindProperty("_totalRunsText").objectReferenceValue = runsText;
                viewSo.FindProperty("_closeButton").objectReferenceValue = closeButton;
                viewSo.ApplyModifiedProperties();

                rootGo.SetActive(false);
                Debug.Log("[MetaProgressionSceneBinder] MetaShopDialogPanel created and bound on Canvas.");
            }

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[MetaProgressionSceneBinder] MainGame scene saved with MetaProgression integration.");
        }

        private static TextMeshProUGUI GetOrCreateText(GameObject parent, string name)
        {
            Transform tr = parent.transform.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
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
