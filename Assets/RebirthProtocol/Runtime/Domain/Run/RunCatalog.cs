namespace RebirthProtocol.Domain
{
    // In-run boons and stacking items (GAME_DESIGN §4), ported from the
    // prototype's effects.ts pools. Boons change behavior, one per ability
    // slot in any draft offer; items stack linearly (with hyperbolic
    // scaling for %-chance effects) and drop mid-fight from crates.

    public enum BoonSlot
    {
        Gun,
        Bomb,
        Pod,
        Melee,
        Dash
    }

    public sealed class Boon
    {
        public string Id;
        public string Name;
        public BoonSlot Slot;
        public string Blurb;
    }

    public sealed class Item
    {
        public string Id;
        public string Name;
        public string Blurb;
    }

    public static class RunCatalog
    {
        public static readonly Boon[] Boons =
        {
            new Boon { Id = "splinter", Name = "Splinter Rounds", Slot = BoonSlot.Gun, Blurb = "Gun hits burst into two weak darts that curve back in." },
            new Boon { Id = "overcharge", Name = "Overcharge Coils", Slot = BoonSlot.Gun, Blurb = "+45% gun damage while your boost gauge is above 70." },
            new Boon { Id = "cluster", Name = "Cluster Shell", Slot = BoonSlot.Bomb, Blurb = "Bomb detonations scatter two follow-up mini-blasts." },
            new Boon { Id = "rearm", Name = "Rearm Protocol", Slot = BoonSlot.Bomb, Blurb = "Knocking the enemy down instantly rearms your bomb." },
            new Boon { Id = "overclock", Name = "Overclocked Cell", Slot = BoonSlot.Pod, Blurb = "Pod recharges 80% faster and fires 25% quicker." },
            new Boon { Id = "vampiric", Name = "Vampiric Relay", Slot = BoonSlot.Pod, Blurb = "Pod hits feed you 5 endurance each." },
            new Boon { Id = "guardcrusher", Name = "Guard Crusher", Slot = BoonSlot.Melee, Blurb = "Melee strikes deal triple damage to shields." },
            new Boon { Id = "momentum", Name = "Momentum Edge", Slot = BoonSlot.Melee, Blurb = "For 2s after a dash, melee hits 60% harder." },
            new Boon { Id = "afterimage", Name = "Afterimage", Slot = BoonSlot.Dash, Blurb = "Dashing leaves a crackling afterimage that detonates on contact." },
            new Boon { Id = "slipstream", Name = "Slipstream", Slot = BoonSlot.Dash, Blurb = "Dashes cost 35% less boost." }
        };

        public static readonly Item[] Items =
        {
            new Item { Id = "plating", Name = "Scrap Plating", Blurb = "+40 max HP" },
            new Item { Id = "kinetic", Name = "Kinetic Cell", Blurb = "Dashing restores 7 endurance" },
            new Item { Id = "trigger", Name = "Trigger Coil", Blurb = "Gun hits may instantly reload (diminishing)" },
            new Item { Id = "impact", Name = "Impact Converter", Blurb = "+3 damage on every hit" },
            new Item { Id = "leech", Name = "Leech Node", Blurb = "Knockdowns you inflict heal 30 HP" }
        };
    }
}
