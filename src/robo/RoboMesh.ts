import * as THREE from "three";

// Primitive-built humanoid shell: the balanced all-rounder archetype per
// docs/ROBOT_SHELL_DESIGN.md §4 — average proportions, no exaggerated
// silhouette. ~2m tall. One hull color + one accent per robo.

export interface RoboMeshParts {
  root: THREE.Group; // positioned at capsule center (y = 1m above feet)
  body: THREE.Group; // rotates to face; tilts for poses
  gunMuzzle: THREE.Object3D; // world-space muzzle for projectile spawn
  materials: THREE.MeshStandardMaterial[];
}

export function buildRoboMesh(hull: number, accent: number): RoboMeshParts {
  const root = new THREE.Group();
  const body = new THREE.Group();
  root.add(body);

  const hullMat = new THREE.MeshStandardMaterial({
    color: hull,
    roughness: 0.5,
    metalness: 0.55,
  });
  const accentMat = new THREE.MeshStandardMaterial({
    color: accent,
    roughness: 0.35,
    metalness: 0.4,
    emissive: accent,
    emissiveIntensity: 0.35,
  });
  const jointMat = new THREE.MeshStandardMaterial({
    color: 0x222228,
    roughness: 0.8,
    metalness: 0.3,
  });

  const box = (
    w: number,
    h: number,
    d: number,
    mat: THREE.MeshStandardMaterial,
    x: number,
    y: number,
    z: number,
    parent: THREE.Object3D = body,
  ): THREE.Mesh => {
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
    m.position.set(x, y, z);
    m.castShadow = true;
    parent.add(m);
    return m;
  };

  // All positions relative to capsule center (1m above feet).
  // Torso
  box(0.62, 0.55, 0.4, hullMat, 0, 0.28, 0);
  box(0.4, 0.2, 0.3, accentMat, 0, 0.28, 0.12); // chest core
  // Pelvis
  box(0.44, 0.25, 0.34, jointMat, 0, -0.08, 0);
  // Head
  box(0.28, 0.24, 0.28, hullMat, 0, 0.72, 0);
  box(0.22, 0.06, 0.05, accentMat, 0, 0.73, 0.13); // visor
  // Shoulders
  box(0.24, 0.22, 0.3, hullMat, -0.46, 0.42, 0);
  box(0.24, 0.22, 0.3, hullMat, 0.46, 0.42, 0);
  // Arms (right arm = gun arm, mount point per shell doc)
  box(0.16, 0.5, 0.18, jointMat, -0.46, 0.05, 0);
  box(0.16, 0.5, 0.18, jointMat, 0.46, 0.05, 0);
  const gunBox = box(0.14, 0.14, 0.5, accentMat, 0.46, -0.05, 0.28);
  // Legs
  box(0.2, 0.62, 0.24, hullMat, -0.18, -0.55, 0);
  box(0.2, 0.62, 0.24, hullMat, 0.18, -0.55, 0);
  // Feet
  box(0.22, 0.12, 0.34, jointMat, -0.18, -0.9, 0.04);
  box(0.22, 0.12, 0.34, jointMat, 0.18, -0.9, 0.04);
  // Backpack (thruster block — boost identity)
  box(0.4, 0.4, 0.18, jointMat, 0, 0.3, -0.28);
  box(0.12, 0.16, 0.1, accentMat, -0.12, 0.18, -0.36);
  box(0.12, 0.16, 0.1, accentMat, 0.12, 0.18, -0.36);

  const gunMuzzle = new THREE.Object3D();
  gunMuzzle.position.set(0, 0, 0.3);
  gunBox.add(gunMuzzle);

  return { root, body, gunMuzzle, materials: [hullMat, accentMat, jointMat] };
}
