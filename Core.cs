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


    [BepInPlugin("z7572.sftgcustomization", "SFTGCustomization", "1.2.1")]
    public class CustomizeCore : BaseUnityPlugin
    {
        public static AssetBundle ab_railgun;
        public static AssetBundle ab_blackhole;
        public static List<GameObject> allPrefabs = new();

        void Awake()
        {
            ab_railgun = Helper.GetAssetBundle(Assembly.GetExecutingAssembly(), "sickashellrailgun");
            ab_blackhole = Helper.GetAssetBundle(Assembly.GetExecutingAssembly(), "nullblackhole");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch]
        public class Patches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameManager), "Start")]
            public static void GameManagerPostfix()
            {
                allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.scene.rootCount == 0).ToList();
                ReplaceLogic();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(LevelCreator), "Start")]
            public static void LevelCreatorStartPostfix()
            {
                allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.scene.rootCount == 0).ToList();
                ReplaceLogic();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(WeaponPickUp), "Awake")]
            public static void WeaponPickUpAwakePostfix(WeaponPickUp __instance)
            {
                if (__instance.IsGroundWeapon)
                {
                    if (__instance.gameObject.name == "Gun39")
                    {
                        ReplaceWeaponCollider(__instance.gameObject, ab_railgun.LoadAsset<GameObject>("Gun39"));
                    }
                }
            }
        }

        private static void ReplaceLogic()
        {
            //var allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>().ToList();
            //var allClips = Resources.FindObjectsOfTypeAll<AudioClip>().ToList();
            foreach (var oriPrefab in allPrefabs)
            {
                if (oriPrefab.name == "39 Beam")
                {
                    ReplaceWeaponCollider(oriPrefab, ab_railgun.LoadAsset<GameObject>("39 Beam"));
                    foreach (Transform child in oriPrefab.transform)
                    {
                        foreach (var part in child.GetComponentsInChildren<ParticleSystemRenderer>())
                        {
                            part.material = ab_railgun.LoadAsset<Material>("GreenGlow2");
                        }
                    }
                    var weapon = oriPrefab.GetComponent<Weapon>();
                    weapon.projectile.GetComponent<TimeEvent>().enabled = false;
                    weapon.clips[0] = ab_railgun.LoadAsset<AudioClip>("lava laser");
                    foreach (Transform child in weapon.projectile.transform)
                    {
                        foreach (var part in child.GetComponentsInChildren<ParticleSystemRenderer>())
                        {
                            part.material = ab_railgun.LoadAsset<Material>("GreenGlow2");
                        }
                    }
                }
                if (oriPrefab.name == "Gun39")
                {
                    ReplaceWeaponCollider(oriPrefab, ab_railgun.LoadAsset<GameObject>("Gun39"));
                }
                if (oriPrefab.name == "41 Black Hole")
                {
                    var newPrefab = ab_blackhole.LoadAsset<GameObject>("BulletBlackHole").transform;
                    var weapon = oriPrefab.GetComponent<Weapon>();
                    var newObj1 = Instantiate(newPrefab.Find("OuterRing"), weapon.projectile.transform);


                }
                if (oriPrefab.name == "BlackHole")
                {
                    var newPrefab = ab_blackhole.LoadAsset<GameObject>("BlackHole").transform;
                    var child = oriPrefab.transform.Find("Hole");
                    child.Find("Particle System (1)").gameObject.SetActive(false);
                    oriPrefab.GetComponent<AudioSource>().clip = ab_blackhole.LoadAsset<AudioClip>("heh, nothing personal kid");
                    var newObj1 = Instantiate(newPrefab.Find("Hole").Find("OuterRing"), child);
                    var anim = newObj1.gameObject.AddComponent<BlackHoleAnim>();
                    anim.target = child;
                    var newObj2 = Instantiate(newPrefab.Find("Hole").Find("NULL"), child);
                    var anim2 = newObj2.gameObject.AddComponent<BlackHoleAnim>();
                    anim2.target = child;
                    anim2.multiplier = 0.7f;
                }
            }
        }
        public class BlackHoleAnim : MonoBehaviour
        {
            public Transform target;
            public float updateInterval = 0f;
            public float multiplier = 1f;
            public bool ignoreX, ignoreY, ignoreZ = false;
            private float lastUpdateTime;
            void Update()
            {
                if (updateInterval <= 0 || Time.time - lastUpdateTime >= updateInterval)
                {
                    SyncScale();
                    lastUpdateTime = Time.time;
                }
            }
            public void SyncScale()
            {
                if (!target) return;
                Vector3 newScale = target.localScale;
                if (ignoreX) newScale.x = transform.localScale.x;
                if (ignoreY) newScale.y = transform.localScale.y;
                if (ignoreZ) newScale.z = transform.localScale.z;
                transform.localScale = newScale * multiplier;
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
                if (!child.name.ToLower().Contains(replaceObjName.ToLower())) continue;

                GameObject newChild = Instantiate(child.gameObject, oriObj.transform);
                newChild.name = child.name;
                Destroy(newChild.GetComponent<BoxCollider>());
            }
        }
    }

    public static class Helper
    {
        // https://github.com/Infinite-75/PVZRHCustomization/blob/master/BepInEx/CustomizeLib.BepInEx/CustomCore.cs#L67
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