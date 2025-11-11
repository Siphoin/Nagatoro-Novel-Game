using UnityEngine;
using System.IO;
using SNEngine.IO;
using SNEngine.Debugging;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;

namespace SNEngine.SaveSystem
{
    public static class CameraSaver
    {
        public static async UniTask SaveCameraRenderToPNGAsync(int size, string fullSavePath)
        {
            await UniTask.WaitForEndOfFrame();

            int width = Screen.width;
            int height = Screen.height;

            Texture2D screenTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenTexture.Apply();

            RenderTexture rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
            Graphics.Blit(screenTexture, rt);

            RenderTexture.active = rt;
            Texture2D croppedTex = new Texture2D(size, size, TextureFormat.RGB24, false);
            croppedTex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            croppedTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            Object.Destroy(screenTexture);

            byte[] bytes = croppedTex.EncodeToPNG();
            Object.Destroy(croppedTex);

            string directoryPath = Path.GetDirectoryName(fullSavePath);

            if (!NovelDirectory.Exists(directoryPath))
            {
                NovelDirectory.Create(directoryPath);
            }

            await NovelFile.WriteAllBytesAsync(fullSavePath, bytes);

            NovelGameDebug.Log($"Camera rendered and saved to: {fullSavePath}");

#if UNITY_EDITOR
            string assetPath = fullSavePath.Replace(Application.dataPath, "Assets");
            UnityEditor.AssetDatabase.ImportAsset(assetPath);
#endif
        }
    }
}