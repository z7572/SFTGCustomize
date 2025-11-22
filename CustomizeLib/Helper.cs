using System;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEngine;
using HarmonyLib;

namespace CustomizeLib;

public static class Helper
{
    class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject runnerObject = new("CoroutineRunner");
                    _instance = runnerObject.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(runnerObject);
                }
                return _instance;
            }
        }
    }

    public static void StartCoroutine(IEnumerator coroutine)
    {
        CoroutineRunner.Instance.StartCoroutine(coroutine);
    }

    // https://github.com/Infinite-75/PVZRHCustomization/blob/master/BepInEx/CustomizeLib.BepInEx/CustomCore.cs#L67
    public static AssetBundle GetAssetBundle(Assembly assembly, string name)
    {
        var logger = BepInEx.Logging.Logger.CreateLogSource("CustomizeLib");
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(assembly.FullName!.Split(',')[0] + "." + name) ?? assembly.GetManifestResourceStream(name)!;
            using MemoryStream stream1 = new();
            stream.CopyTo(stream1);
            var ab = AssetBundle.LoadFromMemory(stream1.ToArray());
            logger.LogInfo($"Successfully load AssetBundle {name}.");

            return ab;
        }
        catch (Exception e)
        {
            logger.LogError(e.Source);
            throw new ArgumentException($"Failed to load {name} \n{e}");
        }
    }

    public static void CopyTo(this Stream source, Stream destination, int bufferSize = 81920)
    {
        byte[] array = new byte[bufferSize];
        int count;
        while ((count = source.Read(array, 0, array.Length)) != 0)
        {
            destination.Write(array, 0, count);
        }
    }

    public static void SetMaterialColor(this Renderer renderer, Color color) => SetMaterialColor(renderer, color, color);

    public static void SetMaterialColor(this Renderer renderer, Color color, Color emissionColor)
    {
        var newMat = new Material(renderer.sharedMaterial) { color = color };
        newMat.SetColor("_EmissionColor", emissionColor);
        renderer.material = newMat;
        if (renderer.gameObject.GetComponent<MaterialCleanup>() == null)
        {
            renderer.gameObject.AddComponent<MaterialCleanup>().SetMaterial(newMat);
        }
    }

    private class MaterialCleanup : MonoBehaviour
    {
        private Material Material;

        public void SetMaterial(Material mat)
        {
            Material = mat;
        }

        void OnDestroy()
        {
            if (Material != null)
            {
                Destroy(Material);
            }
        }
    }

    public static Controller controller;
}
