using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace CustomizeLib
{
    public class CodeTextManager
    {
        private static Font font;
        private static string fullCodeText = "";

        public static void InitTextAndFont(string name)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(assembly.FullName!.Split(',')[0] + "." + name) ?? assembly.GetManifestResourceStream(name)!;
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        fullCodeText = reader.ReadToEnd();
                    }
                }
                font = Font.CreateDynamicFontFromOSFont("Consolas", 14);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading font: {e.Message}");
                return;
            }
        }

        public class SpawnCodes : MonoBehaviour
        {
            void Start()
            {
                StartCoroutine(SpawnCodesRoutine(transform));
            }

            IEnumerator SpawnCodesRoutine(Transform target)
            {
                var count = Random.Range(2, 6);
                //var count = 1;
                for (int i = 0; i < count; i++)
                {
                    var randomOffset = new Vector3(Random.Range(-300f, 300f), Random.Range(-300f, 300f), 0f);
                    while (Vector3.Distance(randomOffset, Vector3.zero) < 100f)
                    {
                        randomOffset = new Vector3(Random.Range(-300f, 300f), Random.Range(-300f, 300f), 0f);
                    }
                    var sizeMultiplier = Random.Range(0.8f, 1.25f);
                    var speedMultiplier = Random.Range(1f, 3f);
                    CreateTextOnObject(target, randomOffset, sizeMultiplier, speedMultiplier);

                    var randomDelay = Random.Range(0.1f, 0.4f);
                    yield return new WaitForSeconds(randomDelay);
                }
            }
        }

        public static Text CreateTextOnObject(Transform target,
            Vector3 localOffset = default, float sizeMultiplier = 1f, float speedMultiplier = 1f)
        {
            var canvasObj = target.GetComponentInChildren<Canvas>()?.gameObject;
            if (canvasObj == null)
            {
                canvasObj = Object.Instantiate(Helper.controller.transform.Find("GameCanvas").gameObject, target);
                Object.Destroy(canvasObj.GetComponent<OrigoMeBro>());
                for (var i = 0; i < canvasObj.transform.childCount; i++)
                {
                    Object.Destroy(canvasObj.transform.GetChild(i).gameObject);
                }
                var pos = target.Find("Hole").position;
                canvasObj.transform.position = new Vector3(pos.x - 0.1f, pos.y, pos.z);
            }

            string textObjName = $"CodeText_{Time.frameCount}{Random.Range(0, 1001)}";
            GameObject textObj = new GameObject(textObjName);
            textObj.transform.SetParent(canvasObj.transform);
            textObj.transform.localPosition = localOffset;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 0.2f;

            var codeFlash = textObj.AddComponent<CodeFlashAnim>();
            codeFlash.speedMultiplier = speedMultiplier;

            Text text = textObj.AddComponent<Text>();
            SetupTextProperties(text, sizeMultiplier);

            return text;
        }

        private static void SetupTextProperties(Text text, float sizeMultiper)
        {
            text.font = font;
            text.fontSize = 50;
            text.fontStyle = FontStyle.Bold;
            text.lineSpacing = 0.8f;
            text.alignment = TextAnchor.LowerLeft;
            text.color = new Color(1f, 1f, 0.6f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;

            RectTransform rt = text.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920, 1080) * sizeMultiper;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
        }

        class CodeFlashAnim : MonoBehaviour
        {
            public Text textComponent;
            public float speedMultiplier;
            private string fullCodeText;
            private Coroutine coroutine;

            private int maxDisplayLines = 25;
            private int charsPerChunk = 35;
            private float chunkDelay = 0.001f;

            void Start()
            {
                textComponent = GetComponent<Text>();
                fullCodeText = CodeTextManager.fullCodeText;
                coroutine = StartCoroutine(ScrollCodeInChunksRoutine());
            }

            void OnDestroy()
            {
                OnDisable();
            }

            void OnDisable()
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }

            private IEnumerator ScrollCodeInChunksRoutine()
            {
                var currentCodeText = "";
                var currentCharIndex = 0;
                textComponent.text = "";
                var charsPerChunk = (int)(this.charsPerChunk * speedMultiplier);

                while (currentCharIndex < fullCodeText.Length)
                {
                    currentCodeText += fullCodeText.Substring(currentCharIndex, Math.Min(charsPerChunk, fullCodeText.Length - currentCharIndex));
                    currentCharIndex += charsPerChunk;
                    string[] lines = currentCodeText.Split('\n');
                    if (lines.Length > maxDisplayLines)
                    {
                        textComponent.text = string.Join("\n", lines, lines.Length - maxDisplayLines, maxDisplayLines);
                    }
                    else
                    {
                        textComponent.text = currentCodeText;
                    }
                    yield return new WaitForSeconds(chunkDelay);
                }
                yield return new WaitForSeconds(0.5f);
                gameObject.SetActive(false);
            }
        }
    }
}
