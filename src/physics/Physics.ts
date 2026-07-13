import RAPIER from "@dimforge/rapier3d-compat";

// Tags colliders so raycasts/hits can be resolved back to game entities.
export type ColliderTag =
  | { kind: "arena" }
  | { kind: "crate"; id: number }
  | { kind: "robo"; robo: "player" | "enemy" };

export class Physics {
  world: RAPIER.World;
  private tags = new Map<number, ColliderTag>();

  private constructor() {
    this.world = new RAPIER.World({ x: 0, y: -9.81, z: 0 });
    // Gravity is applied manually in Robo movement code (kinematic bodies);
    // world gravity only exists for any future dynamic debris.
  }

  static async create(): Promise<Physics> {
    await RAPIER.init();
    return new Physics();
  }

  step(dt: number): void {
    this.world.timestep = dt;
    this.world.step();
  }

  /** Release the WASM world (called when a duel is torn down). */
  dispose(): void {
    this.world.free();
    this.tags.clear();
  }

  tag(collider: RAPIER.Collider, tag: ColliderTag): void {
    this.tags.set(collider.handle, tag);
  }

  untag(collider: RAPIER.Collider): void {
    this.tags.delete(collider.handle);
  }

  tagOf(collider: RAPIER.Collider): ColliderTag | undefined {
    return this.tags.get(collider.handle);
  }
}
