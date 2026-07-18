using System;
using System.Collections.Generic;

namespace RebirthProtocol.Domain
{
    /// What kind of attack landed — drives the on-hit trigger verbs.
    /// None marks spawned extras (splinter darts) so they can't re-trigger
    /// the boon that created them.
    public enum HitSource
    {
        None,
        Gun,
        Melee,
        Bomb,
        Pod
    }

    public readonly struct OnHitOutcome
    {
        public OnHitOutcome(int splinterDarts)
        {
            SplinterDarts = splinterDarts;
        }

        /// Extra homing darts the presentation layer should spawn at the
        /// hit point (Splinter Rounds).
        public int SplinterDarts { get; }
    }

    public readonly struct OnDashOutcome
    {
        public OnDashOutcome(bool spawnAfterimage)
        {
            SpawnAfterimage = spawnAfterimage;
        }

        public bool SpawnAfterimage { get; }
    }

    // Boons + stacking items (GAME_DESIGN §4) hung off a small set of
    // universal trigger verbs (on-hit / on-knockdown / on-dash) so synergies
    // emerge combinatorially. One instance per run, owned by the player and
    // rebound to the fresh CombatantHealth each fight; the enemy has none
    // (boons apply to the player only). Port of the prototype's effects.ts,
    // with the presentation halves (dart spawning, afterimage meshes)
    // returned as outcomes for the Battle layer to act on.
    public sealed class RunEffects
    {
        public const float SplinterDamage = 8f;
        public const float SplinterEnduranceDamage = 4f;
        public const float AfterimageDamage = 40f;
        public const float AfterimageEnduranceDamage = 20f;

        private readonly Random _rng;
        private readonly HashSet<string> _boons = new HashSet<string>();
        private readonly List<Boon> _boonList = new List<Boon>();
        private readonly Dictionary<string, int> _itemStacks = new Dictionary<string, int>();

        private CombatantHealth _ownerHealth;
        private float _momentumTimer;

        /// Set by the duel each fight so rearm/trigger boons can reach the
        /// weapon cooldowns.
        public Action ResetGunCooldown = () => { };
        public Action ResetBombCooldown = () => { };

        public RunEffects(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public IReadOnlyList<Boon> BoonList => _boonList;
        public IReadOnlyDictionary<string, int> ItemStacks => _itemStacks;

        public bool Has(string boonId) => _boons.Contains(boonId);

        public int Stacks(string itemId) => _itemStacks.TryGetValue(itemId, out var n) ? n : 0;

        public void AddBoon(Boon boon)
        {
            if (_boons.Add(boon.Id))
            {
                _boonList.Add(boon);
            }
        }

        /// Pick up an item mid-fight: stacks increment, and Scrap Plating's
        /// max-HP bonus lands immediately on the bound health.
        public void AddItem(Item item)
        {
            _itemStacks[item.Id] = Stacks(item.Id) + 1;
            if (item.Id == "plating")
            {
                _ownerHealth?.IncreaseMaxHp(40f);
            }
        }

        /// Total plating max-HP bonus, applied by the run flow after each
        /// respawn (a fresh CombatantHealth starts from body stats alone).
        public float PlatingMaxHpBonus => 40f * Stacks("plating");

        /// Rebind to the fresh player health at the start of each fight.
        public void Bind(CombatantHealth ownerHealth)
        {
            _ownerHealth = ownerHealth;
            _momentumTimer = 0f;
        }

        public void Tick(float dt)
        {
            _momentumTimer -= dt;
        }

        // --- Hyperbolic scaling for %-chance stacks (Risk of Rain 2 model,
        // §4): approaches 1 but never trivially hits it. ---
        public static float HyperbolicChance(int stacks, float a)
        {
            return 1f - 1f / (1f + a * stacks);
        }

        /// Deterministic random float in [min, max), drawn from the same
        /// seeded RNG as every other run roll. The Battle layer uses this
        /// for boon-driven spawn offsets (Splinter Rounds dart jitter,
        /// Cluster Shell blast scatter) so RunSeedOverride pins those
        /// outcomes too, not just chance procs — Domain stays engine-
        /// agnostic (no Vector3/UnityEngine here), Battle builds its own
        /// offsets from the floats.
        public float NextFloat(float min, float max)
        {
            return (float)(min + _rng.NextDouble() * (max - min));
        }

        // --- Stat queries used by combat systems ---

        public float GunDamageMult(float boostValue)
        {
            var m = 1f;
            if (Has("overcharge") && boostValue > 70f)
            {
                m *= 1.45f;
            }

            return m;
        }

        public float MeleeDamageMult()
        {
            var m = 1f;
            if (Has("momentum") && _momentumTimer > 0f)
            {
                m *= 1.6f;
            }

            return m;
        }

        public float MeleeShieldMult() => Has("guardcrusher") ? 3f : 1f;

        public float DashCostMult() => Has("slipstream") ? 0.65f : 1f;

        public float PodRegenMult() => Has("overclock") ? 1.8f : 1f;

        public float PodFireIntervalMult() => Has("overclock") ? 0.75f : 1f;

        public float FlatDamageBonus() => 3f * Stacks("impact");

        /// Cluster Shell: follow-up mini-blasts per detonation.
        public int ClusterBlasts => Has("cluster") ? 2 : 0;

        // --- Trigger verbs ---

        public OnHitOutcome OnHit(HitSource source)
        {
            var splinterDarts = 0;
            if (source == HitSource.Gun)
            {
                if (Has("splinter"))
                {
                    splinterDarts = 2;
                }

                var trigger = Stacks("trigger");
                if (trigger > 0 && _rng.NextDouble() < HyperbolicChance(trigger, 0.15f))
                {
                    ResetGunCooldown();
                }
            }

            if (source == HitSource.Pod && Has("vampiric"))
            {
                _ownerHealth?.RestoreEndurance(5f);
            }

            return new OnHitOutcome(splinterDarts);
        }

        public void OnKnockdown()
        {
            if (Has("rearm"))
            {
                ResetBombCooldown();
            }

            var leech = Stacks("leech");
            if (leech > 0)
            {
                _ownerHealth?.Heal(30f * leech);
            }
        }

        public OnDashOutcome OnDash()
        {
            _momentumTimer = 2f;
            var kinetic = Stacks("kinetic");
            if (kinetic > 0)
            {
                _ownerHealth?.RestoreEndurance(7f * kinetic);
            }

            return new OnDashOutcome(Has("afterimage"));
        }
    }
}
