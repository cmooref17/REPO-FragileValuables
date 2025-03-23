using HarmonyLib;
using REPO_FragileValuables.Config;
using System.Collections.Generic;
using UnityEngine;

namespace REPO_FragileValuables
{
    [HarmonyPatch]
    public static class ValuableObjectPatcher
    {
        internal static System.Random random = null;
        public static Dictionary<Durability, Durability> customDurabilities = new Dictionary<Durability, Durability>();
        public static Dictionary<Value, Value> customValues = new Dictionary<Value, Value>();

        public static HashSet<ValuableObject> currentFragileValuables = new HashSet<ValuableObject>();


        [HarmonyPatch(typeof(LevelGenerator), "Start")]
        [HarmonyPrefix]
        public static void OnLevelGenerationStart()
        {
            currentFragileValuables.Clear();
        }


        [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
        [HarmonyPrefix]
        public static void OnLevelGenerated()
        {
            Plugin.Log("Spawned " + currentFragileValuables.Count + " increased fragile valuables.");
        }


        [HarmonyPatch(typeof(ValuableObject), "Start")]
        [HarmonyPrefix]
        public static void OnStart(ValuableObject __instance)
        {
            if (random == null)
                random = new System.Random((int)Time.time);

            if (__instance.durabilityPreset == null || __instance.durabilityPreset.fragility < 90 || (__instance.physAttributePreset != null && __instance.physAttributePreset.mass > 1) || (__instance.valuePreset != null && __instance.valuePreset.valueMin >= 5000f))
                return;

            float chance = (float)random.NextDouble();
            if (chance >= 1 - ConfigSettings.fragileValuableChance.Value)
            {
                string objectName = __instance.name;
                if (objectName.Contains("(Clone)"))
                    objectName = objectName.Replace("(Clone)", " - Fragile+ (Clone)");
                else
                    objectName += " - Fragile+";
                __instance.name = objectName;

                if (!customDurabilities.TryGetValue(__instance.durabilityPreset, out var customDurability))
                {
                    customDurability = Durability.Instantiate(__instance.durabilityPreset);
                    customDurability.name = __instance.durabilityPreset.name + " Fragile+";
                    customDurabilities[__instance.durabilityPreset] = customDurability;
                }

                if (!customValues.TryGetValue(__instance.valuePreset, out var customValue))
                {
                    customValue = Value.Instantiate(__instance.valuePreset);
                    customValue.name = __instance.valuePreset.name + " Value+";
                    customValues[__instance.valuePreset] = customValue;
                }

                customDurability.durability = __instance.durabilityPreset.durability * ConfigSettings.durabilityMultiplier.Value;
                customDurability.fragility = __instance.durabilityPreset.fragility * ConfigSettings.fragilityMultiplier.Value;

                customValue.valueMin = __instance.valuePreset.valueMin * ConfigSettings.priceMultiplier.Value;
                customValue.valueMax = __instance.valuePreset.valueMax * ConfigSettings.priceMultiplier.Value;

                currentFragileValuables.Add(__instance);
                Plugin.LogVerbose("Increasing fragility and value of object: " + __instance.name + " - Fragility: " + __instance.durabilityPreset.fragility + " => " + customDurability.fragility + " Durability: " + __instance.durabilityPreset.durability + " => " + customDurability.durability + " ValueMin: " + __instance.valuePreset.valueMin + " => " + customValue.valueMin + " ValueMax: " + __instance.valuePreset.valueMax + " => " + customValue.valueMax);

                __instance.durabilityPreset = customDurability;
                __instance.valuePreset = customValue;
            }
        }
    }
}