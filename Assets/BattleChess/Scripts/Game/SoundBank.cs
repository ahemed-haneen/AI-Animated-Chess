using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleChess
{
    /// <summary>Synthesised placeholder sounds (no audio files needed). Theme assets can override per piece.</summary>
    public class SoundBank : MonoBehaviour
    {
        public static SoundBank I;
        public bool muted;
        const int Rate = 22050;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource[] pool;
        int next;
        System.Random rng = new System.Random(7);

        void Awake()
        {
            I = this;
            pool = new AudioSource[10];
            for (int i = 0; i < pool.Length; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }
            Build();
        }

        public void Play(string key, float volume = 1f, float pitch = 1f)
        {
            AudioClip c;
            if (clips.TryGetValue(key, out c)) Play(c, volume, pitch);
        }

        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (muted || clip == null) return;
            var src = pool[next++ % pool.Length];
            src.pitch = pitch * UnityEngine.Random.Range(0.94f, 1.06f);
            src.PlayOneShot(clip, volume);
        }

        float Noise() { return (float)(rng.NextDouble() * 2 - 1); }

        AudioClip Synth(string name, float dur, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f((float)i / Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            clips[name] = clip;
            return clip;
        }

        // one-pole low-pass noise generator
        Func<float, float> LowNoise(float cutoff)
        {
            float a = Mathf.Exp(-2f * Mathf.PI * cutoff / Rate), y = 0;
            return t => { y = (1 - a) * Noise() + a * y; return y; };
        }

        static float Env(float t, float attack, float decay) { return t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay); }

        void Build()
        {
            const float TAU = Mathf.PI * 2;
            var n1 = LowNoise(3000); Synth("shot", 0.45f, t => n1(t) * 2.2f * Env(t, 0.002f, 0.07f) + Mathf.Sin(TAU * (140 - 200 * t) * t) * 0.5f * Env(t, 0.002f, 0.08f));
            var n2 = LowNoise(500); Synth("cannon", 1.4f, t => n2(t) * 4f * Env(t, 0.004f, 0.35f) + Mathf.Sin(TAU * (70 - 25 * t) * t) * 0.8f * Env(t, 0.004f, 0.3f));
            var n3 = LowNoise(7000); Synth("clash", 0.5f, t => (Mathf.Sin(TAU * 2200 * t) * 0.35f + Mathf.Sin(TAU * 3170 * t) * 0.25f + Mathf.Sin(TAU * 4630 * t) * 0.12f) * Env(t, 0.001f, 0.12f) + n3(t) * Env(t, 0.001f, 0.02f));
            var n4 = LowNoise(1500); Synth("stab", 0.25f, t => n4(t) * 1.6f * Env(t, 0.005f, 0.05f) + Mathf.Sin(TAU * (300 - 600 * t) * t) * 0.3f * Env(t, 0.003f, 0.05f));
            var n5 = LowNoise(400); Synth("thud", 0.5f, t => Mathf.Sin(TAU * (130 - 160 * t) * t) * 0.8f * Env(t, 0.003f, 0.12f) + n5(t) * 1.5f * Env(t, 0.003f, 0.1f));
            Synth("step", 0.08f, t => Mathf.Sin(TAU * (170 - 800 * t) * t) * 0.35f * Env(t, 0.002f, 0.02f));
            Synth("clop", 0.07f, t => Mathf.Sin(TAU * (700 - 3000 * t) * t) * 0.3f * Env(t, 0.001f, 0.015f));
            var n6 = LowNoise(250); Synth("roll", 0.2f, t => n6(t) * 1.2f * Env(t, 0.02f, 0.08f));
            var n7 = LowNoise(1200); Synth("splash", 0.7f, t => n7(t) * 1.5f * Env(t, 0.02f, 0.25f));
            var n8 = LowNoise(1800); Synth("whoosh", 0.4f, t => n8(t) * 1.4f * Mathf.Sin(Mathf.PI * t / 0.4f));
            Synth("select", 0.08f, t => Mathf.Sin(TAU * 520 * t) * 0.3f * Env(t, 0.005f, 0.03f));
            Synth("check", 0.5f, t => (Mathf.Repeat(t, 0.14f) < 0.1f ? Mathf.Sign(Mathf.Sin(TAU * 196 * t)) * 0.18f : 0f) * Env(t, 0.002f, 0.4f));
            Synth("promote", 0.7f, t => Mathf.Sin(TAU * new[] { 523f, 659f, 784f, 1046f }[Mathf.Min(3, (int)(t / 0.08f))] * t) * 0.3f * Env(t % 0.08f + 0.01f, 0.005f, 0.2f));
            Synth("win", 1.2f, t => Mathf.Sin(TAU * new[] { 392f, 523f, 659f, 784f, 1046f }[Mathf.Min(4, (int)(t / 0.12f))] * t) * 0.3f * Env(t, 0.01f, 0.8f));
            Synth("lose", 1.2f, t => Mathf.Sin(TAU * new[] { 392f, 330f, 262f, 196f }[Mathf.Min(3, (int)(t / 0.2f))] * t) * 0.3f * Env(t, 0.01f, 0.8f));
        }
    }
}
