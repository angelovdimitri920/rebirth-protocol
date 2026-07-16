using UnityEngine;
using static RebirthProtocol.Battle.Audio.SynthClips;
using static RebirthProtocol.Battle.Audio.SynthClips.Voice;

namespace RebirthProtocol.Battle.Audio
{
    // Every combat/UI sound from the prototype's sfx.ts, baked once and
    // played through a small round-robin AudioSource pool.
    public sealed class SfxPlayer : MonoBehaviour
    {
        private AudioSource[] _pool;
        private int _next;

        private void Awake()
        {
            _pool = new AudioSource[8];
            for (var i = 0; i < _pool.Length; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.volume = 0.85f;
                _pool[i] = src;
            }
        }

        private void Play(AudioClip clip)
        {
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.PlayOneShot(clip);
        }

        public void Shot() => Play(Bake("shot", Tone(920, 240, 0.09f, Wave.Square, 0.14f)));
        public void PodShot() => Play(Bake("podShot", Tone(1400, 700, 0.05f, Wave.Square, 0.07f)));
        public void Hit() => Play(Bake("hit", Noise(0.08f, 0.2f, 2400), Tone(300, 90, 0.08f, Wave.Triangle, 0.16f)));
        public void Shielded() => Play(Bake("shielded", Tone(520, 480, 0.1f, Wave.Sine, 0.18f)));
        public void MeleeSwing() => Play(Bake("meleeSwing", Noise(0.12f, 0.12f, 1200)));
        public void MeleeHit() => Play(Bake("meleeHit", Noise(0.1f, 0.25f, 3000), Tone(180, 60, 0.14f, Wave.Sawtooth, 0.2f)));
        public void Clash() => Play(Bake("clash", Tone(1800, 1200, 0.12f, Wave.Square, 0.16f), Noise(0.15f, 0.18f, 5000)));
        public void Explosion() => Play(Bake("explosion", Noise(0.5f, 0.32f, 900), Tone(120, 35, 0.45f, Wave.Sine, 0.3f)));
        public void BombThrow() => Play(Bake("bombThrow", Tone(300, 700, 0.22f, Wave.Sine, 0.09f)));
        public void Dash() => Play(Bake("dash", Noise(0.14f, 0.1f, 1800), Tone(200, 600, 0.12f, Wave.Sine, 0.08f)));
        public void Thrust() => Play(Bake("thrust", Tone(140, 320, 0.22f, Wave.Sine, 0.08f), Noise(0.18f, 0.06f, 900)));
        public void Knockdown() => Play(Bake("knockdown", Tone(400, 60, 0.4f, Wave.Sawtooth, 0.22f), Noise(0.3f, 0.18f, 700)));
        public void Rebirth() => Play(Bake("rebirth", Tone(300, 900, 0.3f, Wave.Sine, 0.16f), Tone(450, 1350, 0.3f, Wave.Sine, 0.1f, 0.05f)));
        public void GuardBreak() => Play(Bake("guardBreak", Tone(900, 100, 0.3f, Wave.Square, 0.2f), Noise(0.25f, 0.2f, 4000)));
        public void Eliminate() => Play(Bake("eliminate", Noise(0.4f, 0.28f, 1600), Tone(500, 40, 0.5f, Wave.Sawtooth, 0.24f), Tone(700, 50, 0.55f, Wave.Sawtooth, 0.16f, 0.06f)));
        public void Overheat() => Play(Bake("overheat", Tone(500, 120, 0.35f, Wave.Sawtooth, 0.18f), Noise(0.25f, 0.1f, 500)));
        public void Land() => Play(Bake("land", Noise(0.06f, 0.12f, 500)));
        public void MashTick() => Play(Bake("mashTick", Tone(900, 900, 0.03f, Wave.Square, 0.06f)));
        public void CrateBreak() => Play(Bake("crateBreak", Noise(0.12f, 0.22f, 1400), Tone(150, 60, 0.1f, Wave.Triangle, 0.12f)));
        public void PodToggle(bool deployed) => Play(deployed
            ? Bake("podOn", Tone(500, 900, 0.14f, Wave.Triangle, 0.12f))
            : Bake("podOff", Tone(750, 400, 0.14f, Wave.Triangle, 0.12f)));
        public void UiClick() => Play(Bake("uiClick", Tone(700, 500, 0.05f, Wave.Square, 0.08f)));

        public void Victory() => Play(Bake("victory",
            Tone(523, 523, 0.22f, Wave.Triangle, 0.16f, 0f),
            Tone(659, 659, 0.22f, Wave.Triangle, 0.16f, 0.12f),
            Tone(784, 784, 0.22f, Wave.Triangle, 0.16f, 0.24f),
            Tone(1047, 1047, 0.22f, Wave.Triangle, 0.16f, 0.36f)));

        public void Defeat() => Play(Bake("defeat",
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
