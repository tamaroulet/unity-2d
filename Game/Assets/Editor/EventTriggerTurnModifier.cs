// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using Game.Features.Event;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class EventTriggerTurnModifier
    {
        private const string EventAssetPath = "Assets/Data/Events/Event_MidExam.asset";

        [MenuItem("Tools/Update MidExam Trigger Turn")]
        public static void UpdateMidExamTriggerTurn()
        {
            GameEventSO eventAsset = AssetDatabase.LoadAssetAtPath<GameEventSO>(EventAssetPath);
            if (eventAsset == null)
            {
                Debug.LogError($"[EventTriggerTurnModifier] Asset not found at {EventAssetPath}");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(eventAsset);
            SerializedProperty triggerTurnProp = serializedObject.FindProperty("_triggerTurn");
            if (triggerTurnProp == null)
            {
                Debug.LogError("[EventTriggerTurnModifier] SerializedProperty '_triggerTurn' not found.");
                return;
            }

            int oldTurn = triggerTurnProp.intValue;
            triggerTurnProp.intValue = 11;
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(eventAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[EventTriggerTurnModifier] Successfully updated _triggerTurn for {EventAssetPath}: {oldTurn} -> {triggerTurnProp.intValue}");
        }
    }
}
#endif
