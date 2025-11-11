using UnityEngine;
using System.IO;
using SNEngine.IO;
using Object = UnityEngine.Object;

namespace SNEngine.SaveSystem
{
    public static class CameraSaver
    {
        public static void SaveCameraRenderToPNG(int size, string fullSavePath)
        {
            Camera camera = Camera.main;

            RenderTexture rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            RenderTexture oldRT = camera.targetTexture;

            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);

            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();

            camera.targetTexture = oldRT;
            RenderTexture.active = null;

            Object.Destroy(rt);
            Object.Destroy(tex);

            string directoryPath = Path.GetDirectoryName(fullSavePath);

            if (!NovelDirectory.Exists(directoryPath))
            {
                NovelDirectory.Create(directoryPath);
            }

            NovelFile.WriteAllBytes(fullSavePath, bytes);
            Debug.Log($"Camera rendered and saved to: {fullSavePath}");

#if UNITY_EDITOR
            string assetPath = fullSavePath.Replace(Application.dataPath, "Assets");
            UnityEditor.AssetDatabase.ImportAsset(assetPath);
#endif
        }
    }
}