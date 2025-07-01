using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ItemInfoDisplay;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    private static GUIManager guiManager;
    private static GameObject itemInfoDisplay;
    private static TextMeshProUGUI itemInfoDisplayText;
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
        Harmony.CreateAndPatchAll(typeof(ItemInfoDisplayCharacterItemsUpdatePatch));
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static class ItemInfoDisplayCharacterItemsUpdatePatch 
    {
        [HarmonyPatch(typeof(CharacterItems), "Update")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayCharacterItemsUpdate(CharacterItems __instance)
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
                        else if ((Character.observedCharacter.data.sinceItemAttach == 0) || (Mathf.Abs(Character.observedCharacter.data.sinceItemAttach - lastKnownSinceItemAttach) >= forceUpdateTime))
                        {
                            hasChanged = true;
                            lastKnownSinceItemAttach = Character.observedCharacter.data.sinceItemAttach;
                        }

                        if (!itemInfoDisplay.activeSelf)
                        {
                            itemInfoDisplay.SetActive(true);
                        }
                    }
                    else
                    {
                        if (itemInfoDisplay.activeSelf) 
                        {
                            itemInfoDisplay.SetActive(false);
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

    private static void ProcessItemGameObject()
    {
        Log.LogInfo(Character.observedCharacter.data.sinceItemAttach.ToString());
        Item item = Character.observedCharacter.data.currentItem;
        GameObject itemGameObj = item.gameObject;
        Component[] itemComponents = itemGameObj.GetComponents(typeof(Component));
        itemInfoDisplayText.text = "";
        for (int i = 0; i < itemComponents.Length; i++)
        {
            Log.LogInfo(itemComponents[i]);
            if (itemComponents[i].ToString().Contains("Action_ApplyAffliction"))
            {
                itemInfoDisplayText.text += "TODO: Action_ApplyAffliction\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ApplyAntigrav"))
            {
                itemInfoDisplayText.text += "TODO: Action_ApplyAntigrav\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ApplyInfiniteStamina"))
            {
                itemInfoDisplayText.text += "TODO: Action_ApplyInfiniteStamina\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ApplyMassAffliction"))
            {
                itemInfoDisplayText.text += "TODO: Action_ApplyMassAffliction\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ApplySuperJump"))
            {
                itemInfoDisplayText.text += "TODO: Action_ApplySuperJump\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_CallScoutmaster"))
            {
                itemInfoDisplayText.text += "TODO: Action_CallScoutmaster\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ClearAllStatus"))
            {
                itemInfoDisplayText.text += "TODO: Action_ClearAllStatus\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_Consume"))
            {
                // ignore
            }
            else if (itemComponents[i].ToString().Contains("Action_ConsumeAndSpawn"))
            {
                itemInfoDisplayText.text += "TODO: Action_ConsumeAndSpawn\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_Die"))
            {
                itemInfoDisplayText.text += "TODO: Action_Die\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_Flare"))
            {
                itemInfoDisplayText.text += "TODO: Action_Flare\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_GiveExtraStamina"))
            {
                Action_GiveExtraStamina effect = (Action_GiveExtraStamina)itemComponents[i];
                itemInfoDisplayText.text += ProcessEffect(effect.amount, "Extra Stamina") + "\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_Guidebook"))
            {
                itemInfoDisplayText.text += "TODO: Action_Guidebook\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_GuidebookScroll"))
            {
                itemInfoDisplayText.text += "TODO: Action_GuidebookScroll\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_InflictPoison"))
            {
                itemInfoDisplayText.text += "TODO: Action_InflictPoison\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_LaunchPlayer"))
            {
                itemInfoDisplayText.text += "TODO: Action_LaunchPlayer\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_LightLantern"))
            {
                itemInfoDisplayText.text += "TODO: Action_LightLantern\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ModifyStatus"))
            {
                Action_ModifyStatus effect = (Action_ModifyStatus)itemComponents[i];
                itemInfoDisplayText.text += ProcessEffect(effect.changeAmount, effect.statusType.ToString()) + "\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_MoraleBoost"))
            {
                itemInfoDisplayText.text += "TODO: Action_MoraleBoost\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_OverrideCamera"))
            {
                itemInfoDisplayText.text += "TODO: Action_OverrideCamera\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_Passport"))
            {
                itemInfoDisplayText.text += "TODO: Action_Passport\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_PlayAnimation"))
            {
                // ignore
            }
            else if (itemComponents[i].ToString().Contains("Action_PlayItemAnimation"))
            {
                itemInfoDisplayText.text += "TODO: Action_PlayItemAnimation\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_RaycastDart"))
            {
                itemInfoDisplayText.text += "TODO: Action_RaycastDart\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ReduceUses"))
            {
                itemInfoDisplayText.text += "TODO: Action_ReduceUses\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_RestoreHunger"))
            {
                itemInfoDisplayText.text += "TODO: Action_RestoreHunger\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_ShowBinocularOverlay"))
            {
                itemInfoDisplayText.text += "TODO: Action_ShowBinocularOverlay\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_SpawnGuidebookPage"))
            {
                itemInfoDisplayText.text += "TODO: Action_SpawnGuidebookPage\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_TootMagicBugle"))
            {
                itemInfoDisplayText.text += "TODO: Action_TootMagicBugle\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_Torch"))
            {
                itemInfoDisplayText.text += "TODO: Action_Torch\n";
            }
            else if (itemComponents[i].ToString().Contains("Action_WarpToRandomPlayer"))
            {
                itemInfoDisplayText.text += "TODO: Action_WarpToRandomPlayer\n";
            }
            else if (itemComponents[i].ToString().Contains("Actions_Binoculars"))
            {
                itemInfoDisplayText.text += "TODO: Actions_Binoculars\n";
            }
            else if (itemComponents[i].ToString().Contains("Actions_DefaultConstructActions"))
            {
                itemInfoDisplayText.text += "TODO: Actions_DefaultConstructActions\n";
            }
        }
    }

    private static string ProcessEffect(float amount, string effect)
    {
        string result = "";

        if (amount > 0)
        {
            result += "Gain ";
        }
        else if (amount < 0)
        {
            result += "Remove ";
        }
        result += Mathf.Round(Mathf.Abs(amount) * 100f).ToString() + " " + effectColors[effect] + effect + "</color>";

        return result;
    }

    private static void AddDisplayObject()
    {
        GameObject guiManagerGameObj = GameObject.Find("GAME/GUIManager");
        guiManager = guiManagerGameObj.GetComponent<GUIManager>();
        TMPro.TMP_FontAsset font = guiManager.heroDayText.font;
        GameObject invGameObj = guiManagerGameObj.transform.Find("Canvas_HUD/Inventory").gameObject;

        itemInfoDisplay = new GameObject("ItemInfoDisplay");
        itemInfoDisplay.transform.SetParent(invGameObj.transform);
        RectTransform itemInfoDisplayRect = itemInfoDisplay.AddComponent<RectTransform>();

        /*GameObject itemInfoDisplayBackgroundGameObj = new GameObject("Background");
        itemInfoDisplayBackgroundGameObj.transform.SetParent(itemInfoDisplay.transform);
        UnityEngine.UI.Image itemInfoDisplayBackgroundImage = itemInfoDisplayBackgroundGameObj.AddComponent<UnityEngine.UI.Image>();
        RectTransform itemInfoDisplayBackgroundRect = itemInfoDisplayBackgroundGameObj.GetComponent<RectTransform>();*/

        GameObject itemInfoDisplayTextGameObj = new GameObject("Text");
        itemInfoDisplayTextGameObj.transform.SetParent(itemInfoDisplay.transform);
        itemInfoDisplayText = itemInfoDisplayTextGameObj.AddComponent<TextMeshProUGUI>();
        RectTransform itemInfoDisplayTextRect = itemInfoDisplayTextGameObj.GetComponent<RectTransform>();

        invGameObj.SetActive(true);
        itemInfoDisplay.SetActive(true);
        //itemInfoDisplayBackgroundGameObj.SetActive(true);
        itemInfoDisplayTextGameObj.SetActive(true);

        itemInfoDisplayRect.sizeDelta = new Vector2(280f, 160f);
        //itemInfoDisplayBackgroundRect.sizeDelta = new Vector2(280f, 160f);
        itemInfoDisplayTextRect.sizeDelta = new Vector2(276f, 156f);
        itemInfoDisplayRect.anchorMin = new Vector2(1f, 0f);
        itemInfoDisplayRect.anchorMax = new Vector2(1f, 0f);
        itemInfoDisplayRect.pivot = new Vector2(1f, 0.5f);
        itemInfoDisplayRect.offsetMin = new Vector2(-750f, 250f);
        itemInfoDisplayRect.offsetMax = new Vector2(-75f, 320f);
        //itemInfoDisplayBackgroundImage.color = new Color(0f, 0f, 0f, 0.69f);
        itemInfoDisplayText.font = font;
        itemInfoDisplayText.fontStyle = FontStyles.UpperCase;
        itemInfoDisplayText.fontSize = 20f;
        itemInfoDisplayText.alignment = TextAlignmentOptions.BottomLeft;
        itemInfoDisplayText.lineSpacing = -50f;
        itemInfoDisplayText.text = "";
        itemInfoDisplayText.outlineWidth = 0.08f;
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