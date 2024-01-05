using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.Assertions.Must;

namespace BHCamera.Patches
{
    [HarmonyPatch(typeof (GameNetworkManager))]
    internal class GameNetworkManager_Patch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        private static void Start_Prefix(GameNetworkManager __instance)
        {
            CameraPlugin.Log.LogInfo("Registering Camera Prefab");
            
            var cameraScrap = ScrapLoader.loadedItems["camera"];
            //cameraScrap.spawnPrefab.AddComponent<NetworkObject>();
            __instance.GetComponent<NetworkManager>().PrefabHandler.AddNetworkPrefab(cameraScrap.spawnPrefab);
            
            CameraPlugin.Log.LogInfo("Registering Photo Prefab");
            var photoScrap = ScrapLoader.loadedItems["photo"];
            //photoScrap.spawnPrefab.AddComponent<NetworkObject>();
            __instance.GetComponent<NetworkManager>().PrefabHandler.AddNetworkPrefab(photoScrap.spawnPrefab);
            
        }
    }
}
