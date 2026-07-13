import * as THREE from "three";
import RAPIER from "@dimforge/rapier3d-compat";
import { Physics } from "../physics/Physics";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";

// Arena with roguelite modifier rolls (GAME_DESIGN §3.4): each fight rolls
// {layout, hazard} for variety without hand-building dozens of stages.
// Design principles per docs/HOLOSSEUM_REFERENCE.md: breakable + unbreakable
// mix, walls vs hazards as distinct levers, one memorable landmark.

export type LayoutId = "crossfire" | "bastion" | "scatter";
export type HazardId = "none" | "lava" | "ice" | "conveyor";

export interface ArenaRoll {
  layout: LayoutId;
  hazard: HazardId;
}

export function rollArena(fightIndex: number): ArenaRoll {
  const layouts: LayoutId[] = ["crossfire", "bastion", "scatter"];
  const hazards: HazardId[] = ["none", "lava", "ice", "conveyor"];
  return {
    layout: layouts[Math.floor(Math.random() * layouts.length)],
    // First fight is always hazard-free; keeps run openings readable
    hazard:
      fightIndex === 0
        ? "none"
        : hazards[Math.floor(Math.random() * hazards.length)],
  };
}

interface Crate {
  id: number;
  hp: number;
  mesh: THREE.Mesh;
  collider: RAPIER.Collider;
}

interface LavaPool {
  center: THREE.Vector2;
  radius: number;
}

interface ConveyorStrip {
  minX: number;
  maxX: number;
  minZ: number;
  maxZ: number;
  push: THREE.Vector3;
}

const LAYOUTS: Record<
  LayoutId,
  {
    covers: { pos: [number, number]; size: [number, number, number]; rotY?: number }[];
    crates: [number, number][];
  }
> = {
  crossfire: {
    covers: [
      { pos: [-7, -6], size: [6, 2.6, 1] },
      { pos: [8, 5], size: [5, 3.2, 1], rotY: Math.PI / 5 },
      { pos: [2, -11], size: [4, 2.2, 1], rotY: -Math.PI / 8 },
    ],
    crates: [
      [-1.5, 1.5],
      [1.5, 1.5],
      [-1.5, -1.5],
      [1.5, -1.5],
      [0, 0],
      [-10, 8],
      [11, -8],
    ],
  },
  bastion: {
    // One big central landmark wall (Castle Citadel's lantern principle)
    covers: [
      { pos: [0, 0], size: [8, 3.6, 1.4] },
      { pos: [-11, 9], size: [3, 2, 1], rotY: Math.PI / 4 },
      { pos: [11, -9], size: [3, 2, 1], rotY: Math.PI / 4 },
    ],
    crates: [
      [-6, 4],
      [6, -4],
      [-6, -4],
      [6, 4],
    ],
  },
  scatter: {
    // Crate-heavy, low walls: cover you can chew through (Basic Stage)
    covers: [
      { pos: [-9, 0], size: [4, 1.8, 1], rotY: Math.PI / 2 },
      { pos: [9, 0], size: [4, 1.8, 1], rotY: Math.PI / 2 },
    ],
    crates: [
      [-4, 6],
      [0, 7],
      [4, 6],
      [-4, -6],
      [0, -7],
      [4, -6],
      [-2, 0],
      [2, 0],
      [0, 2.5],
      [0, -2.5],
    ],
  },
};

export class Arena {
  group = new THREE.Group();
  onCrateDestroyed: (at: THREE.Vector3) => void = () => {};
  readonly roll: ArenaRoll;
  private crates = new Map<number, Crate>();
  private nextCrateId = 1;
  private lavaPools: LavaPool[] = [];
  private conveyors: ConveyorStrip[] = [];
  private bodies: RAPIER.RigidBody[] = [];

  constructor(
    private physics: Physics,
    parent: THREE.Object3D,
    roll: ArenaRoll,
  ) {
    this.roll = roll;
    const size = TUNING.arena.size;
    const half = size / 2;

    // --- Floor ---
    const floorColor = roll.hazard === "ice" ? 0x5a7a9c : 0x3a3f5c;
    const floor = new THREE.Mesh(
      new THREE.BoxGeometry(size, 1, size),
      new THREE.MeshStandardMaterial({
        color: floorColor,
        roughness: roll.hazard === "ice" ? 0.15 : 0.8,
        metalness: roll.hazard === "ice" ? 0.5 : 0,
      }),
    );
    floor.position.y = -0.5;
    floor.receiveShadow = true;
    this.group.add(floor);

    const grid = new THREE.GridHelper(size, size, 0x7f9fff, 0x4a5480);
    grid.position.y = 0.02;
    this.group.add(grid);

    const floorBody = this.makeBody();
    const floorCol = physics.world.createCollider(
      RAPIER.ColliderDesc.cuboid(half, 0.5, half).setTranslation(0, -0.5, 0),
      floorBody,
    );
    physics.tag(floorCol, { kind: "arena" });

    // --- Perimeter walls: visible, connected, enclosing the Holosseum.
    // The collider extends above the visible wall (the classic "invisible
    // walls are impenetrable" rule) so nothing boosts out of the arena. ---
    const wallH = TUNING.arena.wallHeight;
    const visH = 2.6; // visible wall height
    const thick = 0.8;
    const wallMat = new THREE.MeshStandardMaterial({
      color: 0x2c3152,
      roughness: 0.55,
      metalness: 0.45,
    });
    const trimMat = new THREE.MeshBasicMaterial({ color: 0x3355ff });
    const wallDefs: { cx: number; cz: number; w: number; d: number }[] = [
      // Full-length north/south walls; east/west tuck between them
      { cx: 0, cz: -half - thick / 2, w: size + thick * 2, d: thick },
      { cx: 0, cz: half + thick / 2, w: size + thick * 2, d: thick },
      { cx: -half - thick / 2, cz: 0, w: thick, d: size },
      { cx: half + thick / 2, cz: 0, w: thick, d: size },
    ];
    for (const { cx, cz, w, d } of wallDefs) {
      const wall = new THREE.Mesh(new THREE.BoxGeometry(w, visH, d), wallMat);
      wall.position.set(cx, visH / 2, cz);
      wall.castShadow = true;
      wall.receiveShadow = true;
      this.group.add(wall);

      // Glowing trim along the top edge
      const trim = new THREE.Mesh(
        new THREE.BoxGeometry(w + 0.04, 0.14, d + 0.04),
        trimMat,
      );
      trim.position.set(cx, visH + 0.07, cz);
      this.group.add(trim);

      const body = this.makeBody();
      const col = physics.world.createCollider(
        RAPIER.ColliderDesc.cuboid(w / 2, wallH / 2, d / 2).setTranslation(
          cx,
          wallH / 2,
          cz,
        ),
        body,
      );
      physics.tag(col, { kind: "arena" });
    }

    // --- Layout: cover walls + crates ---
    const layout = LAYOUTS[roll.layout];
    const coverMat = new THREE.MeshStandardMaterial({
      color: 0x565c80,
      roughness: 0.6,
      metalness: 0.3,
    });
    for (const def of layout.covers) {
      const [w, h, d] = def.size;
      const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), coverMat);
      mesh.position.set(def.pos[0], h / 2, def.pos[1]);
      if (def.rotY) mesh.rotation.y = def.rotY;
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      this.group.add(mesh);

      const body = this.makeBody();
      const col = physics.world.createCollider(
        RAPIER.ColliderDesc.cuboid(w / 2, h / 2, d / 2)
          .setTranslation(def.pos[0], h / 2, def.pos[1])
          .setRotation(quatFromY(def.rotY ?? 0)),
        body,
      );
      physics.tag(col, { kind: "arena" });
    }
    for (const [x, z] of layout.crates) this.spawnCrate(x, z);

    // --- Hazards ---
    if (roll.hazard === "lava") {
      const poolDefs: [number, number, number][] = [
        [-8, 3, 3],
        [8, -3, 3],
        [0, 10, 2.4],
      ];
      for (const [x, z, r] of poolDefs) {
        this.lavaPools.push({ center: new THREE.Vector2(x, z), radius: r });
        const pool = new THREE.Mesh(
          new THREE.CircleGeometry(r, 24),
          new THREE.MeshStandardMaterial({
            color: 0xff4400,
            emissive: 0xff3300,
            emissiveIntensity: 0.9,
          }),
        );
        pool.rotation.x = -Math.PI / 2;
        pool.position.set(x, 0.04, z);
        this.group.add(pool);
      }
    } else if (roll.hazard === "conveyor") {
      const stripDefs: { z: number; dir: number }[] = [
        { z: -6, dir: 1 },
        { z: 6, dir: -1 },
      ];
      for (const { z, dir } of stripDefs) {
        this.conveyors.push({
          minX: -half,
          maxX: half,
          minZ: z - 2,
          maxZ: z + 2,
          push: new THREE.Vector3(4.5 * dir, 0, 0),
        });
        const strip = new THREE.Mesh(
          new THREE.BoxGeometry(size, 0.06, 4),
          new THREE.MeshStandardMaterial({
            color: dir > 0 ? 0x3c5c3c : 0x5c3c5c,
            emissive: dir > 0 ? 0x1c3c1c : 0x3c1c3c,
            emissiveIntensity: 0.5,
          }),
        );
        strip.position.set(0, 0.04, z);
        this.group.add(strip);
      }
    }

    parent.add(this.group);
  }

  private makeBody(): RAPIER.RigidBody {
    const body = this.physics.world.createRigidBody(
      RAPIER.RigidBodyDesc.fixed(),
    );
    this.bodies.push(body);
    return body;
  }

  /** Per-step hazard application; sets robo.onIce / robo.drift + lava DoT. */
  applyHazards(dt: number, robos: Robo[]): void {
    for (const robo of robos) {
      robo.onIce = this.roll.hazard === "ice";
      robo.drift.set(0, 0, 0);
      if (!robo.grounded || robo.health.state === "dead") continue;

      const pos = robo.position;
      for (const pool of this.lavaPools) {
        if (
          new THREE.Vector2(pos.x, pos.z).distanceTo(pool.center) <=
          pool.radius
        ) {
          // Hazard DoT bypasses the shield: environmental, not directional
          robo.health.takeHit(24 * dt, 14 * dt);
        }
      }
      for (const strip of this.conveyors) {
        if (
          pos.x >= strip.minX &&
          pos.x <= strip.maxX &&
          pos.z >= strip.minZ &&
          pos.z <= strip.maxZ
        ) {
          robo.drift.copy(strip.push);
        }
      }
    }
  }

  private spawnCrate(x: number, z: number): void {
    const s = TUNING.crate.size;
    const mesh = new THREE.Mesh(
      new THREE.BoxGeometry(s, s, s),
      new THREE.MeshStandardMaterial({ color: 0x8a6a3a, roughness: 0.9 }),
    );
    mesh.position.set(x, s / 2, z);
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    this.group.add(mesh);

    const body = this.makeBody();
    const collider = this.physics.world.createCollider(
      RAPIER.ColliderDesc.cuboid(s / 2, s / 2, s / 2).setTranslation(
        x,
        s / 2,
        z,
      ),
      body,
    );
    const id = this.nextCrateId++;
    this.physics.tag(collider, { kind: "crate", id });
    this.crates.set(id, { id, hp: TUNING.crate.hp, mesh, collider });
  }

  /** Bomb blasts wipe out any crate inside the radius. */
  damageCratesInRadius(at: THREE.Vector3, radius: number): void {
    for (const crate of [...this.crates.values()]) {
      if (crate.mesh.position.distanceTo(at) <= radius + TUNING.crate.size) {
        crate.hp = 1;
        this.damageCrate(crate.id);
      }
    }
  }

  /** Returns true if the crate was destroyed. Crates don't respawn. */
  damageCrate(id: number): boolean {
    const crate = this.crates.get(id);
    if (!crate) return false;
    crate.hp -= 1;
    if (crate.hp <= 0) {
      const at = crate.mesh.position.clone();
      this.group.remove(crate.mesh);
      this.physics.untag(crate.collider);
      this.physics.world.removeRigidBody(crate.collider.parent()!);
      this.crates.delete(id);
      this.onCrateDestroyed(at);
      return true;
    }
    // Darken as it takes damage
    (crate.mesh.material as THREE.MeshStandardMaterial).color.multiplyScalar(
      0.7,
    );
    return false;
  }
}

function quatFromY(angle: number): RAPIER.Rotation {
  return {
    x: 0,
    y: Math.sin(angle / 2),
    z: 0,
    w: Math.cos(angle / 2),
  };
}
