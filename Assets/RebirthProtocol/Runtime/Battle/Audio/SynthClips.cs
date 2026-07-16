using System;
using System.Collections.Generic;
using UnityEngine;

namespace RebirthProtocol.Battle.Audio
{
    // Procedural audio, ported from the prototype's Web Audio synth
    // (src/core/sfx.ts / music.ts): no audio assets, everything baked to
    // PCM AudioClips at load from oscillator + filtered-noise recipes.
    public static class SynthClips
    {
        public enum Wave
        {
            Sine,
            Square,
            Sawtooth,
            Triangle
        }

        public struct Voice
        {
            public bool IsNoise;
            public float Freq;
            public float EndFreq;
            public float Duration;
            public Wave Waveform;
            public float Volume;
            public float Delay;
            public float NoiseLowpass;
            public bool NoiseHighpass; // music hats use highpass instead
            public float Attack; // 0 = sfx-style instant attack

            public static Voice Tone(float freq, float endFreq, float dur, Wave wave, float vol, float delay = 0f, float attack = 0f)
                => new Voice { Freq = freq, EndFreq = endFreq, Duration = dur, Waveform = wave, Volume = vol, Delay = delay, Attack = attack };

            public static Voice Noise(float dur, float vol, float filterFreq, float delay = 0f, bool highpass = false)
                => new Voice { IsNoise = true, Duration = dur, Volume = vol, NoiseLowpass = filterFreq, Delay = delay, NoiseHighpass = highpass };
        }

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static System.Random _noiseRng = new System.Random(99);

        public static AudioClip Bake(string name, params Voice[] voices)
        {
            if (Cache.TryGetValue(name, out var cached) && cached != null)
            {
                return cached;
            }

            const int sampleRate = 44100;
            var total = 0.05f;
            foreach (var v in voices)
            {
                total = Mathf.Max(total, v.Delay + v.Duration + 0.05f);
            }

            var samples = new float[(int)(sampleRate * total)];
            foreach (var v in voices)
            {
                if (v.IsNoise)
                {
                    RenderNoise(samples, sampleRate, v);
                }
                else
                {
                    RenderTone(samples, sampleRate, v);
                }
            }

            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Clamp(samples[i], -1f, 1f);
            }

            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            Cache[name] = clip;
            return clip;
        }

        private static void RenderTone(float[] samples, int sampleRate, Voice v)
        {
            var start = (int)(v.Delay * sampleRate);
            var count = (int)(v.Duration * sampleRate);
            var endFreq = Mathf.Max(20f, v.EndFreq);
            var freqRatio = endFreq / v.Freq;
            var decayRatio = 0.001f / Mathf.Max(0.001f, v.Volume);
            double phase = 0;

            for (var i = 0; i < count && start + i < samples.Length; i++)
            {
                var t01 = (float)i / count;
                // Exponential frequency sweep + exponential gain decay, same
                // shape as Web Audio's exponentialRampToValueAtTime.
                var freq = v.Freq * Mathf.Pow(freqRatio, t01);
                var gain = v.Volume * Mathf.Pow(decayRatio, t01);
                if (v.Attack > 0f)
                {
                    var attackT = i / (v.Attack * sampleRate);
                    if (attackT < 1f)
                    {
                        gain *= attackT;
                    }
                }

                phase += freq / sampleRate;
                samples[start + i] += Sample(v.Waveform, (float)(phase % 1.0)) * gain;
            }
        }

        private static void RenderNoise(float[] samples, int sampleRate, Voice v)
        {
            var start = (int)(v.Delay * sampleRate);
            var count = (int)(v.Duration * sampleRate);
            var decayRatio = 0.001f / Mathf.Max(0.001f, v.Volume);
            // One-pole filter coefficient for the lowpass/highpass cutoff.
            var a = 1f - Mathf.Exp(-2f * Mathf.PI * v.NoiseLowpass / sampleRate);
            var lp = 0f;

            for (var i = 0; i < count && start + i < samples.Length; i++)
            {
                var white = (float)(_noiseRng.NextDouble() * 2.0 - 1.0);
                lp += a * (white - lp);
                var filtered = v.NoiseHighpass ? white - lp : lp;
                var gain = v.Volume * Mathf.Pow(decayRatio, (float)i / count);
                samples[start + i] += filtered * gain;
            }
        }

        private static float Sample(Wave wave, float phase01) => wave switch
        {
            Wave.Square => phase01 < 0.5f ? 1f : -1f,
            Wave.Sawtooth => 2f * phase01 - 1f,
            Wave.Triangle => 2f * Mathf.Abs(2f * phase01 - 1f) - 1f,
            _ => Mathf.Sin(2f * Mathf.PI * phase01)
        };
    }
}
