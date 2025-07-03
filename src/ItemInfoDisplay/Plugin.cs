using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Jobs;
using UnityEngine;

namespace ItemInfoDisplay;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    private static GUIManager guiManager;
    private static TextMeshProUGUI itemInfoDisplayTextMesh;
    private static Dictionary<string, string> effectColors = new Dictionary<string, string>();
    private static float lastKnownSinceItemAttach;
    private static bool hasChanged;
    private static float forceUpdateTime;

    private void Awake()
    {
        Log = Logger;
        InitEffectColors(effectColors);
        lastKnownSinceItemAttach = 0f;
        hasChanged = true;
        forceUpdateTime = 1f;
        Harmony.CreateAndPatchAll(typeof(ItemInfoDisplayUpdatePatch));
        Harmony.CreateAndPatchAll(typeof(ItemInfoDisplayEquipPatch));
        Harmony.CreateAndPatchAll(typeof(ItemInfoDisplayFinishCookingPatch));
        Harmony.CreateAndPatchAll(typeof(ItemInfoDisplayReduceUsesRPCPatch));
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static class ItemInfoDisplayUpdatePatch 
    {
        [HarmonyPatch(typeof(CharacterItems), "Update")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayUpdate(CharacterItems __instance)
        {
            try
            {
                if (guiManager == null)
                {
                    AddDisplayObject();
                }
                else
                {
                    if (Character.observedCharacter.data.currentItem != null)
                    {
                        if (hasChanged)
                        {
                            hasChanged = false;
                            ProcessItemGameObject();
                        }
                        //else if ((Character.observedCharacter.data.sinceItemAttach == 0) || (Mathf.Abs(Character.observedCharacter.data.sinceItemAttach - lastKnownSinceItemAttach) >= forceUpdateTime))
                        else if (Mathf.Abs(Character.observedCharacter.data.sinceItemAttach - lastKnownSinceItemAttach) >= forceUpdateTime)
                        {
                            hasChanged = true;
                            lastKnownSinceItemAttach = Character.observedCharacter.data.sinceItemAttach;
                        }

                        if (!itemInfoDisplayTextMesh.gameObject.activeSelf)
                        {
                            itemInfoDisplayTextMesh.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        if (itemInfoDisplayTextMesh.gameObject.activeSelf) 
                        {
                            itemInfoDisplayTextMesh.gameObject.SetActive(false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.Message + e.StackTrace);
            }
        }
    }

    private static class ItemInfoDisplayEquipPatch
    {
        [HarmonyPatch(typeof(CharacterItems), "Equip")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayEquip(CharacterItems __instance)
        {
            if (Character.ReferenceEquals(Character.observedCharacter, __instance.character))
            {
                hasChanged = true;
            }
        }
    }

    private static class ItemInfoDisplayFinishCookingPatch
    {
        [HarmonyPatch(typeof(ItemCooking), "FinishCooking")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayFinishCooking(ItemCooking __instance)
        {
            if (Character.ReferenceEquals(Character.observedCharacter, __instance.item.holderCharacter))
            {
                hasChanged = true;
            }
        }
    }

    private static class ItemInfoDisplayReduceUsesRPCPatch
    {
        [HarmonyPatch(typeof(Action_ReduceUses), "ReduceUsesRPC")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayReduceUsesRPC(Action_ReduceUses __instance)
        {
            if (Character.ReferenceEquals(Character.observedCharacter, __instance.character))
            {
                hasChanged = true;
            }
        }
    }

    private static void ProcessItemGameObject()
    {
        Item item = Character.observedCharacter.data.currentItem;
        GameObject itemGameObj = item.gameObject;
        Component[] itemComponents = itemGameObj.GetComponents(typeof(Component));
        string suffix = effectColors["Weight"] + item.carryWeight.ToString() + " WEIGHT</color>";
        itemInfoDisplayTextMesh.text = "";
        itemInfoDisplayTextMesh.text += "12345678901234567890123456789012\n";
        for (int i = 0; i < itemComponents.Length; i++)
        {
            if (itemComponents[i].GetType() == typeof(Action_RestoreHunger))
            {
                Action_RestoreHunger effect = (Action_RestoreHunger)itemComponents[i];
                itemInfoDisplayTextMesh.text += ProcessEffect((effect.restorationAmount * -1f), "Hunger");
            }
            else if (itemComponents[i].GetType() == typeof(Action_GiveExtraStamina))
            {
                Action_GiveExtraStamina effect = (Action_GiveExtraStamina)itemComponents[i];
                itemInfoDisplayTextMesh.text += ProcessEffect(effect.amount, "Extra Stamina");
            }
            else if (itemComponents[i].GetType() == typeof(Action_InflictPoison))
            {
                Action_InflictPoison effect = (Action_InflictPoison)itemComponents[i];
                itemInfoDisplayTextMesh.text += "AFTER " + effect.delay.ToString() + "s, GAIN " + effectColors["Poison"] 
                    + (effect.poisonPerSecond * effect.inflictionTime * 100f).ToString("F1").Replace(".0", "") + " POISON</color> OVER " 
                    + effect.inflictionTime.ToString() + "s\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_ModifyStatus))
            {
                Action_ModifyStatus effect = (Action_ModifyStatus)itemComponents[i];
                itemInfoDisplayTextMesh.text += ProcessEffect(effect.changeAmount, effect.statusType.ToString());
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplyMassAffliction))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_ApplyMassAffliction\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplyAffliction))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_ApplyAffliction\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_ClearAllStatus))
            {
                Action_ClearAllStatus effect = (Action_ClearAllStatus)itemComponents[i];
                itemInfoDisplayTextMesh.text += "CLEAR ALL STATUS";
                if (effect.excludeCurse)
                {
                    itemInfoDisplayTextMesh.text += " EXCEPT " + effectColors["Curse"] + "CURSE</color>";
                }
                if (effect.otherExclusions.Count > 0)
                {
                    foreach (CharacterAfflictions.STATUSTYPE exclusion in effect.otherExclusions)
                    {
                        itemInfoDisplayTextMesh.text += ", " + effectColors[exclusion.ToString()] + exclusion.ToString().ToUpper() + "</color>";
                    }
                }
                itemInfoDisplayTextMesh.text += "\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_ConsumeAndSpawn))
            {
                Action_ConsumeAndSpawn effect = (Action_ConsumeAndSpawn)itemComponents[i];
                if (effect.itemToSpawn.ToString().Contains("Peel"))
                {
                    itemInfoDisplayTextMesh.text += "GAIN A PEEL WHEN EATEN\n";
                }
                else
                {
                    // not sure if used for items other than berrynanas
                    itemInfoDisplayTextMesh.text += "TODO: Action_ConsumeAndSpawn\n";
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_ReduceUses))
            {
                if (item.totalUses > 1)
                {
                    suffix += ", " + item.data.data[DataEntryKey.ItemUses] + " USES";
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplyInfiniteStamina))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_ApplyInfiniteStamina\n";
            }
            else if (itemComponents[i].GetType() == typeof(Actions_DefaultConstructActions))
            {
                itemInfoDisplayTextMesh.text += "CAN BE PLACED\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_LightLantern))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_LightLantern\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_TootMagicBugle))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_TootMagicBugle\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Flare))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_Flare\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_RaycastDart))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_RaycastDart\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Die))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_Die\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_SpawnGuidebookPage))
            {
                itemInfoDisplayTextMesh.text += "CAN BE OPENED\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Guidebook))
            {
                itemInfoDisplayTextMesh.text += "CAN BE READ\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_CallScoutmaster))
            {
                itemInfoDisplayTextMesh.text += "BREAK RULE 0\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_MoraleBoost))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_MoraleBoost\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Passport))
            {
                itemInfoDisplayTextMesh.text += "CUSTOMIZE CHARACTER\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_WarpToRandomPlayer))
            {
                itemInfoDisplayTextMesh.text += "WARP TO RANDOM PLAYER\n";
            }
            else if (itemComponents[i].GetType() == typeof(Actions_Binoculars))
            {
                itemInfoDisplayTextMesh.text += "USE TO LOOK FURTHER\n";
            }
            /*else if (itemComponents[i].GetType() == typeof(Action_OverrideCamera))
            {
                //ignore
            }
            else if (itemComponents[i].GetType() == typeof(Action_ShowBinocularOverlay))
            {
                //ignore
            }
            else if (itemComponents[i].GetType() == typeof(Action_Consume))
            {
                // ignore
            }
            else if (itemComponents[i].GetType() == typeof(Action_PlayAnimation))
            {
                // ignore
            }
            else if (itemComponents[i].GetType() == typeof(Action_GuidebookScroll))
            {
                // ignore
            }
            else if (itemComponents[i].GetType() == typeof(Action_PlayItemAnimation))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_PlayItemAnimation\n"; // unused afaik
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplyAntigrav))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_ApplyAntigrav\n"; // unused afaik
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplySuperJump))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_ApplySuperJump\n"; // unused afaik
            }
            else if (itemComponents[i].GetType() == typeof(Action_LaunchPlayer))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_LaunchPlayer\n"; // unused afaik
            }
            else if (itemComponents[i].GetType() == typeof(Action_Torch))
            {
                itemInfoDisplayTextMesh.text += "TODO: Action_Torch\n"; // unused afaik
            }*/
        }



        itemInfoDisplayTextMesh.text += suffix;
    }

    private static string ProcessEffect(float amount, string effect)
    {
        string result = "";

        if (amount == 0)
        {
            return result;
        }
        else if (amount > 0)
        {
            result += "Gain ";
        }
        else if (amount < 0)
        {
            result += "Remove ";
        }
        result += effectColors[effect] + (Mathf.Abs(amount) * 100f).ToString("F1").Replace(".0", "") + " " + effect + "</color>\n";

        return result.ToUpper();
    }

    private static void AddDisplayObject()
    {
        GameObject guiManagerGameObj = GameObject.Find("GAME/GUIManager");
        guiManager = guiManagerGameObj.GetComponent<GUIManager>();
        TMPro.TMP_FontAsset font = guiManager.heroDayText.font;

        GameObject invGameObj = guiManagerGameObj.transform.Find("Canvas_HUD/Prompts/ItemPromptLayout").gameObject;
        GameObject itemInfoDisplayGameObj = new GameObject("ItemInfoDisplay");
        itemInfoDisplayGameObj.transform.SetParent(invGameObj.transform);
        itemInfoDisplayTextMesh = itemInfoDisplayGameObj.AddComponent<TextMeshProUGUI>();
        RectTransform itemInfoDisplayRect = itemInfoDisplayGameObj.GetComponent<RectTransform>();

        itemInfoDisplayRect.sizeDelta = new Vector2(500f, 0f); // Y is 0, otherwise moves other item prompts
        itemInfoDisplayTextMesh.font = font;
        itemInfoDisplayTextMesh.fontSize = 20f;
        itemInfoDisplayTextMesh.alignment = TextAlignmentOptions.BottomLeft;
        itemInfoDisplayTextMesh.lineSpacing = -50f;
        itemInfoDisplayTextMesh.text = "";
        itemInfoDisplayTextMesh.outlineWidth = 0.08f;
    }
    private static void InitEffectColors(Dictionary<string, string> dict)
    {
        dict.Add("Hunger", "<#FFBD16>");
        dict.Add("Extra Stamina", "<#BFEC1B>");
        dict.Add("Injury", "<#FF5300>");
        dict.Add("Crab", "<#E13542>");
        dict.Add("Poison", "<#A139FF>");
        dict.Add("Cold", "<#00BCFF>");
        dict.Add("Heat", "<#C80918>");
        dict.Add("Hot", "<#C80918>");
        dict.Add("Sleepy", "<#FF5CA4>");
        dict.Add("Drowsy", "<#FF5CA4>");
        dict.Add("Curse", "<#1B0043>");
        dict.Add("Weight", "<#A65A1C>");
    }
}