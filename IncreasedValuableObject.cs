using HarmonyLib;
using Photon.Pun;
using REPO_FragileValuables.Config;
using System.Collections.Generic;
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
                        //IncreaseFragility();
                        photonView.RPC("IncreaseFragilityRPC", RpcTarget.All);
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
            if (!valuableObject || !impactDetector || currentFragileValuables.Contains(valuableObject))
                return;

            string objectName = valuableObject.name;
            if (objectName.Contains("(Clone)"))
                objectName = objectName.Replace("(Clone)", " - Fragile+ (Clone)");
            else
                objectName += " - Fragile+";
            valuableObject.name = objectName;

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

            if (ConfigSettings.verboseLogs.Value)
                Plugin.LogVerbose("Increasing fragility and value of object: " + valuableObject.name + " - Fragility: " + valuableObject.durabilityPreset.fragility + " => " + customDurability.fragility + " Durability: " + valuableObject.durabilityPreset.durability + " => " + customDurability.durability + " ValueMin: " + valuableObject.valuePreset.valueMin + " => " + customValue.valueMin + " ValueMax: " + valuableObject.valuePreset.valueMax + " => " + customValue.valueMax);
            else
                Plugin.Log("Increasing fragility and value of object: " + valuableObject.name);

            this.customDurability = customDurability;
            this.customValue = customValue;
            valuableObject.durabilityPreset = customDurability;
            valuableObject.valuePreset = customValue;
            impactDetector.fragility = valuableObject.durabilityPreset.fragility;
            impactDetector.durability = valuableObject.durabilityPreset.durability;

            currentFragileValuables.Add(valuableObject);
        }


        [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
        [HarmonyPostfix]
        public static void OnLevelGenerated()
        {
            if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                Plugin.Log("Spawned " + currentFragileValuables.Count + " increased fragile valuables.");
        }


        [HarmonyPatch(typeof(ValuableObject), "Awake")]
        [HarmonyPostfix]
        public static void OnAwake(ValuableObject __instance)
        {
            var increasedValuableObject = __instance.gameObject.AddComponent<IncreasedValuableObject>();
        }
    }
}