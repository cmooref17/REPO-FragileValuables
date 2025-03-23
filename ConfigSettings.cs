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
        public static ConfigEntry<float> durabilityMultiplier;
        public static ConfigEntry<float> priceMultiplier;
        public static ConfigEntry<bool> useCustomUIColor;
        public static ConfigEntry<string> fragileUIColor;
        public static ConfigEntry<bool> verboseLogs;
        public static Dictionary<string, ConfigEntryBase> currentConfigEntries = new Dictionary<string, ConfigEntryBase>();

        internal static void BindConfigSettings()
        {
            Plugin.Log("Binding Configs");
            fragileValuableChance = AddConfigEntry(Plugin.instance.Config.Bind("General", "Fragile Valuable Chance", 0.1f, new ConfigDescription("[Host only] Chance for a fragile valuable to spawn with more value, but higher fragility.\nValues will be clamped between 0.01 and 1.0", new AcceptableValueRange<float>(0.01f, 1.0f))));
            fragilityMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Fragility Multiplier", 3.0f, new ConfigDescription("[Host only] Higher value = More fragile. The more fragile an object, the easier it is to damage it. A value of 1 would not modify the fragility. (lighter hits may cause damage)", new AcceptableValueRange<float>(0.1f, 5.0f))));
            //durabilityMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Durability Multiplier", 0.0f, new ConfigDescription("[Host only] Lower value = Less durable. The less durable an object, the more money lost per hit. A value of 1 would not modify the durability.", new AcceptableValueRange<float>(0.0f, 1.0f))));
            priceMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Price Multiplier", 2.0f, new ConfigDescription("[Host only] Price multiplier for fragile valuables. This only affects the objects randomly spawned with increased fragility. A value of 1 would not modify the price.", new AcceptableValueRange<float>(0.1f, 5.0f))));

            useCustomUIColor = AddConfigEntry(Plugin.instance.Config.Bind("UI", "Use Custom UI Color", true, new ConfigDescription("[Client-side] If true, the custom ui color for increased fragile object will apply.\nThese colors are used when discovering an increased fragile object, or for the value color while holding the object.")));
            fragileUIColor = AddConfigEntry(Plugin.instance.Config.Bind("UI", "Discovered Object UI Color", "0,0.8,1", new ConfigDescription("[Client-side] Enter 3 values for RGB, separated by a comma. Example: \"0,0.8,1\"\nValues must be within 0 and 1")));

            verboseLogs = AddConfigEntry(Plugin.instance.Config.Bind("General", "Verbose Logs", false, new ConfigDescription("Enables verbose logs. Useful for debugging.")));

            fragileValuableChance.Value = Mathf.Clamp(fragileValuableChance.Value, 0, 1);
            fragilityMultiplier.Value = Mathf.Clamp(fragilityMultiplier.Value, 0.1f, 5.0f);
            durabilityMultiplier.Value = Mathf.Clamp(durabilityMultiplier.Value, 0.0f, 1);
            priceMultiplier.Value = Mathf.Max(priceMultiplier.Value, 0.1f);
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