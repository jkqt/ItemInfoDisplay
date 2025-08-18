﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

using MonoMod.Utils;

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.Rendering;

using Zorro.UI;
using Zorro.UI.Effects;

using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

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
    private static ConfigEntry<float> configFontSize;
    private static ConfigEntry<float> configOutlineWidth;
    private static ConfigEntry<float> configLineSpacing;
    private static ConfigEntry<float> configSizeDeltaX;
    private static ConfigEntry<float> configForceUpdateTime;

    private void Awake()
    {
        Log = Logger;
        InitEffectColors(effectColors);
        lastKnownSinceItemAttach = 0f;
        hasChanged = true;
        configFontSize = ((BaseUnityPlugin)this).Config.Bind<float>("ItemInfoDisplay", "Font Size", 20f, "Customize the Font Size for description text.");
        configOutlineWidth = ((BaseUnityPlugin)this).Config.Bind<float>("ItemInfoDisplay", "Outline Width", 0.08f, "Customize the Outline Width for item description text.");
        configLineSpacing = ((BaseUnityPlugin)this).Config.Bind<float>("ItemInfoDisplay", "Line Spacing", -35f, "Customize the Line Spacing for item description text.");
        configSizeDeltaX = ((BaseUnityPlugin)this).Config.Bind<float>("ItemInfoDisplay", "Size Delta X", 550f, "Customize the horizontal length of the container for the mod. Increasing moves text left, decreasing moves text right.");
        configForceUpdateTime = ((BaseUnityPlugin)this).Config.Bind<float>("ItemInfoDisplay", "Force Update Time", 1f, "Customize the time in seconds until the mod forces an update for the item.");
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
                        else if (Mathf.Abs(Character.observedCharacter.data.sinceItemAttach - lastKnownSinceItemAttach) >= configForceUpdateTime.Value)
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
            try
            {
                if (Character.ReferenceEquals(Character.observedCharacter, __instance.character))
                {
                    hasChanged = true;
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.Message + e.StackTrace);
            }
        }
    }

    private static class ItemInfoDisplayFinishCookingPatch
    {
        [HarmonyPatch(typeof(ItemCooking), "FinishCooking")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayFinishCooking(ItemCooking __instance)
        {
            try
            {
                if (Character.ReferenceEquals(Character.observedCharacter, __instance.item.holderCharacter))
                {
                    hasChanged = true;
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.Message + e.StackTrace);
            }
        }
    }

    private static class ItemInfoDisplayReduceUsesRPCPatch
    {
        [HarmonyPatch(typeof(Action_ReduceUses), "ReduceUsesRPC")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayReduceUsesRPC(Action_ReduceUses __instance)
        {
            try
            {
                if (Character.ReferenceEquals(Character.observedCharacter, __instance.character))
                {
                    hasChanged = true;
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.Message + e.StackTrace);
            }
        }
    }

    private static void ProcessItemGameObject()
    {
        Item item = Character.observedCharacter.data.currentItem; // not sure why this broke after THE MESA update, made no changes (just rebuilt)
        GameObject itemGameObj = item.gameObject;
        Component[] itemComponents = itemGameObj.GetComponents(typeof(Component));
        bool isConsumable = false;
        string prefixStatus = "";
        string suffixWeight = "";
        string suffixUses = "";
        string suffixCooked = "";
        string suffixAfflictions = "";
        itemInfoDisplayTextMesh.text = "";

        if (Ascents.itemWeightModifier > 0)
        {
            suffixWeight += $"{GetText("WEIGHT", effectColors["Weight"], ((item.carryWeight + Ascents.itemWeightModifier) * 2.5f).ToString("F1").Replace(".0", ""))}</color>";
        }
        else
        {
            suffixWeight += $"{GetText("WEIGHT", effectColors["Weight"], (item.carryWeight * 2.5f).ToString("F1").Replace(".0", ""))}</color>";
        }

        if (itemGameObj.name.Equals("Bugle(Clone)"))
        {
            itemInfoDisplayTextMesh.text += $"{GetText("Bugle")}\n";//MAKE SOME NOISE
        }
        else if (itemGameObj.name.Equals("Pirate Compass(Clone)"))
        {
            itemInfoDisplayTextMesh.text += $"{GetText("Pirate Compass", effectColors["Injury"])}";
            //itemInfoDisplayTextMesh.text += effectColors["Injury"] + "POINTS</color> TO THE NEAREST LUGGAGE\n";
        }
        else if (itemGameObj.name.Equals("Compass(Clone)"))
        {
            itemInfoDisplayTextMesh.text += $"{GetText("Compass", effectColors["Injury"])}";
            //itemInfoDisplayTextMesh.text += effectColors["Injury"] + "POINTS</color> NORTH TO THE PEAK\n";
        }
        else if (itemGameObj.name.Equals("Shell Big(Clone)"))
        {
            itemInfoDisplayTextMesh.text += $"{GetText("Shell Big", effectColors["Hunger"])}";
            //itemInfoDisplayTextMesh.text += "TRY " + effectColors["Hunger"] + "THROWING</color> AT A COCONUT\n";
        }

        for (int i = 0; i < itemComponents.Length; i++)
        {
            if (itemComponents[i].GetType() == typeof(ItemUseFeedback))
            {
                ItemUseFeedback itemUseFeedback = (ItemUseFeedback)itemComponents[i];
                if (itemUseFeedback.useAnimation.Equals("Eat") || itemUseFeedback.useAnimation.Equals("Drink") || itemUseFeedback.useAnimation.Equals("Heal"))
                {
                    isConsumable = true;
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_Consume))
            {
                isConsumable = true;
            }
            else if (itemComponents[i].GetType() == typeof(Action_RestoreHunger))
            {
                Action_RestoreHunger effect = (Action_RestoreHunger)itemComponents[i];
                prefixStatus += ProcessEffect((effect.restorationAmount * -1f), "Hunger");
            }
            else if (itemComponents[i].GetType() == typeof(Action_GiveExtraStamina))
            {
                Action_GiveExtraStamina effect = (Action_GiveExtraStamina)itemComponents[i];
                prefixStatus += ProcessEffect(effect.amount, "Extra Stamina");
            }
            else if (itemComponents[i].GetType() == typeof(Action_InflictPoison))
            {
                Action_InflictPoison effect = (Action_InflictPoison)itemComponents[i];
                prefixStatus += "AFTER " + effect.delay.ToString() + "s, " + ProcessEffectOverTime(effect.poisonPerSecond, 1f, effect.inflictionTime, "Poison");
            }
            else if (itemComponents[i].GetType() == typeof(Action_AddOrRemoveThorns))
            {
                Action_AddOrRemoveThorns effect = (Action_AddOrRemoveThorns)itemComponents[i];
                prefixStatus += ProcessEffect((effect.thornCount * 0.05f), "Thorns"); // TODO: Search for thorns amount per applied thorn
            }
            else if (itemComponents[i].GetType() == typeof(Action_ModifyStatus))
            {
                Action_ModifyStatus effect = (Action_ModifyStatus)itemComponents[i];
                prefixStatus += ProcessEffect(effect.changeAmount, effect.statusType.ToString());
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplyMassAffliction))
            {
                Action_ApplyMassAffliction effect = (Action_ApplyMassAffliction)itemComponents[i];
                suffixAfflictions += "<#CCCCCC>NEARBY PLAYERS WILL RECEIVE:</color>\n";
                suffixAfflictions += ProcessAffliction(effect.affliction);
                if (effect.extraAfflictions.Length > 0)
                {
                    for (int j = 0; j < effect.extraAfflictions.Length; j++)
                    {
                        if (suffixAfflictions.EndsWith('\n'))
                        {
                            suffixAfflictions = suffixAfflictions.Remove(suffixAfflictions.Length - 1);
                        }
                        suffixAfflictions += ",\n" + ProcessAffliction(effect.extraAfflictions[j]);
                    }
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_ApplyAffliction))
            {
                Action_ApplyAffliction effect = (Action_ApplyAffliction)itemComponents[i];
                suffixAfflictions += ProcessAffliction(effect.affliction);
            }
            else if (itemComponents[i].GetType() == typeof(Action_ClearAllStatus))
            {
                Action_ClearAllStatus effect = (Action_ClearAllStatus)itemComponents[i];
                itemInfoDisplayTextMesh.text += effectColors["ItemInfoDisplayPositive"] + "CLEAR ALL STATUS</color>";
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
                itemInfoDisplayTextMesh.text = itemInfoDisplayTextMesh.text.Replace(", <#E13542>CRAB</color>", "") + "\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_ConsumeAndSpawn))
            {
                Action_ConsumeAndSpawn effect = (Action_ConsumeAndSpawn)itemComponents[i];
                if (effect.itemToSpawn.ToString().Contains("Peel"))
                {
                    itemInfoDisplayTextMesh.text += "<#CCCCCC>GAIN A PEEL WHEN EATEN</color>\n";
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_ReduceUses))
            {
                OptionableIntItemData uses = (OptionableIntItemData)item.data.data[DataEntryKey.ItemUses];
                if (uses.HasData)
                {
                    if (uses.Value > 1)
                    {
                        suffixUses += "   " + uses.Value + " USES";
                    }
                }
            }
            else if (itemComponents[i].GetType() == typeof(Lantern))
            {
                Lantern lantern = (Lantern)itemComponents[i];
                if (itemGameObj.name.Equals("Torch(Clone)")){
                    itemInfoDisplayTextMesh.text += "CAN BE LIT\n";
                }
                else {
                    suffixAfflictions += "<#CCCCCC>WHEN LIT, NEARBY PLAYERS RECEIVE:</color>\n";
                }

                if (itemGameObj.name.Equals("Lantern_Faerie(Clone)"))
                {
                    StatusField effect = itemGameObj.transform.Find("FaerieLantern/Light/Heat").GetComponent<StatusField>();
                    suffixAfflictions += ProcessEffectOverTime(effect.statusAmountPerSecond, 1f, lantern.startingFuel, effect.statusType.ToString());
                    foreach (StatusField.StatusFieldStatus status in effect.additionalStatuses)
                    {
                        if (suffixAfflictions.EndsWith('\n'))
                        {
                            suffixAfflictions = suffixAfflictions.Remove(suffixAfflictions.Length - 1);
                        }
                        suffixAfflictions += ",\n" + ProcessEffectOverTime(status.statusAmountPerSecond, 1f, lantern.startingFuel, status.statusType.ToString());
                    }
                }
                else if (itemGameObj.name.Equals("Lantern(Clone)"))
                {
                    StatusField effect = itemGameObj.transform.Find("GasLantern/Light/Heat").GetComponent<StatusField>();
                    suffixAfflictions += ProcessEffectOverTime(effect.statusAmountPerSecond, 1f, lantern.startingFuel, effect.statusType.ToString());
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_RaycastDart))
            {
                Action_RaycastDart effect = (Action_RaycastDart)itemComponents[i];
                isConsumable = true;
                suffixAfflictions += "<#CCCCCC>SHOOT A DART THAT WILL APPLY:</color>\n";
                for (int j = 0; j < effect.afflictionsOnHit.Length; j++)
                {
                    suffixAfflictions += ProcessAffliction(effect.afflictionsOnHit[j]);
                    if (suffixAfflictions.EndsWith('\n'))
                    {
                        suffixAfflictions = suffixAfflictions.Remove(suffixAfflictions.Length - 1);
                    }
                    suffixAfflictions += ",\n";
                }
                if (suffixAfflictions.EndsWith('\n'))
                {
                    suffixAfflictions = suffixAfflictions.Remove(suffixAfflictions.Length - 2);
                }
                suffixAfflictions += "\n";
            }
            else if (itemComponents[i].GetType() == typeof(MagicBugle))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("MagicBugle")}";
                //itemInfoDisplayTextMesh.text += "WHILE PLAYING THE BUGLE,";
            }
            else if (itemComponents[i].GetType() == typeof(ClimbingSpikeComponent))
            {
                itemInfoDisplayTextMesh.text += "PLACE A PITON YOU CAN GRAB\nTO " + effectColors["Extra Stamina"] + "REGENERATE STAMINA</color>\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Flare))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Flare")}";
                //itemInfoDisplayTextMesh.text += "CAN BE LIT\n";
            }
            else if (itemComponents[i].GetType() == typeof(Backpack))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Backpack")}";
                //itemInfoDisplayTextMesh.text += "DROP TO PLACE ITEMS INSIDE\n";
            }
            else if (itemComponents[i].GetType() == typeof(BananaPeel))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("BananaPeel", effectColors["Hunger"])}";
                //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "SLIP</color> WHEN STEPPED ON\n";
            }
            else if (itemComponents[i].GetType() == typeof(Constructable))
            {
                Constructable effect = (Constructable)itemComponents[i];
                if (effect.constructedPrefab.name.Equals("PortableStovetop_Placed"))
                {
                    itemInfoDisplayTextMesh.text += $"{GetText("Constructable_PortableStovetop_Placed", effectColors["Injury"], effect.constructedPrefab.GetComponent<Campfire>().burnsFor.ToString())}";
                    //itemInfoDisplayTextMesh.text += "PLACE A " + effectColors["Injury"] + "COOKING</color> STOVE FOR " + effect.constructedPrefab.GetComponent<Campfire>().burnsFor.ToString() + "s\n";
                }
                else
                {
                    itemInfoDisplayTextMesh.text += $"{GetText("Constructable")}";
                    //itemInfoDisplayTextMesh.text += "CAN BE PLACED\n";
                }
            }
            else if (itemComponents[i].GetType() == typeof(RopeSpool))
            {
                RopeSpool effect = (RopeSpool)itemComponents[i];
                if (effect.isAntiRope)
                {
                    itemInfoDisplayTextMesh.text += $"{GetText("RopeSpool_AntiRope")}";
                    //itemInfoDisplayTextMesh.text += "PLACE A ROPE THAT FLOATS UP\n";
                }
                else
                {
                    itemInfoDisplayTextMesh.text += $"{GetText("RopeSpool")}";
                    //itemInfoDisplayTextMesh.text += "PLACE A ROPE\n";
                }
                itemInfoDisplayTextMesh.text += $"{GetText("RopeSpool_TIP", (effect.minSegments / 4f).ToString("F2").Replace(".0", ""), (Rope.MaxSegments / 4f).ToString("F1").Replace(".0", ""))}";
                //itemInfoDisplayTextMesh.text += "FROM " + (effect.minSegments / 4f).ToString("F2").Replace(".0", "") + "m LONG, UP TO " 
                //    + (Rope.MaxSegments / 4f).ToString("F1").Replace(".0", "") + "m LONG\n";
                //using force update here for remaining length since Rope has no character distinction for Detach_Rpc() hook, maybe unless OK with any player triggering this
                if (configForceUpdateTime.Value <= 1f)
                {
                    suffixUses += "   " + (effect.RopeFuel / 4f).ToString("F2").Replace(".00", "") + "m LEFT";
                }
            }
            else if (itemComponents[i].GetType() == typeof(RopeShooter))
            {
                RopeShooter effect = (RopeShooter)itemComponents[i];
                itemInfoDisplayTextMesh.text += "SHOOT A ROPE ANCHOR WHICH PLACES\nA ROPE THAT ";
                if (effect.ropeAnchorWithRopePref.name.Equals("RopeAnchorForRopeShooterAnti"))
                {
                    itemInfoDisplayTextMesh.text += "FLOATS UP ";
                }
                else
                {
                    itemInfoDisplayTextMesh.text += "DROPS DOWN ";
                }
                itemInfoDisplayTextMesh.text += (effect.maxLength / 4f).ToString("F1").Replace(".0", "") + "m\n";
            }
            else if (itemComponents[i].GetType() == typeof(Antigrav))
            {
                Antigrav effect = (Antigrav)itemComponents[i];
                if (effect.intensity != 0f)
                {
                    suffixAfflictions += effectColors["Injury"] + "WARNING:</color> <#CCCCCC>FLIES AWAY IF DROPPED</color>\n";
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_Balloon))
            {
                suffixAfflictions += "CAN ATTACH TO CHARACTER\n";
            }
            else if (itemComponents[i].GetType() == typeof(VineShooter))
            {
                VineShooter effect = (VineShooter)itemComponents[i];
                itemInfoDisplayTextMesh.text += "SHOOT A CHAIN THAT CONNECTS FROM\nYOUR POSITION TO WHERE YOU SHOOT\nUP TO "
                    + (effect.maxLength / (5f / 3f)).ToString("F1").Replace(".0", "") + "m AWAY\n";
            }
            else if (itemComponents[i].GetType() == typeof(ShelfShroom))
            {
                ShelfShroom effect = (ShelfShroom)itemComponents[i];
                if (effect.instantiateOnBreak.name.Equals("HealingPuffShroomSpawn"))
                {
                    GameObject effect1 = effect.instantiateOnBreak.transform.Find("VFX_SporeHealingExplo").gameObject;
                    AOE effect1AOE = effect1.GetComponent<AOE>();
                    GameObject effect2 = effect1.transform.Find("VFX_SporePoisonExplo").gameObject;
                    AOE effect2AOE = effect2.GetComponent<AOE>();
                    AOE[] effect2AOEs = effect2.GetComponents<AOE>();
                    TimeEvent effect2TimeEvent = effect2.GetComponent<TimeEvent>();
                    RemoveAfterSeconds effect2RemoveAfterSeconds = effect2.GetComponent<RemoveAfterSeconds>();
                    itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> TO RELEASE GAS THAT WILL:\n";
                    itemInfoDisplayTextMesh.text += ProcessEffect((Mathf.Round(effect1AOE.statusAmount * 0.9f * 40f) / 40f), effect1AOE.statusType.ToString()); // incorrect? calculates strangely so i somewhat manually adjusted the values
                    itemInfoDisplayTextMesh.text += ProcessEffectOverTime((Mathf.Round(effect2AOE.statusAmount * (1f / effect2TimeEvent.rate) * 40f) / 40f), 1f, effect2RemoveAfterSeconds.seconds, effect2AOE.statusType.ToString()); // incorrect?
                    if (effect2AOEs.Length > 1)
                    {
                        itemInfoDisplayTextMesh.text += ProcessEffectOverTime((Mathf.Round(effect2AOEs[1].statusAmount * (1f / effect2TimeEvent.rate) * 40f) / 40f), 1f, (effect2RemoveAfterSeconds.seconds + 1f), effect2AOEs[1].statusType.ToString()); // incorrect?
                    } // didn't handle dynamically because there were 2 poison removal AOEs but 1 doesn't seem to work or they are buggy in some way (probably time event rate)?
                }
                else if (effect.instantiateOnBreak.name.Equals("ShelfShroomSpawn"))
                {
                    itemInfoDisplayTextMesh.text += $"{GetText("ShelfShroomSpawn", effectColors["Hunger"])}";
                    //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> TO DEPLOY A PLATFORM\n";
                }
                else if (effect.instantiateOnBreak.name.Equals("BounceShroomSpawn"))
                {
                    itemInfoDisplayTextMesh.text += $"{GetText("BounceShroomSpawn", effectColors["Hunger"])}";
                    //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> TO DEPLOY A BOUNCE PAD\n";
                }
            }
            else if (itemComponents[i].GetType() == typeof(ScoutEffigy))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("ScoutEffigy", effectColors["Extra Stamina"])}";
                //itemInfoDisplayTextMesh.text += effectColors["Extra Stamina"] + "REVIVE</color> A DEAD PLAYER\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Die))
            {
                itemInfoDisplayTextMesh.text += "YOU " + effectColors["Curse"] + "DIE</color> WHEN USED\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_SpawnGuidebookPage))
            {
                isConsumable = true;
                itemInfoDisplayTextMesh.text += "CAN BE OPENED\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Guidebook))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Guidebook")}";
                //itemInfoDisplayTextMesh.text += "CAN BE READ\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_CallScoutmaster))
            {
                itemInfoDisplayTextMesh.text += $"{(GetText("CallScoutmaster", effectColors["Injury"]))}";
                //itemInfoDisplayTextMesh.text += effectColors["Injury"] + "BREAKS RULE 0 WHEN USED</color>\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_MoraleBoost))
            {
                Action_MoraleBoost effect = (Action_MoraleBoost)itemComponents[i];
                if (effect.boostRadius < 0)
                {
                    itemInfoDisplayTextMesh.text += effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " + effectColors["Extra Stamina"] + (effect.baselineStaminaBoost * 100f).ToString("F1").Replace(".0", "") + " EXTRA STAMINA</color>\n";
                }
                else if (effect.boostRadius > 0)
                {
                    itemInfoDisplayTextMesh.text += "<#CCCCCC>NEARBY PLAYERS</color>" + effectColors["ItemInfoDisplayPositive"] + " GAIN</color> " + effectColors["Extra Stamina"] + (effect.baselineStaminaBoost * 100f).ToString("F1").Replace(".0", "") + " EXTRA STAMINA</color>\n";
                }
            }
            else if (itemComponents[i].GetType() == typeof(Breakable))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Breakable", effectColors["Hunger"])}";
                //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> TO CRACK OPEN\n";
            }
            else if (itemComponents[i].GetType() == typeof(Bonkable))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Bonkable", effectColors["Hunger"], effectColors["Injury"])}";
                //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> AT HEAD TO " + effectColors["Injury"] + "BONK</color>\n";
            }
            else if (itemComponents[i].GetType() == typeof(MagicBean))
            {
                MagicBean effect = (MagicBean)itemComponents[i];
                itemInfoDisplayTextMesh.text += $"{GetText("MagicBean", effectColors["Hunger"], (effect.plantPrefab.maxLength / 2f).ToString("F1").Replace(".0", ""))}";
                //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> TO PLANT A VINE THAT GROWS\nPERPENDICULAR TO TERRAIN UP TO\n"
                //    + (effect.plantPrefab.maxLength / 2f).ToString("F1").Replace(".0", "") + "m OR UNTIL IT HITS SOMETHING\n";
            }
            else if (itemComponents[i].GetType() == typeof(BingBong))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("BingBong")}";
                //itemInfoDisplayTextMesh.text += "MASCOT OF BINGBONG AIRWAYS\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_Passport))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Passport")}\n";//OPEN TO CUSTOMIZE CHARACTER
            }
            else if (itemComponents[i].GetType() == typeof(Actions_Binoculars))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Binoculars")}";
                //itemInfoDisplayTextMesh.text += "USE TO LOOK FURTHER\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_WarpToRandomPlayer))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("WarpToRandomPlayer")}";
                //itemInfoDisplayTextMesh.text += "WARP TO RANDOM PLAYER\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_WarpToBiome))
            {
                Action_WarpToBiome effect = (Action_WarpToBiome)itemComponents[i];
                itemInfoDisplayTextMesh.text += $"{GetText("WarpToBiome", effect.segmentToWarpTo.ToString().ToUpper())}";
            }
            else if (itemComponents[i].GetType() == typeof(Parasol))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Parasol")}\n";
                //itemInfoDisplayTextMesh.text += "OPEN TO SLOW YOUR DESCENT\n";
            }
            else if (itemComponents[i].GetType() == typeof(Frisbee))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("Frisbee", effectColors["Hunger"])}";
                //itemInfoDisplayTextMesh.text += effectColors["Hunger"] + "THROW</color> IT\n";
            }
            else if (itemComponents[i].GetType() == typeof(Action_ConstructableScoutCannonScroll))
            {
                itemInfoDisplayTextMesh.text += $"{GetText("ConstructableScoutCannonScroll")}";
                //itemInfoDisplayTextMesh.text += "\n<#CCCCCC>WHEN PLACED, LIGHT FUSE TO:</color>\nLAUNCH SCOUTS IN BARREL\n";
                    //+ "\n<#CCCCCC>LIMITS GRAVITATIONAL ACCELERATION\n(PREVENTS OR LOWERS FALL DAMAGE)</color>\n";
            }
            else if (itemComponents[i].GetType() == typeof(Dynamite))
            {
                Dynamite effect = (Dynamite)itemComponents[i];
                itemInfoDisplayTextMesh.text += effectColors["Injury"] + "EXPLODES</color> FOR UP TO " + effectColors["Injury"] 
                    + (effect.explosionPrefab.GetComponent<AOE>().statusAmount * 100f).ToString("F1").Replace(".0", "") + " INJURY</color>\n<#CCCCCC>ADDITIONAL DAMAGE TAKEN IF HELD</color>\n";
            }
            else if (itemComponents[i].GetType() == typeof(Scorpion))
            {
                Scorpion effect = (Scorpion)itemComponents[i]; // wanted to hide poison info if dead, but mob state does not immediately update on equip leading to visual bug
                if (configForceUpdateTime.Value <= 1f)
                {
                    float effectPoison = Mathf.Max(0.5f, (1f - item.holderCharacter.refs.afflictions.statusSum + 0.05f)) * 100f; // v.1.23.a BASED ON Scorpion.InflictAttack
                    itemInfoDisplayTextMesh.text += "IF ALIVE, " + effectColors["Poison"] + "STINGS</color> YOU\n" + effectColors["Curse"] + "DIES</color> WHEN " + effectColors["Heat"] + "COOKED</color>\n\n" 
                        + "<#CCCCCC>NEXT STING WILL DEAL:</color>\n" + effectColors["Poison"] + effectPoison.ToString("F1").Replace(".0", "") + " POISON</color> OVER " 
                        + effect.totalPoisonTime.ToString("F1").Replace(".0", "") + "s\n<#CCCCCC>(MORE DAMAGE IF HEALTHY)</color>\n";
                }
                else
                {
                    itemInfoDisplayTextMesh.text += "IF ALIVE, " + effectColors["Poison"] + "STINGS</color> YOU\n" + effectColors["Curse"] 
                        + "DIES</color> WHEN " + effectColors["Heat"] + "COOKED</color>\n\n" + "<#CCCCCC>NEXT STING WILL DEAL:</color>\nAT LEAST "
                        + effectColors["Poison"] + "50 POISON</color> OVER " + effect.totalPoisonTime.ToString("F1").Replace(".0", "") + "s\nAT MOST "
                        + effectColors["Poison"] + "105 POISON</color> OVER " + effect.totalPoisonTime.ToString("F1").Replace(".0", "")
                        + "s\n<#CCCCCC>(MORE DAMAGE IF HEALTHY)</color>\n"; // v.1.23.a THERE'S NO VARIABLE FOR POISON AMOUNT, CALCULATED IN Scorpion.InflictAttack
                }
            }
            else if (itemComponents[i].GetType() == typeof(Action_Spawn))
            {
                Action_Spawn effect = (Action_Spawn)itemComponents[i];
                if (effect.objectToSpawn.name.Equals("VFX_Sunscreen"))
                {
                    AOE effectAOE = effect.objectToSpawn.transform.Find("AOE").GetComponent<AOE>();
                    RemoveAfterSeconds effectTime = effect.objectToSpawn.transform.Find("AOE").GetComponent<RemoveAfterSeconds>();
                    itemInfoDisplayTextMesh.text += "<#CCCCCC>SPRAY A " + effectTime.seconds.ToString("F1").Replace(".0", "") + "s MIST THAT APPLIES:</color>\n"
                        + ProcessAffliction(effectAOE.affliction);                }
            }
            else if (itemComponents[i].GetType() == typeof(CactusBall))
            {
                CactusBall effect = (CactusBall)itemComponents[i];
                itemInfoDisplayTextMesh.text += effectColors["Thorns"] + "STICKS</color> TO YOUR BODY\n\nCAN " + effectColors["Hunger"] 
                    + "THROW</color> BY USING\nAT LEAST " + (effect.throwChargeRequirement * 100f).ToString("F1").Replace(".0", "") + "% POWER\n";
            }
            else if (itemComponents[i].GetType() == typeof(BingBongShieldWhileHolding))
            {
                itemInfoDisplayTextMesh.text += "<#CCCCCC>WHILE EQUIPPED, GRANTS:</color>\n" + effectColors["Shield"] + "SHIELD</color> (INVINCIBILITY)\n";
            }
            else if (itemComponents[i].GetType() == typeof(ItemCooking))
            {
                ItemCooking itemCooking = (ItemCooking)itemComponents[i];
                if (itemCooking.wreckWhenCooked && (itemCooking.timesCookedLocal >= 1))
                {

                    suffixCooked += $"{GetText("COOKED_BROKEN", effectColors["Curse"])}";
                    //suffixCooked += "\n" + effectColors["Curse"] + "BROKEN FROM COOKING</color>";
                }
                else if (itemCooking.wreckWhenCooked)
                {
                    suffixCooked += $"{GetText("COOK_BROKEN", effectColors["Curse"])}";
                    //suffixCooked += "\n" + effectColors["Curse"] + "BREAKS IF COOKED</color>";
                }
                else if (itemCooking.timesCookedLocal >= ItemCooking.COOKING_MAX)
                {
                    suffixCooked += $"{GetText("COOKED_MAX", effectColors["Curse"], itemCooking.timesCookedLocal.ToString())}";
                    //suffixCooked += "   " + effectColors["Curse"] + itemCooking.timesCookedLocal.ToString() + "x COOKED\nCANNOT BE COOKED</color>";
                }
                else if (itemCooking.timesCookedLocal == 0)
                {
                    suffixCooked += $"\n{GetText("COOK", effectColors["Extra Stamina"])}</color>";//CAN BE COOKED
                }
                else if (itemCooking.timesCookedLocal == 1)
                {
                    suffixCooked += $"{GetText("COOKED", effectColors["Extra Stamina"], itemCooking.timesCookedLocal.ToString(), effectColors["Hunger"])}";
                    //suffixCooked += "   " + effectColors["Extra Stamina"] + itemCooking.timesCookedLocal.ToString() + "x COOKED</color>\n" + effectColors["Hunger"] + "CAN BE COOKED</color>";
                }
                else if (itemCooking.timesCookedLocal == 2)
                {
                    suffixCooked += $"{GetText("COOKED", effectColors["Hunger"], itemCooking.timesCookedLocal.ToString(), effectColors["Injury"])}";
                    //suffixCooked += "   " + effectColors["Hunger"] + itemCooking.timesCookedLocal.ToString() + "x COOKED</color>\n" + effectColors["Injury"] + "CAN BE COOKED</color>";
                }
                else if (itemCooking.timesCookedLocal == 3)
                {
                    suffixCooked += $"{GetText("COOKED", effectColors["Injury"], itemCooking.timesCookedLocal.ToString(), effectColors["Poison"])}";

                    //suffixCooked += "   " + effectColors["Injury"] + itemCooking.timesCookedLocal.ToString() + "x COOKED</color>\n" + effectColors["Poison"] + "CAN BE COOKED</color>";
                }
                else if (itemCooking.timesCookedLocal >= 4)
                {
                    suffixCooked += $"{GetText("COOKED", effectColors["Poison"], itemCooking.timesCookedLocal.ToString(), "")}";

                    //suffixCooked += "   " + effectColors["Poison"] + itemCooking.timesCookedLocal.ToString() + "x COOKED\nCAN BE COOKED</color>";
                }
            }
        }

        if ((prefixStatus.Length > 0) && isConsumable)
        {
            itemInfoDisplayTextMesh.text = prefixStatus + "\n" + itemInfoDisplayTextMesh.text;
        }
        if (suffixAfflictions.Length > 0)
        {
            itemInfoDisplayTextMesh.text += "\n" + suffixAfflictions;
        }
        itemInfoDisplayTextMesh.text += "\n" + suffixWeight + suffixUses + suffixCooked;
        itemInfoDisplayTextMesh.text = itemInfoDisplayTextMesh.text.Replace("\n\n\n", "\n\n");
    }

    private static string ProcessEffect(float amount, string effect)
    {
        string result = "";
        var color = string.Empty;
        var action = string.Empty;
        if (amount == 0)
        {
            return result;
        }
        else if (amount > 0)
        {
            if (effect.Equals("Extra Stamina"))
            {
                color = effectColors["ItemInfoDisplayPositive"];
            }
            else
            {
                color = effectColors["ItemInfoDisplayNegative"];
            }
            action = "GAIN";
        }
        else if (amount < 0)
        {
            if (effect.Equals("Extra Stamina"))
            {
                color = effectColors["ItemInfoDisplayNegative"];
            }
            else
            {
                color = effectColors["ItemInfoDisplayPositive"];
            }
            action = "REMOVE";
        }
        result += GetText("ProcessEffect", color, GetText(action), effectColors[effect], (Mathf.Abs(amount) * 100f).ToString("F1").Replace(".0", ""), GetText($"Effect_{effect.ToUpper()}").ToUpper());

        //result += effectColors[effect] + (Mathf.Abs(amount) * 100f).ToString("F1").Replace(".0", "") + " " + effect.ToUpper() + "</color>\n";

        return result;
    }

    private static string ProcessEffectOverTime(float amountPerSecond, float rate, float time, string effect)
    {
        string result = "";
        var color = string.Empty;
        var action = string.Empty;
        if ((amountPerSecond == 0) || (time == 0))
        {
            return result;
        }
        else if (amountPerSecond > 0)
        {
            if (effect.Equals("Extra Stamina"))
            {
                color = effectColors["ItemInfoDisplayPositive"];
            }
            else
            {
                color = effectColors["ItemInfoDisplayNegative"];
            }
            action = "GAIN";
        }
        else if (amountPerSecond < 0)
        {
            if (effect.Equals("Extra Stamina"))
            {
                color = effectColors["ItemInfoDisplayNegative"];
            }
            else
            {
                color = effectColors["ItemInfoDisplayPositive"];
            }
            action = "REMOVE";
        }
        result += GetText("ProcessEffectOverTime", color, GetText(action), effectColors[effect], ((Mathf.Abs(amountPerSecond) * time * (1 / rate)) * 100f).ToString("F1").Replace(".0", ""), GetText($"Effect_{effect.ToUpper()}").ToUpper(), time.ToString());
        return result;
    }

    private static string ProcessAffliction(Peak.Afflictions.Affliction affliction)
    {
        string result = "";

        if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.FasterBoi)
        {
            Peak.Afflictions.Affliction_FasterBoi effect = (Peak.Afflictions.Affliction_FasterBoi)affliction;
            result += effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " + (effect.totalTime + effect.climbDelay).ToString("F1").Replace(".0", "") + "s OF " 
                + effectColors["Extra Stamina"] + Mathf.Round(effect.moveSpeedMod * 100f).ToString("F1").Replace(".0", "") + "% BONUS RUN SPEED</color> OR\n"
                + effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " + effect.totalTime.ToString("F1").Replace(".0", "") + "s OF " + effectColors["Extra Stamina"] 
                + Mathf.Round(effect.climbSpeedMod * 100f).ToString("F1").Replace(".0", "") + "% BONUS CLIMB SPEED</color>\nAFTERWARDS, " + effectColors["ItemInfoDisplayNegative"] 
                + "GAIN</color> " + effectColors["Drowsy"] + (effect.drowsyOnEnd * 100f).ToString("F1").Replace(".0", "") + " DROWSY</color>\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.ClearAllStatus)
        {
            Peak.Afflictions.Affliction_ClearAllStatus effect = (Peak.Afflictions.Affliction_ClearAllStatus)affliction;
            result += effectColors["ItemInfoDisplayPositive"] + "CLEAR ALL STATUS</color>";
            if (effect.excludeCurse)
            {
                result += " EXCEPT " + effectColors["Curse"] + "CURSE</color>";
            }
            result += "\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.AddBonusStamina)
        {
            Peak.Afflictions.Affliction_AddBonusStamina effect = (Peak.Afflictions.Affliction_AddBonusStamina)affliction;
            result += effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " + effectColors["Extra Stamina"] + (effect.staminaAmount * 100f).ToString("F1").Replace(".0", "") + " EXTRA STAMINA</color>\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.InfiniteStamina)
        {
            Peak.Afflictions.Affliction_InfiniteStamina effect = (Peak.Afflictions.Affliction_InfiniteStamina)affliction;
            if (effect.climbDelay > 0)
            {
                result += effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " + (effect.totalTime + effect.climbDelay).ToString("F1").Replace(".0", "") + "s OF " 
                    + effectColors["Extra Stamina"] + "INFINITE RUN STAMINA</color> OR\n" + effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " 
                    + effect.totalTime.ToString("F1").Replace(".0", "") + "s OF " + effectColors["Extra Stamina"] + "INFINITE CLIMB STAMINA</color>\n";
            }
            else
            {
                result += effectColors["ItemInfoDisplayPositive"] + "GAIN</color> " + (effect.totalTime).ToString("F1").Replace(".0", "") + "s OF " + effectColors["Extra Stamina"] + "INFINITE STAMINA\n";
            }
            if (effect.drowsyAffliction != null)
            {
                result += "AFTERWARDS, " + ProcessAffliction(effect.drowsyAffliction);
            }
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.AdjustStatus)
        {
            Peak.Afflictions.Affliction_AdjustStatus effect = (Peak.Afflictions.Affliction_AdjustStatus)affliction;
            if (effect.statusAmount > 0)
            {
                if (effect.Equals("Extra Stamina"))
                {
                    result += effectColors["ItemInfoDisplayPositive"];
                }
                else
                {
                    result += effectColors["ItemInfoDisplayNegative"];
                }
                result += "GAIN</color> ";
            }
            else
            {
                if (effect.Equals("Extra Stamina"))
                {
                    result += effectColors["ItemInfoDisplayNegative"];
                }
                else
                {
                    result += effectColors["ItemInfoDisplayPositive"];
                }
                result += "REMOVE</color> ";
            }
            result += effectColors[effect.statusType.ToString()] + (Mathf.Abs(effect.statusAmount) * 100f).ToString("F1").Replace(".0", "")
                + " " + effect.statusType.ToString().ToUpper() + "</color>\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.DrowsyOverTime)
        {
            Peak.Afflictions.Affliction_AdjustDrowsyOverTime effect = (Peak.Afflictions.Affliction_AdjustDrowsyOverTime)affliction; // 1.6.a
            if (effect.statusPerSecond > 0)
            {
                result += effectColors["ItemInfoDisplayNegative"] + "GAIN</color> ";
            }
            else
            {
                result += effectColors["ItemInfoDisplayPositive"] + "REMOVE</color> ";
            }
            result += effectColors["Drowsy"] + (Mathf.Round((Mathf.Abs(effect.statusPerSecond) * effect.totalTime * 100f) * 0.4f) / 0.4f).ToString("F1").Replace(".0", "")
                + " DROWSY</color> OVER " + effect.totalTime.ToString("F1").Replace(".0", "") + "s\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.ColdOverTime)
        {
            Peak.Afflictions.Affliction_AdjustColdOverTime effect = (Peak.Afflictions.Affliction_AdjustColdOverTime)affliction; // 1.6.a
            if (effect.statusPerSecond > 0)
            {
                result += effectColors["ItemInfoDisplayNegative"] + "GAIN</color> ";
            }
            else
            {
                result += effectColors["ItemInfoDisplayPositive"] + "REMOVE</color> ";
            }
            result += effectColors["Cold"] + (Mathf.Abs(effect.statusPerSecond) * effect.totalTime * 100f).ToString("F1").Replace(".0", "")
                + " COLD</color> OVER " + effect.totalTime.ToString("F1").Replace(".0", "") + "s\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.Chaos)
        {
            result += effectColors["ItemInfoDisplayPositive"] + "CLEAR ALL STATUS</color>, THEN RANDOMIZE\n" + effectColors["Hunger"] + "HUNGER</color>, "
                + effectColors["Extra Stamina"] + "EXTRA STAMINA</color>, " + effectColors["Injury"] + "INJURY</color>,\n" + effectColors["Poison"] + "POISON</color>, "
                + effectColors["Cold"] + "COLD</color>, " + effectColors["Hot"] + "HEAT</color>, " + effectColors["Drowsy"] + "DROWSY</color>\n";
        }
        else if (affliction.GetAfflictionType() is Peak.Afflictions.Affliction.AfflictionType.Sunscreen)
        {
            Peak.Afflictions.Affliction_Sunscreen effect = (Peak.Afflictions.Affliction_Sunscreen)affliction;
            result += "PREVENT " + effectColors["Heat"] + "HEAT</color> IN MESA'S SUN FOR " + effect.totalTime.ToString("F1").Replace(".0", "") + "s\n";
        }

        return result;
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

        itemInfoDisplayRect.sizeDelta = new Vector2(configSizeDeltaX.Value, 0f); // Y is 0, otherwise moves other item prompts
        itemInfoDisplayTextMesh.font = font;
        itemInfoDisplayTextMesh.fontSize = configFontSize.Value;
        itemInfoDisplayTextMesh.alignment = TextAlignmentOptions.BottomLeft;
        itemInfoDisplayTextMesh.lineSpacing = configLineSpacing.Value;
        itemInfoDisplayTextMesh.text = "";
        itemInfoDisplayTextMesh.outlineWidth = configOutlineWidth.Value;

        LoadLocalizedText();
    }

    private static string GetText(string key, params string[] args)
    {
        return string.Format(LocalizedText.GetText($"Mod_{Name}_{key}".ToUpper()), args);
    }

    private static void LoadLocalizedText()
    {
        var LocalizedTextTable = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,List<string>>>(ItemInfoDisplay.Properties.Resources.Localized_Text);
        if (LocalizedTextTable != null)
        {
            foreach (var item in LocalizedTextTable)
            {
                var values = item.Value;
                string firstValue = values[0];
                values = values.Select(x => string.IsNullOrEmpty(x) ? firstValue : x).ToList();
                LocalizedText.MAIN_TABLE.Add($"Mod_{Name}_{item.Key}".ToUpper(), values);
            }
        }
        else
        {
            Log.LogError($"LoadLocalizedText Fail");
        }
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
        dict.Add("Thorns", "<#768E00>");
        dict.Add("Shield", "<#D48E00>");

        dict.Add("ItemInfoDisplayPositive", "<#DDFFDD>");
        dict.Add("ItemInfoDisplayNegative", "<#FFCCCC>");
    }
}