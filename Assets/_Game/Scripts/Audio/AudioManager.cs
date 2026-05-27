using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace StrafAdvance
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxPoolSize = 8;

        [System.Serializable]
        public struct SoundEntry
        {
            public SoundID id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private SoundEntry[] sounds;
        [SerializeField] private AudioClip[] musicTracks;

        private Dictionary<SoundID, SoundEntry> _map;
        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private float _sfxVolume = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _map = new Dictionary<SoundID, SoundEntry>();
            foreach (var entry in sounds)
                _map[entry.id] = entry;

            // Auto-load generated placeholder clips for any unassigned SoundID
            foreach (SoundID id in System.Enum.GetValues(typeof(SoundID)))
            {
                if (_map.ContainsKey(id)) continue;
                var clip = Resources.Load<AudioClip>($"Audio/Generated/{id}");
                if (clip != null)
                    _map[id] = new SoundEntry { id = id, clip = clip, volume = 0.8f };
                else
                    Debug.LogWarning($"[AudioManager] no clip for {id}");
            }

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                if (_sfxGroup != null) src.outputAudioMixerGroup = _sfxGroup;
                _sfxPool.Enqueue(src);
            }

            if (musicSource != null && _musicGroup != null)
                musicSource.outputAudioMixerGroup = _musicGroup;
        }

        public void PlaySFX(SoundID id)
        {
            if (!_map.TryGetValue(id, out SoundEntry entry)) return;
            var src = _sfxPool.Dequeue();
            src.clip = entry.clip;
            src.volume = _mixer != null ? entry.volume : entry.volume * _sfxVolume;
            src.Play();
            _sfxPool.Enqueue(src);
        }

        public void PlayMusic(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= musicTracks.Length) return;
            musicSource.clip = musicTracks[trackIndex];
            musicSource.loop = true;
            musicSource.Play();
        }

        public void PlayGeneratedBgm(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        // ── volume controls ──────────────────────────────────────────────────────

        static float VolToDb(float v) => v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;

        public void SetMusicVolume(float v)
        {
            if (_mixer != null) { _mixer.SetFloat("Music_Vol", VolToDb(v)); return; }
            if (musicSource != null) musicSource.volume = v;
        }

        public void SetSFXVolume(float v)
        {
            if (_mixer != null) { _mixer.SetFloat("SFX_Vol", VolToDb(v)); return; }
            _sfxVolume = Mathf.Clamp01(v);
            foreach (var src in _sfxPool) src.volume = _sfxVolume;
        }

        public void SetUIVolume(float v)
        {
            if (_mixer != null) _mixer.SetFloat("UI_Vol", VolToDb(v));
        }
    }
}
