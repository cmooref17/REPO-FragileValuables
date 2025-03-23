using BepInEx.Configuration;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using REPO_FragileValuables.UI;
using System.Linq;

namespace REPO_FragileValuables.Config
{
    [Serializable]
    public static class ConfigSettings
    {
        public static ConfigEntry<float> fragileValuableChance;
        public static ConfigEntry<float> fragilityMultiplier;
        //public static ConfigEntry<float> durabilityMultiplier;
        public static ConfigEntry<float> priceMultiplier;
        public static ConfigEntry<int> minFragilityThreshold;
        public static ConfigEntry<int> minValueThreshold;
        public static ConfigEntry<int> maxValueThreshold;
        public static ConfigEntry<int> minMassThreshold;
        public static ConfigEntry<int> maxMassThreshold;

        public static ConfigEntry<bool> useCustomUIColor;
        public static ConfigEntry<string> fragileUIColor;
        public static ConfigEntry<bool> verboseLogs;
        public static Dictionary<string, ConfigEntryBase> currentConfigEntries = new Dictionary<string, ConfigEntryBase>();

        internal static void BindConfigSettings()
        {
            Plugin.Log("Binding Configs");
            fragilityMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Increased Fragility Multiplier", 3.0f, new ConfigDescription("[Host only] Higher value = More fragile. The more fragile an object, the easier it is to damage it. A value of 1 would not modify the fragility. (lighter hits may cause damage)", new AcceptableValueRange<float>(0.1f, 5.0f))));
            //durabilityMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Increased Durability Multiplier", 0.0f, new ConfigDescription("[Host only] Lower value = Less durable. The less durable an object, the more money lost per hit. A value of 1 would not modify the durability.", new AcceptableValueRange<float>(0.0f, 1.0f))));
            priceMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Increased Price Multiplier", 2.0f, new ConfigDescription("[Host only] Price multiplier for fragile valuables. This only affects the objects randomly spawned with increased fragility. A value of 1 would not modify the price.", new AcceptableValueRange<float>(0.1f, 5.0f))));

            fragileValuableChance = AddConfigEntry(Plugin.instance.Config.Bind("Spawn Rules", "Increased Fragility Chance", 0.1f, new ConfigDescription("[Host only] Chance for a fragile valuable to spawn with more value, but higher fragility.\nValues will be clamped between 0.01 and 1.0", new AcceptableValueRange<float>(0.01f, 1.0f))));
            minFragilityThreshold = AddConfigEntry(Plugin.instance.Config.Bind("Spawn Rules", "Min Fragility Threshold", 90, new ConfigDescription("[Host only] Only objects with a default fragility of this number and ABOVE will have a chance to increase in fragility and value.", new AcceptableValueRange<int>(0, 100))));
            minValueThreshold = AddConfigEntry(Plugin.instance.Config.Bind("Spawn Rules", "Min Value Threshold", 1000, new ConfigDescription("[Host only] Only objects with a default min value of this number and ABOVE will have a chance to increase in fragility and value.", new AcceptableValueRange<int>(0, 100000))));
            maxValueThreshold = AddConfigEntry(Plugin.instance.Config.Bind("Spawn Rules", "Max Value Threshold", 5000, new ConfigDescription("[Host only] Only objects with a default min value of this number and BELOW will have a chance to increase in fragility and value.\nSet to -1 to ignore this criteria.", new AcceptableValueRange<int>(-1, 100000))));
            minMassThreshold  = AddConfigEntry(Plugin.instance.Config.Bind("Spawn Rules", "Min Mass Threshold", 0, new ConfigDescription("[Host only] Only objects with a default mass of this number and ABOVE will have a chance to increase in fragility and value. (for reference a small vase has a mass of 2)", new AcceptableValueRange<int>(0, 10))));
            maxMassThreshold = AddConfigEntry(Plugin.instance.Config.Bind("Spawn Rules", "Max Mass Threshold", 2, new ConfigDescription("[Host only] Only objects with a default mass of this number and BELOW will have a chance to increase in fragility and value. (for reference a small vase has a mass of 2)\nSet to -1 to ignore this criteria.", new AcceptableValueRange<int>(-1, 10))));

            useCustomUIColor = AddConfigEntry(Plugin.instance.Config.Bind("UI", "Use Custom UI Color", true, new ConfigDescription("[Client-side] If true, the custom ui color for increased fragile object will apply.\nThese colors are used when discovering an increased fragile object, or for the value color while holding the object.")));
            fragileUIColor = AddConfigEntry(Plugin.instance.Config.Bind("UI", "Discovered Object UI Color", "0,0.8,1", new ConfigDescription("[Client-side] Enter 3 values for RGB, separated by a comma. Example: \"0,0.8,1\"\nValues must be within 0 and 1")));

            verboseLogs = AddConfigEntry(Plugin.instance.Config.Bind("General", "Verbose Logs", false, new ConfigDescription("Enables verbose logs. Useful for debugging.")));

            fragilityMultiplier.Value = Mathf.Clamp(fragilityMultiplier.Value, 0.1f, 5.0f);
            //durabilityMultiplier.Value = Mathf.Clamp(durabilityMultiplier.Value, 0.0f, 1);
            priceMultiplier.Value = Mathf.Max(priceMultiplier.Value, 0.1f);

            fragileValuableChance.Value = Mathf.Clamp(fragileValuableChance.Value, 0, 1);
            minFragilityThreshold.Value = Mathf.Clamp(minFragilityThreshold.Value, 0, 100);
            minValueThreshold.Value = Mathf.Clamp(minValueThreshold.Value, -1, 100000);
            maxValueThreshold.Value = Mathf.Clamp(maxValueThreshold.Value, minValueThreshold.Value, 100000);
            minMassThreshold.Value = Mathf.Clamp(minMassThreshold.Value, -1, 10);
            maxMassThreshold.Value = Mathf.Clamp(maxMassThreshold.Value, minMassThreshold.Value, 10);
        }


        internal static Color ParseUIColorString()
        {
            try
            {
                string[] colorValues = fragileUIColor.Value.Split(',');
                if (colorValues == null || colorValues.Length != 3)
                    throw new Exception("Must specify 3 color values");
                float r = float.Parse(colorValues[0]);
                float g = float.Parse(colorValues[1]);
                float b = float.Parse(colorValues[2]);
                Color color = new Color(r, g, b);
                return color;
            }
            catch (Exception e)
            {
                Plugin.LogError("Error parsing fragile object ui color string: " + fragileUIColor.Value + "\n" + e);
                fragileUIColor.Value = (string)fragileUIColor.DefaultValue;
                return ValuableUIPatcher.invalidColor;
            }
        }


        internal static ConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
        {
            currentConfigEntries.Add(configEntry.Definition.Key, configEntry);
            return configEntry;
        }
    }
}