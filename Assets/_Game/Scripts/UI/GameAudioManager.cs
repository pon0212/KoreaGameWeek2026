using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TastePassport.UI
{
    public enum GameSoundCue
    {
        BookOpen,
        PageFlip,
        Stamp,
        Unlock,
        Craft,
        FestivalComplete
    }

    /// <summary>Saved mix preferences shared by the UI and generated audio channels.</summary>
    public static class GameAudioSettings
    {
        private const string MusicKey = "tp_music_volume";
        private const string SfxKey = "tp_sfx_volume";
        private const string AmbienceKey = "tp_ambience_volume";
        private const string MuteKey = "tp_sound";

        public static float MusicVolume => PlayerPrefs.GetFloat(MusicKey, .68f);
        public static float SfxVolume => PlayerPrefs.GetFloat(SfxKey, .62f);
        public static float AmbienceVolume => PlayerPrefs.GetFloat(AmbienceKey, .28f);
        public static bool Muted => PlayerPrefs.GetInt(MuteKey, 1) == 0;

        public static void SetMusicVolume(float value) => PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        public static void SetSfxVolume(float value) => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        public static void SetAmbienceVolume(float value) => PlayerPrefs.SetFloat(AmbienceKey, Mathf.Clamp01(value));
        public static void SetMuted(bool value) => PlayerPrefs.SetInt(MuteKey, value ? 0 : 1);
    }

    /// <summary>Mixes the existing chapter music and tap sound with original ambient and event cues.</summary>
    public sealed class GameAudioManager : MonoBehaviour
    {
        private const int SampleRate = 22050;
        // The music was previously mixed at roughly 7% of full output. Keep the saved
        // player slider meaningful while making the default laptop mix clearly audible.
        private const float ChapterMusicBaseVolume = .78f;
        private readonly Dictionary<GameSoundCue, AudioClip> cues = new Dictionary<GameSoundCue, AudioClip>();
        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioSource ambienceSource;
        private AudioClip ambienceClip;
        private Coroutine ambienceTransition;
        private AudioClip observedMusicClip;
        private float musicVolumeReadyAt;
        private float saveSettingsAt = -1f;
        private bool ambienceKorean;
        private bool hasAmbience;

        public float MusicVolume => GameAudioSettings.MusicVolume;
        public float SfxVolume => GameAudioSettings.SfxVolume;
        public float AmbienceVolume => GameAudioSettings.AmbienceVolume;
        public bool Muted => GameAudioSettings.Muted;

        public static GameAudioManager For(GameObject owner)
        {
            var manager = owner.GetComponent<GameAudioManager>();
            if (manager == null) manager = owner.AddComponent<GameAudioManager>();
            return manager;
        }

        private void Awake()
        {
            FindChannels();
            CreateAmbienceSource();
            ApplyMute();
            ApplySfxVolume();
        }

        private void Update()
        {
            FindChannels();
            ApplyMute();
            ApplySfxVolume();

            if (musicSource != null && musicSource.clip != observedMusicClip)
            {
                observedMusicClip = musicSource.clip;
                musicVolumeReadyAt = Time.unscaledTime + 1.25f;
            }
            if (musicSource != null && musicSource.clip != null && Time.unscaledTime >= musicVolumeReadyAt)
                musicSource.volume = ChapterMusicBaseVolume * MusicVolume;
            if (ambienceTransition == null) ApplyAmbienceVolume();
            if (saveSettingsAt >= 0f && Time.unscaledTime >= saveSettingsAt)
            {
                PlayerPrefs.Save();
                saveSettingsAt = -1f;
            }
        }

        public void NotifyMusicTransition()
        {
            musicVolumeReadyAt = Time.unscaledTime + 1.3f;
        }

        public void SetMusicVolume(float value)
        {
            GameAudioSettings.SetMusicVolume(value);
            ScheduleSave();
            if (musicSource != null && Time.unscaledTime >= musicVolumeReadyAt)
                musicSource.volume = ChapterMusicBaseVolume * MusicVolume;
        }

        public void SetSfxVolume(float value)
        {
            GameAudioSettings.SetSfxVolume(value);
            ScheduleSave();
            ApplySfxVolume();
        }

        public void SetAmbienceVolume(float value)
        {
            GameAudioSettings.SetAmbienceVolume(value);
            ScheduleSave();
            if (ambienceTransition == null) ApplyAmbienceVolume();
        }

        public void SetMuted(bool value)
        {
            GameAudioSettings.SetMuted(value);
            ScheduleSave();
            ApplyMute();
        }

        public void SetEnvironment(bool korean)
        {
            if (hasAmbience && ambienceKorean == korean) return;
            ambienceKorean = korean;
            hasAmbience = true;
            AudioClip next = CreateAmbience(korean);
            if (ambienceTransition != null) StopCoroutine(ambienceTransition);
            ambienceTransition = StartCoroutine(SwapAmbience(next));
        }

        public void PlayCue(GameSoundCue cue)
        {
            if (Muted || SfxVolume <= 0f || sfxSource == null) return;
            AudioClip clip;
            if (!cues.TryGetValue(cue, out clip))
            {
                clip = CreateCue(cue);
                cues.Add(cue, clip);
            }
            sfxSource.PlayOneShot(clip);
        }

        private void FindChannels()
        {
            foreach (var source in GetComponents<AudioSource>())
            {
                if (source.loop)
                {
                    if (musicSource == null || musicSource.clip == null && source.clip != null) musicSource = source;
                }
                else if (sfxSource == null) sfxSource = source;
            }
        }

        private void CreateAmbienceSource()
        {
            var channel = new GameObject("Journey ambience", typeof(AudioSource));
            channel.transform.SetParent(transform, false);
            ambienceSource = channel.GetComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = 0f;
        }

        private IEnumerator SwapAmbience(AudioClip next)
        {
            yield return FadeAmbienceTo(0f, .24f);
            if (ambienceSource != null)
            {
                ambienceSource.Stop();
                ambienceSource.clip = next;
                ambienceSource.Play();
            }
            if (ambienceClip != null) Destroy(ambienceClip);
            ambienceClip = next;
            float elapsed = 0f;
            const float fadeDuration = .48f;
            while (elapsed < fadeDuration && ambienceSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                ambienceSource.volume = Mathf.Lerp(0f, AmbienceTargetVolume(), Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
                yield return null;
            }
            ApplyAmbienceVolume();
            ambienceTransition = null;
        }

        private IEnumerator FadeAmbienceTo(float target, float duration)
        {
            if (ambienceSource == null) yield break;
            float initial = ambienceSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ambienceSource.volume = Mathf.Lerp(initial, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            ambienceSource.volume = target;
        }

        private AudioClip CreateAmbience(bool korean)
        {
            const int seconds = 6;
            int sampleCount = SampleRate * seconds;
            const int noiseCount = 2048;
            var noise = new float[noiseCount];
            var random = new System.Random(korean ? 2741 : 8417);
            for (int i = 0; i < noise.Length; i++) noise[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            var samples = new float[sampleCount];
            int slowCycles = korean ? 7 : 11;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float noisePosition = progress * noiseCount;
                int low = (int)noisePosition;
                int high = (low + 1) % noiseCount;
                float blend = Mathf.SmoothStep(0f, 1f, noisePosition - low);
                float softNoise = Mathf.Lerp(noise[low], noise[high], blend);
                float swell = .72f + .28f * Mathf.Sin(Mathf.PI * 2f * slowCycles * progress);
                float lowTone = Mathf.Sin(Mathf.PI * 2f * (korean ? 47f : 59f) * progress);
                samples[i] = (softNoise * .76f + lowTone * .08f) * swell * .055f;
            }
            var clip = AudioClip.Create(korean ? "Dau Xu ambient - Jeonju" : "Dau Xu ambient - Hoi An",
                sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateCue(GameSoundCue cue)
        {
            float duration = cue == GameSoundCue.FestivalComplete ? .72f : cue == GameSoundCue.Unlock ? .52f : .3f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];
            float[] notes = CueNotes(cue);
            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;
                float notePosition = progress * notes.Length;
                int note = Mathf.Min(notes.Length - 1, (int)notePosition);
                float localProgress = notePosition - note;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(localProgress)) * (1f - progress * .35f);
                float time = i / (float)SampleRate;
                float fundamental = Mathf.Sin(Mathf.PI * 2f * notes[note] * time);
                float overtone = Mathf.Sin(Mathf.PI * 4f * notes[note] * time) * .18f;
                float transient = cue == GameSoundCue.Stamp ? Mathf.Exp(-progress * 52f) * .12f : 0f;
                samples[i] = (fundamental + overtone + transient) * envelope * .24f;
            }
            var clip = AudioClip.Create("Dau Xu SFX - " + cue, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float[] CueNotes(GameSoundCue cue)
        {
            switch (cue)
            {
                case GameSoundCue.BookOpen: return new[] { 392f, 523.25f };
                case GameSoundCue.PageFlip: return new[] { 659.25f, 440f };
                case GameSoundCue.Stamp: return new[] { 196f, 392f };
                case GameSoundCue.Unlock: return new[] { 392f, 493.88f, 587.33f };
                case GameSoundCue.Craft: return new[] { 329.63f, 392f, 493.88f };
                default: return new[] { 392f, 493.88f, 587.33f, 783.99f };
            }
        }

        private float AmbienceTargetVolume() => Muted ? 0f : AmbienceVolume * .24f;

        private void ApplyMute()
        {
            AudioListener.volume = Muted ? 0f : 1f;
        }

        private void ApplySfxVolume()
        {
            if (sfxSource != null) sfxSource.volume = .25f * SfxVolume;
        }

        private void ApplyAmbienceVolume()
        {
            if (ambienceSource != null && ambienceClip != null) ambienceSource.volume = AmbienceTargetVolume();
        }

        private void ScheduleSave()
        {
            saveSettingsAt = Time.unscaledTime + .45f;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (ambienceSource != null) ambienceSource.Stop();
            if (ambienceClip != null) Destroy(ambienceClip);
            foreach (var clip in cues.Values) if (clip != null) Destroy(clip);
        }
    }
}
