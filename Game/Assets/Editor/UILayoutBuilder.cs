// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorScripts
{
    public static class UILayoutBuilder
    {
        private static readonly Color ColorBgMain = new Color(0.08f, 0.09f, 0.12f, 1.0f);
        private static readonly Color ColorBgHeader = new Color(0.12f, 0.15f, 0.20f, 0.98f);
        private static readonly Color ColorBgFooter = new Color(0.10f, 0.12f, 0.16f, 0.98f);
        private static readonly Color ColorBgDialog = new Color(0.14f, 0.17f, 0.24f, 0.98f);
        private static readonly Color ColorBgBossDialog = new Color(0.20f, 0.10f, 0.12f, 0.98f);
        private static readonly Color ColorButtonBg = new Color(0.18f, 0.22f, 0.30f, 1.0f);
        private static readonly Color ColorBarBg = new Color(0.18f, 0.20f, 0.26f, 1.0f);
        private static readonly Color ColorOverlay = new Color(0.0f, 0.0f, 0.0f, 0.80f);

        private static readonly Color ColorStamina = new Color(0.22f, 0.85f, 0.45f, 1.0f);
        private static readonly Color ColorSkill = new Color(0.25f, 0.65f, 0.98f, 1.0f);
        private static readonly Color ColorMental = new Color(0.92f, 0.35f, 0.65f, 1.0f);
        private static readonly Color ColorBossHp = new Color(0.92f, 0.25f, 0.25f, 1.0f);
        private static readonly Color ColorShield = new Color(0.30f, 0.75f, 0.95f, 1.0f);

        [MenuItem("Tools/Setup Complete UI Layout (Simple Shapes)")]
        public static void SetupCompleteLayout()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[UILayoutBuilder] PlayMode detected. Stopping PlayMode to rebuild UI safely...");
                EditorApplication.isPlaying = false;
            }

            // 1. スプライト生成とインポートを完了
            ProceduralSpriteGenerator.GenerateAllSprites();
            AssetDatabase.SaveAssets();

            // 2. シーンを開いて新規参照を取得
            string scenePath = "Assets/Scenes/MainGame.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 3. Canvas の取得
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem（新 Input System: InputSystemUIInputModule 対応）
            UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
            else
            {
                // 古い StandaloneInputModule があれば除去して InputSystemUIInputModule に置き換え
                UnityEngine.EventSystems.StandaloneInputModule oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null)
                {
                    Object.DestroyImmediate(oldModule);
                }
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            // 古い壊れた子オブジェクトを一括クリア（クリーンビルド: Canvas は子を持たない）
            int childCount = canvas.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                Object.DestroyImmediate(child.gameObject);
            }

            // 4. UiBootstrapper の配置（ADR 0002 段階 5: UI は実行時にコードから構築する）
            if (canvas.GetComponent<UiBootstrapper>() == null)
            {
                canvas.gameObject.AddComponent<UiBootstrapper>();
            }

            // 5. GameFlowController へのバインド
            RebindGameFlowControllerReferences();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UILayoutBuilder] Scene scaffolding (Camera / EventSystem / Canvas / UiBootstrapper) successfully built and saved.");
            SceneBindingReport.GenerateSceneSnapshot();
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/Sprites/{name}.png");
        }


        // --- UI 生成ヘルパー群 ---

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }
            return rect;
        }

        private static void ConfigureModalPanel(Transform panelTr, Vector2 size, Color bgColor)
        {
            EnsureRectTransform(panelTr.gameObject);
            RectTransform rect = panelTr.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image overlayImg = panelTr.GetComponent<Image>() ?? panelTr.gameObject.AddComponent<Image>();
            overlayImg.color = ColorOverlay;

            Transform rootTr = panelTr.Find("PanelRoot");
            GameObject rootGo = rootTr != null ? rootTr.gameObject : new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelTr, false);

            RectTransform rootRect = EnsureRectTransform(rootGo);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = size;
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>() ?? rootGo.AddComponent<Image>();
            rootImg.sprite = LoadSprite("Frame_Card");
            rootImg.type = Image.Type.Sliced;
            rootImg.color = bgColor;
        }

        private static GameObject CreateGaugeGroup(RectTransform parent, string name, string iconName, Color barColor, Vector2 pos, string label)
        {
            Transform existing = parent.Find(name);
            GameObject groupGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            groupGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(groupGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(320, 60);

            // アイコン
            AttachIcon(rect, "Icon", LoadSprite(iconName), new Vector2(-130, 0), new Vector2(40, 40));

            // ゲージ背景
            Transform barBgTr = rect.Find("BarBg");
            GameObject barBgGo = barBgTr != null ? barBgTr.gameObject : new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            barBgGo.transform.SetParent(rect, false);
            RectTransform barBgRect = EnsureRectTransform(barBgGo);
            barBgRect.anchoredPosition = new Vector2(20, -5);
            barBgRect.sizeDelta = new Vector2(220, 24);
            barBgGo.GetComponent<Image>().color = ColorBarBg;

            // ゲージバー
            Transform barFillTr = barBgRect.Find("BarFill");
            GameObject barFillGo = barFillTr != null ? barFillTr.gameObject : new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFillGo.transform.SetParent(barBgRect, false);
            RectTransform barFillRect = EnsureRectTransform(barFillGo);
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0.7f, 1f); // 70% 仮置き
            barFillRect.sizeDelta = Vector2.zero;
            barFillGo.GetComponent<Image>().color = barColor;

            // ラベル
            CreateLabel(rect, "Label", label, new Vector2(20, 15), new Vector2(220, 24), 18, TextAlignmentOptions.Center);
            return groupGo;
        }

        private static void CreateCommandButton(RectTransform parent, string name, string iconName, string text, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(btnGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 120);

            Image img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = ColorButtonBg;

            AttachIcon(rect, "Icon", LoadSprite(iconName), new Vector2(-80, 0), new Vector2(56, 56));
            CreateLabel(rect, "Text", text, new Vector2(40, 0), new Vector2(180, 80), 22, TextAlignmentOptions.Center);
        }

        private static void CreateModalButton(RectTransform parent, string name, string text, Vector2 pos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(btnGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.45f, 0.85f, 1.0f);

            CreateLabel(rect, "Text", text, Vector2.zero, size, 22, TextAlignmentOptions.Center);
        }



        private static void AttachIcon(Transform parent, string iconName, Sprite sprite, Vector2 anchoredPos, Vector2 size)
        {
            if (parent == null) return;
            Transform existing = parent.Find(iconName);
            GameObject iconGo = existing != null ? existing.gameObject : new GameObject(iconName, typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(iconGo);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = iconGo.GetComponent<Image>() ?? iconGo.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private static GameObject CreateLabel(RectTransform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            GameObject labelGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(labelGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>() ?? labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return labelGo;
        }

        private static void RebindGameFlowControllerReferences()
        {
            GameFlowController controller = Object.FindFirstObjectByType<GameFlowController>();
            if (controller == null) return;

            SerializedObject so = new SerializedObject(controller);
            BindAsset<GameRulesSO>(so, "_gameRules", "Assets/Data/Rules/GameRules.asset");
            BindAsset<CommandResolverSO>(so, "_commandResolver", "Assets/Data/Commands/CommandResolver.asset");
            BindAsset<GameEventCatalogSO>(so, "_eventCatalog", "Assets/Resources/GameEventCatalog.asset");
            BindAsset<EventResolverSO>(so, "_eventResolver", "Assets/Data/Events/EventResolver.asset");
            BindAsset<EndingRulesSO>(so, "_endingRules", "Assets/Data/Endings/EndingRules.asset");
            BindAsset<EndingResolverSO>(so, "_endingResolver", "Assets/Data/Endings/EndingResolver.asset");
            BindAsset<GameStateEventChannelSO>(so, "_gameStateChannel", "Assets/Resources/GameStateChannel.asset");
            BindAsset<GameEventFiredChannelSO>(so, "_eventFiredChannel", "Assets/Resources/EventFiredChannel.asset");
            BindAsset<EndingDecidedChannelSO>(so, "_endingDecidedChannel", "Assets/Resources/EndingDecidedChannel.asset");
            BindAsset<RelicCatalogSO>(so, "_relicCatalog", "Assets/Features/Relic/Instances/RelicCatalog.asset");
            BindAsset<RelicResolverSO>(so, "_relicResolver", "Assets/Features/Relic/Instances/RelicResolver.asset");
            BindAsset<BossCatalogSO>(so, "_bossCatalog", "Assets/Features/Boss/Instances/BossCatalog.asset");
            BindAsset<AutoBattleResolverSO>(so, "_autoBattleResolver", "Assets/Features/Boss/Instances/AutoBattleResolver.asset");
            BindAsset<MetaPointResolverSO>(so, "_metaPointResolver", "Assets/Features/MetaProgression/Instances/MetaPointResolver.asset");
            BindAsset<MetaUnlockCatalogSO>(so, "_metaUnlockCatalog", "Assets/Resources/MetaUnlockCatalog.asset");

            int[] bossBattleTurns = { 6, 12, 18, 24 };
            SerializedProperty turnsProp = so.FindProperty("_bossBattleTurns");
            turnsProp.ClearArray();
            turnsProp.arraySize = bossBattleTurns.Length;
            for (int i = 0; i < bossBattleTurns.Length; i++)
            {
                turnsProp.GetArrayElementAtIndex(i).intValue = bossBattleTurns[i];
            }

            so.ApplyModifiedProperties();
        }

        private static void BindComponentReference<T>(SerializedObject so, string propName, Transform rootTr, string containerName, params string[] candidatePaths) where T : Component
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogError($"[UILayoutBuilder] Serialized property not found: {propName}");
                return;
            }

            if (rootTr == null)
            {
                Debug.LogError($"[UILayoutBuilder] Root transform is null, cannot bind {propName}. Existing reference is kept.");
                return;
            }

            T comp = null;
            foreach (string path in candidatePaths)
            {
                Transform targetTr = rootTr.Find(path);
                if (targetTr != null)
                {
                    comp = targetTr.GetComponent<T>();
                    if (comp != null)
                    {
                        break;
                    }
                }
            }

            if (comp != null)
            {
                prop.objectReferenceValue = comp;
            }
            else
            {
                Debug.LogError($"[UILayoutBuilder] Element ({typeof(T).Name}) not found for '{propName}' (searched: {string.Join(", ", candidatePaths)}) under {containerName}. Existing reference is kept.");
            }
        }




        // 参照先が見つからないときは既存の結線を残したままエラーで知らせる。
        // 黙って null を書き込むと、シーン再構築のたびに Inspector のアサインが消えて原因を追えなくなる。
        private static void BindAsset<T>(SerializedObject so, string propName, string assetPath) where T : Object
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogError($"[UILayoutBuilder] Serialized property not found: {propName}");
                return;
            }

            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[UILayoutBuilder] Asset not found: {assetPath} ({typeof(T).Name}). Existing reference on {propName} is kept.");
                return;
            }

            prop.objectReferenceValue = asset;
        }

        private static void SetViewProperty(SerializedObject so, string propName, Transform tr)
        {
            if (tr == null) return;
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                Component view = tr.GetComponent<MonoBehaviour>();
                if (view != null)
                {
                    prop.objectReferenceValue = view;
                }
            }
        }
    }
}
#endif
