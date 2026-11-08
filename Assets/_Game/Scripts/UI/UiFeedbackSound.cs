using UnityEngine;
using UnityEngine.UI;

namespace TastePassport.UI
{
    public static class StoryNarration
    {
        public static string LastStatus { get; private set; } = "ready";

        public static bool IsSupported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static string Speak(string language, string text)
        {
            if (PlayerPrefs.GetInt("tp_sound", 1) != 1) return LastStatus = "muted";
            if (string.IsNullOrWhiteSpace(text)) return LastStatus = "error";
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var bridge = new AndroidJavaClass("com.tastepassport.tts.TastePassportTts"))
                {
                    bridge.CallStatic("speak", activity, language == "ko" ? "ko-KR" : "vi-VN", text,
                        GameAudioSettings.NarrationVolume);
                    return LastStatus = bridge.CallStatic<string>("getStatus");
                }
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("Taste Passport Android narration failed: " + error.Message);
                return LastStatus = "unavailable";
            }
#else
            Debug.Log("Taste Passport narration is available in Android builds; requested language: " + language);
            return LastStatus = "pc_unavailable";
#endif
        }

        public static string GetStatus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass("com.tastepassport.tts.TastePassportTts"))
                    return LastStatus = bridge.CallStatic<string>("getStatus");
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("Taste Passport Android narration status check failed: " + error.Message);
                return LastStatus = "unavailable";
            }
#else
            return LastStatus = "pc_unavailable";
#endif
        }

        public static void SetVolume(float value) => GameAudioSettings.SetNarrationVolume(value);

        public static void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass("com.tastepassport.tts.TastePassportTts"))
                    bridge.CallStatic("stop");
            }
            catch (System.Exception error) { Debug.LogWarning("Taste Passport Android narration stop failed: " + error.Message); }
#endif
        }
    }

    // Small generated cue keeps the MVP self-contained until licensed audio is available.
    [RequireComponent(typeof(AudioSource))]
    public sealed class UiFeedbackSound : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip tap;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.25f;
            tap = CreateTap();
        }

        public void PlayTap()
        {
            if (PlayerPrefs.GetInt("tp_sound", 1) == 1 && source != null && tap != null)
                source.PlayOneShot(tap);
        }

        private static AudioClip CreateTap()
        {
            const int rate = 22050;
            const int count = 1544;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;
                float envelope = (1f - progress) * (1f - progress);
                float frequency = 660f + 220f * progress;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * envelope * 0.35f;
            }
            var clip = AudioClip.Create("Taste Passport UI tap", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (tap != null) Destroy(tap);
        }
    }
}

namespace TastePassport.UI
{
    /// <summary>Low-cost UI cherry blossoms that drift behind interactive content.</summary>
    public sealed class PetalDriftEffect : MonoBehaviour
    {
        private const int FlowerCount = 6;
        private RectTransform[] flowers;
        private float[] speeds;
        private float[] offsets;
        private float[] phases;
        private RectTransform area;

        public void Initialize(Sprite petalSprite)
        {
            area = transform as RectTransform;
            flowers = new RectTransform[FlowerCount];
            speeds = new float[FlowerCount];
            offsets = new float[FlowerCount];
            phases = new float[FlowerCount];
            for (int i = 0; i < FlowerCount; i++)
            {
                var root = new GameObject("Blossom " + i, typeof(RectTransform));
                root.transform.SetParent(transform, false);
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(38, 38);
                flowers[i] = rect;
                speeds[i] = Random.Range(22f, 42f);
                offsets[i] = Random.Range(0f, 1000f);
                phases[i] = Random.Range(0f, Mathf.PI * 2f);
                for (int p = 0; p < 5; p++)
                {
                    float angle = p * Mathf.PI * 2f / 5f;
                    var petal = new GameObject("Petal " + p, typeof(RectTransform), typeof(Image));
                    petal.transform.SetParent(rect, false);
                    var pr = petal.GetComponent<RectTransform>();
                    pr.sizeDelta = new Vector2(10, 15);
                    pr.anchoredPosition = new Vector2(Mathf.Cos(angle) * 8, Mathf.Sin(angle) * 8);
                    pr.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
                    var image = petal.GetComponent<Image>();
                    image.sprite = petalSprite;
                    image.color = new Color(0.91f, 0.43f, 0.53f, 0.42f);
                    image.raycastTarget = false;
                }
                var center = new GameObject("Blossom center", typeof(RectTransform), typeof(Image));
                center.transform.SetParent(rect, false);
                center.GetComponent<RectTransform>().sizeDelta = new Vector2(7, 7);
                var centerImage = center.GetComponent<Image>();
                centerImage.sprite = petalSprite;
                centerImage.color = new Color(0.94f, 0.68f, 0.34f, 0.65f);
                centerImage.raycastTarget = false;
            }
        }

        private void Update()
        {
            if (area == null || flowers == null || area.rect.width <= 0 || area.rect.height <= 0) return;
            float width = area.rect.width;
            float height = area.rect.height;
            for (int i = 0; i < flowers.Length; i++)
            {
                float y = Mathf.Repeat(offsets[i] + Time.unscaledTime * speeds[i], height + 60f) - 30f;
                float x = Mathf.Repeat((i + 0.5f) * width / flowers.Length + Mathf.Sin(Time.unscaledTime * 0.35f + phases[i]) * 48f, width);
                flowers[i].anchoredPosition = new Vector2(x, y);
                flowers[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 0.6f + phases[i]) * 22f);
            }
        }
    }

    /// <summary>Procedural, lightweight ambient themes for the three journey chapters.</summary>
    public sealed class StageAudioDirector : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip currentClip;
        private Coroutine transition;

        public static StageAudioDirector For(GameObject owner)
        {
            var director = owner.GetComponent<StageAudioDirector>();
            if (director == null) director = owner.AddComponent<StageAudioDirector>();
            return director;
        }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        public void PlayStage(int stage, bool korean)
        {
            stage = Mathf.Clamp(stage, 1, 3);
            string signature = (korean ? "ko" : "vi") + "-chapter-" + stage;
            if (currentClip != null && currentClip.name == signature) return;
            currentClip = CreateTheme(signature, stage, korean);
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(SwapTheme(currentClip));
        }

        private System.Collections.IEnumerator SwapTheme(AudioClip next)
        {
            float initial = source.volume;
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(initial, 0f, t / .45f);
                yield return null;
            }
            source.Stop();
            source.clip = next;
            source.Play();
            for (float t = 0; t < .7f; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, .14f, t / .7f);
                yield return null;
            }
            source.volume = .14f;
            transition = null;
        }

        private static AudioClip CreateTheme(string name, int stage, bool korean)
        {
            const int rate = 22050;
            const int seconds = 6;
            int count = rate * seconds;
            float[] samples = new float[count];
            float[,] roots = korean
                ? new float[,] { { 293.66f, 349.23f, 440f }, { 261.63f, 329.63f, 392f }, { 246.94f, 329.63f, 440f } }
                : new float[,] { { 261.63f, 329.63f, 392f }, { 293.66f, 349.23f, 440f }, { 261.63f, 349.23f, 415.30f } };
            int row = stage - 1;
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)rate;
                float phrase = time % 2f;
                float noteIndex = Mathf.Floor(phrase / .5f);
                float frequency = roots[row, (int)noteIndex % 3];
                float fade = Mathf.Min(1f, time * 2f, (seconds - time) * 2f);
                float pluck = Mathf.Exp(-(phrase % .5f) * 3.5f);
                float fundamental = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float overtone = Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * time) * .22f;
                float bed = Mathf.Sin(2f * Mathf.PI * (frequency * .5f) * time) * .12f;
                samples[i] = (fundamental + overtone + bed) * pluck * fade * .12f;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (source != null) source.Stop();
            if (currentClip != null) Destroy(currentClip);
        }
    }
}
