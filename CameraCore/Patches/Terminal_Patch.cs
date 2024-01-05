using System;
using System.Collections;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BHCamera.Patches
{
  [HarmonyPatch(typeof(Terminal))]
  internal class Terminal_Patch
  {
    

    public static void MainPatch(Terminal __instance)
    {
      CameraPlugin.Log.LogInfo("Adding Camera Item To Terminal");
      RegisterCameraItem(__instance);
      CameraPlugin.Log.LogInfo("Adding Photo Item To Terminal");
      RegisterPhotoItem(__instance);
    }

    private static void RegisterCameraItem(Terminal __instance)
    {
      Item scrap = ScrapLoader.loadedItems["camera"];
      Item itemProperties = scrap.spawnPrefab.GetComponent<CameraItem>().itemProperties;
      ((List<Item>)StartOfRound.Instance.allItemsList.itemsList).Add(itemProperties);

      TerminalKeyword terminalKeyword1 = ((IEnumerable<TerminalKeyword>) __instance.terminalNodes.allKeywords).First<TerminalKeyword>((Func<TerminalKeyword, bool>) (keyword => keyword.word == "info"));
      TerminalKeyword defaultVerb = ((IEnumerable<TerminalKeyword>) __instance.terminalNodes.allKeywords).First<TerminalKeyword>((Func<TerminalKeyword, bool>) (keyword => keyword.word == "buy"));
      TerminalNode result = defaultVerb.compatibleNouns[0].result.terminalOptions[1].result;
      
      __instance.buyableItemsList = __instance.buyableItemsList.AddToArray(itemProperties);
      
        string itemName = itemProperties.itemName;
        string str = itemName;
        
        TerminalNode terminalNode1 = ScriptableObject.CreateInstance<TerminalNode>();
        terminalNode1.name = itemName.Replace(" ", "-") + "BuyNode2";
        terminalNode1.displayText = "Ordered [variableAmount] " + str + ". Your new balance is [playerCredits].\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.\r\n\r\n";
        terminalNode1.clearPreviousText = true;
        terminalNode1.maxCharactersToType = 15;

        terminalNode1.buyItemIndex = __instance.buyableItemsList.Length - 1;
        terminalNode1.isConfirmationNode = false;
        terminalNode1.itemCost = itemProperties.creditsWorth;
        terminalNode1.playSyncedClip = 0;
        
        TerminalNode terminalNode2 = ScriptableObject.CreateInstance<TerminalNode>();
        terminalNode2.name = itemName.Replace(" ", "-") + "BuyNode1";
        terminalNode2.displayText = "You have requested to order " + str + ". Amount: [variableAmount].\nTotal cost of items: [totalCost].\n\nPlease CONFIRM or DENY.\r\n\r\n";
        terminalNode2.clearPreviousText = true;
        terminalNode2.maxCharactersToType = 35;
        
        terminalNode2.buyItemIndex = __instance.buyableItemsList.Length - 1;
        terminalNode2.isConfirmationNode = true;
        terminalNode2.overrideOptions = true;
        terminalNode2.itemCost = itemProperties.creditsWorth;
        terminalNode2.terminalOptions = new CompatibleNoun[2]
        {
          new CompatibleNoun()
          {
            noun = ((IEnumerable<TerminalKeyword>) __instance.terminalNodes.allKeywords).First<TerminalKeyword>((Func<TerminalKeyword, bool>) (keyword2 => keyword2.word == "confirm")),
            result = terminalNode1
          },
          new CompatibleNoun()
          {
            noun = ((IEnumerable<TerminalKeyword>) __instance.terminalNodes.allKeywords).First<TerminalKeyword>((Func<TerminalKeyword, bool>) (keyword2 => keyword2.word == "deny")),
            result = result
          }
        };
        TerminalKeyword terminalKeyword2 = CreateTerminalKeyword(itemName.ToLowerInvariant().Replace(" ", "-"), defaultVerb: defaultVerb);
        List<TerminalKeyword> list2 = ((IEnumerable<TerminalKeyword>) __instance.terminalNodes.allKeywords).ToList<TerminalKeyword>();
        list2.Add(terminalKeyword2);
        __instance.terminalNodes.allKeywords = list2.ToArray();
        List<CompatibleNoun> list3 = ((IEnumerable<CompatibleNoun>) defaultVerb.compatibleNouns).ToList<CompatibleNoun>();
        list3.Add(new CompatibleNoun()
        {
          noun = terminalKeyword2,
          result = terminalNode2
        });
        defaultVerb.compatibleNouns = list3.ToArray();
        var terminalNode3 = ScriptableObject.CreateInstance<TerminalNode>();
        terminalNode3.name = itemName.Replace(" ", "-") + "InfoNode";
        terminalNode3.displayText = "[No information about this object was found.]\n\n";
        terminalNode3.clearPreviousText = true;
        terminalNode3.maxCharactersToType = 25;
        
        __instance.terminalNodes.allKeywords = list2.ToArray();
        List<CompatibleNoun> list4 = ((IEnumerable<CompatibleNoun>) terminalKeyword1.compatibleNouns).ToList<CompatibleNoun>();
        list4.Add(new CompatibleNoun()
        {
          noun = terminalKeyword2,
          result = terminalNode3
        });
        terminalKeyword1.compatibleNouns = list4.ToArray();
        CameraPlugin.Log.LogInfo("Registered Camera to Shop:");
        
        __instance.itemSalesPercentages = new int[__instance.buyableItemsList.Length];
        for (int index = 0; index < __instance.itemSalesPercentages.Length; ++index)
        {
          //Debug.Log((object) string.Format("Item sales percentages #{0}: {1}", (object) index, (object) __instance.itemSalesPercentages[index]));
          __instance.itemSalesPercentages[index] = 100;
        }
    }
    
    private static void RegisterPhotoItem(Terminal __instance)
    {
      Item scrap = ScrapLoader.loadedItems["photo"];
      Item itemProperties = scrap.spawnPrefab.GetComponent<PhotoItem>().itemProperties;
      ((List<Item>)StartOfRound.Instance.allItemsList.itemsList).Add(itemProperties);
    }

    public static TerminalKeyword CreateTerminalKeyword(
      string word,
      bool isVerb = false,
      CompatibleNoun[] compatibleNouns = null,
      TerminalNode specialKeywordResult = null,
      TerminalKeyword defaultVerb = null,
      bool accessTerminalObjects = false)
    {
      TerminalKeyword instance = ScriptableObject.CreateInstance<TerminalKeyword>();
      instance.name = word;
      instance.word = word;
      instance.isVerb = isVerb;
      instance.compatibleNouns = compatibleNouns;
      instance.specialKeywordResult = specialKeywordResult;
      instance.defaultVerb = defaultVerb;
      instance.accessTerminalObjects = accessTerminalObjects;
      return instance;
    }
    
  }
}

