using System;

namespace RebirthProtocol.Domain
{
    // Run structure (GAME_DESIGN §4, Stage 3): a run is a fixed sequence of
    // duels against escalating rivals. Player HP persists between fights
    // (with a 15%-of-max heal); endurance/shield/boost reset per fight.
    // Death ends the run. Ported from the prototype's run.ts.
    public sealed class RunState
    {
        public const int FightsPerRun = 5;

        public int FightIndex;

        /// Player HP carried from the previous fight; null on a fresh run.
        public float? CarriedHp;

        public int RerollsLeft = 1;

        public bool IsFinalFight => FightIndex == FightsPerRun - 1;

        /// Flat enemy power multiplier per fight (applied to HP and ATK).
        public static float EnemyPowerMult(int fightIndex) => 1f + 0.12f * fightIndex;

        /// Starting HP for the current fight: carried HP plus a 15%-of-max
        /// heal, capped at max. A fresh run starts at full.
        public float StartingHp(float maxHp)
        {
            if (!CarriedHp.HasValue)
            {
                return maxHp;
            }

            return Math.Min(maxHp, CarriedHp.Value + maxHp * 0.15f);
        }
    }
}
