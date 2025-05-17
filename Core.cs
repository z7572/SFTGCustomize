using BepInEx;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using LevelEditor;

namespace CustomizeLib
{


    [BepInPlugin("z7572.customizelib", "CustomizeLib", "1.0")]
    public class CustomizeLib : BaseUnityPlugin
    {
        public static AssetBundle ab;
        public static List<GameObject> allPrefabs = new();

        void Awake()
        {
            ab = Helper.GetAssetBundle(Assembly.GetExecutingAssembly(), "sickashellrailgun");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(GameManager), "Start")]
        public class GameManagerPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                ReplaceLogic();
            }
        }

        [HarmonyPatch(typeof(LevelCreator), "Start")]
        public class LevelCreatorPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                ReplaceLogic();
            }
        }

        private static void ReplaceLogic()
        {
            allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.scene.rootCount == 0).ToList();
            var allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>().ToList();
            var allClips = Resources.FindObjectsOfTypeAll<AudioClip>().ToList();

            foreach (var oriPrefab in allPrefabs)
            {
                if (oriPrefab.name == "39 Beam")
                {
                    ReplaceWeaponCollider(oriPrefab, ab.LoadAsset<GameObject>("39 Beam"));
                    foreach (Transform child in oriPrefab.transform)
                    {
                        foreach (var part in child.GetComponentsInChildren<ParticleSystemRenderer>())
                        {
                            part.material = ab.LoadAsset<Material>("GreenGlow2");
                        }
                    }
                    var weapon = oriPrefab.GetComponent<Weapon>();
                    weapon.projectile.GetComponent<TimeEvent>().enabled = false;
                    weapon.clips[0] = ab.LoadAsset<AudioClip>("lava laser");
                    foreach (Transform child in weapon.projectile.transform)
                    {
                        foreach (var part in child.GetComponentsInChildren<ParticleSystemRenderer>())
                        {
                            part.material = ab.LoadAsset<Material>("GreenGlow2");
                        }
                    }
                }
                if (oriPrefab.name == "Gun39")
                {
                    ReplaceWeaponCollider(oriPrefab, ab.LoadAsset<GameObject>("Gun39"));
                }
            }
        }

        public static void ReplaceWeaponCollider(GameObject oriObj, GameObject newObj, string replaceObjName = "collider")
        {
            foreach (Transform child in oriObj.transform)
            {
                if (!child.name.ToLower().Contains(replaceObjName.ToLower())) continue;

                child.GetComponent<MeshRenderer>().enabled = false;
            }
            //foreach (var renderer in newObj.GetComponentsInChildren<Renderer>())
            //{
            //    foreach (Material mat in renderer.materials)
            //    {
            //        mat.shader = Shader.Find("Standard");
            //    }
            //}
            foreach (Transform child in newObj.transform)
            {
                if (!child.name.ToLower().Contains("collider")) continue;

                GameObject newChild = Instantiate(child.gameObject, oriObj.transform);
                newChild.name = child.name;
                Destroy(newChild.GetComponent<BoxCollider>());
            }

        }
    }

    public class Helper
    {
        // https://github.com/Infinite-75/PVZRHCustomization
        public static AssetBundle GetAssetBundle(Assembly assembly, string name)
        {
            try
            {
                using Stream stream = assembly.GetManifestResourceStream(assembly.FullName!.Split(',')[0] + "." + name) ?? assembly.GetManifestResourceStream(name)!;
                using MemoryStream stream1 = new();
                stream.CopyTo(stream1);
                var ab = AssetBundle.LoadFromMemory(stream1.ToArray());
                BepInEx.Logging.Logger.CreateLogSource("CustomizeLib").LogInfo($"Successfully load AssetBundle {name}.");

                return ab;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Source);
                throw new ArgumentException($"Failed to load {name} \n{e}");
            }
        }

    }

    public static class Extensions
    {
        // .NET 6.0 feature
        public static void CopyTo(this Stream source, Stream destination, int bufferSize = 81920)
        {
            byte[] array = new byte[bufferSize];
            int count;
            while ((count = source.Read(array, 0, array.Length)) != 0)
            {
                destination.Write(array, 0, count);
            }
        }

    }
}