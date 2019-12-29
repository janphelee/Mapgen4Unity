using UnityEngine;
using System.Collections;
using System.IO;
using Unity.Collections;
using System.Collections.Generic;

namespace Phevolution
{
    public class DebugHelper
    {
        public void readTargetTexture(Camera camera, Texture2D output)
        {
            var currentRT = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;
            output.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            output.Apply();

            RenderTexture.active = currentRT;
        }

        // Take a "screenshot" of a camera's Render Texture.
        public Texture2D renderTargetImage(Camera camera, Shader shader, string tag = "", FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            // The Render Texture in RenderTexture.active is the one
            // that will be read by ReadPixels.
            var currentRT = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            // Render the camera's view.
            camera.RenderWithShader(shader, tag);

            // Make a new texture and read the active Render Texture into it.
            Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGBA32, false, false);
            image.filterMode = filterMode;
            image.wrapMode = wrapMode;
            image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            image.Apply();

            // Replace the original active Render Texture.
            RenderTexture.active = currentRT;
            return image;
        }

        public static void SaveToPng(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            FileStream file = File.Open(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(bytes);
            file.Close();

            Debug.Log($"saveTo path:{path}");
        }

        public static void SaveArray<T>(string fileName, NativeArray<T> d, int limit = int.MaxValue) where T : struct
        {
            var fileInfo = new FileInfo($"{Application.streamingAssetsPath}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < d.Length && i < limit; ++i)
                streamWriter.WriteLine($"{i} {d[i]}");
            streamWriter.Close();
            streamWriter.Dispose();
        }
        public static void SaveArray<T>(string fileName, T[] d, int limit = int.MaxValue) where T : struct
        {
            var fileInfo = new FileInfo($"{Application.streamingAssetsPath}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < d.Length && i < limit; ++i)
                streamWriter.WriteLine($"{i} {d[i]}");
            streamWriter.Close();
            streamWriter.Dispose();
        }
        public static void SaveArray<T>(string fileName, List<T> d, int limit = int.MaxValue) where T : struct
        {
            SaveArray(fileName, d.ToArray(), limit);
        }
        public static void SaveArray<T>(string fileName, HashSet<T> d, int limit = int.MaxValue) where T : struct
        {
            var fileInfo = new FileInfo($"{Application.streamingAssetsPath}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            int i = 0;
            foreach (var a in d)
            {
                streamWriter.WriteLine($"{i++} {a}");
                if (i >= limit) break;
            }
            streamWriter.Close();
            streamWriter.Dispose();
        }

    }

}
