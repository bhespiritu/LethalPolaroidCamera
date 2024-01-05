using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using LC_API.BundleAPI;
using UnityEngine;
using Unity.Netcode;

namespace BHCamera
{
    public class ScrapLoader
    {
        public static Dictionary<string, Item> loadedItems = new Dictionary<string, Item>();
        public static Dictionary<string, AudioClip> loadedAudio = new Dictionary<string, AudioClip>();

        public static bool loaded = false;

        public void setup()
        {
            CameraPlugin.Log.LogInfo("Adding ScrapLoader Event");
            load();
        }
        
        public void load()
        {
            string cameraBundlePath = Path.Combine(Paths.PluginPath, CameraPlugin.CONFIG["AUTHOR"] + "-PolaroidCamera", CameraPlugin.CONFIG["PLUGIN_ID"] + "/" + CameraPlugin.CONFIG["CAMERA_BUNDLE_PATH"]);
            var bundle = BundleLoader.LoadAssetBundle(cameraBundlePath);
            
            var cameraPrefab = bundle.GetAsset<GameObject>("Assets/Camera/Assets/ViewFinder.prefab");
            var cameraIconTexture = bundle.GetAsset<Texture2D>("Assets/Camera/Assets/camera.png");
            var cameraSprite = Sprite.Create(cameraIconTexture, new Rect(0.0f, 0.0f, cameraIconTexture.width, cameraIconTexture.height), new Vector2(0.5f, 0.5f), 100f);
            CameraPlugin.Log.LogInfo("Loading Camera Item");
            {
                var item = ScriptableObject.CreateInstance<Item>();
                item.name = "PolaroidCamera";
                item.itemName = "Polaroid Camera";
                item.canBeGrabbedBeforeGameStart = true;
                item.isScrap = false;
                item.minValue = 20;
                item.maxValue = 80;
                item.creditsWorth = CameraPlugin.CameraConfig.cameraPrice;
                item.weight = (float)((double)5 / 100.0 + 1.0);
                item.spawnPrefab = cameraPrefab;
                item.twoHanded = false;
                item.twoHandedAnimation = false;
                item.requiresBattery = true;
                item.isConductiveMetal = true;
                item.itemIcon = cameraSprite;
                item.syncGrabFunction = false;
                item.syncUseFunction = false;
                item.syncDiscardFunction = false;
                item.syncInteractLRFunction = false;
                item.verticalOffset = 0.1f;
                item.restingRotation = new Vector3(0, 0, 0);
                item.positionOffset = new Vector3(0,.25f, -0.125f);
                item.rotationOffset = new Vector3(-90, -90, 0);
                item.meshOffset = false;
                item.meshVariants = Array.Empty<Mesh>();
                item.materialVariants = Array.Empty<Material>();
                item.canBeInspected = true;
                item.itemIsTrigger = true;
                item.batteryUsage = 1f / 10;
                CameraItem cameraItem = cameraPrefab.AddComponent<CameraItem>();
                cameraItem.insertedBattery = new Battery(false, 1);
                cameraItem.useCooldown = 1;
                cameraItem.grabbable = true;
                cameraItem.itemProperties = item;
                cameraItem.mainObjectRenderer = cameraPrefab.GetComponent<MeshRenderer>();
                cameraItem.renderDistance = 1000;
                cameraItem.flashSequence = new float[]{0.1f,0.1f, 0.5f} ;
                cameraItem.flashIndicator = cameraItem.transform.Find("PolaroidCameraModel").Find("Lens")
                    .GetComponent<MeshRenderer>();
                // AudioSource audioSource = cameraPrefab.AddComponent<AudioSource>();
                // audioSource.playOnAwake = false;
                // audioSource.spatialBlend = 1f;
                Transform transform = cameraPrefab.transform.Find("ScanNode");
                if ((UnityEngine.Object)transform != (UnityEngine.Object)null)
                {
                    ScanNodeProperties scanNodeProperties = transform.gameObject.AddComponent<ScanNodeProperties>();
                    scanNodeProperties.maxRange = 13;
                    scanNodeProperties.minRange = 1;
                    scanNodeProperties.headerText = item.itemName;
                    scanNodeProperties.subText = "Value: ";
                    scanNodeProperties.nodeType = 2;
                }

                loadedItems["camera"] = item;
            }
            
            var photoPrefab = bundle.GetAsset<GameObject>("Assets/Camera/Assets/Photo.prefab");
            var photoIconTexture = bundle.GetAsset<Texture2D>("Assets/Camera/Assets/photo.png");
            var photoSprite = Sprite.Create(photoIconTexture, new Rect(0.0f, 0.0f, photoIconTexture.width, photoIconTexture.height), new Vector2(0.5f, 0.5f), 100f);
            {
                var item = ScriptableObject.CreateInstance<Item>();
                item.name = "Photo";
                item.itemName = "Photo";
                item.canBeGrabbedBeforeGameStart = true;
                item.isScrap = true;
                item.minValue = 20;
                item.maxValue = 80;
                item.weight = (float)((double)5 / 100.0 + 1.0);
                item.spawnPrefab = photoPrefab;
                item.twoHanded = false;
                item.twoHandedAnimation = false;
                item.requiresBattery = false;
                item.isConductiveMetal = false;
                item.itemIcon = photoSprite;
                item.syncGrabFunction = false;
                item.syncUseFunction = false;
                item.syncDiscardFunction = false;
                item.syncInteractLRFunction = false;
                item.verticalOffset = 0.01f;
                item.restingRotation = new Vector3(90, 0, 0);
                item.positionOffset = new Vector3(0.25f,0,-0.125f);
                item.rotationOffset = new Vector3(-90, -90, 0);
                item.meshOffset = false;
                item.meshVariants = Array.Empty<Mesh>();
                item.materialVariants = Array.Empty<Material>();
                item.canBeInspected = true;
                item.saveItemVariable = true;
                
                
                PhotoItem photoItem = photoPrefab.AddComponent<PhotoItem>();
                photoItem.grabbable = true;
                photoItem.itemProperties = item;
                photoItem.mainObjectRenderer = photoPrefab.GetComponent<MeshRenderer>();
                AudioSource audioSource = photoPrefab.AddComponent<AudioSource>();

                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                Transform transform = photoPrefab.transform.Find("ScanNode");
                if ((UnityEngine.Object)transform != (UnityEngine.Object)null)
                {
                    ScanNodeProperties scanNodeProperties = transform.gameObject.AddComponent<ScanNodeProperties>();
                    scanNodeProperties.maxRange = 13;
                    scanNodeProperties.minRange = 1;
                    scanNodeProperties.headerText = item.itemName;
                    scanNodeProperties.subText = "Value: ";
                    scanNodeProperties.nodeType = 2;
                }

                loadedItems["photo"] = item;
            }
            
            loadedAudio["cameraClick"] =  bundle.GetAsset<AudioClip>("Assets/Camera/Assets/click.mp3");

            loaded = true;
        }
        
        
    }
}