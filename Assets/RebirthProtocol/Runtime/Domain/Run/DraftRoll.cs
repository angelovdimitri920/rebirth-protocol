using System;
using System.Collections.Generic;

namespace RebirthProtocol.Domain
{
    // Post-fight boon draft (GAME_DESIGN §4): 3 choices from 3 DIFFERENT
    // ability slots (Hades's one-boon-per-slot discipline), never offering
    // a boon already owned. Deterministic given the run's seeded Random.
    public static class DraftRoll
    {
        public static Boon[] Offer(RunEffects owned, Random rng)
        {
            var bySlot = new Dictionary<BoonSlot, List<Boon>>();
            foreach (var boon in RunCatalog.Boons)
            {
                if (owned.Has(boon.Id))
                {
                    continue;
                }

                if (!bySlot.TryGetValue(boon.Slot, out var group))
                {
                    bySlot[boon.Slot] = group = new List<Boon>();
                }

                group.Add(boon);
            }

            var slots = new List<BoonSlot>(bySlot.Keys);
            for (var i = slots.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            var count = Math.Min(3, slots.Count);
            var offer = new Boon[count];
            for (var i = 0; i < count; i++)
            {
                var group = bySlot[slots[i]];
                offer[i] = group[rng.Next(group.Count)];
            }

            return offer;
        }
    }
}
