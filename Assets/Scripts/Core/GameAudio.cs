using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    public enum SoundId
    {
        Hit,
        Pop,
        Boom,
        Coin,
        Shotgun,
        Fork,
        Spray,
        Moo,
        CowLost,
        WaveStart,
        Win,
        Lose,
        LevelUp,
        Build,
        Upgrade,
        Click,
        Tractor,
        Freeze,
        Buzz,
        Wind,
        Zap,
        Splat,
        Bark,
    }

    public enum MusicId
    {
        Farm,
        Level,
    }

    /// <summary>
    /// Geräusche und Musik, komplett per Code erzeugt (Sinus, Rechteck, Rauschen) – keine Sounddateien nötig.
    /// Später lassen sich die erzeugten Klänge durch echte Aufnahmen ersetzen.
    /// </summary>
    public static class GameAudio
    {
        const int Rate = 22050;
        const int PoolSize = 10;
        const float MinRepeatInterval = 0.06f;

        static GameObject root;
        static AudioSource[] pool;
        static int next;
        static AudioSource music;
        static MusicId? currentMusic;
        static readonly Dictionary<SoundId, AudioClip> clips = new Dictionary<SoundId, AudioClip>();
        static readonly Dictionary<MusicId, AudioClip> songs = new Dictionary<MusicId, AudioClip>();
        static readonly Dictionary<SoundId, float> lastPlayed = new Dictionary<SoundId, float>();

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt("ton", 1) == 1;
            set { PlayerPrefs.SetInt("ton", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt("musik", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("musik", value ? 1 : 0);
                PlayerPrefs.Save();
                Ensure();
                if (value && currentMusic.HasValue) StartSong(currentMusic.Value);
                else music.Stop();
            }
        }

        static void Ensure()
        {
            if (root != null) return;
            root = new GameObject("Audio");
            Object.DontDestroyOnLoad(root);
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                pool[i] = root.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
            music = root.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.22f;
            music.playOnAwake = false;
        }

        public static void Play(SoundId id, float volume = 1f)
        {
            if (!SoundOn) return;
            Ensure();
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < MinRepeatInterval) return;
            lastPlayed[id] = now;
            if (!clips.TryGetValue(id, out var clip))
            {
                clip = MakeClip(id.ToString(), Synthesize(id));
                clips[id] = clip;
            }
            var src = pool[next];
            next = (next + 1) % PoolSize;
            src.pitch = Random.Range(0.94f, 1.06f);
            src.volume = Mathf.Clamp01(volume * BaseVolume(id));
            src.clip = clip;
            src.Play();
        }

        public static void PlayMusic(MusicId id)
        {
            Ensure();
            if (currentMusic == id && music.isPlaying) return;
            currentMusic = id;
            if (MusicOn) StartSong(id);
        }

        static void StartSong(MusicId id)
        {
            if (!songs.TryGetValue(id, out var song))
            {
                song = MakeClip("Musik " + id, ComposeMusic(id));
                songs[id] = song;
            }
            music.clip = song;
            music.Play();
        }

        static float BaseVolume(SoundId id)
        {
            switch (id)
            {
                case SoundId.Hit: return 0.25f;
                case SoundId.Fork: return 0.3f;
                case SoundId.Spray: return 0.2f;
                case SoundId.Coin: return 0.35f;
                case SoundId.Click: return 0.4f;
                case SoundId.Buzz: return 0.25f;
                case SoundId.Shotgun: return 0.45f;
                default: return 0.6f;
            }
        }

        static AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------------------------------------------------------------- Bausteine der Klangerzeugung

        static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        /// <summary>Hüllkurve: kurzer Anstieg, dann exponentielles Abklingen.</summary>
        static float Env(float t, float duration, float attack = 0.005f, float decayPower = 3f)
        {
            if (t < attack) return t / attack;
            float k = Mathf.Clamp01((t - attack) / Mathf.Max(0.0001f, duration - attack));
            return Mathf.Pow(1f - k, decayPower);
        }

        static float Sine(float phase) => Mathf.Sin(phase * 2f * Mathf.PI);
        static float Square(float phase) => Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f;
        static float Saw(float phase) => Mathf.Repeat(phase, 1f) * 2f - 1f;
        static float Tri(float phase) => Mathf.Abs(Mathf.Repeat(phase, 1f) * 4f - 2f) - 1f;

        static System.Random noiseRng = new System.Random(1);
        static float Noise() => (float)noiseRng.NextDouble() * 2f - 1f;

        /// <summary>Ton mit Frequenz-Verlauf (von → bis) in einen Puffer mischen.</summary>
        static void AddTone(float[] buf, float start, float duration, float freqFrom, float freqTo, float volume,
            System.Func<float, float> wave, float decayPower = 3f, float attack = 0.005f, float vibrato = 0f)
        {
            int s0 = Mathf.FloorToInt(start * Rate);
            int n = Mathf.Min(buf.Length - s0, Mathf.CeilToInt(duration * Rate));
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = t / duration;
                float f = Mathf.Lerp(freqFrom, freqTo, k) * (1f + vibrato * Mathf.Sin(t * 2f * Mathf.PI * 5.5f));
                phase += f / Rate;
                buf[s0 + i] += wave(phase) * volume * Env(t, duration, attack, decayPower);
            }
        }

        /// <summary>Gefiltertes Rauschen (cutoff 0..1, klein = dumpf).</summary>
        static void AddNoise(float[] buf, float start, float duration, float volume, float cutoffFrom, float cutoffTo, float decayPower = 3f)
        {
            int s0 = Mathf.FloorToInt(start * Rate);
            int n = Mathf.Min(buf.Length - s0, Mathf.CeilToInt(duration * Rate));
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float a = Mathf.Lerp(cutoffFrom, cutoffTo, t / duration);
                y += a * (Noise() - y);
                buf[s0 + i] += y * volume * Env(t, duration, 0.002f, decayPower);
            }
        }

        static float[] Normalize(float[] buf, float peak = 0.9f)
        {
            float max = 0.0001f;
            foreach (float v in buf) max = Mathf.Max(max, Mathf.Abs(v));
            float g = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
            return buf;
        }

        // ---------------------------------------------------------------- Geräusche

        static float[] Synthesize(SoundId id)
        {
            float[] b;
            switch (id)
            {
                case SoundId.Hit:
                    b = Buffer(0.09f);
                    AddNoise(b, 0f, 0.08f, 0.8f, 0.6f, 0.1f);
                    AddTone(b, 0f, 0.08f, 180f, 90f, 0.6f, Sine);
                    break;
                case SoundId.Pop:
                    b = Buffer(0.14f);
                    AddTone(b, 0f, 0.13f, 900f, 180f, 1f, Sine);
                    AddNoise(b, 0f, 0.05f, 0.4f, 0.8f, 0.3f);
                    break;
                case SoundId.Boom:
                    b = Buffer(0.7f);
                    AddNoise(b, 0f, 0.65f, 1f, 0.25f, 0.02f, 2f);
                    AddTone(b, 0f, 0.5f, 90f, 40f, 0.8f, Sine, 2f);
                    break;
                case SoundId.Coin:
                    b = Buffer(0.22f);
                    AddTone(b, 0f, 0.07f, 988f, 988f, 0.5f, Square, 1f);
                    AddTone(b, 0.06f, 0.15f, 1319f, 1319f, 0.5f, Square, 2f);
                    break;
                case SoundId.Shotgun:
                    b = Buffer(0.3f);
                    AddNoise(b, 0f, 0.28f, 1f, 0.9f, 0.05f, 4f);
                    AddTone(b, 0f, 0.12f, 140f, 60f, 0.7f, Sine);
                    break;
                case SoundId.Zap:
                    b = Buffer(0.22f);
                    AddNoise(b, 0f, 0.2f, 0.7f, 0.95f, 0.02f, 3f);
                    AddTone(b, 0f, 0.2f, 1400f, 300f, 0.35f, Square, 2f);
                    break;
                case SoundId.Splat:
                    b = Buffer(0.25f);
                    AddNoise(b, 0f, 0.22f, 0.9f, 0.15f, 0.2f, 2f);
                    AddTone(b, 0f, 0.15f, 160f, 70f, 0.5f, Sine);
                    break;
                case SoundId.Bark:
                    b = Buffer(0.2f);
                    AddTone(b, 0f, 0.08f, 520f, 380f, 0.7f, Saw, 2f);
                    AddTone(b, 0.1f, 0.08f, 560f, 400f, 0.6f, Saw, 2f);
                    AddNoise(b, 0f, 0.18f, 0.3f, 0.5f, 0.1f);
                    break;
                case SoundId.Fork:
                    b = Buffer(0.14f);
                    AddNoise(b, 0f, 0.13f, 0.8f, 0.05f, 0.5f, 1.5f);
                    break;
                case SoundId.Spray:
                    b = Buffer(0.3f);
                    AddNoise(b, 0f, 0.28f, 0.8f, 0.7f, 0.4f, 1.2f);
                    break;
                case SoundId.Moo:
                    b = Moo(1f);
                    break;
                case SoundId.CowLost:
                    b = Moo(0.75f);
                    AddTone(b, 0.5f, 0.6f, 440f, 220f, 0.25f, Tri, 1.5f);
                    break;
                case SoundId.WaveStart:
                    b = Buffer(0.9f);
                    AddTone(b, 0f, 0.85f, 220f, 230f, 0.5f, Saw, 1.2f, 0.06f, 0.01f);
                    AddTone(b, 0f, 0.85f, 330f, 345f, 0.35f, Saw, 1.2f, 0.06f, 0.01f);
                    break;
                case SoundId.Win:
                    b = Buffer(1.3f);
                    float[] win = { 523f, 659f, 784f, 1047f };
                    for (int i = 0; i < win.Length; i++) AddTone(b, i * 0.15f, i == 3 ? 0.7f : 0.2f, win[i], win[i], 0.45f, Square, 1.5f);
                    break;
                case SoundId.Lose:
                    b = Buffer(1.2f);
                    float[] lose = { 392f, 330f, 262f, 196f };
                    for (int i = 0; i < lose.Length; i++) AddTone(b, i * 0.22f, 0.4f, lose[i], lose[i] * 0.97f, 0.45f, Tri, 1.5f);
                    break;
                case SoundId.LevelUp:
                    b = Buffer(0.6f);
                    float[] up = { 523f, 784f, 1047f, 1568f };
                    for (int i = 0; i < up.Length; i++) AddTone(b, i * 0.08f, 0.25f, up[i], up[i], 0.4f, Square, 2f);
                    break;
                case SoundId.Build:
                    b = Buffer(0.4f);
                    AddNoise(b, 0f, 0.06f, 1f, 0.3f, 0.1f);
                    AddTone(b, 0f, 0.07f, 220f, 140f, 0.6f, Sine);
                    AddNoise(b, 0.18f, 0.06f, 1f, 0.3f, 0.1f);
                    AddTone(b, 0.18f, 0.07f, 240f, 150f, 0.6f, Sine);
                    break;
                case SoundId.Upgrade:
                    b = Buffer(0.5f);
                    AddTone(b, 0f, 0.45f, 400f, 1200f, 0.5f, Tri, 1.5f);
                    AddTone(b, 0.05f, 0.4f, 600f, 1800f, 0.25f, Sine, 1.5f);
                    break;
                case SoundId.Click:
                    b = Buffer(0.04f);
                    AddTone(b, 0f, 0.03f, 1500f, 1200f, 0.6f, Square, 4f);
                    break;
                case SoundId.Tractor:
                    b = Buffer(1.6f);
                    AddTone(b, 0f, 1.55f, 38f, 52f, 0.8f, p => Saw(p) * (Mathf.Repeat(p * 0.25f, 1f) < 0.5f ? 1f : 0.4f), 0.6f, 0.05f);
                    AddNoise(b, 0f, 1.5f, 0.3f, 0.15f, 0.2f, 0.8f);
                    break;
                case SoundId.Freeze:
                    b = Buffer(0.7f);
                    for (int i = 0; i < 5; i++) AddTone(b, i * 0.08f, 0.3f, 2400f - i * 250f, 2000f - i * 250f, 0.25f, Sine, 2f);
                    AddNoise(b, 0f, 0.6f, 0.2f, 0.9f, 0.6f, 2f);
                    break;
                case SoundId.Buzz:
                    b = Buffer(0.35f);
                    AddTone(b, 0f, 0.34f, 210f, 230f, 0.5f, Saw, 1f, 0.02f, 0.04f);
                    break;
                case SoundId.Wind:
                    b = Buffer(0.6f);
                    AddNoise(b, 0f, 0.58f, 0.9f, 0.05f, 0.25f, 1f);
                    break;
                default:
                    b = Buffer(0.05f);
                    break;
            }
            return Normalize(b);
        }

        /// <summary>„Muuh“: tiefer, leicht schwankender Ton mit dumpfer Klangfarbe.</summary>
        static float[] Moo(float pitch)
        {
            var b = Buffer(1.1f);
            int n = b.Length;
            float phase = 0f, y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = (125f - 30f * t) * pitch * (1f + 0.015f * Mathf.Sin(t * 2f * Mathf.PI * 6f));
                phase += f / Rate;
                float raw = Saw(phase);
                // Klangfarbe öffnet sich zu Beginn ("m" → "uh") und schließt wieder
                float cutoff = 0.05f + 0.12f * Mathf.Sin(Mathf.Clamp01(t / 1.05f) * Mathf.PI);
                y += cutoff * (raw - y);
                float env = Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((1.08f - t) / 0.25f);
                b[i] = y * env;
            }
            return b;
        }

        // ---------------------------------------------------------------- Musik

        /// <summary>Kurze Musikschleife im Chiptune-Stil: Bass, Begleitung und Melodie (C-Dur pentatonisch).</summary>
        static float[] ComposeMusic(MusicId id)
        {
            bool level = id == MusicId.Level;
            float bpm = level ? 124f : 96f;
            float beat = 60f / bpm;
            const int bars = 8;
            var b = Buffer(bars * 4 * beat + 0.01f);

            // Akkordfolge C – Am – F – G (zwei Durchläufe)
            float[] roots = { 130.81f, 110f, 87.31f, 98f };
            int[][] chordSteps = { new[] { 0, 4, 7 }, new[] { 0, 3, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 } };
            float[] scale = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f };
            var rng = new System.Random(level ? 7 : 3);

            for (int bar = 0; bar < bars; bar++)
            {
                float barStart = bar * 4 * beat;
                int c = bar % 4;
                float root = roots[c];
                // Bass: Grundton auf 1 und 3, Quinte auf 2 und 4
                for (int q = 0; q < 4; q++)
                {
                    float f = q % 2 == 0 ? root : root * 1.5f;
                    AddTone(b, barStart + q * beat, beat * 0.9f, f, f, level ? 0.32f : 0.26f, Tri, 2f);
                }
                // Begleitung: gebrochener Akkord in Achteln
                for (int e = 0; e < 8; e++)
                {
                    int step = chordSteps[c][e % 3];
                    float f = root * 2f * Mathf.Pow(2f, step / 12f);
                    AddTone(b, barStart + e * beat * 0.5f, beat * 0.45f, f, f, 0.07f, Square, 3f);
                }
                // Melodie: zufällige, aber feste Folge aus der Tonleiter
                int notes = level ? 8 : 4;
                for (int m = 0; m < notes; m++)
                {
                    if (rng.NextDouble() < 0.25) continue;
                    float f = scale[rng.Next(scale.Length)];
                    float len = 4f * beat / notes;
                    AddTone(b, barStart + m * len, len * 0.85f, f, f, 0.12f, p => Square(p) * 0.6f + Tri(p) * 0.4f, 2f);
                }
                if (level)
                {
                    // Schlagzeug: Bassdrum auf jedem Schlag, Hi-Hat dazwischen
                    for (int q = 0; q < 4; q++)
                    {
                        AddTone(b, barStart + q * beat, 0.12f, 120f, 45f, 0.35f, Sine, 3f);
                        AddNoise(b, barStart + (q + 0.5f) * beat, 0.04f, 0.12f, 0.9f, 0.9f, 4f);
                    }
                }
            }
            return Normalize(b, 0.8f);
        }
    }
}
