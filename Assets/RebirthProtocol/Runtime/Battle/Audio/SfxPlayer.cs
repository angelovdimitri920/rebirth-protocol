using UnityEngine;
using static RebirthProtocol.Battle.Audio.SynthClips;
using static RebirthProtocol.Battle.Audio.SynthClips.Voice;

namespace RebirthProtocol.Battle.Audio
{
    // Every combat/UI sound from the prototype's sfx.ts, baked once and
    // played through a small round-robin AudioSource pool. Combat sounds
    // are positional (3D spatial blend, attenuated over the arena's scale)
    // so a hit on the far side of the Holosseum reads as distant; UI/run
    // cues (menu clicks, victory/defeat stingers) stay flat 2D since
    // they're feedback to the player, not something happening in the world.
    public sealed class SfxPlayer : MonoBehaviour
    {
        private const float MinDistance = 4f;
        private const float MaxDistance = 40f; // beyond the 32m arena diagonal

        private AudioSource[] _pool;
        private int _next;
        private AudioSource _uiSource;

        private void Awake()
        {
            _pool = new AudioSource[8];
            for (var i = 0; i < _pool.Length; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.volume = 0.85f;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = MinDistance;
                src.maxDistance = MaxDistance;
                _pool[i] = src;
            }

            _uiSource = gameObject.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.volume = 0.85f;
            _uiSource.spatialBlend = 0f;
        }

        /// Play at a world position (combat/environmental sounds).
        private void PlayAt(AudioClip clip, Vector3 position)
        {
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.transform.position = position;
            src.PlayOneShot(clip);
        }

        /// Play flat/non-positional (UI and run-level feedback).
        private void PlayFlat(AudioClip clip)
        {
            _uiSource.PlayOneShot(clip);
        }

        public void Shot(Vector3 pos) => PlayAt(Bake("shot", Tone(920, 240, 0.09f, Wave.Square, 0.14f)), pos);
        public void PodShot(Vector3 pos) => PlayAt(Bake("podShot", Tone(1400, 700, 0.05f, Wave.Square, 0.07f)), pos);
        public void Hit(Vector3 pos) => PlayAt(Bake("hit", Noise(0.08f, 0.2f, 2400), Tone(300, 90, 0.08f, Wave.Triangle, 0.16f)), pos);
        public void Shielded(Vector3 pos) => PlayAt(Bake("shielded", Tone(520, 480, 0.1f, Wave.Sine, 0.18f)), pos);
        public void MeleeSwing(Vector3 pos) => PlayAt(Bake("meleeSwing", Noise(0.12f, 0.12f, 1200)), pos);
        public void MeleeHit(Vector3 pos) => PlayAt(Bake("meleeHit", Noise(0.1f, 0.25f, 3000), Tone(180, 60, 0.14f, Wave.Sawtooth, 0.2f)), pos);
        public void Clash(Vector3 pos) => PlayAt(Bake("clash", Tone(1800, 1200, 0.12f, Wave.Square, 0.16f), Noise(0.15f, 0.18f, 5000)), pos);
        public void Explosion(Vector3 pos) => PlayAt(Bake("explosion", Noise(0.5f, 0.32f, 900), Tone(120, 35, 0.45f, Wave.Sine, 0.3f)), pos);
        public void BombThrow(Vector3 pos) => PlayAt(Bake("bombThrow", Tone(300, 700, 0.22f, Wave.Sine, 0.09f)), pos);
        public void Dash(Vector3 pos) => PlayAt(Bake("dash", Noise(0.14f, 0.1f, 1800), Tone(200, 600, 0.12f, Wave.Sine, 0.08f)), pos);
        public void Thrust(Vector3 pos) => PlayAt(Bake("thrust", Tone(140, 320, 0.22f, Wave.Sine, 0.08f), Noise(0.18f, 0.06f, 900)), pos);
        public void Knockdown(Vector3 pos) => PlayAt(Bake("knockdown", Tone(400, 60, 0.4f, Wave.Sawtooth, 0.22f), Noise(0.3f, 0.18f, 700)), pos);
        public void Rebirth(Vector3 pos) => PlayAt(Bake("rebirth", Tone(300, 900, 0.3f, Wave.Sine, 0.16f), Tone(450, 1350, 0.3f, Wave.Sine, 0.1f, 0.05f)), pos);
        public void GuardBreak(Vector3 pos) => PlayAt(Bake("guardBreak", Tone(900, 100, 0.3f, Wave.Square, 0.2f), Noise(0.25f, 0.2f, 4000)), pos);
        public void Eliminate(Vector3 pos) => PlayAt(Bake("eliminate", Noise(0.4f, 0.28f, 1600), Tone(500, 40, 0.5f, Wave.Sawtooth, 0.24f), Tone(700, 50, 0.55f, Wave.Sawtooth, 0.16f, 0.06f)), pos);
        public void Overheat(Vector3 pos) => PlayAt(Bake("overheat", Tone(500, 120, 0.35f, Wave.Sawtooth, 0.18f), Noise(0.25f, 0.1f, 500)), pos);
        public void Land(Vector3 pos) => PlayAt(Bake("land", Noise(0.06f, 0.12f, 500)), pos);
        public void MashTick(Vector3 pos) => PlayAt(Bake("mashTick", Tone(900, 900, 0.03f, Wave.Square, 0.06f)), pos);
        public void CrateBreak(Vector3 pos) => PlayAt(Bake("crateBreak", Noise(0.12f, 0.22f, 1400), Tone(150, 60, 0.1f, Wave.Triangle, 0.12f)), pos);
        public void HazardSizzle(Vector3 pos) => PlayAt(Bake("hazardSizzle", Noise(0.25f, 0.08f, 3000)), pos);
        public void PodToggle(bool deployed, Vector3 pos) => PlayAt(deployed
            ? Bake("podOn", Tone(500, 900, 0.14f, Wave.Triangle, 0.12f))
            : Bake("podOff", Tone(750, 400, 0.14f, Wave.Triangle, 0.12f)), pos);

        // --- UI / run: flat, always clearly audible regardless of camera ---
        public void UiClick() => PlayFlat(Bake("uiClick", Tone(700, 500, 0.05f, Wave.Square, 0.08f)));

        public void Victory() => PlayFlat(Bake("victory",
            Tone(523, 523, 0.22f, Wave.Triangle, 0.16f, 0f),
            Tone(659, 659, 0.22f, Wave.Triangle, 0.16f, 0.12f),
            Tone(784, 784, 0.22f, Wave.Triangle, 0.16f, 0.24f),
            Tone(1047, 1047, 0.22f, Wave.Triangle, 0.16f, 0.36f)));

        public void Defeat() => PlayFlat(Bake("defeat",
            Tone(392, 368, 0.3f, Wave.Sawtooth, 0.13f, 0f),
            Tone(330, 310, 0.3f, Wave.Sawtooth, 0.13f, 0.15f),
            Tone(262, 246, 0.3f, Wave.Sawtooth, 0.13f, 0.3f),
            Tone(196, 184, 0.3f, Wave.Sawtooth, 0.13f, 0.45f)));
    }

    /// Static access point so combat code can fire sounds without threading
    /// a reference through every class. Null-safe: silent when unset.
    public static class GameAudio
    {
        public static SfxPlayer Sfx;
    }
}
