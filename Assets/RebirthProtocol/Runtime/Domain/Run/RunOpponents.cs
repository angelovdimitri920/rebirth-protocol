namespace RebirthProtocol.Domain
{
    // The run's five rival pilots — variety first, then raw power (ported
    // from the prototype's run.ts presets, arm choices deliberately sample
    // gun/melee and bomb/shield so the player sees every combat pattern
    // across a run). Names follow the neo-feudal Orders in
    // docs/SETTING_AND_FACTIONS.md; each rival carries a signature part
    // offered as Spoils of War after their defeat (the victor claims the
    // loser's arms — the setting doc's chivalric justification for part
    // drops).
    public sealed class RivalPreset
    {
        public string PilotName;
        public string OrderName;

        // Exactly one of these is the rival's signature spoil, matching the
        // Loadout arm mutex: a gun/melee spoil replaces the right arm, a
        // bomb/shield spoil replaces the left.
        public GunPart SpoilsGun;
        public MeleeWeaponPart SpoilsMelee;
        public BombPart SpoilsBomb;
        public ShieldPart SpoilsShield;

        public string SpoilsName =>
            SpoilsGun?.Name ?? SpoilsMelee?.Name ?? SpoilsBomb?.Name ?? SpoilsShield?.Name;

        /// Claim the spoil: swap it into the loadout, clearing the other
        /// half of its arm slot (gun XOR melee, bomb XOR shield).
        public void ApplySpoils(Loadout loadout)
        {
            if (SpoilsGun != null)
            {
                loadout.Gun = SpoilsGun;
                loadout.Melee = null;
            }
            else if (SpoilsMelee != null)
            {
                loadout.Melee = SpoilsMelee;
                loadout.Gun = null;
            }
            else if (SpoilsBomb != null)
            {
                loadout.Bomb = SpoilsBomb;
                loadout.Shield = null;
            }
            else if (SpoilsShield != null)
            {
                loadout.Shield = SpoilsShield;
                loadout.Bomb = null;
            }
        }

        /// Fresh Loadout per call: the run flow mutates loadouts (spoils),
        /// so presets never hand out a shared instance.
        public System.Func<Loadout> BuildLoadout;
    }

    public static class RunOpponents
    {
        public static RivalPreset ForFight(int fightIndex)
        {
            var presets = Presets;
            return presets[fightIndex < presets.Length ? fightIndex : presets.Length - 1];
        }

        private static RivalPreset[] _presets;

        public static RivalPreset[] Presets => _presets ??= new[]
        {
            // F1: mirror-ish all-rounder, gun + bomb — teachable opener.
            new RivalPreset
            {
                PilotName = "Bannerlord Cassian",
                OrderName = "The Aureate Legion",
                SpoilsGun = PartsCatalog.Guns[0],
                BuildLoadout = () => new Loadout
                {
                    Body = PartsCatalog.Bodies[0],
                    Gun = PartsCatalog.Guns[0],
                    Bomb = PartsCatalog.Bombs[0],
                    Legs = PartsCatalog.Legs[0],
                    Pod = PartsCatalog.Pods[0]
                }
            },
            // F2: fast harasser, still ranged.
            new RivalPreset
            {
                PilotName = "Skald Maren",
                OrderName = "Order of the Winter Wing",
                SpoilsGun = PartsCatalog.Guns[1],
                BuildLoadout = () => new Loadout
                {
                    Body = PartsCatalog.Bodies[1],
                    Gun = PartsCatalog.Guns[1],
                    Bomb = PartsCatalog.Bombs[0],
                    Legs = PartsCatalog.Legs[1],
                    Pod = PartsCatalog.Pods[1]
                }
            },
            // F3: melee rush behind a shield — teaches respect for range.
            new RivalPreset
            {
                PilotName = "Vesk the Unseen",
                OrderName = "The Umbral Concordat",
                SpoilsMelee = PartsCatalog.MeleeWeapons[2],
                BuildLoadout = () => new Loadout
                {
                    Body = PartsCatalog.Bodies[2],
                    Melee = PartsCatalog.MeleeWeapons[2],
                    Shield = PartsCatalog.Shields[0],
                    Legs = PartsCatalog.Legs[2],
                    Pod = PartsCatalog.Pods[0]
                }
            },
            // F4: the wall — heavy gun behind a heavy shield.
            new RivalPreset
            {
                PilotName = "Warden Aldric",
                OrderName = "The Rust Cross Commandery",
                SpoilsShield = PartsCatalog.Shields[1],
                BuildLoadout = () => new Loadout
                {
                    Body = PartsCatalog.Bodies[3],
                    Gun = PartsCatalog.Guns[2],
                    Shield = PartsCatalog.Shields[1],
                    Legs = PartsCatalog.Legs[0],
                    Pod = PartsCatalog.Pods[0]
                }
            },
            // F5: heavy melee brawler, everything at once.
            new RivalPreset
            {
                PilotName = "Grandmaster Otho",
                OrderName = "The Rust Cross Commandery",
                SpoilsMelee = PartsCatalog.MeleeWeapons[1],
                BuildLoadout = () => new Loadout
                {
                    Body = PartsCatalog.Bodies[3],
                    Melee = PartsCatalog.MeleeWeapons[1],
                    Shield = PartsCatalog.Shields[1],
                    Legs = PartsCatalog.Legs[1],
                    Pod = PartsCatalog.Pods[1]
                }
            }
        };
    }
}
