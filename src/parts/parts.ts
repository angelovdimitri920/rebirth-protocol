import { TUNING } from "../core/tuning";

// Loadout (GAME_DESIGN §2.1, extended): Chassis + Right Arm + Left Arm +
// Legs + Pod. Right Arm is a mutually exclusive choice between a ranged
// Gun and a melee weapon; Left Arm is a mutually exclusive choice between
// a Bomb and a Shield. You can't hold a melee weapon and a gun at once, or
// a bomb and a shield -- exactly one option per arm, picked from either
// category. computeStats() flattens a Loadout into the derived numbers
// Robo consumes.

export type DashType = "normal" | "long" | "vanish";

export interface BodyPart {
  id: string;
  name: string;
  blurb: string;
  hpMult: number;
  defMult: number; // incoming damage multiplier (lower = tankier)
  atkMult: number; // outgoing damage multiplier
  dashType: DashType;
  dashCount: number; // air dashes per airborne stretch
  speedMult: number; // body weight affects ground speed
}

export interface GunPart {
  id: string;
  name: string;
  blurb: string;
  damage: number;
  enduranceDamage: number;
  fireInterval: number;
  projectileSpeed: number;
  homingTurnRate: number;
}

/** Melee weapons (GAME_DESIGN §3.1): higher damage than guns, but you have
 *  to close distance, and hit/whiff recovery act as the "can't spam it"
 *  cooldown. weaponShape drives which mesh RoboMesh builds for it. */
export interface MeleeWeaponPart {
  id: string;
  name: string;
  blurb: string;
  weaponShape: "saber" | "hammer" | "daggers";
  damage: number;
  enduranceDamage: number;
  hitRange: number;
  hitArcDegrees: number;
  swingActiveTime: number;
  hitRecovery: number; // recovery after a CONNECTING swing
  whiffRecovery: number; // recovery after a MISSED swing -- the real cooldown
  knockbackSpeed: number;
}

/** Bombs are hold-to-aim, release-to-throw: a reticule tracks the default
 *  aim point while the bomb button is held, and deploys where the reticule
 *  sits the instant it's released. `reticuleAnchor` "target" tracks the
 *  enemy (clamped to `reticuleRange`); "self" is a fixed point straight
 *  ahead of your own robo at `reticuleRange` -- a closer-range, higher-
 *  commitment throw that can't be aimed further out. */
export interface BombPart {
  id: string;
  name: string;
  blurb: string;
  damage: number;
  enduranceDamage: number;
  cooldown: number;
  blastRadius: number;
  arcHeight: number; // lob apex above the midpoint
  reticuleAnchor: "target" | "self";
  reticuleRange: number; // m: clamp for "target", fixed ahead-distance for "self"
}

/** Shields (GAME_DESIGN §3.2): must be actively engaged (held) to block --
 *  engaging instantly halts all horizontal momentum, even mid-air, and
 *  disables dashing entirely for as long as it's held. Even engaged, block
 *  percentages are never 100%, so some chip damage always lands, and a hit
 *  from behind blocks far less than one from the front. The blocked
 *  portion drains shieldHp instead of the robo's own HP/endurance; when
 *  shieldHp hits 0 it guard-breaks into the same knockdown state a
 *  depleted endurance bar causes -- no second free defense layer. */
export interface ShieldPart {
  id: string;
  name: string;
  blurb: string;
  shieldHp: number;
  regenPerSec: number;
  regenDelay: number; // seconds unengaged and unhit before regen resumes
  frontBlockPercent: number; // fraction of incoming damage blocked, front arc
  backBlockPercent: number; // fraction blocked when hit from behind while up
  meleeParryEnduranceDamage: number; // bonus endurance dealt back to an
  // attacker whose melee swing connects into this shield while it's up
}

export interface PodPart {
  id: string;
  name: string;
  blurb: string;
  damage: number;
  enduranceDamage: number;
  fireInterval: number;
  energyMax: number; // pods run on their own pool (GAME_DESIGN §2.1)
  energyPerShot: number;
  energyRegenPerSec: number;
}

export interface LegsPart {
  id: string;
  name: string;
  blurb: string;
  speedMult: number;
  jumpMult: number;
  extraDashes: number; // added to body dashCount
  landRecoveryMult: number;
}

export type RightArm =
  | { kind: "gun"; part: GunPart }
  | { kind: "melee"; part: MeleeWeaponPart };

export type LeftArm =
  | { kind: "bomb"; part: BombPart }
  | { kind: "shield"; part: ShieldPart };

export interface Loadout {
  body: BodyPart;
  rightArm: RightArm;
  leftArm: LeftArm;
  legs: LegsPart;
  pod: PodPart;
}

/** Flattened stats the Robo actually runs on. */
export interface RoboStats {
  maxHp: number;
  defMult: number;
  atkMult: number;
  runSpeed: number;
  jumpThrust: number;
  dashType: DashType;
  dashCount: number;
  landRecoveryMult: number;
}

export function computeStats(l: Loadout): RoboStats {
  return {
    maxHp: Math.round(TUNING.health.maxHp * l.body.hpMult),
    defMult: l.body.defMult,
    atkMult: l.body.atkMult,
    runSpeed: TUNING.move.runSpeed * l.body.speedMult * l.legs.speedMult,
    jumpThrust: TUNING.boost.jumpThrust * l.legs.jumpMult,
    dashType: l.body.dashType,
    dashCount: l.body.dashCount + l.legs.extraDashes,
    landRecoveryMult: l.legs.landRecoveryMult,
  };
}

// --- Catalogs (silhouette basis: docs/ROBOT_SHELL_DESIGN.md §2) ---

export const BODIES: BodyPart[] = [
  {
    id: "vanguard",
    name: "Vanguard",
    blurb: "Balanced all-rounder. Two air-dashes, no weaknesses, no edges.",
    hpMult: 1.0,
    defMult: 1.0,
    atkMult: 1.0,
    dashType: "normal",
    dashCount: 2,
    speedMult: 1.0,
  },
  {
    id: "skylance",
    name: "Skylance",
    blurb: "Glass-cannon flier. One long dash, hits hard, folds fast.",
    hpMult: 0.8,
    defMult: 1.2,
    atkMult: 1.25,
    dashType: "long",
    dashCount: 1,
    speedMult: 1.05,
  },
  {
    id: "wraith",
    name: "Wraith",
    blurb: "Evader. Three short vanish-dashes that phase through shots.",
    hpMult: 0.9,
    defMult: 1.1,
    atkMult: 0.9,
    dashType: "vanish",
    dashCount: 3,
    speedMult: 1.0,
  },
  {
    id: "bulwark",
    name: "Bulwark",
    blurb: "Slow tank. One dash, huge health pool, shrugs off hits.",
    hpMult: 1.45,
    defMult: 0.75,
    atkMult: 1.0,
    dashType: "normal",
    dashCount: 1,
    speedMult: 0.8,
  },
];

export const GUNS: GunPart[] = [
  {
    id: "blaster",
    name: "Blaster",
    blurb: "The baseline. Honest damage, honest tracking.",
    damage: 35,
    enduranceDamage: 16,
    fireInterval: 0.38,
    projectileSpeed: 32,
    homingTurnRate: 2.2,
  },
  {
    id: "needler",
    name: "Needler",
    blurb: "Rapid stream of weak, hard-curving darts. Death by pressure.",
    damage: 14,
    enduranceDamage: 7,
    fireInterval: 0.13,
    projectileSpeed: 36,
    homingTurnRate: 3.4,
  },
  {
    id: "ram-cannon",
    name: "Ram Cannon",
    blurb: "Slow, straight, brutal. One hit shreds endurance.",
    damage: 90,
    enduranceDamage: 48,
    fireInterval: 1.15,
    projectileSpeed: 26,
    homingTurnRate: 0.6,
  },
];

export const MELEE_WEAPONS: MeleeWeaponPart[] = [
  {
    id: "saber",
    name: "Saber",
    blurb: "Balanced blade. No glaring weakness, no standout edge.",
    weaponShape: "saber",
    damage: 130,
    enduranceDamage: 55,
    hitRange: 3.0,
    hitArcDegrees: 70,
    swingActiveTime: 0.18,
    hitRecovery: 0.45,
    whiffRecovery: 0.95,
    knockbackSpeed: 10,
  },
  {
    id: "warhammer",
    name: "Warhammer",
    blurb:
      "Massive damage and knockback, but whiff this and you're standing there a long time.",
    weaponShape: "hammer",
    damage: 210,
    enduranceDamage: 90,
    hitRange: 3.4,
    hitArcDegrees: 80,
    swingActiveTime: 0.3,
    hitRecovery: 0.75,
    whiffRecovery: 1.4,
    knockbackSpeed: 16,
  },
  {
    id: "twin-fang",
    name: "Twin Fang",
    blurb: "Fast, light, low-commitment. Weaker per hit, but barely punishable.",
    weaponShape: "daggers",
    damage: 85,
    enduranceDamage: 35,
    hitRange: 2.6,
    hitArcDegrees: 70,
    swingActiveTime: 0.12,
    hitRecovery: 0.28,
    whiffRecovery: 0.6,
    knockbackSpeed: 7,
  },
];

export const BOMBS: BombPart[] = [
  {
    id: "impact",
    name: "Impact Bomb",
    blurb: "Standard lobbed shell. Reticule tracks the enemy -- hold to aim, release to throw.",
    damage: 80,
    enduranceDamage: 35,
    cooldown: 5,
    blastRadius: 3.2,
    arcHeight: 5,
    reticuleAnchor: "target",
    reticuleRange: 20,
  },
  {
    id: "quake",
    name: "Quake Bomb",
    blurb:
      "Huge blast, heavy endurance crush, long rearm. Reticule is fixed just ahead of you -- close-range, high commitment.",
    damage: 120,
    enduranceDamage: 70,
    cooldown: 9,
    blastRadius: 4.5,
    arcHeight: 6.5,
    reticuleAnchor: "self",
    reticuleRange: 4,
  },
];

export const SHIELDS: ShieldPart[] = [
  {
    id: "aegis",
    name: "Aegis Barrier",
    blurb:
      "Energy shield: fast regen, but only blocks ~75% up front and ~25% from behind. Hold to raise -- rooted in place while up.",
    shieldHp: 180,
    regenPerSec: 25,
    regenDelay: 2.0,
    frontBlockPercent: 0.75,
    backBlockPercent: 0.25,
    meleeParryEnduranceDamage: 20,
  },
  {
    id: "bastion",
    name: "Bastion Plate",
    blurb:
      "Physical plate: bigger buffer, blocks ~92% up front, recharges slowly. Hold to raise -- rooted in place while up.",
    shieldHp: 260,
    regenPerSec: 6,
    regenDelay: 3.5,
    frontBlockPercent: 0.92,
    backBlockPercent: 0.4,
    meleeParryEnduranceDamage: 32,
  },
];

export const PODS: PodPart[] = [
  {
    id: "sentry",
    name: "Sentry Pod",
    blurb: "Steady chip fire. Keeps them honest while you reposition.",
    damage: 8,
    enduranceDamage: 5,
    fireInterval: 0.8,
    energyMax: 100,
    energyPerShot: 12,
    energyRegenPerSec: 9,
  },
  {
    id: "hornet",
    name: "Hornet Pod",
    blurb: "Fast bursts that drain its cell quickly. Feast then famine.",
    damage: 6,
    enduranceDamage: 8,
    fireInterval: 0.35,
    energyMax: 80,
    energyPerShot: 16,
    energyRegenPerSec: 7,
  },
];

export const LEGS: LegsPart[] = [
  {
    id: "strider",
    name: "Strider Legs",
    blurb: "Neutral gait. Nothing gained, nothing owed.",
    speedMult: 1.0,
    jumpMult: 1.0,
    extraDashes: 0,
    landRecoveryMult: 1.0,
  },
  {
    id: "cheetah",
    name: "Cheetah Legs",
    blurb: "Fast and low. Ground speed up, jump suffers.",
    speedMult: 1.3,
    jumpMult: 0.85,
    extraDashes: 0,
    landRecoveryMult: 1.1,
  },
  {
    id: "cricket",
    name: "Cricket Legs",
    blurb: "Sky rig: extra dash and clean landings, sluggish on foot.",
    speedMult: 0.85,
    jumpMult: 1.15,
    extraDashes: 1,
    landRecoveryMult: 0.7,
  },
];

export const RIGHT_ARM_CATALOG: RightArm[] = [
  ...GUNS.map((part) => ({ kind: "gun" as const, part })),
  ...MELEE_WEAPONS.map((part) => ({ kind: "melee" as const, part })),
];

export const LEFT_ARM_CATALOG: LeftArm[] = [
  ...BOMBS.map((part) => ({ kind: "bomb" as const, part })),
  ...SHIELDS.map((part) => ({ kind: "shield" as const, part })),
];

export function defaultLoadout(): Loadout {
  return {
    body: BODIES[0],
    rightArm: { kind: "gun", part: GUNS[0] },
    leftArm: { kind: "bomb", part: BOMBS[0] },
    legs: LEGS[0],
    pod: PODS[0],
  };
}

const STORAGE_KEY = "rebirth-protocol.loadout.v2";

interface SavedLoadout {
  body: string;
  rightArmKind: "gun" | "melee";
  rightArmId: string;
  leftArmKind: "bomb" | "shield";
  leftArmId: string;
  legs: string;
  pod: string;
}

export function saveLoadout(l: Loadout): void {
  const saved: SavedLoadout = {
    body: l.body.id,
    rightArmKind: l.rightArm.kind,
    rightArmId: l.rightArm.part.id,
    leftArmKind: l.leftArm.kind,
    leftArmId: l.leftArm.part.id,
    legs: l.legs.id,
    pod: l.pod.id,
  };
  localStorage.setItem(STORAGE_KEY, JSON.stringify(saved));
}

export function loadLoadout(): Loadout {
  const fallback = defaultLoadout();
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return fallback;
  try {
    const s = JSON.parse(raw) as SavedLoadout;
    const rightArm: RightArm =
      s.rightArmKind === "melee"
        ? {
            kind: "melee",
            part: MELEE_WEAPONS.find((p) => p.id === s.rightArmId) ?? MELEE_WEAPONS[0],
          }
        : {
            kind: "gun",
            part: GUNS.find((p) => p.id === s.rightArmId) ?? GUNS[0],
          };
    const leftArm: LeftArm =
      s.leftArmKind === "shield"
        ? {
            kind: "shield",
            part: SHIELDS.find((p) => p.id === s.leftArmId) ?? SHIELDS[0],
          }
        : {
            kind: "bomb",
            part: BOMBS.find((p) => p.id === s.leftArmId) ?? BOMBS[0],
          };
    return {
      body: BODIES.find((p) => p.id === s.body) ?? fallback.body,
      rightArm,
      leftArm,
      legs: LEGS.find((p) => p.id === s.legs) ?? fallback.legs,
      pod: PODS.find((p) => p.id === s.pod) ?? fallback.pod,
    };
  } catch {
    return fallback;
  }
}
