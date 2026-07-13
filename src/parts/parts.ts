import { TUNING } from "../core/tuning";

// Five-slot loadout (GAME_DESIGN §2.1) + shield layer (§3.2).
// Each slot offers 2-4 parts with real tradeoffs, not strict upgrades.
// computeStats() flattens a Loadout into the derived numbers Robo consumes.

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

export interface BombPart {
  id: string;
  name: string;
  blurb: string;
  damage: number;
  enduranceDamage: number;
  cooldown: number;
  blastRadius: number;
  arcHeight: number; // lob apex above the midpoint
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

export interface ShieldPart {
  id: string;
  name: string;
  blurb: string;
  shieldHp: number; // 0 = no shield
  regenPerSec: number;
  regenDelay: number; // seconds unhit before regen resumes
  arcDegrees: number; // frontal protection arc
}

export interface Loadout {
  body: BodyPart;
  gun: GunPart;
  bomb: BombPart;
  pod: PodPart;
  legs: LegsPart;
  shield: ShieldPart;
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

export const BOMBS: BombPart[] = [
  {
    id: "impact",
    name: "Impact Bomb",
    blurb: "Standard lobbed shell. Area denial on a short clock.",
    damage: 80,
    enduranceDamage: 35,
    cooldown: 5,
    blastRadius: 3.2,
    arcHeight: 5,
  },
  {
    id: "quake",
    name: "Quake Bomb",
    blurb: "Huge blast, heavy endurance crush, long rearm.",
    damage: 120,
    enduranceDamage: 70,
    cooldown: 9,
    blastRadius: 4.5,
    arcHeight: 6.5,
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

export const SHIELDS: ShieldPart[] = [
  {
    id: "none",
    name: "No Shield",
    blurb: "Nothing between you and the fight.",
    shieldHp: 0,
    regenPerSec: 0,
    regenDelay: 0,
    arcDegrees: 0,
  },
  {
    id: "aegis",
    name: "Aegis Barrier",
    blurb:
      "Front-arc energy shield. Breaks into a guard-crush knockdown, so watch the bar.",
    shieldHp: 180,
    regenPerSec: 25,
    regenDelay: 2.5,
    arcDegrees: 120,
  },
];

export const CATALOG = {
  body: BODIES,
  gun: GUNS,
  bomb: BOMBS,
  pod: PODS,
  legs: LEGS,
  shield: SHIELDS,
};

export function defaultLoadout(): Loadout {
  return {
    body: BODIES[0],
    gun: GUNS[0],
    bomb: BOMBS[0],
    pod: PODS[0],
    legs: LEGS[0],
    shield: SHIELDS[0],
  };
}

/** The dummy's contrasting preset: tanky brawler with shield and quake. */
export function enemyLoadout(): Loadout {
  return {
    body: BODIES[3], // Bulwark
    gun: GUNS[2], // Ram Cannon
    bomb: BOMBS[1], // Quake
    pod: PODS[0], // Sentry
    legs: LEGS[0], // Strider
    shield: SHIELDS[1], // Aegis
  };
}

const STORAGE_KEY = "rebirth-protocol.loadout";

export function saveLoadout(l: Loadout): void {
  localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify({
      body: l.body.id,
      gun: l.gun.id,
      bomb: l.bomb.id,
      pod: l.pod.id,
      legs: l.legs.id,
      shield: l.shield.id,
    }),
  );
}

export function loadLoadout(): Loadout {
  const fallback = defaultLoadout();
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return fallback;
  try {
    const ids = JSON.parse(raw) as Record<string, string>;
    return {
      body: BODIES.find((p) => p.id === ids.body) ?? fallback.body,
      gun: GUNS.find((p) => p.id === ids.gun) ?? fallback.gun,
      bomb: BOMBS.find((p) => p.id === ids.bomb) ?? fallback.bomb,
      pod: PODS.find((p) => p.id === ids.pod) ?? fallback.pod,
      legs: LEGS.find((p) => p.id === ids.legs) ?? fallback.legs,
      shield: SHIELDS.find((p) => p.id === ids.shield) ?? fallback.shield,
    };
  } catch {
    return fallback;
  }
}
