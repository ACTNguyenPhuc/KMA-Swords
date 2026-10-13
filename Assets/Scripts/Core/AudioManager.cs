using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    /// <summary>
    /// Âm thanh hiệu ứng được tổng hợp bằng mã (không cần tệp âm thanh), kèm rung nhẹ trên Android.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SR = 22050;
        static AudioManager inst;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        AudioSource[] pool;
        int next;
        static float lastVibrate;
        System.Random rnd = new System.Random(12345);

        static AudioManager Get()
        {
            if (inst != null) return inst;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AudioManager>();
            inst.Build();
            return inst;
        }

        public static void Warmup() { Get(); }

        public static void Play(string name, float volume = 1f)
        {
            if (!SaveManager.Sfx) return;
            var a = Get();
            if (!a.clips.TryGetValue(name, out var clip)) return;
            float now = Time.unscaledTime;
            if (a.lastPlay.TryGetValue(name, out float last) && now - last < 0.06f) return;
            a.lastPlay[name] = now;
            var src = a.pool[a.next];
            a.next = (a.next + 1) % a.pool.Length;
            src.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            src.PlayOneShot(clip, volume);
        }

        public static void Vibrate()
        {
            if (!SaveManager.Vibrate) return;
            if (Time.unscaledTime - lastVibrate < 1.5f) return;
            lastVibrate = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        void Build()
        {
            pool = new AudioSource[10];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].ignoreListenerPause = true;
            }

            Add("click", 0.08f, t => Sin(880, t) * Exp(t, 45) * 0.5f);
            Add("select", 0.1f, t => Sin(700 + 1800 * t, t) * Exp(t, 30) * 0.45f);
            Add("command", 0.13f, t => (Sin(520, t) * Exp(t, 25) + 0.5f * Sin(780, t) * Exp(t, 30)) * 0.45f);
            Add("hit", 0.16f, t => (Noise() * Exp(t, 35) * 0.55f + Sin(140, t) * Exp(t, 20) * 0.55f));
            Add("heavy", 0.35f, t => (Noise() * Exp(t, 14) * 0.6f + Sin(75, t) * Exp(t, 8) * 0.8f));
            AddFiltered("arrow", 0.16f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.16f) * 0.5f, 0.35f);
            Add("coin", 0.2f, t => Square(t < 0.06f ? 1320 : 1760, t) * Exp(t, 14) * 0.18f);
            Add("build", 0.32f, t => (Sin(170, t) * Exp(t, 30) + (t > 0.13f ? Sin(150, t - 0.13f) * Exp(t - 0.13f, 30) : 0f) + Noise() * Exp(t, 50) * 0.3f) * 0.6f);
            AddFiltered("horn", 1.0f, t => (Saw(110 + 3 * Mathf.Sin(t * 30), t) + 0.6f * Saw(165, t)) * Env(t, 0.08f, 1.0f, 0.3f) * 0.35f, 0.12f);
            AddFiltered("boss", 1.2f, t => (Saw(62, t) + Noise() * 0.4f) * Env(t, 0.1f, 1.2f, 0.5f) * 0.5f, 0.08f);
            AddFiltered("explode", 0.7f, t => (Noise() * 0.9f + Sin(55, t)) * Exp(t, 5) * 0.8f, 0.12f);
            Add("heal", 0.4f, t => (Sin(880, t) * Exp(t, 8) + (t > 0.08f ? Sin(1175, t) * Exp(t - 0.08f, 8) : 0) + (t > 0.16f ? Sin(1480, t) * Exp(t - 0.16f, 8) : 0)) * 0.18f);
            Add("spawn", 0.22f, t => Sin(300 + 900 * t, t) * Exp(t, 10) * 0.35f);
            Add("die", 0.25f, t => Sin(520 - 900 * t, t) * Exp(t, 9) * 0.35f);
            Add("die_ally", 0.35f, t => Sin(330 - 400 * t, t) * Exp(t, 7) * 0.4f);
            Add("error", 0.2f, t => Square(180, t) * (t < 0.07f || (t > 0.11f && t < 0.18f) ? 0.22f : 0f));
            Add("cleared", 0.6f, t => Arp(t, new[] { 523f, 659f, 784f }, 0.12f) * 0.3f);
            Add("victory", 1.4f, t => Arp(t, new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.16f) * 0.32f);
            Add("defeat", 1.3f, t => Arp(t, new[] { 392f, 311f, 262f, 196f }, 0.28f) * 0.32f);
            Add("page", 0.12f, t => Noise() * Env(t, 0.02f, 0.12f, 0.08f) * 0.2f);
        }

        void Add(string name, float dur, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * SR);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
            Fade(d);
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(d, 0);
            clips[name] = clip;
        }

        void AddFiltered(string name, float dur, Func<float, float> f, float alpha)
        {
            int n = Mathf.CeilToInt(dur * SR);
            var d = new float[n];
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                y += alpha * (f(i / (float)SR) - y);
                d[i] = Mathf.Clamp(y * 1.6f, -1f, 1f);
            }
            Fade(d);
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(d, 0);
            clips[name] = clip;
        }

        static void Fade(float[] d)
        {
            int f = Mathf.Min(200, d.Length / 4);
            for (int i = 0; i < f; i++)
            {
                d[d.Length - 1 - i] *= i / (float)f;
                d[i] *= i / (float)f;
            }
        }

        float Noise() => (float)(rnd.NextDouble() * 2.0 - 1.0);
        static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
        static float Square(float hz, float t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * hz * t));
        static float Saw(float hz, float t) => 2f * (t * hz - Mathf.Floor(t * hz + 0.5f));
        static float Exp(float t, float k) => Mathf.Exp(-t * k);

        static float Env(float t, float attack, float total, float release)
        {
            if (t < attack) return t / attack;
            if (t > total - release) return Mathf.Max(0f, (total - t) / release);
            return 1f;
        }

        static float Arp(float t, float[] notes, float step)
        {
            int i = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / step));
            float lt = t - i * step;
            float tail = i == notes.Length - 1 ? 3f : 9f;
            return (Mathf.Sin(2f * Mathf.PI * notes[i] * t) * 0.7f + Mathf.Sin(4f * Mathf.PI * notes[i] * t) * 0.15f) * Mathf.Exp(-lt * tail) * Mathf.Min(1f, lt * 60f);
        }
    }
}
