using UnityEngine;

namespace BHCamera
{
    public class DebugGUI : MonoBehaviour
    {
        private Texture2D _debugImage = null;
        private int _debugWidth;
        private int _debugHeight;

        private float scaleFactor = 5;

        public DebugGUI()
        {
            
        }

        public Texture2D debugImage
        {
            get => _debugImage;
            set
            {
                _debugWidth = Mathf.FloorToInt(value.width*scaleFactor);
                _debugHeight = Mathf.FloorToInt(value.height*scaleFactor);
                _debugImage = Resize(value, _debugWidth, _debugHeight);
            }
        }

        private Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture rt=new RenderTexture(targetX, targetY,24);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D,rt);
            Texture2D result=new Texture2D(targetX,targetY);
            result.ReadPixels(new Rect(0,0,targetX,targetY),0,0);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            if (debugImage)
            {
                
                GUI.Box(new Rect(200, 200, _debugWidth, _debugHeight), debugImage);
            }
        }
    }
}