using System;
using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class RunDomainTests
    {
        // --- RunState ---

        [Test]
        public void EnemyPowerMultEscalatesFlatPerFight()
        {
            Assert.That(RunState.EnemyPowerMult(0), Is.EqualTo(1f));
            Assert.That(RunState.EnemyPowerMult(4), Is.EqualTo(1.48f).Within(0.0001f));
        }

        [Test]
        public void StartingHpIsFullOnAFreshRunAndCarriedPlusHealAfterwards()
        {
            var run = new RunState();
            Assert.That(run.StartingHp(1000f), Is.EqualTo(1000f));

            run.CarriedHp = 400f;
            Assert.That(run.StartingHp(1000f), Is.EqualTo(550f), "carried 400 + 15% of max");

            run.CarriedHp = 950f;
            Assert.That(run.StartingHp(1000f), Is.EqualTo(1000f), "heal caps at max");
        }

        [Test]
        public void FinalFightIsTheFifth()
        {
            var run = new RunState { FightIndex = 3 };
            Assert.That(run.IsFinalFight, Is.False);
            run.FightIndex = 4;
            Assert.That(run.IsFinalFight, Is.True);
        }

        // --- Hyperbolic scaling ---

        [Test]
        public void HyperbolicChanceApproachesButNeverReachesOne()
        {
            Assert.That(RunEffects.HyperbolicChance(0, 0.15f), Is.EqualTo(0f));
            Assert.That(RunEffects.HyperbolicChance(3, 0.15f), Is.EqualTo(1f - 1f / 1.45f).Within(0.0001f));
            Assert.That(RunEffects.HyperbolicChance(1000, 0.15f), Is.LessThan(1f));
        }

        // --- RunEffects stat queries ---

        [Test]
        public void OverchargeBoostsGunDamageOnlyAboveTheBoostGate()
        {
            var fx = NewEffects();
            Assert.That(fx.GunDamageMult(80f), Is.EqualTo(1f), "no boon, no bonus");

            fx.AddBoon(BoonById("overcharge"));
            Assert.That(fx.GunDamageMult(80f), Is.EqualTo(1.45f));
            Assert.That(fx.GunDamageMult(50f), Is.EqualTo(1f), "gauge at or below 70 pays nothing");
        }

        [Test]
        public void MomentumEdgeWindowsMeleeDamageForTwoSecondsAfterADash()
        {
            var fx = NewEffects();
            fx.AddBoon(BoonById("momentum"));
            Assert.That(fx.MeleeDamageMult(), Is.EqualTo(1f), "no dash yet");

            fx.OnDash();
            Assert.That(fx.MeleeDamageMult(), Is.EqualTo(1.6f));

            fx.Tick(2.5f);
            Assert.That(fx.MeleeDamageMult(), Is.EqualTo(1f), "window expired");
        }

        [Test]
        public void SimpleBoonMultipliersMatchTheCatalogNumbers()
        {
            var fx = NewEffects();
            fx.AddBoon(BoonById("guardcrusher"));
            fx.AddBoon(BoonById("slipstream"));
            fx.AddBoon(BoonById("overclock"));
            fx.AddBoon(BoonById("cluster"));

            Assert.That(fx.MeleeShieldMult(), Is.EqualTo(3f));
            Assert.That(fx.DashCostMult(), Is.EqualTo(0.65f));
            Assert.That(fx.PodRegenMult(), Is.EqualTo(1.8f));
            Assert.That(fx.PodFireIntervalMult(), Is.EqualTo(0.75f));
            Assert.That(fx.ClusterBlasts, Is.EqualTo(2));
        }

        [Test]
        public void ImpactConverterAddsFlatDamagePerStack()
        {
            var fx = NewEffects();
            Assert.That(fx.FlatDamageBonus(), Is.EqualTo(0f));
            fx.AddItem(ItemById("impact"));
            fx.AddItem(ItemById("impact"));
            Assert.That(fx.FlatDamageBonus(), Is.EqualTo(6f));
        }

        // --- Items on the bound health ---

        [Test]
        public void ScrapPlatingRaisesMaxAndCurrentHpImmediately()
        {
            var fx = NewEffects();
            var health = new CombatantHealth();
            fx.Bind(health);
            health.TakeHit(100f, 0f);

            fx.AddItem(ItemById("plating"));

            Assert.That(health.MaxHp, Is.EqualTo(1040f));
            Assert.That(health.Hp, Is.EqualTo(940f), "the +40 lands on current HP too");
            Assert.That(fx.PlatingMaxHpBonus, Is.EqualTo(40f));
        }

        [Test]
        public void KineticCellRestoresEnduranceOnDash()
        {
            var fx = NewEffects();
            var health = new CombatantHealth();
            fx.Bind(health);
            fx.AddItem(ItemById("kinetic"));
            fx.AddItem(ItemById("kinetic"));
            health.TakeHit(0f, 50f);

            fx.OnDash();

            Assert.That(health.Endurance, Is.EqualTo(health.MaxEndurance - 50f + 14f));
        }

        [Test]
        public void VampiricRelayFeedsEnduranceOnPodHitsOnly()
        {
            var fx = NewEffects();
            var health = new CombatantHealth();
            fx.Bind(health);
            fx.AddBoon(BoonById("vampiric"));
            health.TakeHit(0f, 50f);

            fx.OnHit(HitSource.Gun);
            Assert.That(health.Endurance, Is.EqualTo(health.MaxEndurance - 50f), "gun hits feed nothing");

            fx.OnHit(HitSource.Pod);
            Assert.That(health.Endurance, Is.EqualTo(health.MaxEndurance - 50f + 5f));
        }

        // --- Trigger verbs ---

        [Test]
        public void SplinterRoundsSpawnTwoDartsOnGunHitsButNeverOnTheirOwnHits()
        {
            var fx = NewEffects();
            fx.AddBoon(BoonById("splinter"));

            Assert.That(fx.OnHit(HitSource.Gun).SplinterDarts, Is.EqualTo(2));
            Assert.That(fx.OnHit(HitSource.None).SplinterDarts, Is.EqualTo(0), "darts can't chain darts");
            Assert.That(fx.OnHit(HitSource.Pod).SplinterDarts, Is.EqualTo(0));
        }

        [Test]
        public void TriggerCoilProcIsSeedDeterministicAndScalesWithStacks()
        {
            // Random(1)'s first NextDouble is ~0.2487: below the 3-stack
            // chance (0.310) but above the 1-stack chance (0.130) — so the
            // same roll procs with 3 stacks and whiffs with 1.
            var procs = 0;
            var fx = new RunEffects(new Random(1)) { ResetGunCooldown = () => procs++ };
            fx.AddItem(ItemById("trigger"));
            fx.AddItem(ItemById("trigger"));
            fx.AddItem(ItemById("trigger"));
            fx.OnHit(HitSource.Gun);
            Assert.That(procs, Is.EqualTo(1), "3 stacks: proc");

            procs = 0;
            fx = new RunEffects(new Random(1)) { ResetGunCooldown = () => procs++ };
            fx.AddItem(ItemById("trigger"));
            fx.OnHit(HitSource.Gun);
            Assert.That(procs, Is.EqualTo(0), "1 stack: same roll whiffs");
        }

        [Test]
        public void KnockdownTriggersRearmAndLeech()
        {
            var rearmed = 0;
            var fx = new RunEffects(new Random(7)) { ResetBombCooldown = () => rearmed++ };
            var health = new CombatantHealth();
            fx.Bind(health);
            fx.AddBoon(BoonById("rearm"));
            fx.AddItem(ItemById("leech"));
            health.TakeHit(100f, 0f);

            fx.OnKnockdown();

            Assert.That(rearmed, Is.EqualTo(1));
            Assert.That(health.Hp, Is.EqualTo(930f), "leech heals 30 per stack");
        }

        [Test]
        public void AfterimageSpawnsOnlyWithTheBoon()
        {
            var fx = NewEffects();
            Assert.That(fx.OnDash().SpawnAfterimage, Is.False);
            fx.AddBoon(BoonById("afterimage"));
            Assert.That(fx.OnDash().SpawnAfterimage, Is.True);
        }

        // --- CombatantHealth run-layer additions ---

        [Test]
        public void HealAndRestoreEnduranceCapAtMax()
        {
            var health = new CombatantHealth();
            health.TakeHit(100f, 50f);

            health.Heal(500f);
            health.RestoreEndurance(500f);

            Assert.That(health.Hp, Is.EqualTo(health.MaxHp));
            Assert.That(health.Endurance, Is.EqualTo(health.MaxEndurance));
        }

        [Test]
        public void SetHpClampsIntoTheLivingRange()
        {
            var health = new CombatantHealth();
            health.SetHp(-50f);
            Assert.That(health.Hp, Is.EqualTo(1f), "run carry can never kill");
            health.SetHp(99999f);
            Assert.That(health.Hp, Is.EqualTo(health.MaxHp));
        }

        // --- GunCycle reset ---

        [Test]
        public void ResetCooldownAllowsAnImmediateRefire()
        {
            var gun = new GunCycle(0.38f);
            Assert.That(gun.TryFire(), Is.True);
            Assert.That(gun.TryFire(), Is.False, "still cooling down");

            gun.ResetCooldown();
            Assert.That(gun.TryFire(), Is.True);
        }

        // --- Power multiplier ---

        [Test]
        public void ComputeStatsScalesHpAndAtkByThePowerMult()
        {
            var loadout = PartsCatalog.DefaultLoadout();
            var baseline = PartsCatalog.ComputeStats(loadout);
            var scaled = PartsCatalog.ComputeStats(loadout, RunState.EnemyPowerMult(4));

            Assert.That(scaled.MaxHp, Is.EqualTo(MathF.Round(baseline.MaxHp * 1.48f)));
            Assert.That(scaled.AtkMult, Is.EqualTo(baseline.AtkMult * 1.48f).Within(0.0001f));
            Assert.That(scaled.DefMult, Is.EqualTo(baseline.DefMult), "defense is untouched");
            Assert.That(scaled.RunSpeed, Is.EqualTo(baseline.RunSpeed), "speed is untouched");
        }

        // --- Draft rolls ---

        [Test]
        public void DraftOffersThreeBoonsFromThreeDifferentSlots()
        {
            var offer = DraftRoll.Offer(NewEffects(), new Random(11));

            Assert.That(offer.Length, Is.EqualTo(3));
            Assert.That(offer[0].Slot, Is.Not.EqualTo(offer[1].Slot));
            Assert.That(offer[1].Slot, Is.Not.EqualTo(offer[2].Slot));
            Assert.That(offer[0].Slot, Is.Not.EqualTo(offer[2].Slot));
        }

        [Test]
        public void DraftNeverOffersAnOwnedBoonAndShrinksWhenSlotsRunOut()
        {
            var fx = NewEffects();
            // Own both gun, both bomb, and both pod boons: only melee and
            // dash slots remain draftable.
            foreach (var id in new[] { "splinter", "overcharge", "cluster", "rearm", "overclock", "vampiric" })
            {
                fx.AddBoon(BoonById(id));
            }

            var offer = DraftRoll.Offer(fx, new Random(3));

            Assert.That(offer.Length, Is.EqualTo(2), "only two slots have anything left");
            foreach (var boon in offer)
            {
                Assert.That(fx.Has(boon.Id), Is.False);
                Assert.That(boon.Slot, Is.EqualTo(BoonSlot.Melee).Or.EqualTo(BoonSlot.Dash));
            }
        }

        [Test]
        public void DraftIsDeterministicForTheSameSeed()
        {
            var a = DraftRoll.Offer(NewEffects(), new Random(42));
            var b = DraftRoll.Offer(NewEffects(), new Random(42));

            Assert.That(a.Length, Is.EqualTo(b.Length));
            for (var i = 0; i < a.Length; i++)
            {
                Assert.That(a[i].Id, Is.EqualTo(b[i].Id));
            }
        }

        // --- Rivals & Spoils of War ---

        [Test]
        public void EveryRivalHasANameAnOrderALoadoutAndASpoil()
        {
            Assert.That(RunOpponents.Presets.Length, Is.EqualTo(RunState.FightsPerRun));
            foreach (var rival in RunOpponents.Presets)
            {
                Assert.That(rival.PilotName, Is.Not.Null.And.Not.Empty);
                Assert.That(rival.OrderName, Is.Not.Null.And.Not.Empty);
                Assert.That(rival.SpoilsName, Is.Not.Null.And.Not.Empty);

                var loadout = rival.BuildLoadout();
                Assert.That(loadout.HasGun ^ loadout.HasMelee, Is.True, "right arm mutex holds");
                Assert.That(loadout.HasBomb ^ loadout.HasShield, Is.True, "left arm mutex holds");
                Assert.That(loadout, Is.Not.SameAs(rival.BuildLoadout()), "fresh instance per call");
            }
        }

        [Test]
        public void SpoilsSwapRespectsTheArmMutex()
        {
            var loadout = PartsCatalog.DefaultLoadout(); // gun + bomb

            // F3's spoil is a melee weapon: taking it must clear the gun.
            var meleeRival = RunOpponents.ForFight(2);
            meleeRival.ApplySpoils(loadout);
            Assert.That(loadout.HasMelee, Is.True);
            Assert.That(loadout.HasGun, Is.False);
            Assert.That(loadout.Melee, Is.SameAs(meleeRival.SpoilsMelee));

            // F4's spoil is a shield: taking it must clear the bomb.
            var shieldRival = RunOpponents.ForFight(3);
            shieldRival.ApplySpoils(loadout);
            Assert.That(loadout.HasShield, Is.True);
            Assert.That(loadout.HasBomb, Is.False);
            Assert.That(loadout.Shield, Is.SameAs(shieldRival.SpoilsShield));
        }

        // --- helpers ---

        private static RunEffects NewEffects() => new RunEffects(new Random(1234));

        private static Boon BoonById(string id)
        {
            foreach (var boon in RunCatalog.Boons)
            {
                if (boon.Id == id)
                {
                    return boon;
                }
            }

            throw new InvalidOperationException($"no boon '{id}'");
        }

        private static Item ItemById(string id)
        {
            foreach (var item in RunCatalog.Items)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            throw new InvalidOperationException($"no item '{id}'");
        }
    }
}
