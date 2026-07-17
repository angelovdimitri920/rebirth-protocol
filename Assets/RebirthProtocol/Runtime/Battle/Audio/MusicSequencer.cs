using UnityEngine;
using static RebirthProtocol.Battle.Audio.SynthClips;
using static RebirthProtocol.Battle.Audio.SynthClips.Voice;

namespace RebirthProtocol.Battle.Audio
{
    public enum MusicMode
    {
        None,
        Hangar,
        Combat
    }

    // Procedural background music, ported from the prototype's music.ts:
    // a step-sequencer scheduled against the DSP clock (Unity's equivalent
    // of the Web Audio lookahead scheduler — schedule with PlayScheduled at
    // absolute AudioSettings.dspTime so timing doesn't jitter with the
    // frame loop). Two loops over a shared 4-bar Am-F-C-G progression:
    // a calm 84 BPM hangar pad and a driving 128 BPM combat loop.
    public sealed class MusicSequencer : MonoBehaviour
    {
        private const int StepsPerBar = 16;
        private const int BarsPerProgression = 4;
        private const float CombatBpm = 128f;
        private const float HangarBpm = 84f;
        private const double ScheduleAheadSec = 0.15;

        // Three 4-bar minor-key progressions the combat loop rotates through
        // per fight, so back-to-back duels don't all sound like one track.
        private static readonly float[][] Progressions =
        {
            new[] { 110f, 87.31f, 130.81f, 98f },   // A minor: i - VI - III - VII
            new[] { 82.41f, 110f, 98f, 73.42f },    // E minor lean
            new[] { 73.42f, 87.31f, 116.54f, 98f }, // D minor lean
        };

        private static int _trackCounter;
        private float[] _roots = Progressions[0];
        private int _trackId; // included in clip cache keys so progressions don't collide

        private AudioSource[] _pool;
        private int _next;
        private MusicMode _mode = MusicMode.None;
        private double _nextNoteTime;
        private int _step;
        private int _bar;

        private void Awake()
        {
            _pool = new AudioSource[12];
            for (var i = 0; i < _pool.Length; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.volume = 0.6f;
                _pool[i] = src;
            }
        }

        /// Switch tracks; no-op if already playing the requested mode.
        public void Play(MusicMode mode)
        {
            if (_mode == mode)
            {
                return;
            }

            // Combat rotates to the next progression each time it starts;
            // the hangar always uses the first so it feels like "home base".
            _trackId = mode == MusicMode.Combat ? _trackCounter++ % Progressions.Length : 0;
            _roots = Progressions[_trackId];

            _mode = mode;
            _step = 0;
            _bar = 0;
            _nextNoteTime = AudioSettings.dspTime + 0.1;
        }

        public void Stop()
        {
            _mode = MusicMode.None;
        }

        private void Update()
        {
            if (_mode == MusicMode.None)
            {
                return;
            }

            var bpm = _mode == MusicMode.Combat ? CombatBpm : HangarBpm;
            var secondsPerStep = 60.0 / bpm / 4.0; // 16th notes

            while (_nextNoteTime < AudioSettings.dspTime + ScheduleAheadSec)
            {
                if (_mode == MusicMode.Combat)
                {
                    ScheduleCombatStep(_step, _bar, _nextNoteTime);
                }
                else
                {
                    ScheduleHangarStep(_step, _bar, _nextNoteTime);
                }

                _nextNoteTime += secondsPerStep;
                _step += 1;
                if (_step >= StepsPerBar)
                {
                    _step = 0;
                    _bar = (_bar + 1) % BarsPerProgression;
                }
            }
        }

        private void Schedule(AudioClip clip, double time)
        {
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.clip = clip;
            src.PlayScheduled(time);
        }

        // --- Combat: four-on-the-floor kick, backbeat snare, driving bass,
        // sparse arp ---
        private void ScheduleCombatStep(int step, int bar, double time)
        {
            var root = _roots[bar];

            if (step % 4 == 0)
            {
                Schedule(Bake("m_kick", Tone(130, 45, 0.12f, Wave.Sine, 0.32f, 0f, 0.005f), Noise(0.03f, 0.14f, 200)), time);
            }

            // Snare/clap on beats 2 and 4 for drive (root-independent key).
            if (step == 4 || step == 12)
            {
                Schedule(Bake("m_snare", Noise(0.09f, 0.16f, 2000, 0f, highpass: true), Noise(0.04f, 0.1f, 5000, 0f, highpass: true)), time);
            }

            var hatAccent = step % 4 == 2;
            Schedule(Bake(hatAccent ? "m_hatA" : "m_hat", Noise(0.03f, hatAccent ? 0.08f : 0.04f, 7000, 0f, highpass: true)), time);

            if (step % 2 == 0)
            {
                Schedule(Bake($"m_bass{_trackId}_{bar}", Tone(root, root * 0.98f, 0.16f, Wave.Sawtooth, 0.14f, 0f, 0.008f)), time);
            }

            // Lead fill on the odd bars only, so two of every four bars breathe.
            if ((bar == 1 || bar == 3) && step % 4 == 2)
            {
                var third = root * Mathf.Pow(2f, 3f / 12f) * 2f;
                var fifth = root * Mathf.Pow(2f, 7f / 12f) * 2f;
                var notes = new[] { root * 2f, third, fifth, third };
                var slot = step / 4 % notes.Length;
                Schedule(Bake($"m_lead{_trackId}_{bar}_{slot}", Tone(notes[slot], notes[slot], 0.16f, Wave.Triangle, 0.1f, 0f, 0.01f)), time);
            }
        }

        // --- Hangar: sustained detuned pad + sparse bell arpeggio ---
        private void ScheduleHangarStep(int step, int bar, double time)
        {
            var root = _roots[bar];

            if (step == 0)
            {
                Schedule(Bake($"m_pad{_trackId}_{bar}",
                    Tone(root, root, 3.6f, Wave.Sine, 0.05f, 0f, 0.4f),
                    Tone(root * 1.004f, root * 1.004f, 3.6f, Wave.Sine, 0.035f, 0f, 0.4f),
                    Tone(root * Mathf.Pow(2f, 7f / 12f), root * Mathf.Pow(2f, 7f / 12f), 3.4f, Wave.Triangle, 0.022f, 0f, 0.4f)), time);
            }

            if (step == 4 || step == 9 || step == 13)
            {
                var scale = new[] { root * 2f, root * 2f * Mathf.Pow(2f, 3f / 12f), root * 2f * Mathf.Pow(2f, 7f / 12f) };
                var index = step == 4 ? 0 : step == 9 ? 1 : 2;
                var note = scale[index];
                Schedule(Bake($"m_bell{_trackId}_{bar}_{index}", Tone(note, note * 0.995f, 0.6f, Wave.Sine, 0.055f, 0f, 0.015f)), time);
            }
        }
    }
}
