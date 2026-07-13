import * as THREE from "three";
import RAPIER from "@dimforge/rapier3d-compat";
import { Physics } from "../physics/Physics";
import { TUNING } from "../core/tuning";

// Stage 1 arena, per docs/HOLOSSEUM_REFERENCE.md §4: neutral Basic-Stage-style
// duel space. Flat bounded floor, destructible crates mid-arena, a few
// unbreakable wall segments for line-of-sight play, invisible boundary walls.

interface Crate {
  id: number;
  hp: number;
  mesh: THREE.Mesh;
  collider: RAPIER.Collider;
}

export class Arena {
  group = new THREE.Group();
  private crates = new Map<number, Crate>();
  private nextCrateId = 1;

  constructor(
    private physics: Physics,
    scene: THREE.Scene,
  ) {
    const size = TUNING.arena.size;
    const half = size / 2;

    // --- Floor: holographic grid look ---
    const floor = new THREE.Mesh(
      new THREE.BoxGeometry(size, 1, size),
      new THREE.MeshStandardMaterial({ color: 0x3a3f5c, roughness: 0.8 }),
    );
    floor.position.y = -0.5;
    floor.receiveShadow = true;
    this.group.add(floor);

    const grid = new THREE.GridHelper(size, size, 0x7f9fff, 0x4a5480);
    grid.position.y = 0.02;
    this.group.add(grid);

    // Glowing rim strips marking the holosseum boundary
    const rimMat = new THREE.MeshBasicMaterial({ color: 0x3355ff });
    const rimStrips: [number, number, number, number][] = [
      // [cx, cz, w, d]
      [0, -half - 0.15, size + 0.6, 0.3],
      [0, half + 0.15, size + 0.6, 0.3],
      [-half - 0.15, 0, 0.3, size],
      [half + 0.15, 0, 0.3, size],
    ];
    for (const [cx, cz, w, d] of rimStrips) {
      const strip = new THREE.Mesh(new THREE.BoxGeometry(w, 0.3, d), rimMat);
      strip.position.set(cx, 0.05, cz);
      this.group.add(strip);
    }

    const floorBody = physics.world.createRigidBody(
      RAPIER.RigidBodyDesc.fixed(),
    );
    const floorCol = physics.world.createCollider(
      RAPIER.ColliderDesc.cuboid(half, 0.5, half).setTranslation(0, -0.5, 0),
      floorBody,
    );
    physics.tag(floorCol, { kind: "arena" });

    // --- Invisible boundary walls ---
    const wallH = TUNING.arena.wallHeight;
    const wallDefs: [number, number, number, number][] = [
      // [cx, cz, halfX, halfZ]
      [0, -half, half, 0.5],
      [0, half, half, 0.5],
      [-half, 0, 0.5, half],
      [half, 0, 0.5, half],
    ];
    for (const [cx, cz, hx, hz] of wallDefs) {
      const body = physics.world.createRigidBody(RAPIER.RigidBodyDesc.fixed());
      const col = physics.world.createCollider(
        RAPIER.ColliderDesc.cuboid(hx, wallH / 2, hz).setTranslation(
          cx,
          wallH / 2,
          cz,
        ),
        body,
      );
      physics.tag(col, { kind: "arena" });
    }

    // --- Unbreakable cover walls (asymmetric, break line of sight) ---
    const coverMat = new THREE.MeshStandardMaterial({
      color: 0x565c80,
      roughness: 0.6,
      metalness: 0.3,
    });
    const coverDefs: {
      pos: [number, number];
      size: [number, number, number]; // w, h, d
      rotY?: number;
    }[] = [
      { pos: [-7, -6], size: [6, 2.6, 1] },
      { pos: [8, 5], size: [5, 3.2, 1], rotY: Math.PI / 5 },
      { pos: [2, -11], size: [4, 2.2, 1], rotY: -Math.PI / 8 },
    ];
    for (const def of coverDefs) {
      const [w, h, d] = def.size;
      const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), coverMat);
      mesh.position.set(def.pos[0], h / 2, def.pos[1]);
      if (def.rotY) mesh.rotation.y = def.rotY;
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      this.group.add(mesh);

      const body = physics.world.createRigidBody(RAPIER.RigidBodyDesc.fixed());
      const col = physics.world.createCollider(
        RAPIER.ColliderDesc.cuboid(w / 2, h / 2, d / 2)
          .setTranslation(def.pos[0], h / 2, def.pos[1])
          .setRotation(quatFromY(def.rotY ?? 0)),
        body,
      );
      physics.tag(col, { kind: "arena" });
    }

    // --- Destructible crates, clustered mid-arena (Basic Stage style) ---
    const cratePositions: [number, number][] = [
      [-1.5, 1.5],
      [1.5, 1.5],
      [-1.5, -1.5],
      [1.5, -1.5],
      [0, 0],
      [-10, 8],
      [11, -8],
    ];
    for (const [x, z] of cratePositions) this.spawnCrate(x, z);

    scene.add(this.group);
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

    const body = this.physics.world.createRigidBody(
      RAPIER.RigidBodyDesc.fixed(),
    );
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
      this.group.remove(crate.mesh);
      this.physics.untag(crate.collider);
      this.physics.world.removeRigidBody(crate.collider.parent()!);
      this.crates.delete(id);
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
