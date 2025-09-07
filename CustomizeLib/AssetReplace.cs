using BepInEx;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomizeLib.Testing;

public static class TextureStore
{
    private static Dictionary<string, string> texturePathDict = new();

    public static void Init()
    {
        var textureDir = Path.Combine(Paths.PluginPath, "Textures");

        if (!Directory.Exists(textureDir))
        {
            Directory.CreateDirectory(textureDir);
        }
        try
        {
            var textureFiles = Directory.GetFiles(textureDir, "*.png", SearchOption.AllDirectories);
            foreach (string path in textureFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                texturePathDict[fileName] = path;
                Debug.Log("Loaded texture: " + fileName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Texture loading error: " + e.Message);
        }

        ReplaceAllTextures();
    }

    private static void ReplaceAllTextures()
    {
        foreach (Texture2D tex in from Texture2D texture2D in Resources.FindObjectsOfTypeAll<Texture2D>()
                                  where !texture2D.name.StartsWith("replaced_")
                                  select texture2D)
        {
            if (texturePathDict.TryGetValue(tex.name, out string replacePath))
            {
                if (TryReplaceTexture(tex, replacePath))
                {
                    tex.name = "replaced_" + tex.name;
                    Debug.Log("Replaced texture: " + tex.name);
                }
            }
        }
    }

    private static bool TryReplaceTexture(Texture2D originalTex, string replacePath)
    {
        if (originalTex == null || !File.Exists(replacePath)) return false;

        try
        {
            var replaceTex = new Texture2D(originalTex.width, originalTex.height, TextureFormat.RGBA32, false);
            var texData = File.ReadAllBytes(replacePath);

            replaceTex.LoadImage(texData);
            var pixels = replaceTex.GetPixels();
            originalTex.SetPixels(pixels);
            originalTex.Apply();
            //originalTex = replaceTex;

            Object.Destroy(replaceTex);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Replace error for " + originalTex.name + ": " + e.Message);
            return false;
        }
    }
}