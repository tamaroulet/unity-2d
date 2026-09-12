// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    /// <summary>
    /// イベントチャンネル SO の実体が欠けていたら作る。
    ///
    /// 型は定義されていて CreateAssetMenu も付いているのに .asset が存在しない、
    /// という状態が起きうる。その場合 UILayoutBuilder は
    /// 「Asset not found ... Existing reference is kept」と警告して結線を諦め、
    /// 参照が null のままシーンが保存される。結果として画面は出るが操作が伝わらず、
    /// 原因の分かりにくいソフトロックになる（ターン 6 の詰みがこれだった）。
    ///
    /// .agents/rules/development-rules.md の改訂に従い、資産の生成は Editor API を通す。
    /// 冪等であり、既に存在すれば何もしない。
    /// </summary>
    public static class MissingChannelAssetCreator
    {
        private const string ChannelDir = "Assets/Data/Channels";

        [MenuItem("Tools/Create Missing Event Channel Assets")]
        public static void CreateMissingChannels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[MissingChannelAssetCreator] Play モード中は実行できません。");
                return;
            }

            EnsureChannel<RelicAcquiredChannelSO>("RelicAcquiredChannel");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureChannel<T>(string fileName) where T : ScriptableObject
        {
            string path = $"{ChannelDir}/{fileName}.asset";

            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[MissingChannelAssetCreator] Already exists: {path}");
                return;
            }

            // 同じ型の資産が別名で置かれている可能性を先に潰す。
            // 重複して作ると購読先が分かれ、イベントが届かなくなる。
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length > 0)
            {
                string found = AssetDatabase.GUIDToAssetPath(guids[0]);
                Debug.LogWarning($"[MissingChannelAssetCreator] {typeof(T).Name} already exists "
                    + $"under a different name: {found}. Not creating {path}.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ChannelDir))
            {
                Debug.LogError($"[MissingChannelAssetCreator] Folder not found: {ChannelDir}");
                return;
            }

            T channel = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(channel, path);
            Debug.Log($"[MissingChannelAssetCreator] Created {path}");
        }
    }
}
#endif
