using HarmonyLib;
using Photon.Pun;
using REPO_FragileValuables.Config;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace REPO_FragileValuables
{
    [HarmonyPatch]
    public class IncreasedValuableObject : MonoBehaviour
    {
        internal static System.Random random = null;
        public static Dictionary<Durability, Durability> customDurabilities = new Dictionary<Durability, Durability>();
        public static Dictionary<Value, Value> customValues = new Dictionary<Value, Value>();

        public static HashSet<ValuableObject> currentFragileValuables = new HashSet<ValuableObject>();

        public ValuableObject valuableObject;
        public PhotonView photonView;
        public PhysGrabObjectImpactDetector impactDetector;
        public Durability customDurability;
        public Value customValue;
        public bool increasedFragile = false;
        public bool initialized = false;

        private static bool roundInitialized = false;
        private static int newHaulGoal = 0;

        private static bool modLoadError = false;

        private static FieldInfo dollarValueOriginalField = typeof(ValuableObject).GetField("dollarValueOriginal", BindingFlags.Public | BindingFlags.Instance);
        private static FieldInfo dollarValueCurrentField = typeof(ValuableObject).GetField("dollarValueCurrent", BindingFlags.Public | BindingFlags.Instance);

        private static FieldInfo dollarValueOriginalPrivateField = typeof(ValuableObject).GetField("dollarValueOriginal", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo dollarValueCurrentPrivateField = typeof(ValuableObject).GetField("dollarValueCurrent", BindingFlags.NonPublic | BindingFlags.Instance);


        public void Awake()
        {
            valuableObject = GetComponent<ValuableObject>();
            photonView = gameObject.GetComponent<PhotonView>();
            impactDetector = gameObject.GetComponent<PhysGrabObjectImpactDetector>();
        }


        public void Start()
        {
            if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
            {
                if (random == null)
                    random = new System.Random((int)Time.time);

                if (valuableObject && photonView && impactDetector && valuableObject.durabilityPreset != null && valuableObject.valuePreset != null && valuableObject.physAttributePreset != null
                    && (ConfigSettings.minFragilityThreshold.Value == -1 || valuableObject.durabilityPreset.fragility >= ConfigSettings.minFragilityThreshold.Value)
                    && (ConfigSettings.minValueThreshold.Value == -1 || valuableObject.valuePreset.valueMin >= ConfigSettings.minValueThreshold.Value)
                    && (ConfigSettings.maxValueThreshold.Value == -1 || valuableObject.valuePreset.valueMin <= ConfigSettings.maxValueThreshold.Value)
                    && (ConfigSettings.minMassThreshold.Value == -1 || valuableObject.physAttributePreset.mass >= ConfigSettings.minMassThreshold.Value)
                    && (ConfigSettings.maxMassThreshold.Value == -1 || valuableObject.physAttributePreset.mass <= ConfigSettings.maxMassThreshold.Value))
                {
                    float chance = (float)random.NextDouble();
                    if (chance >= 1 - ConfigSettings.fragileValuableChance.Value)
                    {
                        currentFragileValuables.Add(valuableObject);
                        increasedFragile = true;
                        initialized = false;
                        return;
                    }
                }
            }

            if (!valuableObject || !currentFragileValuables.Contains(valuableObject))
                enabled = false;
        }


        public void OnDestroy()
        {
            if (valuableObject && currentFragileValuables.Contains(valuableObject))
                currentFragileValuables.Remove(valuableObject);
        }


        [PunRPC]
        public void IncreaseFragilityRPC()
        {
            IncreaseFragility();
        }


        public void IncreaseFragility()
        {
            if (!valuableObject || !impactDetector || initialized)
                return;

            if (!customDurabilities.TryGetValue(valuableObject.durabilityPreset, out var customDurability))
            {
                customDurability = Durability.Instantiate(valuableObject.durabilityPreset);
                customDurability.name = valuableObject.durabilityPreset.name + " Fragile+";
                customDurabilities[valuableObject.durabilityPreset] = customDurability;
            }

            if (!customValues.TryGetValue(valuableObject.valuePreset, out var customValue))
            {
                customValue = Value.Instantiate(valuableObject.valuePreset);
                customValue.name = valuableObject.valuePreset.name + " Value+";
                customValues[valuableObject.valuePreset] = customValue;
            }

            customDurability.durability = valuableObject.durabilityPreset.durability * 0.01f; // ConfigSettings.durabilityMultiplier.Value;
            customDurability.fragility = valuableObject.durabilityPreset.fragility * ConfigSettings.fragilityMultiplier.Value;
            customValue.valueMin = valuableObject.valuePreset.valueMin * ConfigSettings.priceMultiplier.Value;
            customValue.valueMax = valuableObject.valuePreset.valueMax * ConfigSettings.priceMultiplier.Value;

            if (!ConfigSettings.verboseLogs.Value)
                Plugin.Log("Increasing fragility and value of object: " + valuableObject.name);
            Plugin.LogVerbose("Increasing fragility and value of object: " + valuableObject.name + " - Fragility: " + valuableObject.durabilityPreset.fragility + " => " + customDurability.fragility + " Durability: " + valuableObject.durabilityPreset.durability + " => " + customDurability.durability + " ValueMin: " + valuableObject.valuePreset.valueMin + " => " + customValue.valueMin + " ValueMax: " + valuableObject.valuePreset.valueMax + " => " + customValue.valueMax);

            this.customDurability = customDurability;
            this.customValue = customValue;
            valuableObject.durabilityPreset = customDurability;
            valuableObject.valuePreset = customValue;
            impactDetector.fragility = valuableObject.durabilityPreset.fragility;
            impactDetector.durability = valuableObject.durabilityPreset.durability;

            if (!SemiFunc.IsMultiplayer() || SemiFunc.IsMasterClient())
            {
                float dollarValueOriginal;
                float dollarAmount = 0;
                try
                {
                    // If on older version of REPO
                    dollarValueOriginal = (float)dollarValueOriginalField.GetValue(valuableObject);
                    dollarAmount = (int)(dollarValueOriginal * ConfigSettings.priceMultiplier.Value);
                    dollarValueCurrentField.SetValue(valuableObject, dollarAmount);
                }
                catch
                {
                    try
                    {
                        // If on newer version of REPO
                        dollarValueOriginal = (float)dollarValueOriginalPrivateField.GetValue(valuableObject);
                        dollarAmount = (int)(dollarValueOriginal * ConfigSettings.priceMultiplier.Value);
                        dollarValueCurrentPrivateField.SetValue(valuableObject, dollarAmount);
                    }
                    catch(System.Exception e)
                    {
                        Plugin.LogError("Something bad happened on this version of REPO. Could not increase fragility of object.\n" + e);
                        return;
                    }
                }
                if (SemiFunc.IsMultiplayer())
                {
                    photonView.RPC("DollarValueSetRPC", RpcTarget.All, dollarAmount);
                }
            }

            increasedFragile = true;
            initialized = true;
            currentFragileValuables.Add(valuableObject);
        }


        [HarmonyPatch(typeof(RoundDirector), "StartRoundLogic")]
        [HarmonyPrefix]
        public static void OnStartRound(ref int value, RoundDirector __instance)
        {
            if (roundInitialized)
                return;
            roundInitialized = true;

            if ((!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient))
            {
                foreach (var valuableObject in currentFragileValuables)
                {
                    if (valuableObject.gameObject.TryGetComponent(out IncreasedValuableObject increasedValuable) && increasedValuable.increasedFragile && !increasedValuable.initialized)
                    {
                        float dollarValueCurrent;
                        try
                        {
                            // If on older version of REPO
                            dollarValueCurrent = (float)dollarValueCurrentField.GetValue(valuableObject);
                            dollarValueOriginalField.SetValue(valuableObject, dollarValueCurrent);
                        }
                        catch
                        {
                            // If on newer version of REPO
                            try
                            {
                                dollarValueCurrent = (float)dollarValueCurrentPrivateField.GetValue(valuableObject);
                                dollarValueOriginalPrivateField.SetValue(valuableObject, dollarValueCurrent);
                            }
                            catch(System.Exception e)
                            {
                                Plugin.LogError("Something bad happened on this version of REPO. Could not initailize mod settings.\n" + e);
                                modLoadError = true;
                                return;
                            }
                        }
                        float originalValue = dollarValueCurrent;
                        increasedValuable.IncreaseFragility();
                        if (SemiFunc.IsMultiplayer())
                            increasedValuable.photonView.RPC("IncreaseFragilityRPC", RpcTarget.All);
                        float newValue = dollarValueCurrent;
                        if (ConfigSettings.increasedValuesIncreaseGoal.Value && newValue != originalValue)
                            value += (int)(newValue - originalValue);
                    }
                }
                Plugin.Log("Spawned " + currentFragileValuables.Count + " increased fragile valuables." + (ConfigSettings.increasedValuesIncreaseGoal.Value ? (" New goal: " + value) : ""));
            }
        }


        [HarmonyPatch(typeof(ValuableObject), "Awake")]
        [HarmonyPostfix]
        public static void OnAwake(ValuableObject __instance)
        {
            var increasedValuableObject = __instance.gameObject.AddComponent<IncreasedValuableObject>();
        }


        [HarmonyPatch(typeof(RoundDirector), "Start")]
        [HarmonyPrefix]
        public static void OnRoundStart(RoundDirector __instance)
        {
            roundInitialized = false;
        }
    }
}