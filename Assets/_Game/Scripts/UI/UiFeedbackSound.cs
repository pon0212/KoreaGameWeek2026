using UnityEngine;

namespace TastePassport.UI
{
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
