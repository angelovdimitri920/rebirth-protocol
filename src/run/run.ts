import {
  BODIES,
  BOMBS,
  GUNS,
  LEGS,
  PODS,
  SHIELDS,
  type Loadout,
} from "../parts/parts";

// Run structure (GAME_DESIGN §4): a run is a fixed sequence of duels.
// Enemy builds escalate and vary so different player builds meet different
// problems; a flat power multiplier stacks on top per fight.

export const FIGHTS_PER_RUN = 5;

/** Enemy loadout per fight index — variety first, then raw power. */
export function enemyForFight(i: number): Loadout {
  const presets: Loadout[] = [
    // F1: mirror-ish all-rounder, no shield -- teachable opener
    {
      body: BODIES[0],
      gun: GUNS[0],
      bomb: BOMBS[0],
      pod: PODS[0],
      legs: LEGS[0],
      shield: SHIELDS[0],
    },
    // F2: fast harasser
    {
      body: BODIES[1],
      gun: GUNS[1],
      bomb: BOMBS[0],
      pod: PODS[1],
      legs: LEGS[1],
      shield: SHIELDS[0],
    },
    // F3: evasive skirmisher with shield
    {
      body: BODIES[2],
      gun: GUNS[0],
      bomb: BOMBS[1],
      pod: PODS[0],
      legs: LEGS[2],
      shield: SHIELDS[1],
    },
    // F4: the wall
    {
      body: BODIES[3],
      gun: GUNS[2],
      bomb: BOMBS[1],
      pod: PODS[0],
      legs: LEGS[0],
      shield: SHIELDS[1],
    },
    // F5: everything at once
    {
      body: BODIES[3],
      gun: GUNS[2],
      bomb: BOMBS[1],
      pod: PODS[1],
      legs: LEGS[1],
      shield: SHIELDS[1],
    },
  ];
  return presets[Math.min(i, presets.length - 1)];
}

/** Flat enemy power multiplier per fight (applied to HP and ATK). */
export function enemyPowerMult(i: number): number {
  return 1 + 0.12 * i;
}

export class RunState {
  fightIndex = 0;
  /** Player HP carried between fights (roguelite pressure). */
  carriedHp: number | null = null;
  rerollsLeft = 1;

  get isFinalFight(): boolean {
    return this.fightIndex === FIGHTS_PER_RUN - 1;
  }
}
