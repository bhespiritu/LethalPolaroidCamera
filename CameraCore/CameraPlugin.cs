using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BHCamera;
using BHCamera.Patches;
using HarmonyLib;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BHCamera
{
    [BepInPlugin("itbeblockhead.lethalcompany.cameraitem", "Camera Item", "1.0.0")]
    [BepInDependency("LC_API", BepInDependency.DependencyFlags.HardDependency)]
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
        public static bool sessionWaiting=false;
        public static bool alreadypatched=false;
        public static bool isPatching = false;
        public static bool ishost=false;

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
            
            CameraPlugin.Log = this.Logger;
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

            Harmony.PatchAll(typeof(MenuManager_Patch));
            Harmony.PatchAll(typeof (GameNetworkManager_Patch));
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo("Loading Scene " + scene.name );
            if (scene.name == "MainMenu")
            {
                sessionWaiting = true;
                alreadypatched = false;
            }
            if (scene.name == "SampleSceneRelay")
            {
                if(!isPatching && !WaitForSession().GetAwaiter().IsCompleted)
                {
                    WaitForSession().Start();
                }
                GameObject debugObj = GameObject.Find("DebugGUI");
                if (!debugObj)
                {
                    debugObj = Instantiate(new GameObject("DebugGUI"));
                    debugObj.AddComponent<DebugGUI>();
                    DontDestroyOnLoad(debugObj);
                }
                
            }
        }
        
        private async Task WaitForSession()
        {
            isPatching = true;
            try
            {
                while (sessionWaiting)
                    await Task.Delay(1000);

                if (alreadypatched)
                    return;
                Terminal_Patch.MainPatch(GameObject.Find("TerminalScript").GetComponent<Terminal>());
                CameraImageRegistry.GetInstance().LoadImagesFromSave();
                
                alreadypatched = true;
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
            finally
            {
                isPatching = false;
            }
        }
    }
}

public class CameraPluginConfig
{
    private ConfigEntry<string> _configImageFormat;
    private ConfigEntry<int> _configPhotoResolution;

    private ConfigEntry<string> _configRateLimit;

    public readonly ImageSettings ServerImageSettings = new ImageSettings();
    public readonly ImageSettings ClientImageSettings = new ImageSettings();
    

    public float RateLimit
    {
        get
        {
            String rawValue = NormalizeConfigValue(_configRateLimit.Value);
            String rawNumVal = Regex.Replace(rawValue, "[^0-9]*", "");
            String suffix = Regex.Replace(rawValue, "[0-9]*", "").ToLower();

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
        _configImageFormat = instance.Config.Bind<string>("Image Settings", "ImageFormat", "RGB24", "Use Unity's built-in TextureFormat for potential values. This will have heavy effects on the performance");
        _configPhotoResolution = instance.Config.Bind<int>("Image Settings", "ImageResolution", 64, "The resolution of the images taken by a camera. Changing this will break all existing images");

        _configRateLimit = instance.Config.Bind<string>("Network Settings", "RateLimit", "", "WIP. Unused. use b (bytes), kb (kilobytes), or mb (megabytes). Leaving it blank will leave the rate limit unbounded");
        
        
        {
            if (!Enum.TryParse<TextureFormat>(NormalizeConfigValue(_configImageFormat.Value), out var format))
            {
                CameraPlugin.Log.LogError("Invalid Setting for Image Format. Defaulting to R8.");
                format = TextureFormat.R8;
            }

            ServerImageSettings.ImageFormat = format;
        }
        ServerImageSettings.ImageResolution = _configPhotoResolution.Value;
    }

    private static string NormalizeConfigValue(string value)
    {
        return value.Trim();
    }

    public class ImageSettings
    {
        public TextureFormat ImageFormat;
        public int ImageResolution;
    }
}
