// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class SerializedBindingTest
    {
        [UnityTest]
        public IEnumerator MainGame_AllViews_SerializedFields_MustNotBeNull()
        {
            yield return LoadScene();
            var unbound = new List<string>();
            foreach (var mb in FindViews(null))
                Inspect(mb, unbound);

            if (unbound.Count > 0)
                Assert.Fail($"Found {unbound.Count} unbound field(s):\n" + string.Join("\n", unbound));
        }

        private static IEnumerator LoadScene()
        {
            if (SceneManager.GetActiveScene().name != "MainGame")
            {
                var op = SceneManager.LoadSceneAsync("MainGame", LoadSceneMode.Single);
                while (!op.isDone) yield return null;
                yield return null;
            }
        }

        private static IEnumerable<MonoBehaviour> FindViews(Type filter)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Namespace == "Game.UI" && (filter == null || mb.GetType() == filter))
                    yield return mb;
        }

        private static void Inspect(MonoBehaviour mb, List<string> unbound)
        {
            string path = GetPath(mb.transform);
            for (Type t = mb.GetType(); t != null && t.Namespace == "Game.UI"; t = t.BaseType)
            {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (!f.IsDefined(typeof(SerializeField), true) || f.FieldType.IsValueType) continue;
                    object val = f.GetValue(mb);
                    if (val == null || (val is UnityEngine.Object u && u == null))
                        unbound.Add($"  - [Unbound] {path} -> {mb.GetType().Name}.{f.Name} ({f.FieldType.Name})");
                }
            }
        }

        private static string GetPath(Transform t) => t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;
    }
}
