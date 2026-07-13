import * as THREE from "three";
import { Robo } from "../robo/Robo";
import { Projectiles } from "../combat/Projectiles";

// Boons + stacking items (GAME_DESIGN §4), hung off a small set of
// universal trigger verbs (on-hit / on-kill / on-knockdown / on-dash) so
// synergies emerge combinatorially. One Effects instance per run, owned by
// the player; rebound to the fresh robo each duel.

export type BoonSlot = "gun" | "bomb" | "pod" | "melee" | "dash";

export interface Boon {
  id: string;
  name: string;
  slot: BoonSlot;
  blurb: string;
}

export const BOON_POOL: Boon[] = [
  {
    id: "splinter",
    name: "Splinter Rounds",
    slot: "gun",
    blurb: "Gun hits burst into two weak darts that curve back in.",
  },
  {
    id: "overcharge",
    name: "Overcharge Coils",
    slot: "gun",
    blurb: "+45% gun damage while your boost gauge is above 70.",
  },
  {
    id: "cluster",
    name: "Cluster Shell",
    slot: "bomb",
    blurb: "Bomb detonations scatter two follow-up mini-blasts.",
  },
  {
    id: "rearm",
    name: "Rearm Protocol",
    slot: "bomb",
    blurb: "Knocking the enemy down instantly rearms your bomb.",
  },
  {
    id: "overclock",
    name: "Overclocked Cell",
    slot: "pod",
    blurb: "Pod recharges 80% faster and fires 25% quicker.",
  },
  {
    id: "vampiric",
    name: "Vampiric Relay",
    slot: "pod",
    blurb: "Pod hits feed you 5 endurance each.",
  },
  {
    id: "guardcrusher",
    name: "Guard Crusher",
    slot: "melee",
    blurb: "Melee strikes deal triple damage to shields.",
  },
  {
    id: "momentum",
    name: "Momentum Edge",
    slot: "melee",
    blurb: "For 2s after a dash, melee hits 60% harder.",
  },
  {
    id: "afterimage",
    name: "Afterimage",
    slot: "dash",
    blurb: "Dashing leaves a crackling afterimage that detonates on contact.",
  },
  {
    id: "slipstream",
    name: "Slipstream",
    slot: "dash",
    blurb: "Dashes cost 35% less boost.",
  },
];

export interface Item {
  id: string;
  name: string;
  blurb: string;
}

export const ITEM_POOL: Item[] = [
  { id: "plating", name: "Scrap Plating", blurb: "+40 max HP" },
  { id: "kinetic", name: "Kinetic Cell", blurb: "Dashing restores 7 endurance" },
  {
    id: "trigger",
    name: "Trigger Coil",
    blurb: "Gun hits may instantly reload (diminishing)",
  },
  { id: "impact", name: "Impact Converter", blurb: "+3 damage on every hit" },
  {
    id: "leech",
    name: "Leech Node",
    blurb: "Knockdowns you inflict heal 30 HP",
  },
];

// Hyperbolic scaling for %-chance stacks (Risk of Rain 2 model, §4):
// approaches 1 but never trivially hits it.
export function hyperbolicChance(stacks: number, a: number): number {
  return 1 - 1 / (1 + a * stacks);
}

interface Afterimage {
  mesh: THREE.Mesh;
  timer: number;
  armed: boolean;
}

export class Effects {
  private boons = new Set<string>();
  readonly boonList: Boon[] = [];
  readonly itemStacks = new Map<string, number>();

  // Rebound per duel
  private owner!: Robo;
  private enemy!: Robo;
  private duelRoot!: THREE.Object3D;
  private projectiles!: Projectiles;
  /** Set by the duel so rearm/trigger boons can reach weapon cooldowns. */
  resetBombCooldown: () => void = () => {};
  resetGunCooldown: () => void = () => {};

  private momentumTimer = 0;
  private afterimages: Afterimage[] = [];

  has(id: string): boolean {
    return this.boons.has(id);
  }

  stacks(id: string): number {
    return this.itemStacks.get(id) ?? 0;
  }

  addBoon(boon: Boon): void {
    this.boons.add(boon.id);
    this.boonList.push(boon);
  }

  addItem(item: Item): void {
    this.itemStacks.set(item.id, this.stacks(item.id) + 1);
    if (item.id === "plating") {
      // Immediate effect; also applied to maxHp at duel start
      this.owner.health.maxHpBonus += 40;
      this.owner.health.hp += 40;
    }
  }

  bind(
    owner: Robo,
    enemy: Robo,
    duelRoot: THREE.Object3D,
    projectiles: Projectiles,
  ): void {
    this.owner = owner;
    this.enemy = enemy;
    this.duelRoot = duelRoot;
    this.projectiles = projectiles;
    this.momentumTimer = 0;
    this.afterimages = [];
    owner.health.maxHpBonus = 40 * this.stacks("plating");
  }

  // --- Stat queries used by combat systems ---

  gunDamageMult(): number {
    let m = 1;
    if (this.has("overcharge") && this.owner.boost > 70) m *= 1.45;
    return m;
  }

  meleeDamageMult(): number {
    let m = 1;
    if (this.has("momentum") && this.momentumTimer > 0) m *= 1.6;
    return m;
  }

  meleeShieldMult(): number {
    return this.has("guardcrusher") ? 3 : 1;
  }

  dashCostMult(): number {
    return this.has("slipstream") ? 0.65 : 1;
  }

  podRegenMult(): number {
    return this.has("overclock") ? 1.8 : 1;
  }

  podFireIntervalMult(): number {
    return this.has("overclock") ? 0.75 : 1;
  }

  flatDamageBonus(): number {
    return 3 * this.stacks("impact");
  }

  // --- Trigger verbs ---

  onHit(source: "gun" | "melee" | "bomb" | "pod", at: THREE.Vector3): void {
    if (source === "gun" && this.has("splinter")) {
      for (let i = 0; i < 2; i++) {
        const jitter = new THREE.Vector3(
          (Math.random() - 0.5) * 3,
          1 + Math.random(),
          (Math.random() - 0.5) * 3,
        );
        this.projectiles.spawn(at.clone().add(jitter), at, "player", {
          damage: 8 + this.flatDamageBonus(),
          enduranceDamage: 4,
          speed: 22,
          homingTurnRate: 4,
        });
      }
    }
    if (source === "gun" && this.stacks("trigger") > 0) {
      if (Math.random() < hyperbolicChance(this.stacks("trigger"), 0.15)) {
        this.resetGunCooldown();
      }
    }
    if (source === "pod" && this.has("vampiric")) {
      this.owner.health.restoreEndurance(5);
    }
  }

  onKnockdown(): void {
    if (this.has("rearm")) this.resetBombCooldown();
    const leech = this.stacks("leech");
    if (leech > 0) this.owner.health.heal(30 * leech);
  }

  onDash(): void {
    this.momentumTimer = 2;
    const kinetic = this.stacks("kinetic");
    if (kinetic > 0) this.owner.health.restoreEndurance(7 * kinetic);
    if (this.has("afterimage")) {
      const mesh = new THREE.Mesh(
        new THREE.SphereGeometry(0.6, 10, 10),
        new THREE.MeshBasicMaterial({
          color: 0x33e0ff,
          transparent: true,
          opacity: 0.5,
        }),
      );
      mesh.position.copy(this.owner.position);
      this.duelRoot.add(mesh);
      this.afterimages.push({ mesh, timer: 1.5, armed: true });
    }
  }

  /** Cluster Shell hook: called by Bomb on detonation. Returns extra blast
   *  offsets to detonate (empty when the boon isn't held). */
  clusterOffsets(): THREE.Vector3[] {
    if (!this.has("cluster")) return [];
    return [0, 1].map(
      () =>
        new THREE.Vector3(
          (Math.random() - 0.5) * 5,
          0,
          (Math.random() - 0.5) * 5,
        ),
    );
  }

  update(dt: number): void {
    this.momentumTimer -= dt;

    for (let i = this.afterimages.length - 1; i >= 0; i--) {
      const a = this.afterimages[i];
      a.timer -= dt;
      (a.mesh.material as THREE.MeshBasicMaterial).opacity = Math.max(
        0.15,
        a.timer / 1.5,
      ) * 0.6;
      if (
        a.armed &&
        this.enemy.health.state !== "dead" &&
        a.mesh.position.distanceTo(this.enemy.position) < 1.6
      ) {
        a.armed = false;
        a.timer = Math.min(a.timer, 0.15);
        const dir = this.enemy.position.clone().sub(a.mesh.position);
        this.enemy.receiveHit(40 + this.flatDamageBonus(), 20, dir.normalize());
      }
      if (a.timer <= 0) {
        this.duelRoot.remove(a.mesh);
        this.afterimages.splice(i, 1);
      }
    }
  }
}

/** No-op effects for the AI enemy: every query returns neutral values. */
export function neutralEffects(): Effects {
  return new Effects();
}
