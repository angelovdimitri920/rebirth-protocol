import * as THREE from "three";
import type { Loadout } from "../parts/parts";

// Data-driven primitive-built shell: every chassis, weapon, shield, bomb,
// leg, and pod choice gets a genuinely different silhouette, not a color
// swap. Chassis proportions per docs/ROBOT_SHELL_DESIGN.md §2. One hull
// color + one accent color per robo, shared across every part for cohesion.

export interface RoboMeshParts {
  root: THREE.Group; // positioned at capsule center (y = 1m above feet)
  body: THREE.Group; // rotates to face; tilts for poses
  gunMuzzle: THREE.Object3D; // world-space muzzle for projectile spawn (guns only)
  materials: THREE.MeshStandardMaterial[];
}

interface Mats {
  hull: THREE.MeshStandardMaterial;
  accent: THREE.MeshStandardMaterial;
  joint: THREE.MeshStandardMaterial;
}

// Each chassis gets its own base paint identity (not just silhouette) so
// they're recognizable at a glance regardless of who's piloting them. Team
// identity still reads clearly through the accent glow (always the
// player's cyan or the enemy's orange) plus a light tint of the team's hull
// color blended in here, so player vs. enemy stays legible even when both
// happen to be flying the same chassis.
const CHASSIS_PALETTE: Record<string, { hull: number; joint: number }> = {
  vanguard: { hull: 0x5a7099, joint: 0x262b38 }, // steel blue-grey, all-rounder
  skylance: { hull: 0xdde4f0, joint: 0x3c4658 }, // pale aerospace silver
  wraith: { hull: 0x342a44, joint: 0x171320 }, // dark violet-black, stealth
  bulwark: { hull: 0x9a7038, joint: 0x3e2c16 }, // bronze-olive, heavy armor
};

function tint(base: number, teamColor: number, amount: number): number {
  return new THREE.Color(base).lerp(new THREE.Color(teamColor), amount).getHex();
}

interface ChassisAnchors {
  rightArm: THREE.Vector3;
  leftArm: THREE.Vector3;
  back: THREE.Vector3;
  legX: number;
  legY: number;
  legScale: number;
}

export function buildRoboMesh(
  loadout: Loadout,
  hull: number,
  accent: number,
): RoboMeshParts {
  const root = new THREE.Group();
  const body = new THREE.Group();
  root.add(body);

  const palette = CHASSIS_PALETTE[loadout.body.id] ?? CHASSIS_PALETTE.vanguard;
  const mats: Mats = {
    hull: new THREE.MeshStandardMaterial({
      color: tint(palette.hull, hull, 0.22),
      roughness: 0.5,
      metalness: 0.55,
    }),
    accent: new THREE.MeshStandardMaterial({
      color: accent,
      roughness: 0.35,
      metalness: 0.4,
      emissive: accent,
      emissiveIntensity: 0.35,
    }),
    joint: new THREE.MeshStandardMaterial({
      color: tint(palette.joint, hull, 0.15),
      roughness: 0.8,
      metalness: 0.3,
    }),
  };

  const anchors = buildChassis(loadout.body.id, body, mats);
  const gunMuzzle = buildRightArm(loadout, body, anchors, mats);
  buildLeftArm(loadout, body, anchors, mats);
  buildLegs(loadout.legs.id, body, anchors, mats);
  buildBackpack(loadout.pod.id, body, anchors, mats);

  return { root, body, gunMuzzle, materials: [mats.hull, mats.accent, mats.joint] };
}

// --- Shared primitive helpers ---

function box(
  parent: THREE.Object3D,
  mat: THREE.MeshStandardMaterial,
  w: number,
  h: number,
  d: number,
  x: number,
  y: number,
  z: number,
  rotY = 0,
): THREE.Mesh {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
  m.position.set(x, y, z);
  m.rotation.y = rotY;
  m.castShadow = true;
  parent.add(m);
  return m;
}

function cyl(
  parent: THREE.Object3D,
  mat: THREE.MeshStandardMaterial,
  radiusTop: number,
  radiusBottom: number,
  length: number,
  x: number,
  y: number,
  z: number,
  rotX = Math.PI / 2, // default: axis along local Z (forward)
  rotY = 0,
): THREE.Mesh {
  const m = new THREE.Mesh(
    new THREE.CylinderGeometry(radiusTop, radiusBottom, length, 10),
    mat,
  );
  m.position.set(x, y, z);
  m.rotation.x = rotX;
  m.rotation.y = rotY;
  m.castShadow = true;
  parent.add(m);
  return m;
}

// --- Chassis (docs/ROBOT_SHELL_DESIGN.md §2) ---

function buildChassis(bodyId: string, body: THREE.Group, mats: Mats): ChassisAnchors {
  switch (bodyId) {
    case "skylance":
      return buildSkylanceChassis(body, mats);
    case "wraith":
      return buildWraithChassis(body, mats);
    case "bulwark":
      return buildBulwarkChassis(body, mats);
    default:
      return buildVanguardChassis(body, mats);
  }
}

function buildVanguardChassis(body: THREE.Group, { hull, accent, joint }: Mats): ChassisAnchors {
  // Legionnaire (docs/WARBAND_THEME_REFERENCE.md §5): banded lorica-
  // segmentata-style torso plating, a tall rectangular chest panel
  // echoing a scutum shield, a transverse centurion-style helmet crest,
  // a helmet brow rim, hanging pteruges strips at the waist, and stepped
  // pauldron caps over the shoulders -- Gundam/Armored-Core panel-line
  // detail layered onto Roman legionary silhouette language.
  box(body, hull, 0.62, 0.55, 0.4, 0, 0.28, 0); // torso core
  box(body, joint, 0.66, 0.04, 0.44, 0, 0.12, 0); // lower banding seam
  box(body, joint, 0.66, 0.04, 0.44, 0, 0.4, 0); // upper banding seam
  box(body, accent, 0.3, 0.32, 0.06, 0, 0.3, 0.23); // scutum-echo chest panel
  box(body, hull, 0.34, 0.05, 0.07, 0, 0.47, 0.23); // chest panel top trim
  box(body, joint, 0.44, 0.25, 0.34, 0, -0.08, 0); // pelvis
  box(body, joint, 0.07, 0.16, 0.05, -0.16, -0.24, 0.16); // pteruges strips
  box(body, joint, 0.07, 0.16, 0.05, 0, -0.24, 0.16);
  box(body, joint, 0.07, 0.16, 0.05, 0.16, -0.24, 0.16);
  box(body, hull, 0.28, 0.24, 0.28, 0, 0.72, 0); // head
  box(body, joint, 0.3, 0.04, 0.29, 0, 0.79, 0); // helmet brow rim
  box(body, accent, 0.22, 0.06, 0.05, 0, 0.73, 0.13); // visor
  box(body, hull, 0.34, 0.1, 0.06, 0, 0.88, 0); // transverse crest
  box(body, hull, 0.24, 0.22, 0.3, -0.46, 0.42, 0);
  box(body, hull, 0.24, 0.22, 0.3, 0.46, 0.42, 0);
  box(body, joint, 0.27, 0.07, 0.33, -0.46, 0.55, 0); // pauldron cap
  box(body, joint, 0.27, 0.07, 0.33, 0.46, 0.55, 0);
  box(body, joint, 0.16, 0.5, 0.18, -0.46, 0.05, 0);
  box(body, joint, 0.16, 0.5, 0.18, 0.46, 0.05, 0);
  return {
    rightArm: new THREE.Vector3(0.46, -0.05, 0.05),
    leftArm: new THREE.Vector3(-0.46, -0.05, 0.05),
    back: new THREE.Vector3(0, 0.3, -0.28),
    legX: 0.18,
    legY: -0.55,
    legScale: 1.0,
  };
}

function buildSkylanceChassis(body: THREE.Group, { hull, accent, joint }: Mats): ChassisAnchors {
  // Tall, sleek, sweptback wing-fins -- glass-cannon flier identity
  box(body, hull, 0.5, 0.64, 0.32, 0, 0.36, 0);
  box(body, accent, 0.32, 0.22, 0.24, 0, 0.36, 0.1);
  box(body, joint, 0.38, 0.22, 0.28, 0, 0.02, 0);
  box(body, hull, 0.24, 0.2, 0.24, 0, 0.84, 0);
  box(body, accent, 0.18, 0.05, 0.04, 0, 0.85, 0.11);
  box(body, hull, 0.2, 0.2, 0.26, -0.38, 0.5, 0);
  box(body, hull, 0.2, 0.2, 0.26, 0.38, 0.5, 0);
  // Sweptback fins
  box(body, accent, 0.5, 0.06, 0.2, -0.58, 0.5, -0.14, 0.5);
  box(body, accent, 0.5, 0.06, 0.2, 0.58, 0.5, -0.14, -0.5);
  box(body, joint, 0.14, 0.46, 0.16, -0.38, 0.14, 0);
  box(body, joint, 0.14, 0.46, 0.16, 0.38, 0.14, 0);
  return {
    rightArm: new THREE.Vector3(0.38, 0.08, 0.04),
    leftArm: new THREE.Vector3(-0.38, 0.08, 0.04),
    back: new THREE.Vector3(0, 0.42, -0.24),
    legX: 0.16,
    legY: -0.5,
    legScale: 1.12,
  };
}

function buildWraithChassis(body: THREE.Group, { hull, joint }: Mats): ChassisAnchors {
  // Shorter, angular, fully-armored, minimal glow -- stealth/evader identity
  box(body, hull, 0.56, 0.48, 0.34, 0, 0.22, 0);
  box(body, joint, 0.34, 0.14, 0.26, 0, 0.22, 0.1);
  box(body, joint, 0.42, 0.22, 0.32, 0, -0.1, 0);
  box(body, hull, 0.24, 0.2, 0.22, 0, 0.6, 0);
  box(body, joint, 0.18, 0.03, 0.04, 0, 0.61, 0.1); // narrow visor slit
  box(body, hull, 0.22, 0.18, 0.28, -0.42, 0.36, 0);
  box(body, hull, 0.22, 0.18, 0.28, 0.42, 0.36, 0);
  box(body, joint, 0.15, 0.46, 0.17, -0.42, 0, 0);
  box(body, joint, 0.15, 0.46, 0.17, 0.42, 0, 0);
  // Cape/vent panel, angular
  box(body, joint, 0.5, 0.4, 0.05, 0, 0.16, -0.2, 0.06);
  return {
    rightArm: new THREE.Vector3(0.42, -0.08, 0.04),
    leftArm: new THREE.Vector3(-0.42, -0.08, 0.04),
    back: new THREE.Vector3(0, 0.28, -0.22),
    legX: 0.17,
    legY: -0.52,
    legScale: 0.96,
  };
}

function buildBulwarkChassis(body: THREE.Group, { hull, accent, joint }: Mats): ChassisAnchors {
  // Wide, bulky, huge pauldrons -- tank identity
  box(body, hull, 0.86, 0.62, 0.52, 0, 0.26, 0);
  box(body, accent, 0.5, 0.22, 0.36, 0, 0.26, 0.14);
  box(body, joint, 0.62, 0.28, 0.46, 0, -0.12, 0);
  box(body, hull, 0.32, 0.28, 0.32, 0, 0.72, 0);
  box(body, accent, 0.24, 0.06, 0.05, 0, 0.73, 0.15);
  box(body, hull, 0.36, 0.32, 0.38, -0.6, 0.42, 0);
  box(body, hull, 0.36, 0.32, 0.38, 0.6, 0.42, 0);
  box(body, joint, 0.2, 0.5, 0.22, -0.58, -0.02, 0);
  box(body, joint, 0.2, 0.5, 0.22, 0.58, -0.02, 0);
  return {
    rightArm: new THREE.Vector3(0.58, -0.16, 0.08),
    leftArm: new THREE.Vector3(-0.58, -0.16, 0.08),
    back: new THREE.Vector3(0, 0.3, -0.32),
    legX: 0.26,
    legY: -0.56,
    legScale: 1.06,
  };
}

// --- Right arm: gun (ranged) or melee weapon, mutually exclusive ---

function buildRightArm(
  loadout: Loadout,
  body: THREE.Group,
  anchors: ChassisAnchors,
  mats: Mats,
): THREE.Object3D {
  const { x, y, z } = anchors.rightArm;
  const arm = new THREE.Group();
  arm.position.set(x, y, z);
  body.add(arm);

  const tip = new THREE.Object3D();

  if (loadout.rightArm.kind === "gun") {
    switch (loadout.rightArm.part.id) {
      case "needler":
        cyl(arm, mats.accent, 0.05, 0.05, 0.42, -0.06, 0, 0.22);
        cyl(arm, mats.accent, 0.05, 0.05, 0.42, 0.06, 0, 0.22);
        box(arm, mats.joint, 0.16, 0.14, 0.16, 0, 0, -0.02);
        tip.position.set(0, 0, 0.42);
        break;
      case "ram-cannon":
        cyl(arm, mats.hull, 0.17, 0.17, 0.55, 0, 0, 0.24);
        cyl(arm, mats.accent, 0.2, 0.2, 0.08, 0, 0, 0.5);
        box(arm, mats.joint, 0.22, 0.22, 0.2, 0, 0, -0.06);
        tip.position.set(0, 0, 0.54);
        break;
      default: // blaster
        box(arm, mats.accent, 0.14, 0.14, 0.5, 0, 0, 0.24);
        box(arm, mats.joint, 0.18, 0.16, 0.16, 0, 0, -0.02);
        tip.position.set(0, 0, 0.48);
    }
  } else {
    switch (loadout.rightArm.part.weaponShape) {
      case "hammer":
        cyl(arm, mats.joint, 0.05, 0.05, 0.55, 0, 0, 0.28, Math.PI / 2);
        box(arm, mats.hull, 0.34, 0.3, 0.3, 0, 0, 0.62);
        box(arm, mats.accent, 0.36, 0.05, 0.05, 0, 0.16, 0.62);
        tip.position.set(0, 0, 0.75);
        break;
      case "daggers":
        box(arm, mats.accent, 0.05, 0.05, 0.48, -0.08, 0, 0.24, 0.35);
        box(arm, mats.accent, 0.05, 0.05, 0.48, 0.08, 0, 0.24, -0.35);
        box(arm, mats.joint, 0.14, 0.14, 0.14, 0, 0, -0.02);
        tip.position.set(0, 0, 0.5);
        break;
      default: // saber
        box(arm, mats.accent, 0.06, 0.06, 1.1, 0, 0, 0.58);
        box(arm, mats.joint, 0.22, 0.06, 0.08, 0, 0, 0.06); // cross-guard
        box(arm, mats.joint, 0.1, 0.16, 0.18, 0, 0, -0.04); // hilt
        tip.position.set(0, 0, 1.1);
    }
  }

  arm.add(tip);
  return tip;
}

// --- Left arm: bomb or shield, mutually exclusive ---

function buildLeftArm(
  loadout: Loadout,
  body: THREE.Group,
  anchors: ChassisAnchors,
  mats: Mats,
): void {
  const { x, y, z } = anchors.leftArm;
  const arm = new THREE.Group();
  arm.position.set(x, y, z);
  body.add(arm);

  if (loadout.leftArm.kind === "bomb") {
    if (loadout.leftArm.part.id === "quake") {
      cyl(arm, mats.hull, 0.19, 0.19, 0.4, 0, 0, 0.2);
      box(arm, mats.accent, 0.05, 0.28, 0.05, -0.2, 0, 0.1);
      box(arm, mats.accent, 0.05, 0.28, 0.05, 0.2, 0, 0.1);
      box(arm, mats.joint, 0.2, 0.2, 0.18, 0, 0, -0.04);
    } else {
      cyl(arm, mats.hull, 0.14, 0.14, 0.3, 0, 0, 0.16);
      const cap = new THREE.Mesh(
        new THREE.SphereGeometry(0.14, 10, 10),
        mats.accent,
      );
      cap.position.set(0, 0, 0.32);
      cap.castShadow = true;
      arm.add(cap);
      box(arm, mats.joint, 0.16, 0.16, 0.14, 0, 0, -0.04);
    }
  } else {
    if (loadout.leftArm.part.id === "bastion") {
      box(arm, mats.hull, 0.44, 0.62, 0.08, 0, 0.08, 0.1);
      box(arm, mats.joint, 0.48, 0.06, 0.09, 0, 0.38, 0.1);
      box(arm, mats.joint, 0.48, 0.06, 0.09, 0, -0.22, 0.1);
      box(arm, mats.accent, 0.06, 0.42, 0.02, 0, 0.08, 0.15);
    } else {
      const disc = new THREE.Mesh(
        new THREE.CylinderGeometry(0.26, 0.26, 0.05, 16),
        (() => {
          const m = mats.accent.clone();
          m.transparent = true;
          m.opacity = 0.75;
          return m;
        })(),
      );
      disc.rotation.x = Math.PI / 2;
      disc.position.set(0, 0, 0.08);
      disc.castShadow = true;
      arm.add(disc);
      box(arm, mats.joint, 0.14, 0.14, 0.12, 0, 0, -0.06);
    }
  }
}

// --- Legs ---

function buildLegs(legsId: string, body: THREE.Group, anchors: ChassisAnchors, mats: Mats): void {
  const { legX, legY, legScale } = anchors;
  const h = 0.62 * legScale;
  const footZ = legsId === "cheetah" ? 0.12 : 0.04;

  for (const side of [-1, 1]) {
    const x = legX * side;
    if (legsId === "cheetah") {
      box(body, mats.hull, 0.16, h, 0.2, x, legY, 0);
      box(body, mats.joint, 0.2, 0.1, 0.4, x, legY - h / 2 - 0.04, footZ);
    } else if (legsId === "cricket") {
      box(body, mats.hull, 0.2, h, 0.24, x, legY, 0);
      cyl(body, mats.accent, 0.06, 0.06, 0.16, x + 0.13 * side, legY, -0.08, 0, Math.PI / 2);
      box(body, mats.joint, 0.22, 0.12, 0.34, x, legY - h / 2 - 0.04, footZ);
    } else {
      box(body, mats.hull, 0.2, h, 0.24, x, legY, 0);
      box(body, mats.joint, 0.22, 0.12, 0.34, x, legY - h / 2 - 0.04, footZ);
    }
  }
}

// --- Backpack: baseline thrusters (always present) + pod-mount visual ---

function buildBackpack(
  podId: string,
  body: THREE.Group,
  anchors: ChassisAnchors,
  mats: Mats,
): void {
  const { x, y, z } = anchors.back;
  box(body, mats.joint, 0.4, 0.4, 0.18, x, y, z);
  box(body, mats.accent, 0.12, 0.16, 0.1, x - 0.12, y - 0.12, z - 0.08);
  box(body, mats.accent, 0.12, 0.16, 0.1, x + 0.12, y - 0.12, z - 0.08);

  if (podId === "hornet") {
    box(body, mats.hull, 0.24, 0.14, 0.1, x - 0.16, y + 0.2, z - 0.02, 0.3);
    box(body, mats.hull, 0.24, 0.14, 0.1, x + 0.16, y + 0.2, z - 0.02, -0.3);
    box(body, mats.accent, 0.05, 0.05, 0.16, x - 0.16, y + 0.2, z - 0.12);
    box(body, mats.accent, 0.05, 0.05, 0.16, x + 0.16, y + 0.2, z - 0.12);
  } else {
    const dish = new THREE.Mesh(new THREE.CylinderGeometry(0.14, 0.16, 0.06, 12), mats.hull);
    dish.position.set(x, y + 0.22, z);
    dish.castShadow = true;
    body.add(dish);
    cyl(body, mats.accent, 0.02, 0.02, 0.1, x, y + 0.3, z, Math.PI / 2, 0);
  }
}
