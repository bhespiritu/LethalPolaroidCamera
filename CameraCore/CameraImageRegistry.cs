using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BHCamera
{
    public class CameraImageRegistry
    {
        private static CameraImageRegistry instance;

        public static CameraImageRegistry GetInstance()
        {
            return instance ??= new CameraImageRegistry();
        }

        private int _nextId = 1;

        private readonly Dictionary<int, byte[]> _imageRegistry;

        private CameraImageRegistry()
        {
            _imageRegistry = new Dictionary<int, byte[]>();
            Reset();
        }
        
        public void Reset()
        {
            _imageRegistry.Clear();
            _nextId = 1;
        }

        public void LoadImagesFromSave()
        {
            string saveFileName = GameNetworkManager.Instance.currentSaveFileName;

            string[] photos = ES3.GetFiles("photos/" + saveFileName);

            int maxId = 0;
            
            foreach (var filename in photos)
            {
                //TODO not very robust
                int key = int.Parse(Regex.Replace(filename, "[^0-9]", ""));
                maxId = Math.Max(maxId, key);

                byte[] value = ES3.LoadImage("photos/" + filename).GetRawTextureData();

                _imageRegistry[key] = value;
            }

            _nextId = maxId + 1;
        }

        public void LoadImageFromSave(int id)
        {
            string saveFileName = GameNetworkManager.Instance.currentSaveFileName;

            byte[] value = ES3.LoadRawBytes("photos/" + saveFileName +
                                                "/Photo_" + id + ".raw");



            _imageRegistry[id] = value;
        }


        public void SaveImages()
        {
            
            var resolution = CameraPlugin.CameraConfig.ServerImageSettings.ImageResolution;
            Texture2D tex = new Texture2D(resolution, resolution,
                CameraPlugin.CameraConfig.ServerImageSettings.ImageFormat, false);
            foreach (var entry in _imageRegistry)
            {
                ES3.SaveRaw(entry.Value,"photos/" + GameNetworkManager.Instance.currentSaveFileName + "/Photo_"+entry.Key + ".raw");
            }

            Object.Destroy(tex);
        }

        public void SaveImage(int id)
        {
            ES3.SaveRaw(_imageRegistry[id],"photos/" + GameNetworkManager.Instance.currentSaveFileName + "/Photo_"+id + ".raw");
        }
        
        public int RegisterImage(byte[] imageData, int? index = null)
        {
            int usedIndex;
            if (index.HasValue)
            {
                usedIndex = index.Value;
                _imageRegistry[index.Value] = imageData; 
            }
            else
            {
                while (_imageRegistry.ContainsKey(_nextId))
                {
                    _nextId++;
                }

                usedIndex = _nextId;
                _imageRegistry[_nextId] = imageData;
            }
            return usedIndex;
        }

        public byte[] this[int index]
        {
            get => _imageRegistry[index];
            set => RegisterImage(value, index);
        }

        public bool has(int index) => _imageRegistry.ContainsKey(index);




    }
}