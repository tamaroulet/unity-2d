// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Features.Boss;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class BossAssetGenerator
    {
        [MenuItem("Tools/Generate Boss Assets")]
        public static void GenerateAssets()
        {
            const string dir = "Assets/Features/Boss/Instances";
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Features/Boss/Instances")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Features/Boss/Instances"));
            }

            // 1. Bosses for Act 1 - 4
            BossSO b1 = CreateOrUpdateBoss($"{dir}/Boss_Act1_01.asset", 1, "Boss_Act1_01", 80, 15, 10, 5, 1.5f);
            BossSO b2 = CreateOrUpdateBoss($"{dir}/Boss_Act2_01.asset", 2, "Boss_Act2_01", 140, 22, 15, 10, 1.8f);
            BossSO b3 = CreateOrUpdateBoss($"{dir}/Boss_Act3_01.asset", 3, "Boss_Act3_01", 220, 30, 20, 15, 2.0f);
            BossSO b4 = CreateOrUpdateBoss($"{dir}/Boss_Act4_01.asset", 4, "Boss_Act4_01", 320, 40, 25, 20, 2.2f);

            // 2. BossCatalog
            string catalogPath = $"{dir}/BossCatalog.asset";
            BossCatalogSO catalog = AssetDatabase.LoadAssetAtPath<BossCatalogSO>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BossCatalogSO>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty bossesProp = serializedCatalog.FindProperty("_bosses");
            bossesProp.ClearArray();
            bossesProp.arraySize = 4;
            bossesProp.GetArrayElementAtIndex(0).objectReferenceValue = b1;
            bossesProp.GetArrayElementAtIndex(1).objectReferenceValue = b2;
            bossesProp.GetArrayElementAtIndex(2).objectReferenceValue = b3;
            bossesProp.GetArrayElementAtIndex(3).objectReferenceValue = b4;
            serializedCatalog.ApplyModifiedProperties();

            // 3. AutoBattleResolver
            string resolverPath = $"{dir}/AutoBattleResolver.asset";
            AutoBattleResolverSO resolver = AssetDatabase.LoadAssetAtPath<AutoBattleResolverSO>(resolverPath);
            if (resolver == null)
            {
                resolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
                AssetDatabase.CreateAsset(resolver, resolverPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BossAssetGenerator] 4 Acts Boss assets generated successfully.");
        }

        private static BossSO CreateOrUpdateBoss(
            string path, int id, string name, int maxHp, int atk, int mentalPressure, int shield, float specialMul)
        {
            BossSO boss = AssetDatabase.LoadAssetAtPath<BossSO>(path);
            if (boss == null)
            {
                boss = ScriptableObject.CreateInstance<BossSO>();
                AssetDatabase.CreateAsset(boss, path);
            }

            SerializedObject so = new SerializedObject(boss);
            so.FindProperty("_bossId").intValue = id;
            so.FindProperty("_bossName").stringValue = name;
            so.FindProperty("_maxHp").intValue = maxHp;
            so.FindProperty("_attackPower").intValue = atk;
            so.FindProperty("_mentalPressurePower").intValue = mentalPressure;
            so.FindProperty("_guardShieldAmount").intValue = shield;
            so.FindProperty("_specialAttackMultiplier").floatValue = specialMul;

            SerializedProperty actionProp = so.FindProperty("_actionPattern");
            actionProp.ClearArray();
            actionProp.arraySize = 4;
            actionProp.GetArrayElementAtIndex(0).enumValueIndex = (int)BossActionKind.Attack;
            actionProp.GetArrayElementAtIndex(1).enumValueIndex = (int)BossActionKind.MentalPressure;
            actionProp.GetArrayElementAtIndex(2).enumValueIndex = (int)BossActionKind.Guard;
            actionProp.GetArrayElementAtIndex(3).enumValueIndex = (int)BossActionKind.SpecialAttack;
            so.ApplyModifiedProperties();

            return boss;
        }
    }
}
#endif
