import { TUNING } from "../core/tuning";
import { sfx } from "../core/sfx";

// Twin-bar system per GAME_DESIGN §2.2: HP pool + endurance bar.
// Endurance empties -> knockdown (mash to shorten) -> rebirth invincibility.
// Judgment call for Stage 1: robos are fully invulnerable while downed (not
// just damage-reduced) to keep the comeback rhythm clean with one enemy.

export type HealthState = "active" | "knockdown" | "rebirth" | "dead";

export type HitResult = "hit" | "knockdown" | "killed" | "invulnerable";

export class Health {
  private readonly baseMaxHp: number;
  /** Extra max HP from items (Scrap Plating). */
  maxHpBonus = 0;
  hp: number;
  endurance = TUNING.health.maxEndurance;
  state: HealthState = "active";
  /** Remaining time in knockdown or rebirth, whichever is active. */
  stateTimer = 0;
  private timeSinceHit = Infinity;

  constructor(maxHp: number) {
    this.baseMaxHp = maxHp;
    this.hp = maxHp;
  }

  get maxHp(): number {
    return this.baseMaxHp + this.maxHpBonus;
  }

  heal(amount: number): void {
    if (this.state === "dead") return;
    this.hp = Math.min(this.maxHp, this.hp + amount);
  }

  restoreEndurance(amount: number): void {
    if (this.state !== "active") return;
    this.endurance = Math.min(
      TUNING.health.maxEndurance,
      this.endurance + amount,
    );
  }

  takeHit(damage: number, enduranceDamage: number): HitResult {
    if (this.state === "dead") return "invulnerable";
    if (this.state === "knockdown" || this.state === "rebirth")
      return "invulnerable";

    this.hp -= damage;
    this.timeSinceHit = 0;
    if (this.hp <= 0) {
      this.hp = 0;
      this.state = "dead";
      return "killed";
    }

    this.endurance -= enduranceDamage;
    if (this.endurance <= 0) {
      this.endurance = 0;
      this.knockDown();
      return "knockdown";
    }
    return "hit";
  }

  /** Direct endurance drain outside the normal hit pipeline (shield-parry
   *  punish) -- still triggers knockdown if it empties the bar. */
  drainEndurance(amount: number): void {
    if (this.state !== "active") return;
    this.endurance -= amount;
    if (this.endurance <= 0) {
      this.endurance = 0;
      this.knockDown();
    }
  }

  /** Immediate knockdown regardless of endurance (shield guard-break). */
  knockDown(): void {
    if (this.state !== "active") return;
    this.endurance = 0;
    this.state = "knockdown";
    this.stateTimer = TUNING.health.knockdownDuration;
  }

  /** Mash press while downed: shaves recovery time. */
  mash(): void {
    if (this.state === "knockdown") {
      this.stateTimer = Math.max(
        TUNING.health.knockdownMinDuration -
          (TUNING.health.knockdownDuration - this.stateTimer),
        this.stateTimer - TUNING.health.knockdownMashReduction,
      );
      sfx.mashTick();
    }
  }

  update(dt: number): void {
    if (this.state === "dead") return;
    this.timeSinceHit += dt;

    if (this.state === "knockdown") {
      this.stateTimer -= dt;
      if (this.stateTimer <= 0) {
        // Stand up: full endurance + rebirth invincibility window
        this.state = "rebirth";
        this.stateTimer = TUNING.health.rebirthDuration;
        this.endurance = TUNING.health.maxEndurance;
      }
    } else if (this.state === "rebirth") {
      this.stateTimer -= dt;
      if (this.stateTimer <= 0) this.state = "active";
    } else if (
      this.timeSinceHit > TUNING.health.enduranceRegenDelay &&
      this.endurance < TUNING.health.maxEndurance
    ) {
      this.endurance = Math.min(
        TUNING.health.maxEndurance,
        this.endurance + TUNING.health.enduranceRegenPerSec * dt,
      );
    }
  }
}
