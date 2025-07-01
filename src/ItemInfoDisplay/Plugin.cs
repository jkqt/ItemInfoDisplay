using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
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
    private static float lastKnownSinceItemAttach;
    private static bool hasChanged;
    private static float forceUpdateTime;

    private void Awake()
    {
        Log = Logger;
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
                        //Log.LogInfo("sinceItemAttach: " + Character.observedCharacter.data.sinceItemAttach.ToString());
                        if (hasChanged)
                        {
                            hasChanged = false;
                            ProcessItemGameObject();
                        }
                        else if (Mathf.Abs(Character.observedCharacter.data.sinceItemAttach - lastKnownSinceItemAttach) >= forceUpdateTime)
                        {
                            hasChanged = true;
                            lastKnownSinceItemAttach = Character.observedCharacter.data.sinceItemAttach;
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

    private static class ItemInfoDisplayCharacterItemsUnequipPatch
    {
        [HarmonyPatch(typeof(CharacterItems), "UnAttatchEquipedItem")]
        [HarmonyPrefix]
        private static void ItemInfoDisplayCharacterItemsUnequip()
        {
            itemInfoDisplay.SetActive(false);
            hasChanged = false;
        }
    }

    private static class ItemInfoDisplayCharacterItemsEquipPatch
    {
        [HarmonyPatch(typeof(CharacterItems), "Equip")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayCharacterItemsEquip()
        {
            itemInfoDisplay.SetActive(true);
            hasChanged = true;
        }
    }

    private static class ItemInfoDisplayItemCookingFinishCookingPatch
    {
        [HarmonyPatch(typeof(ItemCooking), "FinishCooking")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayItemCookingFinishCooking()
        {
            hasChanged = true;
        }
    }

    private static class ItemInfoDisplayActionReduceUsesReduceUsesRPCPatch
    {
        [HarmonyPatch(typeof(Action_ReduceUses), "ReduceUsesRPC")]
        [HarmonyPostfix]
        private static void ItemInfoDisplayActionReduceUsesReduceUsesRPC()
        {
            hasChanged = true;
        }
    }
    

    private static void ProcessItemGameObject()
    {
        Log.LogInfo(Character.observedCharacter.data.sinceItemAttach.ToString());
        Item item = Character.observedCharacter.data.currentItem;
        GameObject itemGameObj = item.gameObject;
        Component[] itemComponents = itemGameObj.GetComponents(typeof(Component));
        for (int i = 0; i < itemComponents.Length; i++)
        {
            Log.LogInfo(itemComponents[i].ToString());
        }
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
        TextMeshProUGUI itemInfoDisplayText = itemInfoDisplayTextGameObj.AddComponent<TextMeshProUGUI>();
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
        itemInfoDisplayText.fontSize = 20f;
        itemInfoDisplayText.alignment = TextAlignmentOptions.BottomLeft;
        itemInfoDisplayText.lineSpacing = -50f;
        itemInfoDisplayText.text = "1234567890123456789012345\n1234567890123456789012345\n1234567890123456789012345\n1234567890123456789012345\n1234567890123456789012345\n1234567890123456789012345\n1234567890123456789012345";
        itemInfoDisplayText.outlineWidth = 0.08f;
    }
}