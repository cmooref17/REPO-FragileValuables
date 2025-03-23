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


        public void Awake()
        {
            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
                return;

            if (random == null)
                random = new System.Random((int)Time.time);

            valuableObject = GetComponent<ValuableObject>();

            if (!valuableObject || valuableObject.durabilityPreset == null || valuableObject.durabilityPreset.fragility < 90 || (valuableObject.physAttributePreset != null && valuableObject.physAttributePreset.mass > 1) || (valuableObject.valuePreset != null && valuableObject.valuePreset.valueMin >= 5000f))
                return;

            float chance = (float)random.NextDouble();
            if (chance >= 1 - ConfigSettings.fragileValuableChance.Value)
            {
                enabled = true;
                IncreaseFragility();
            }
        }


        public void Start()
        {
            if ((GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient) || !valuableObject)
                return;

            if (currentFragileValuables.Contains(valuableObject))
            {
                PhotonView photonView = gameObject.GetComponent<PhotonView>();
                photonView.RPC("IncreaseFragilityRPC", RpcTarget.All);
            }
        }


        [PunRPC]
        public void IncreaseFragilityRPC()
        {
            if ((GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient) || !valuableObject)
                return;
            IncreaseFragility();
        }


        public void IncreaseFragility()
        {
            enabled = true;
            if (currentFragileValuables.Contains(valuableObject))
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

            currentFragileValuables.Add(valuableObject);
            Plugin.LogVerbose("Increasing fragility and value of object: " + valuableObject.name + " - Fragility: " + valuableObject.durabilityPreset.fragility + " => " + customDurability.fragility + " Durability: " + valuableObject.durabilityPreset.durability + " => " + customDurability.durability + " ValueMin: " + valuableObject.valuePreset.valueMin + " => " + customValue.valueMin + " ValueMax: " + valuableObject.valuePreset.valueMax + " => " + customValue.valueMax);

            valuableObject.durabilityPreset = customDurability;
            valuableObject.valuePreset = customValue;
        }


        [HarmonyPatch(typeof(LevelGenerator), "Start")]
        [HarmonyPrefix]
        private static void OnLevelGenerationStart()
        {
            currentFragileValuables.Clear();
        }


        [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
        [HarmonyPostfix]
        private static void OnLevelGenerated()
        {
            if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                Plugin.Log("Spawned " + currentFragileValuables.Count + " increased fragile valuables.");
        }


        [HarmonyPatch(typeof(ValuableObject), "Awake")]
        [HarmonyPostfix]
        private static void OnAwake(ValuableObject __instance)
        {
            var increasedValuableObject = __instance.gameObject.AddComponent<IncreasedValuableObject>();
            increasedValuableObject.enabled = false;
        }
    }
}