using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

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

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _map = new Dictionary<SoundID, SoundEntry>();
            foreach (var entry in sounds) _map[entry.id] = entry;

            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Enqueue(src);
            }
        }

        public void PlaySFX(SoundID id)
        {
            if (!_map.TryGetValue(id, out SoundEntry entry)) return;
            AudioSource src = _sfxPool.Dequeue();
            src.clip = entry.clip;
            src.volume = entry.volume * _sfxVolume;
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

        public void SetMusicVolume(float v) => musicSource.volume = v;

        public void SetSFXVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            foreach (AudioSource src in _sfxPool)
                src.volume = _sfxVolume;
        }
    }
}
