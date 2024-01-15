using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BHCamera;
using BHCamera.Patches;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace BHCamera
{
    [BepInPlugin("itbeblockhead.lethalcompany.cameraitem", "Camera Item", "1.0.0")]
    [BepInDependency("LC_API")]
    [BepInDependency("evaisa.lethallib")]
    public class CameraPlugin : BaseUnityPlugin
    {
        private static readonly Harmony Harmony = new Harmony(nameof (CameraPlugin));
        
        public static readonly Dictionary<string, string> CONFIG = new()
            {
                {"PLUGIN_ID", "Camera-Mod"},
                {"AUTHOR", "BlockHead"},
                {"CAMERA_BUNDLE_PATH","bundles/camera.assetbundle"}
            }
            ;
        
        public static ScrapLoader ScrapLoader;

        public static ManualLogSource Log;

        public static CameraPluginConfig CameraConfig = new CameraPluginConfig();
        
        private void Awake()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            
            Log = Logger;
            CameraConfig.SetUp(this);
            
            ScrapLoader = new ScrapLoader();
            ScrapLoader.setup();

            var itemAsset = ScrapLoader.loadedItems["camera"];
            
            if(itemAsset.spawnPrefab.GetComponent<NetworkTransform>() == null)
            {
                var networkTransform = itemAsset.spawnPrefab.AddComponent<NetworkTransform>();
                networkTransform.SlerpPosition = false;
                networkTransform.Interpolate = false;
                networkTransform.SyncPositionX = false;
                networkTransform.SyncPositionY = false;
                networkTransform.SyncPositionZ = false;
                networkTransform.SyncScaleX = false;
                networkTransform.SyncScaleY = false;
                networkTransform.SyncScaleZ = false;
                networkTransform.UseHalfFloatPrecision = true;
            }
            
            itemAsset = ScrapLoader.loadedItems["photo"];
            
            if(itemAsset.spawnPrefab.GetComponent<NetworkTransform>() == null)
            {
                var networkTransform = itemAsset.spawnPrefab.AddComponent<NetworkTransform>();
                networkTransform.SlerpPosition = false;
                networkTransform.Interpolate = false;
                networkTransform.SyncPositionX = false;
                networkTransform.SyncPositionY = false;
                networkTransform.SyncPositionZ = false;
                networkTransform.SyncScaleX = false;
                networkTransform.SyncScaleY = false;
                networkTransform.SyncScaleZ = false;
                networkTransform.UseHalfFloatPrecision = true;
            }

            Items.RegisterShopItem(ScrapLoader.loadedItems["camera"], CameraConfig.cameraPrice);
            Items.RegisterItem(ScrapLoader.loadedItems["photo"]);
                
            Harmony.PatchAll(typeof (GameNetworkManager_Patch));
            Harmony.PatchAll(typeof (CameraPluginConfig));
        }
    }
}

public class CameraPluginConfig : SyncedInstance<CameraPluginConfig>
{
    private static ConfigEntry<string> _configImageFormat;
    private static ConfigEntry<int> _configPhotoResolution;

    private static ConfigEntry<string> _configRateLimit;
    
    private static ConfigEntry<int> _configCameraPrice;

    public ImageSettings imageSettings;

    public int cameraPrice => 50;


    private string rateLimit;

    public CameraPluginConfig()
    {
        InitInstance(this);
    }

    public float RateLimit
    {
        get
        {
            String rawValue = NormalizeConfigValue(rateLimit);
            String rawNumVal = Regex.Replace(rawValue, "[0-9]*", "");
            String suffix = Regex.Replace(rawValue, "[^0-9]*", "").ToLower();

            int numVal = int.Parse(rawNumVal);
            
            int multiplier = 1;
            switch (suffix)
            {
                case "mb":
                    multiplier = 1_000_000;
                    break;
                case "kb":
                    multiplier = 1_000;
                    break;
                case "b":
                    break;
                default:
                    CameraPlugin.Log.LogError("Invalid Suffix for Rate Limit Given: " + suffix + " Defaulting to b");
                    break;
            }

            return numVal * multiplier;
        }
    }

    public void SetUp(CameraPlugin instance)
    {
        _configImageFormat = instance.Config.Bind("Image Settings", "ImageFormat", "RGB24", "Use Unity's built-in TextureFormat for potential values. This will have heavy effects on the performance");
        _configPhotoResolution = instance.Config.Bind("Image Settings", "ImageResolution", 64, "The resolution of the images taken by a camera. Changing this will break all existing images");

        _configRateLimit = instance.Config.Bind("Network Settings", "RateLimit", "", "WIP. Unused. use b (bytes), kb (kilobytes), or mb (megabytes). Leaving it blank will leave the rate limit unbounded");

        _configCameraPrice = instance.Config.Bind("General Settings", "CameraPrice", 50,
            "The price that shows up on the store");
        
        {
            if (!Enum.TryParse<TextureFormat>(NormalizeConfigValue(_configImageFormat.Value), out var format))
            {
                CameraPlugin.Log.LogError("Invalid Setting for Image Format. Defaulting to R8.");
                format = TextureFormat.R8;
            }

            //imageSettings.ImageFormat = format;
        }
        //imageSettings.ImageResolution = _configPhotoResolution.Value;

        //cameraPrice = _configCameraPrice.Value;

        rateLimit = _configRateLimit.Value;
    }

    private static string NormalizeConfigValue(string value)
    {
        return value.Trim();
    }
    
    public static void RequestSync() {
        if (!IsClient) return;
    
        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessageManager.SendNamedMessage("PolaroidCamera_OnRequestConfigSync", 0uL, stream);
    }
    
    public static void OnRequestSync(ulong clientId, FastBufferReader _) {
        if (!IsHost) return;
    
        CameraPlugin.Log.LogInfo($"Config sync request received from client: {clientId}");
    
        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;
    
        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);
    
        try {
            stream.WriteValueSafe(in value);
            stream.WriteBytesSafe(array);
    
            MessageManager.SendNamedMessage("PolaroidCamera_OnReceiveConfigSync", clientId, stream);
        } catch(Exception e) {
            CameraPlugin.Log.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
        }
    }
    
    public static void OnReceiveSync(ulong _, FastBufferReader reader) {
        if (!reader.TryBeginRead(IntSize)) {
            CameraPlugin.Log.LogError("Config sync error: Could not begin reading buffer.");
            return;
        }
    
        reader.ReadValueSafe(out int val);
        if (!reader.TryBeginRead(val)) {
            CameraPlugin.Log.LogError("Config sync error: Host could not sync.");
            return;
        }
    
        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);
    
        SyncInstance(data);
    
        CameraPlugin.Log.LogInfo("Successfully synced config with host.");
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void InitializeLocalPlayer() {
        if (IsHost) {
            MessageManager.RegisterNamedMessageHandler("PolaroidCamera_OnRequestConfigSync", OnRequestSync);
            Synced = true;
    
            return;
        }
    
        Synced = false;
        MessageManager.RegisterNamedMessageHandler("PolaroidCamera_OnReceiveConfigSync", OnReceiveSync);
        RequestSync();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
    public static void PlayerLeave() {
        RevertSync();
        CameraImageRegistry.GetInstance().Reset();
    }

    public struct ImageSettings
    {
        public TextureFormat ImageFormat => TextureFormat.RGB24;
        public int ImageResolution => 256;
    }
}
