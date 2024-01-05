using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace BHCamera
{
    public class PhotoItem : GrabbableObject
    {
        public NetworkVariable<int> photoID = new NetworkVariable<int>();

        private bool _photoDeveloped = false;

        private bool _developingPhoto = false;

        private float _developed = 0;
        
        public float developmentSpeed = 1f/5;
        
        private readonly Dictionary<ulong, ClientRpcParams> _idCache = new();
        private Material _filmMaterial;
        private static readonly int DevelopProgress = Shader.PropertyToID("_developProgress");

        private static CameraPluginConfig.ImageSettings _imageSettings = CameraPlugin.CameraConfig.ServerImageSettings;
        
        public override void Start()
        {
            base.Start();
            developmentSpeed = 1f / 5;
            if (IsServer)
            {
                _imageSettings = CameraPlugin.CameraConfig.ServerImageSettings;
            }
            else
            {
                
            }
            
            _filmMaterial = transform.Find("Film").GetComponent<MeshRenderer>().material;
        }

        // public override void OnNetworkSpawn()
        // {
        //     base.OnNetworkSpawn();
        // }

        public override void GrabItem()
        {
            base.GrabItem();
            //ChangeOwnershipOfProp(NetworkManager.Singleton.LocalClientId);
            if (!_photoDeveloped && !_developingPhoto)
            {
                if (photoID.Value >= 0)
                {
                    if (CameraImageRegistry.GetInstance().has(photoID.Value))
                    {
                        DevelopPhoto();
                    }
                    else
                    {
                        _developingPhoto = true;
                        RequestImage_ServerRpc(NetworkManager.Singleton.LocalClientId, photoID.Value);
                    }
                }
                else
                {
                    CameraPlugin.Log.LogError("Item has Invalid photoID: " + photoID.Value);
                }
            }
        }

        private void DevelopPhoto()
        {
            byte[] imageData = CameraImageRegistry.GetInstance()[photoID.Value];

            var photoResolution = _imageSettings.ImageResolution;
            Texture2D tex = new Texture2D(photoResolution, photoResolution, _imageSettings.ImageFormat, false);
            tex.filterMode = FilterMode.Point;
            tex.LoadRawTextureData(imageData);
            tex.Apply();
            
            _filmMaterial.mainTexture = tex;
            _photoDeveloped = true;
        }

        public override void Update()
        {
            base.Update();

            if (_photoDeveloped)
            {
                _developed += Time.deltaTime*developmentSpeed;
                _developed = Mathf.Min(_developed, 1);
                _filmMaterial.SetFloat(DevelopProgress, _developed);
            }
            
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void RequestImage_ServerRpc(ulong returnId, int id)
        {
            if (IsServer)
            {
                _developingPhoto = false;
                _photoDeveloped = true;
            }
            
            if (!CameraImageRegistry.GetInstance().has(id))
            {
                CameraImageRegistry.GetInstance().LoadImageFromSave(id);
                
                if (!CameraImageRegistry.GetInstance().has(id))
                {
                    CameraPlugin.Log.LogError("Requested Image ID " + id + " Does not have a registered image");
                }
            }
            
            if (!_idCache.ContainsKey(returnId))
            {
                _idCache[returnId] =  new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new []{returnId}
                    }
                };
            }
            
            ClientRpcParams clientRpcParams = _idCache[returnId];
            
            RegisterImage_ClientRpc(id, CameraImageRegistry.GetInstance()[id], clientRpcParams);
        }
        
        [ClientRpc]
        public void RegisterImage_ClientRpc(int id, byte[] imageData, ClientRpcParams clientRpcParams = default)
        {
            CameraImageRegistry.GetInstance()[id] = imageData;
            _developingPhoto = false;
            
            DevelopPhoto();
        }

        public override int GetItemDataToSave()
        {
            return photoID.Value;
        }
        
        public override void LoadItemSaveData(int saveData)
        {
            this.photoID.Value = saveData;
        }
    }
}