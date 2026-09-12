// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorScripts
{
    public static class SceneBindingReport
    {
        private const string ScenePath = "Assets/Scenes/MainGame.unity";

        private struct ComponentSnapshot
        {
            public string ScenePath;
            public bool IsActive;
            public string TypeName;
            public List<string> FieldLines;
            public int BoundCount;
            public int UnboundCount;
        }

        [MenuItem("Tools/Report Unbound Serialized Fields")]
        public static void ReportUnboundSerializedFields()
        {
            EnsureSceneLoaded();
            List<MonoBehaviour> targetComponents = CollectTargetComponents();

            int unboundCount = 0;
            List<string> reportLines = new List<string>();

            foreach (MonoBehaviour mb in targetComponents)
            {
                Type type = mb.GetType();
                while (type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour) && type != typeof(Component) && type != typeof(UnityEngine.Object))
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    foreach (FieldInfo field in fields)
                    {
                        bool isSerialized = field.IsPublic || field.IsDefined(typeof(SerializeField), true);
                        if (!isSerialized) continue;

                        if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                        {
                            UnityEngine.Object val = field.GetValue(mb) as UnityEngine.Object;
                            if (val == null)
                            {
                                unboundCount++;
                                reportLines.Add($"{mb.GetType().Name} / {field.Name}");
                            }
                        }
                    }
                    type = type.BaseType;
                }
            }

            Debug.Log($"[SceneBindingReport] === Total Unbound Fields: {unboundCount} ===\n" +
                      string.Join("\n", reportLines));
        }

        [MenuItem("Tools/Generate Scene Snapshot")]
        public static void GenerateSceneSnapshot()
        {
            EnsureSceneLoaded();
            List<MonoBehaviour> targetComponents = CollectTargetComponents();

            List<ComponentSnapshot> snapshots = new List<ComponentSnapshot>();
            int totalBound = 0;
            int totalUnbound = 0;

            foreach (MonoBehaviour mb in targetComponents)
            {
                Type mbType = mb.GetType();
                List<FieldInfo> serializedFields = new List<FieldInfo>();

                Type type = mbType;
                while (type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour) && type != typeof(Component) && type != typeof(UnityEngine.Object))
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    foreach (FieldInfo field in fields)
                    {
                        bool isSerialized = field.IsPublic || field.IsDefined(typeof(SerializeField), true);
                        if (!isSerialized) continue;

                        if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                        {
                            serializedFields.Add(field);
                        }
                    }
                    type = type.BaseType;
                }

                serializedFields.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                List<string> fieldLines = new List<string>();
                int compBound = 0;
                int compUnbound = 0;

                foreach (FieldInfo field in serializedFields)
                {
                    UnityEngine.Object val = field.GetValue(mb) as UnityEngine.Object;
                    string targetStr;
                    if (val == null)
                    {
                        compUnbound++;
                        targetStr = "<unbound>";
                    }
                    else
                    {
                        compBound++;
                        if (val is ScriptableObject so)
                        {
                            targetStr = $"{so.name} ({so.GetType().Name})";
                        }
                        else if (val is Component comp)
                        {
                            targetStr = $"{comp.gameObject.name}/{comp.GetType().Name} ({comp.GetType().Name})";
                        }
                        else if (val is GameObject go)
                        {
                            targetStr = $"{go.name} (GameObject)";
                        }
                        else
                        {
                            targetStr = $"{val.name} ({val.GetType().Name})";
                        }
                    }
                    fieldLines.Add($"    {field.Name} -> {targetStr}");
                }

                totalBound += compBound;
                totalUnbound += compUnbound;

                snapshots.Add(new ComponentSnapshot
                {
                    ScenePath = GetScenePath(mb.gameObject),
                    IsActive = mb.gameObject.activeInHierarchy,
                    TypeName = mbType.Name,
                    FieldLines = fieldLines,
                    BoundCount = compBound,
                    UnboundCount = compUnbound
                });
            }

            snapshots.Sort((a, b) =>
            {
                int c = string.Compare(a.ScenePath, b.ScenePath, StringComparison.Ordinal);
                if (c != 0) return c;
                return string.Compare(a.TypeName, b.TypeName, StringComparison.Ordinal);
            });

            StringBuilder sb = new StringBuilder();
            foreach (ComponentSnapshot snap in snapshots)
            {
                sb.Append(snap.ScenePath).Append("  [active=").Append(snap.IsActive ? "true" : "false").Append("]\n");
                sb.Append("  ").Append(snap.TypeName).Append("\n");
                foreach (string line in snap.FieldLines)
                {
                    sb.Append(line).Append("\n");
                }
            }
            sb.Append($"=== components: {snapshots.Count} / bound: {totalBound} / unbound: {totalUnbound} ===\n");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string snapshotDir = Path.Combine(projectRoot, "docs", "snapshot");
            string outputPath = Path.Combine(snapshotDir, "scene_bindings.txt");

            if (!Directory.Exists(snapshotDir))
            {
                Directory.CreateDirectory(snapshotDir);
            }

            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[SceneBindingReport] Generated scene snapshot ({snapshots.Count} components, bound: {totalBound}, unbound: {totalUnbound}): {outputPath}");
        }

        private static void EnsureSceneLoaded()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static List<MonoBehaviour> CollectTargetComponents()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();
            List<MonoBehaviour> result = new List<MonoBehaviour>();

            foreach (GameObject root in rootObjects)
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour mb in behaviours)
                {
                    if (mb != null && mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("Game."))
                    {
                        result.Add(mb);
                    }
                }
            }

            return result;
        }

        private static string GetScenePath(GameObject go)
        {
            Transform current = go.transform;
            string path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            return path;
        }
    }
}
#endif
