// SPDX-AI-Disclosure: ai-generated
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using TMPro;
using Game.Features.Relic;
using Game.Features.Command;
using Game.Features.Event;
using Game.Features.Boss;

namespace Game.Editor
{
    public static class PlayerFacingTextTools
    {
        [MenuItem("Tools/Rewrite Player-Facing Text To English")]
        public static void RewritePlayerFacingTextToEnglish()
        {
            int modifiedCount = 0;

            // 1. Relics
            var relicData = new[]
            {
                ("Assets/Features/Relic/Instances/Relic_01_IronBoots.asset", "Iron Geta", "Recover 2 Stamina at the start of each turn."),
                ("Assets/Features/Relic/Instances/Relic_02_FocusBand.asset", "Focus Headband", "Gain +3 extra Skill on each command."),
                ("Assets/Features/Relic/Instances/Relic_03_EnergyDrink.asset", "Energy Drink", "Recover 2 Mental at the end of each turn."),
                ("Assets/Features/Relic/Instances/Relic_04_LightArmor.asset", "Light Armor", "Stamina cost of commands reduced by 20%."),
                ("Assets/Features/Relic/Instances/Relic_05_MeditationRing.asset", "Meditation Ring", "Recover 3 Mental at the start of each turn."),
                ("Assets/Features/Relic/Instances/Relic_06_PowerWrist.asset", "Power Wristband", "Gain +5 Skill on each command, but Stamina cost +3.")
            };

            foreach (var (path, name, desc) in relicData)
            {
                RelicSO relic = AssetDatabase.LoadAssetAtPath<RelicSO>(path);
                if (relic != null)
                {
                    SerializedObject so = new SerializedObject(relic);
                    SerializedProperty nameProp = so.FindProperty("_displayName");
                    SerializedProperty descProp = so.FindProperty("_description");
                    if (nameProp != null) nameProp.stringValue = name;
                    if (descProp != null) descProp.stringValue = desc;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(relic);
                    modifiedCount++;
                }
            }

            // 2. Commands
            var commandData = new[]
            {
                ("Assets/Data/Commands/Study.asset", "STUDY"),
                ("Assets/Data/Commands/Train.asset", "TRAIN"),
                ("Assets/Data/Commands/Rest.asset", "REST")
            };

            foreach (var (path, name) in commandData)
            {
                CommandDataSO cmd = AssetDatabase.LoadAssetAtPath<CommandDataSO>(path);
                if (cmd != null)
                {
                    SerializedObject so = new SerializedObject(cmd);
                    SerializedProperty nameProp = so.FindProperty("_commandName");
                    if (nameProp != null) nameProp.stringValue = name;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(cmd);
                    modifiedCount++;
                }
            }

            // 3. Events
            var eventData = new[]
            {
                ("Assets/Data/Events/Event_MidExam.asset", "Midterm Exam", "The midterm exam is here. Everything you have studied is put to the test!")
            };

            foreach (var (path, name, body) in eventData)
            {
                GameEventSO evt = AssetDatabase.LoadAssetAtPath<GameEventSO>(path);
                if (evt != null)
                {
                    SerializedObject so = new SerializedObject(evt);
                    SerializedProperty nameProp = so.FindProperty("_displayName");
                    SerializedProperty bodyProp = so.FindProperty("_body");
                    if (nameProp != null) nameProp.stringValue = name;
                    if (bodyProp != null) bodyProp.stringValue = body;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(evt);
                    modifiedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerFacingTextTools] Successfully rewrote {modifiedCount} assets to English.");
        }

        [MenuItem("Tools/Report Non-ASCII Player-Facing Text")]
        public static void ReportNonAsciiPlayerFacingText()
        {
            int nonAsciiCount = 0;

            // A. Check ScriptableObjects in Assets/
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject soObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (soObj == null) continue;

                SerializedObject so = new SerializedObject(soObj);
                SerializedProperty prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.String)
                    {
                        string val = prop.stringValue;
                        if (!string.IsNullOrEmpty(val) && HasNonAscii(val))
                        {
                            Debug.LogError($"[PlayerFacingTextTools] Non-ASCII text found in asset: {path} | Property: {prop.propertyPath} | Value: '{val}'");
                            nonAsciiCount++;
                        }
                    }
                }
            }

            // B. Check active scene TextMeshPro components
            TextMeshProUGUI[] tmps = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tmp in tmps)
            {
                if (!string.IsNullOrEmpty(tmp.text) && HasNonAscii(tmp.text))
                {
                    Debug.LogError($"[PlayerFacingTextTools] Non-ASCII text found in Scene TMP: {tmp.gameObject.name} (Path: {GetHierarchyPath(tmp.transform)}) | Text: '{tmp.text}'");
                    nonAsciiCount++;
                }
            }

            if (nonAsciiCount == 0)
            {
                Debug.Log("[PlayerFacingTextTools] All player-facing text is pure ASCII. OK.");
            }
            else
            {
                Debug.LogError($"[PlayerFacingTextTools] Non-ASCII check FAILED: {nonAsciiCount} non-ASCII strings found.");
            }
        }

        private static bool HasNonAscii(string text)
        {
            foreach (char c in text)
            {
                if (c > 127) return true;
            }
            return false;
        }

        private static string GetHierarchyPath(Transform tr)
        {
            if (tr.parent == null) return tr.name;
            return GetHierarchyPath(tr.parent) + "/" + tr.name;
        }
    }
}
