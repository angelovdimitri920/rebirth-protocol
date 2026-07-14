import {
  BODIES,
  BOMBS,
  GUNS,
  LEGS,
  MELEE_WEAPONS,
  PODS,
  SHIELDS,
  type Loadout,
} from "../parts/parts";

// Run structure (GAME_DESIGN §4): a run is a fixed sequence of duels.
// Enemy builds escalate and vary so different player builds meet different
// problems; a flat power multiplier stacks on top per fight. Enemy arm
// choices deliberately sample gun/melee and bomb/shield so the player sees
// every combat pattern across a run.

export const FIGHTS_PER_RUN = 5;

/** Enemy loadout per fight index — variety first, then raw power. */
export function enemyForFight(i: number): Loadout {
  const presets: Loadout[] = [
    // F1: mirror-ish all-rounder, gun + bomb -- teachable opener
    {
      body: BODIES[0],
      rightArm: { kind: "gun", part: GUNS[0] },
      leftArm: { kind: "bomb", part: BOMBS[0] },
      legs: LEGS[0],
      pod: PODS[0],
    },
    // F2: fast harasser, still ranged
    {
      body: BODIES[1],
      rightArm: { kind: "gun", part: GUNS[1] },
      leftArm: { kind: "bomb", part: BOMBS[0] },
      legs: LEGS[1],
      pod: PODS[1],
    },
    // F3: melee rush with a shield -- teaches the player to respect range
    {
      body: BODIES[2],
      rightArm: { kind: "melee", part: MELEE_WEAPONS[2] }, // Khopesh
      leftArm: { kind: "shield", part: SHIELDS[0] },
      legs: LEGS[2],
      pod: PODS[0],
    },
    // F4: the wall -- heavy gun + heavy shield
    {
      body: BODIES[3],
      rightArm: { kind: "gun", part: GUNS[2] },
      leftArm: { kind: "shield", part: SHIELDS[1] },
      legs: LEGS[0],
      pod: PODS[0],
    },
    // F5: heavy melee brawler, everything at once
    {
      body: BODIES[3],
      rightArm: { kind: "melee", part: MELEE_WEAPONS[1] }, // Warhammer
      leftArm: { kind: "shield", part: SHIELDS[1] },
      legs: LEGS[1],
      pod: PODS[1],
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
