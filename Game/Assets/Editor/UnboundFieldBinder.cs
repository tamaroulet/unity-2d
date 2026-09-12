// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorScripts
{
    /// <summary>
    /// 未結線の [SerializeField] に、シーン内の対応する要素を割り当てる。
    ///
    /// 方針:
    ///   ・在るものは結ぶ。無いものは報告するだけで、勝手に作らない。
    ///     UI を新しく作るかどうかは設計の判断であり、このスクリプトが決めることではない。
    ///   ・候補が複数見つかった場合も結ばない。どれが正しいかは人間が決める。
    ///   ・シーンを保存しない。人間が画面で確かめてから保存する。
    ///
    /// 使い方:
    ///   1. Tools/Report Missing UI Targets   … 何も変えずに、結べるもの／無いものを一覧する
    ///   2. Tools/Bind Found UI Targets       … 一意に見つかったものだけを結ぶ（保存はしない）
    ///   3. 画面で確認してから Ctrl+S で保存する
    /// </summary>
    public static class UnboundFieldBinder
    {
        private const string ScenePath = "Assets/Scenes/MainGame.unity";

        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        /// <summary>
        /// フィールド名から、探すべき GameObject 名の手がかりを引く。
        /// 手がかりが無いフィールドは、型が一意に決まるときだけ結ぶ。
        /// </summary>
        private static readonly Dictionary<string, string> NameHints = new Dictionary<string, string>
        {
            { "_staminaGauge", "stamina" },
            { "_skillGauge", "skill" },
            { "_mentalGauge", "mental" },
            { "_costText", "cost" },
            { "_bossHpSlider", "hp" },
            { "_battleLogText", "log" },
        };

        [MenuItem("Tools/Report Missing UI Targets")]
        public static void Report()
        {
            Run(apply: false);
        }

        [MenuItem("Tools/Bind Found UI Targets")]
        public static void Bind()
        {
            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var bound = new List<string>();
            var missing = new List<string>();
            var ambiguous = new List<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb.GetType().Namespace != "Game.UI")
                    {
                        continue;
                    }

                    InspectComponent(mb, apply, bound, missing, ambiguous);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(apply ? "=== 結線を適用した ===" : "=== 報告のみ。何も変えていない ===");
            sb.AppendLine();

            sb.AppendLine($"[結べる／結んだ] {bound.Count} 件");
            foreach (string s in bound) sb.AppendLine("  " + s);
            sb.AppendLine();

            sb.AppendLine($"[対象がシーンに無い] {missing.Count} 件  ← UI を作るかどうかは設計の判断");
            foreach (string s in missing) sb.AppendLine("  " + s);
            sb.AppendLine();

            if (ambiguous.Count > 0)
            {
                sb.AppendLine($"[候補が複数あり結ばなかった] {ambiguous.Count} 件  ← どれが正しいかは人間が決める");
                foreach (string s in ambiguous) sb.AppendLine("  " + s);
                sb.AppendLine();
            }

            if (apply && bound.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                sb.AppendLine("シーンを dirty にした。保存はしていない。");
                sb.AppendLine("画面で確認してから Ctrl+S で保存すること。");
            }

            Debug.Log(sb.ToString());
        }

        private static void InspectComponent(
            MonoBehaviour mb, bool apply,
            List<string> bound, List<string> missing, List<string> ambiguous)
        {
            SerializedObject so = null;

            for (Type t = mb.GetType(); t != null && t.Namespace == "Game.UI"; t = t.BaseType)
            {
                foreach (FieldInfo f in t.GetFields(FieldFlags))
                {
                    if (!f.IsDefined(typeof(SerializeField), true))
                    {
                        continue;
                    }

                    if (!typeof(Component).IsAssignableFrom(f.FieldType))
                    {
                        continue;
                    }

                    if ((Component)f.GetValue(mb) != null)
                    {
                        continue;
                    }

                    string label = $"{Path(mb.transform)} -> {mb.GetType().Name}.{f.Name} ({f.FieldType.Name})";
                    List<Component> found = Search(mb, f.FieldType, f.Name);

                    if (found.Count == 0)
                    {
                        missing.Add(label);
                        continue;
                    }

                    if (found.Count > 1)
                    {
                        ambiguous.Add(label + "  候補: " +
                                      string.Join(" / ", found.Select(c => Path(c.transform))));
                        continue;
                    }

                    bound.Add(label + "  <- " + Path(found[0].transform));

                    if (apply)
                    {
                        so ??= new SerializedObject(mb);
                        SerializedProperty p = so.FindProperty(f.Name);
                        if (p != null)
                        {
                            p.objectReferenceValue = found[0];
                        }
                    }
                }
            }

            if (apply && so != null)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 探す範囲は、そのコンポーネント自身の配下と、_panelRoot が指す配下。
        /// シーン全体は探さない。関係の無いものを掴むのを避けるため。
        /// </summary>
        private static List<Component> Search(MonoBehaviour owner, Type fieldType, string fieldName)
        {
            var roots = new List<Transform> { owner.transform };

            FieldInfo panelRoot = owner.GetType().GetField("_panelRoot", FieldFlags)
                                  ?? owner.GetType().BaseType?.GetField("_panelRoot", FieldFlags);
            if (panelRoot != null && panelRoot.GetValue(owner) is GameObject go && go != null)
            {
                roots.Add(go.transform);
            }

            NameHints.TryGetValue(fieldName, out string hint);

            var hits = new List<Component>();
            foreach (Transform r in roots)
            {
                foreach (Component c in r.GetComponentsInChildren(fieldType, true))
                {
                    if (c == null || hits.Contains(c))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(hint) && !PathContains(c.transform, r, hint))
                    {
                        continue;
                    }

                    hits.Add(c);
                }
            }

            return hits;
        }

        /// <summary>対象自身から探索の根までの名前のどこかに、手がかりが含まれるか。</summary>
        private static bool PathContains(Transform target, Transform root, string hint)
        {
            for (Transform t = target; t != null; t = t.parent)
            {
                if (t.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (t == root)
                {
                    break;
                }
            }

            return false;
        }

        private static string Path(Transform t)
        {
            return t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
        }
    }
}
