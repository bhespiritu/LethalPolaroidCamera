using HarmonyLib;

namespace BHCamera.Patches
{
    
    [HarmonyPatch(typeof (MenuManager))]
    internal class MenuManager_Patch
    {
        [HarmonyPatch("StartHosting")]
        [HarmonyPostfix]
        public static void StartHosting_Postfix(MenuManager __instance)
        {
            CameraPlugin.ishost = true;
            CameraPlugin.sessionWaiting = false;
            //CameraPlugin.Log.LogInfo((object) "LethalExpansion Host Started.");
        }

        [HarmonyPatch("StartAClient")]
        [HarmonyPostfix]
        public static void StartAClient_Postfix(MenuManager __instance)
        {
            CameraPlugin.ishost = false;
            CameraPlugin.sessionWaiting = false;
            //LethalExpansion.LethalExpansion.Log.LogInfo((object) "LethalExpansion LAN Client Started.");
        }
        
    }
}
