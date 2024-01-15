using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BHCamera
{
    public class CameraItem : GrabbableObject
    {
        public float renderDistance;
        
        private RenderTexture _cameraViewTexture;

        private Camera camera;

        private Light flashBulb;
        
        private static readonly int MainTexture = Shader.PropertyToID("MainTex");
        private static CameraPluginConfig.ImageSettings _imageSettings = CameraPlugin.CameraConfig.imageSettings;

        private float flashTimeout = 1;

        public float[] flashSequence = {0.1f,0.1f,0.5f};

        private bool dropFlash = false;

        public MeshRenderer flashIndicator;
        private PlayerControllerB previousPlayerHeldBy;

        private AudioClip photoClick;
        private AudioSource cameraAudio;

        private void Awake()
        {
            if (IsServer)
            {
                //CameraImageRegistry.GetInstance().LoadImagesFromSave();
            }
            
            var photoResolution = _imageSettings.ImageResolution;
            _cameraViewTexture = new RenderTexture(photoResolution, photoResolution, 24);
            _cameraViewTexture.filterMode = FilterMode.Point;
            camera = GetComponentInChildren<Camera>();
            flashBulb = GetComponentInChildren<Light>();
            flashBulb.enabled = false;

            photoClick = ScrapLoader.loadedAudio["cameraClick"];
            cameraAudio = GetComponent<AudioSource>();
        }

        public override void GrabItem()
        {
            base.GrabItem();
            this.previousPlayerHeldBy = this.playerHeldBy;
        }

        public override void Start()
        {
            base.Start();
            
            
            
            camera.Render();

            if (!_cameraViewTexture.IsCreated())
            {
                _cameraViewTexture.Create();
            }
            
            transform.Find("Screen").GetComponent<MeshRenderer>().material.SetTexture(MainTexture,_cameraViewTexture);
            transform.Find("Screen").GetComponent<MeshRenderer>().material.mainTexture = _cameraViewTexture;
            
            camera.targetTexture = _cameraViewTexture;
            camera.cullingMask = 20649983;
            camera.farClipPlane = this.renderDistance;
            camera.nearClipPlane = 0.55f;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            //this.playerHeldBy.activatingItem = true;
            if (buttonDown && this.insertedBattery.charge > 0)
            {
                dropFlash = true;
                StartCoroutine(flashLight());
                StartCoroutine(doNextFrame(this.TakePicture));
            }
            //this.playerHeldBy.activatingItem = false;
        }

        public override void Update()
        {
            base.Update();
            if (isHeld && !isHeldByEnemy)
            {
                camera.Render();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            //if (_cameraViewTexture.IsCreated()) _cameraViewTexture.Release();
        }

        private void TakePicture()
        {
            var photoResolution = _imageSettings.ImageResolution;
            Texture2D tex = new Texture2D(photoResolution, photoResolution, _imageSettings.ImageFormat, false);
            tex.filterMode = FilterMode.Point;
            
            var tmp = RenderTexture.active;
            RenderTexture.active = _cameraViewTexture;
            tex.ReadPixels(new Rect(0, 0, _cameraViewTexture.width, _cameraViewTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;

            byte[] imageData = tex.GetRawTextureData();
            CameraPlugin.Log.LogInfo("Number of Bytes: " + imageData.Length);
            
            TakePicture_ServerRpc(imageData);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void TakePicture_ServerRpc(byte[] imageData)
        {
            if (!IsHost) return;



            int id = CameraImageRegistry.GetInstance().RegisterImage(imageData);
            CameraImageRegistry.GetInstance().SaveImage(id);
            
            CameraPlugin.Log.LogInfo("Uploaded Image to Server: " + id);
            CameraPlugin.Log.LogInfo("Number of Bytes: " + imageData.Length);
            
            var pos = this.transform.position + Vector3.up * 0.25f;
            
            Transform parent = (!((Object) this.playerHeldBy != (Object) null) || !this.playerHeldBy.isInElevator) && !StartOfRound.Instance.inShipPhase || !((Object) RoundManager.Instance.spawnedScrapContainer != (Object) null) ? StartOfRound.Instance.elevatorTransform : StartOfRound.Instance.elevatorTransform;
            GameObject photo = Instantiate(ScrapLoader.loadedItems["photo"].spawnPrefab, pos, Quaternion.identity, parent);
            PhotoItem photoItem = photo.GetComponent<PhotoItem>();
            photoItem.hasHitGround = false;
            GrabbableObject component = photo.GetComponent<GrabbableObject>();
            component.startFallingPosition = pos;
            //this.StartCoroutine(this.SetObjectToHitGroundSFX(component));
            component.targetFloorPosition = component.GetItemFloorPosition(this.transform.position);
            if ((Object) this.previousPlayerHeldBy != (Object) null && this.previousPlayerHeldBy.isInHangarShipRoom)
                this.previousPlayerHeldBy.SetItemInElevator(true, true, component);
            //photo.GetComponent<NetworkObject>().Spawn();
            component.NetworkObject.Spawn();
            photoItem.photoID.Value = id;
            this.SpawnPhotoClientRpc((NetworkObjectReference) gameObject.GetComponent<NetworkObject>(),pos);
            this.FlashCamera_ClientRpc(Time.time);
        }
        
        [ClientRpc]
        public void SpawnPhotoClientRpc(
            NetworkObjectReference netObjectRef,
            Vector3 startFallingPos)
        {
            if ((Object) this.playerHeldBy != (Object) null)
            {
                this.playerHeldBy.activatingItem = false;
                //this.DestroyObjectInHand(this.playerHeldBy);
            }
            if (this.IsServer)
                return;
            cameraAudio.PlayOneShot(this.photoClick);
            this.StartCoroutine(this.waitForPhotoToSpawnOnClient(netObjectRef, startFallingPos));
        }
        
        private IEnumerator waitForPhotoToSpawnOnClient(
            NetworkObjectReference netObjectRef,
            Vector3 startFallingPos)
        {
            NetworkObject netObject = (NetworkObject) null;
            float startTime = Time.realtimeSinceStartup;
            while ((double) Time.realtimeSinceStartup - (double) startTime < 8.0 && !netObjectRef.TryGet(out netObject))
                yield return (object) new WaitForSeconds(0.03f);
            if ((Object) netObject == (Object) null)
            {
                Debug.Log((object) "No network object found");
            }
            else
            {
                yield return (object) new WaitForEndOfFrame();
                GrabbableObject component = netObject.GetComponent<GrabbableObject>();
                component.startFallingPosition = startFallingPos;
                component.fallTime = 0.0f;
                component.hasHitGround = false;
                component.reachedFloorTarget = false;
                if ((Object) this.previousPlayerHeldBy != (Object) null && this.previousPlayerHeldBy.isInHangarShipRoom)
                    this.previousPlayerHeldBy.SetItemInElevator(true, true, component);
            }
        }

        [ClientRpc]
        private void FlashCamera_ClientRpc(float time)
        {
            if (Time.time < time + flashTimeout && !dropFlash)
            {
                StartCoroutine(flashLight());
            }
        }

        private IEnumerator flashLight()
        {
            dropFlash = false;
            toggleFlash(true);
            foreach (var duration in flashSequence)
            {
                yield return new WaitForSeconds(duration);
                toggleFlash();     
            }
            toggleFlash(false);
        }

        private void toggleFlash(bool? isOn = null)
        {
            var flashStatus = flashBulb.enabled;
            flashStatus = isOn ?? !flashStatus;
            flashBulb.enabled = flashStatus;
            flashIndicator.material.color = (flashStatus) ? Color.yellow : Color.black;
        }

        private IEnumerator doNextFrame(Action action)
        {
            yield return null;
            action();
        }

    }
}
