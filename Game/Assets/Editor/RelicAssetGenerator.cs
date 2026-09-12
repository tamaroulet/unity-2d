// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Relic;
using UnityEditor;
using UnityEngine;

namespace Game.Features.Relic.Editor
{
    public static class RelicAssetGenerator
    {
        [MenuItem("Tools/Generate Relic Assets")]
        public static void GenerateRelics()
        {
            string dir = "Assets/Features/Relic/Instances";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/Features/Relic", "Instances");
            }

            List<RelicSO> relics = new List<RelicSO>();

            relics.Add(CreateRelic(dir, "Relic_01_IronBoots", 1, "Iron Geta", "Recover 2 Stamina at the start of each turn.", RelicTriggerKind.OnTurnStart, 2, 0, 0, 1f));
            relics.Add(CreateRelic(dir, "Relic_02_FocusBand", 2, "Focus Headband", "Gain +3 extra Skill on each command.", RelicTriggerKind.OnCommandExecuted, 0, 3, 0, 1f));
            relics.Add(CreateRelic(dir, "Relic_03_EnergyDrink", 3, "Energy Drink", "Recover 2 Mental at the end of each turn.", RelicTriggerKind.OnTurnEnd, 0, 0, 2, 1f));
            relics.Add(CreateRelic(dir, "Relic_04_LightArmor", 4, "Light Armor", "Stamina cost of commands reduced by 20%.", RelicTriggerKind.OnCommandExecuted, 0, 0, 0, 0.8f));
            relics.Add(CreateRelic(dir, "Relic_05_MeditationRing", 5, "Meditation Ring", "Recover 3 Mental at the start of each turn.", RelicTriggerKind.OnTurnStart, 0, 0, 3, 1f));
            relics.Add(CreateRelic(dir, "Relic_06_PowerWrist", 6, "Power Wristband", "Gain +5 Skill on each command, but Stamina cost +3.", RelicTriggerKind.OnCommandExecuted, -3, 5, 0, 1f));

            RelicCatalogSO catalog = ScriptableObject.CreateInstance<RelicCatalogSO>();
            FieldInfo field = typeof(RelicCatalogSO).GetField("_relics", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(catalog, relics);

            AssetDatabase.CreateAsset(catalog, $"{dir}/RelicCatalog.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RelicAssetGenerator] 6 Relic assets and RelicCatalog.asset created successfully.");
        }

        private static RelicSO CreateRelic(string dir, string assetName, int id, string displayName, string desc, RelicTriggerKind trigger, int stam, int skill, int men, float costMult)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(RelicSO).GetField("_relicId", flags).SetValue(relic, id);
            typeof(RelicSO).GetField("_displayName", flags).SetValue(relic, displayName);
            typeof(RelicSO).GetField("_description", flags).SetValue(relic, desc);
            typeof(RelicSO).GetField("_triggerKind", flags).SetValue(relic, trigger);
            typeof(RelicSO).GetField("_staminaDeltaBonus", flags).SetValue(relic, stam);
            typeof(RelicSO).GetField("_skillDeltaBonus", flags).SetValue(relic, skill);
            typeof(RelicSO).GetField("_mentalDeltaBonus", flags).SetValue(relic, men);
            typeof(RelicSO).GetField("_staminaCostMultiplier", flags).SetValue(relic, costMult);

            AssetDatabase.CreateAsset(relic, $"{dir}/{assetName}.asset");
            return relic;
        }
    }
}
