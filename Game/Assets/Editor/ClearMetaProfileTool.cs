// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using Game.Features.MetaProgression;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class ClearMetaProfileTool
    {
        [MenuItem("Tools/Clear Meta Profile")]
        public static void ClearMetaProfile()
        {
            PlayerPrefs.DeleteKey(MetaProfileStore.PrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[ClearMetaProfileTool] Meta profile cleared from PlayerPrefs.");
        }
    }
}
#endif
