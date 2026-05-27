using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StrafAdvance.Editor
{
    public static class ProceduralSfxGenerator
    {
        const int k_Rate = 22050;
        const string k_Out = "Assets/Resources/Audio/Generated";

        [MenuItem("Strafe/Generate Placeholder SFX")]
        static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio"))
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");
            if (!AssetDatabase.IsValidFolder(k_Out))
                AssetDatabase.CreateFolder("Assets/Resources/Audio", "Generated");

            foreach (SoundID id in Enum.GetValues(typeof(SoundID)))
                Write(id, MakeSamples(id));

            AssetDatabase.Refresh();
            Debug.Log($"[ProceduralSfx] {Enum.GetValues(typeof(SoundID)).Length} clips → {k_Out}");
        }

        static float[] MakeSamples(SoundID id) => id switch
        {
            SoundID.Shoot          => Burst(880f,  0.08f, 10f),
            SoundID.EnemyHit       => Noise(0.07f, 12f),
            SoundID.PlayerHit      => Burst(150f,  0.18f, 5f),
            SoundID.EnemyDeath     => Sweep(800f,  100f,  0.30f),
            SoundID.LevelComplete  => Arpeg(new[]{523f, 659f, 784f}, 0.12f),
            SoundID.BossRoar       => Mix(Burst(80f, 0.4f, 3f),  Noise(0.4f, 2f)),
            SoundID.BossPhase2     => Sweep(180f,  440f,  0.45f),
            SoundID.PowerUpCollect => Sweep(800f,  2000f, 0.18f),
            SoundID.Dodge          => Sweep(400f,  1200f, 0.12f),
            SoundID.ShieldHit      => Burst(900f,  0.22f, 14f),
            SoundID.ComboTier      => Burst(1200f, 0.12f, 16f),
            SoundID.PerkUnlock     => Arpeg(new[]{600f, 900f, 1200f}, 0.10f),
            SoundID.UIClick        => Burst(1400f, 0.04f, 35f),
            SoundID.UIConfirm      => Burst(880f,  0.14f, 12f),
            SoundID.EliteDeath     => Mix(Sweep(600f, 80f, 0.35f), Noise(0.35f, 5f)),
            _                      => Burst(440f,  0.10f, 10f),
        };

        // ── generators ──────────────────────────────────────────────────────────

        static float[] Burst(float freq, float dur, float decay)
        {
            int n = (int)(k_Rate * dur);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / k_Rate;
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-decay * t) * 0.8f;
            }
            return s;
        }

        static float[] Sweep(float f0, float f1, float dur)
        {
            int n = (int)(k_Rate * dur);
            var s = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += Mathf.Lerp(f0, f1, t) / k_Rate;
                s[i] = Mathf.Sin(2f * Mathf.PI * phase) * (1f - t * 0.9f) * 0.8f;
            }
            return s;
        }

        static float[] Noise(float dur, float decay)
        {
            int n = (int)(k_Rate * dur);
            var s = new float[n];
            var rng = new System.Random(42);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / k_Rate;
                s[i] = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-decay * t) * 0.6f;
            }
            return s;
        }

        static float[] Arpeg(float[] freqs, float noteDur)
        {
            int noteN = (int)(k_Rate * noteDur);
            var s = new float[noteN * freqs.Length];
            for (int fi = 0; fi < freqs.Length; fi++)
                for (int i = 0; i < noteN; i++)
                {
                    float t = (float)i / k_Rate;
                    s[fi * noteN + i] = Mathf.Sin(2f * Mathf.PI * freqs[fi] * t)
                                        * Mathf.Exp(-12f * t) * 0.8f;
                }
            return s;
        }

        static float[] Mix(float[] a, float[] b)
        {
            int n = Math.Max(a.Length, b.Length);
            var s = new float[n];
            float peak = 0f;
            for (int i = 0; i < n; i++)
            {
                s[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
                if (Mathf.Abs(s[i]) > peak) peak = Mathf.Abs(s[i]);
            }
            if (peak > 0.001f) for (int i = 0; i < n; i++) s[i] /= peak * 1.1f;
            return s;
        }

        // ── WAV writer ───────────────────────────────────────────────────────────

        static void Write(SoundID id, float[] samples)
        {
            string assetPath = $"{k_Out}/{id}.wav";
            string fullPath  = assetPath.Replace("Assets", Application.dataPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            int dataBytes = samples.Length * 2;
            using var ms = new MemoryStream(44 + dataBytes);
            using var bw = new BinaryWriter(ms);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataBytes);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); bw.Write((short)1); bw.Write((short)1);
            bw.Write(k_Rate); bw.Write(k_Rate * 2);
            bw.Write((short)2); bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataBytes);
            foreach (float v in samples)
                bw.Write((short)Mathf.Clamp(v * 32767f, -32768f, 32767f));

            bw.Flush();
            File.WriteAllBytes(fullPath, ms.ToArray());
        }
    }
}
