using HarmonyLib;
using REPO_FragileValuables.Config;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace REPO_FragileValuables.UI
{
    [HarmonyPatch]
    public static class ValuableUIPatcher
    {
        internal readonly static Color invalidColor = new Color(0, 0, 0, 0);
        internal readonly static Color defaultUIColor = new Color(0, 0.8f, 1f, 1);
        public static Color uiColorMiddle = defaultUIColor;
        public static Color uiColorCorner = defaultUIColor;
        public static Color uiColorPriceText = defaultUIColor;
        public static int displayTime = 2;
        public static float minAlpha = 0.05f;
        public static float maxAlpha = 0.5f;
        public static int flashSpeed = 1;

        private static Color previousTextUIColor;

        internal static Dictionary<ValuableDiscoverGraphic, Image> middleImages = new Dictionary<ValuableDiscoverGraphic, Image>();


        [HarmonyPatch(typeof(LevelGenerator), "Start")]
        [HarmonyPrefix]
        public static void OnLevelGenerationStart()
        {
            middleImages.Clear();

            Color uiColor = ConfigSettings.ParseUIColorString();
            if (uiColor == invalidColor)
            {
                uiColor = defaultUIColor;
                Plugin.LogErrorVerbose("Failed to parse ui color. Reverting to default: " + uiColor);
            }
            else
                Plugin.LogVerbose("Successfully parsed color: " + uiColor);

            uiColorMiddle = new Color(uiColor.r, uiColor.g, uiColor.b, 0.1f);
            uiColorCorner = new Color(uiColor.r, uiColor.g, uiColor.b, 0.8f);
            uiColorPriceText = new Color(uiColor.r, uiColor.g, uiColor.b, 1f);
        }


        [HarmonyPatch(typeof(ValuableDiscoverGraphic), "Start")]
        [HarmonyPrefix]
        public static void OnStart(ref ValuableDiscoverGraphic.State ___state, ValuableDiscoverGraphic __instance)
        {
            if (___state != ValuableDiscoverGraphic.State.Discover || !ConfigSettings.useCustomUIColor.Value)
                return;

            PhysGrabObject target = __instance.target;
            var valuable = target.GetComponent<ValuableObject>();

            if (!valuable || !IncreasedValuableObject.currentFragileValuables.Contains(valuable))
                return;

            __instance.waitTime = displayTime;
            __instance.colorDiscoverMiddle = uiColorMiddle;
            __instance.colorDiscoverCorner = uiColorCorner;

            Image imageMiddle = __instance.middle?.GetComponent<Image>();
            if (imageMiddle)
            {
                imageMiddle.color = uiColorMiddle;
                middleImages[__instance] = imageMiddle;
            }
            else
                Plugin.LogError("Failed to modify fragile valuable's ValuableDiscoverGraphic UI (middle) color.");

            Image[] imageCorners = new Image[] { __instance.topLeft?.GetComponent<Image>(), __instance.topRight?.GetComponent<Image>(), __instance.botLeft?.GetComponent<Image>(), __instance.botRight?.GetComponent<Image>() };
            for (int i = 0; i < imageCorners.Length; i++)
            {
                if (imageCorners[i])
                    imageCorners[i].color = uiColorCorner;
                else
                    Plugin.LogError("Failed to modify fragile valuable's ValuableDiscoverGraphic UI (corner at index " + i + ") color.");
            }
        }


        [HarmonyPatch(typeof(ValuableDiscoverGraphic), "Update")]
        [HarmonyPostfix]
        public static void UpdateAlpha(ref ValuableDiscoverGraphic.State ___state, ref float ___waitTimer, ValuableDiscoverGraphic __instance)
        {
            if (___state != ValuableDiscoverGraphic.State.Discover || !middleImages.TryGetValue(__instance, out Image image))
                return;

            float value = __instance.waitTime - ___waitTimer;
            float targetAlpha = (Mathf.Sin(2f * (value - 1f / (4f * flashSpeed)) * Mathf.PI * flashSpeed) / 2f) * (maxAlpha - minAlpha) + (maxAlpha + minAlpha) / 2f; // Just some simple math

            float alpha = Mathf.Lerp(image.color.a, targetAlpha, 15 * Time.deltaTime);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }


        [HarmonyPatch(typeof(WorldSpaceUIValue), "Show")]
        [HarmonyPrefix]
        public static void OnShowValuePrefix(ref PhysGrabObject ___currentPhysGrabObject, PhysGrabObject _grabObject, int _value, bool _cost, Vector3 _offset, WorldSpaceUIValue __instance)
        {
            if (!ConfigSettings.useCustomUIColor.Value)
                return;

            previousTextUIColor = __instance.colorValue;
            if ((___currentPhysGrabObject && ___currentPhysGrabObject != _grabObject) || _cost)
                return;

            ValuableObject valuableObject = _grabObject.GetComponent<ValuableObject>();
            if (valuableObject && IncreasedValuableObject.currentFragileValuables.Contains(valuableObject))
                __instance.colorValue = uiColorPriceText;
        }


        [HarmonyPatch(typeof(WorldSpaceUIValue), "Show")]
        [HarmonyPostfix]
        public static void OnShowValuePostfix(PhysGrabObject _grabObject, int _value, bool _cost, Vector3 _offset, WorldSpaceUIValue __instance)
        {
            if (!ConfigSettings.useCustomUIColor.Value)
                return;

            __instance.colorValue = previousTextUIColor;
        }
    }
}