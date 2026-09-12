// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.GameFlow;
using Game.Features.Relic;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Relic.Editor
{
    public static class RelicSceneBinder
    {
        [MenuItem("Tools/Bind Relics to MainGame Scene")]
        public static void BindRelicsToScene()
        {
            string dir = "Assets/Features/Relic/Instances";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/Features/Relic", "Instances");
            }

            // 1. RelicResolver.asset の作成
            string resolverPath = $"{dir}/RelicResolver.asset";
            RelicResolverSO relicResolver = AssetDatabase.LoadAssetAtPath<RelicResolverSO>(resolverPath);
            if (relicResolver == null)
            {
                relicResolver = ScriptableObject.CreateInstance<RelicResolverSO>();
                AssetDatabase.CreateAsset(relicResolver, resolverPath);
            }

            // 2. RelicAcquiredChannel.asset の作成
            string channelPath = $"{dir}/RelicAcquiredChannel.asset";
            RelicAcquiredChannelSO relicChannel = AssetDatabase.LoadAssetAtPath<RelicAcquiredChannelSO>(channelPath);
            if (relicChannel == null)
            {
                relicChannel = ScriptableObject.CreateInstance<RelicAcquiredChannelSO>();
                AssetDatabase.CreateAsset(relicChannel, channelPath);
            }

            RelicCatalogSO catalog = AssetDatabase.LoadAssetAtPath<RelicCatalogSO>($"{dir}/RelicCatalog.asset");

            AssetDatabase.SaveAssets();

            // 3. MainGame.unity シーンを開く
            string scenePath = "Assets/Scenes/MainGame.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 4. GameFlowController を探してバインド
            GameFlowController flowController = Object.FindFirstObjectByType<GameFlowController>();
            if (flowController != null)
            {
                SerializedObject so = new SerializedObject(flowController);
                so.FindProperty("_relicCatalog").objectReferenceValue = catalog;
                so.FindProperty("_relicResolver").objectReferenceValue = relicResolver;
                so.FindProperty("_relicAcquiredChannel").objectReferenceValue = relicChannel;
                so.ApplyModifiedProperties();
                Debug.Log("[RelicSceneBinder] GameFlowController bound with Relic assets.");
            }

            // 5. Canvas 配下に RelicDraftDialogPanel を配置
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find("RelicDraftDialogPanel");
                GameObject dialogGo;
                if (existing != null)
                {
                    dialogGo = existing.gameObject;
                }
                else
                {
                    dialogGo = new GameObject("RelicDraftDialogPanel");
                    dialogGo.transform.SetParent(canvas.transform, false);
                }

                RectTransform rect = dialogGo.GetComponent<RectTransform>() ?? dialogGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                RelicDraftDialogView dialogView = dialogGo.GetComponent<RelicDraftDialogView>() ?? dialogGo.AddComponent<RelicDraftDialogView>();

                // カード3枚の子要素を作成
                List<RelicCardView> cardViews = new List<RelicCardView>();
                for (int i = 1; i <= 3; i++)
                {
                    string cardName = $"Card_{i}";
                    Transform cardTr = dialogGo.transform.Find(cardName);
                    GameObject cardGo = cardTr != null ? cardTr.gameObject : new GameObject(cardName);
                    cardGo.transform.SetParent(dialogGo.transform, false);

                    RelicCardView cardView = cardGo.GetComponent<RelicCardView>() ?? cardGo.AddComponent<RelicCardView>();
                    cardViews.Add(cardView);
                }

                SerializedObject dSo = new SerializedObject(dialogView);
                dSo.FindProperty("_relicAcquiredChannel").objectReferenceValue = relicChannel;
                dSo.FindProperty("_panelRoot").objectReferenceValue = dialogGo;
                SerializedProperty cardsProp = dSo.FindProperty("_cardViews");
                cardsProp.ClearArray();
                for (int i = 0; i < cardViews.Count; i++)
                {
                    cardsProp.InsertArrayElementAtIndex(i);
                    cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardViews[i];
                }
                dSo.ApplyModifiedProperties();

                dialogGo.SetActive(false); // 初期状態は非表示
                Debug.Log("[RelicSceneBinder] RelicDraftDialogPanel configured in Canvas.");
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RelicSceneBinder] MainGame.unity scene saved successfully.");
        }
    }
}
