// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Features.MetaProgression;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class MetaProgressionAssetGenerator
    {
        [MenuItem("Tools/Generate Meta Progression Assets")]
        public static void GenerateAssets()
        {
            string dir = "Assets/Features/MetaProgression/Instances";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/Features/MetaProgression", "Instances");
            }

            // 1. MetaPointResolver.asset
            string resolverPath = $"{dir}/MetaPointResolver.asset";
            MetaPointResolverSO resolver = AssetDatabase.LoadAssetAtPath<MetaPointResolverSO>(resolverPath);
            if (resolver == null)
            {
                resolver = ScriptableObject.CreateInstance<MetaPointResolverSO>();
                AssetDatabase.CreateAsset(resolver, resolverPath);
            }

            // 2. MetaUnlockSO アセット群
            MetaUnlockSO u1 = CreateOrUpdateUnlock($"{dir}/Unlock_Stat_Stamina_01.asset", 1, "Unlock_Stat_Stamina_01", 50, MetaUnlockKind.InitialStaminaBonus, 10, 0);
            MetaUnlockSO u2 = CreateOrUpdateUnlock($"{dir}/Unlock_Stat_Skill_01.asset", 2, "Unlock_Stat_Skill_01", 80, MetaUnlockKind.InitialSkillBonus, 5, 0);
            MetaUnlockSO u3 = CreateOrUpdateUnlock($"{dir}/Unlock_Stat_Mental_01.asset", 3, "Unlock_Stat_Mental_01", 50, MetaUnlockKind.InitialMentalBonus, 10, 0);

            // 3. MetaUnlockCatalog.asset
            string catalogPath = $"{dir}/MetaUnlockCatalog.asset";
            MetaUnlockCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MetaUnlockCatalogSO>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MetaUnlockCatalogSO>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            SerializedObject catalogSo = new SerializedObject(catalog);
            SerializedProperty listProp = catalogSo.FindProperty("_unlocks");
            listProp.ClearArray();
            listProp.InsertArrayElementAtIndex(0);
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = u1;
            listProp.InsertArrayElementAtIndex(1);
            listProp.GetArrayElementAtIndex(1).objectReferenceValue = u2;
            listProp.InsertArrayElementAtIndex(2);
            listProp.GetArrayElementAtIndex(2).objectReferenceValue = u3;
            catalogSo.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MetaProgressionAssetGenerator] Successfully generated MetaProgression assets.");
        }

        private static MetaUnlockSO CreateOrUpdateUnlock(string path, int id, string name, int cost, MetaUnlockKind kind, int bonus, int relicId)
        {
            MetaUnlockSO unlock = AssetDatabase.LoadAssetAtPath<MetaUnlockSO>(path);
            if (unlock == null)
            {
                unlock = ScriptableObject.CreateInstance<MetaUnlockSO>();
                AssetDatabase.CreateAsset(unlock, path);
            }

            SerializedObject so = new SerializedObject(unlock);
            so.FindProperty("_unlockId").intValue = id;
            so.FindProperty("_unlockName").stringValue = name;
            so.FindProperty("_cost").intValue = cost;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_bonusValue").intValue = bonus;
            so.FindProperty("_targetRelicId").intValue = relicId;
            so.ApplyModifiedProperties();

            return unlock;
        }
    }
}
#endif
