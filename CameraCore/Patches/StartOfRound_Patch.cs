using HarmonyLib;
using UnityEngine.SceneManagement;

namespace BHCamera.Patches
{
  [HarmonyPatch(typeof(StartOfRound))]
  internal class StartOfRound_Patch
  {

    // [HarmonyPatch("StartGame")]
    // [HarmonyPostfix]
    // public static void StartGame_Postfix(StartOfRound __instance)
    // {
    //   // if (__instance.currentLevel.name.StartsWith("Assets/Mods/"))
    //   //   SceneManager.LoadScene(__instance.currentLevel.name, LoadSceneMode.Additive);
    //   // LethalExpansion.LethalExpansion.Log.LogInfo((object)"Game started.");
    // }

    [HarmonyPatch("OnPlayerConnectedClientRpc")]
    [HarmonyPostfix]
    private static void OnPlayerConnectedClientRpc_Postfix(
      StartOfRound __instance,
      ulong clientId,
      int connectedPlayers,
      ulong[] connectedPlayerIdsOrdered,
      int assignedPlayerObjectId,
      int serverMoneyAmount,
      int levelID,
      int profitQuota,
      int timeUntilDeadline,
      int quotaFulfilled,
      int randomSeed)
    {
      if (!CameraPlugin.ishost)
      {
        CameraPlugin.ishost = false;
        CameraPlugin.sessionWaiting = false;
        //CameraPlugin.Log.LogInfo((object)("LethalExpansion Client Started." +
        //                                                    __instance.NetworkManager.LocalClientId.ToString()));
        
        
      }

      if (__instance.IsServer)
      {
        CameraImageRegistry.GetInstance().LoadImagesFromSave();
      }
      // else
      //   NetworkPacketManager.Instance.sendPacket(NetworkPacketManager.packetType.request, "clientinfo", string.Empty,
      //     (long)clientId);
    }

  }
}