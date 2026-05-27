using UnityEngine;

namespace StrafAdvance
{
    // Generates a loopable 8-second drone pad at runtime and starts playing it via AudioManager.
    public class ProceduralDrone : MonoBehaviour
    {
        const int k_Rate     = 22050;
        const int k_Duration = 8;

        void Start()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGeneratedBgm(Generate());
        }

        static AudioClip Generate()
        {
            int n = k_Rate * k_Duration;
            var data = new float[n];

            // Layered open-fifth pad: A1 + E2 + A2 + E3
            float[] freqs = { 55f, 82.5f, 110f, 165f };
            float[] amps  = { 0.35f, 0.20f, 0.18f, 0.12f };

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / k_Rate;
                float sample = 0f;
                for (int fi = 0; fi < freqs.Length; fi++)
                {
                    float lfo = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.3f * t + fi);
                    sample += Mathf.Sin(2f * Mathf.PI * freqs[fi] * t) * amps[fi] * lfo;
                }
                // Crossfade envelope: fade in first 0.5s, fade out last 0.5s for seamless loop
                float env = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((k_Duration - t) / 0.5f);
                data[i] = sample * env;
            }

            var clip = AudioClip.Create("ProceduralDrone", n, 1, k_Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
