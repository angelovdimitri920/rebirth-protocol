// Every gameplay-feel number lives here. Stage 1's go/no-go is entirely
// about how these feel, so they're centralized for fast iteration.

export const TUNING = {
  // --- Arena ---
  arena: {
    size: 32, // square, meters
    // Invisible boundary extends well above the visible wall, and a
    // matching invisible ceiling caps it -- a fully sealed Holosseum
    // volume, tall enough that even a maxed-out hover (~29m, see the
    // boost block below) can't clear it.
    wallHeight: 34,
  },

  // --- Robo movement ---
  move: {
    runSpeed: 9, // m/s ground
    airControlSpeed: 6, // m/s horizontal steering while airborne
    gravity: -28, // stronger than earth gravity: snappy jumps
    turnRate: 14, // rad/s mesh facing interpolation
  },

  // --- Boost economy (GAME_DESIGN §3.3) ---
  boost: {
    max: 100,
    jumpThrust: 13, // m/s vertical velocity while thrusting
    thrustDrainPerSec: 45, // holding jump/hover
    airDashCost: 28,
    airDashSpeed: 24, // m/s burst
    airDashDuration: 0.22, // s
    // Landing recovery: base + scale * fractionOfGaugeSpent
    landRecoveryBase: 0.1, // s
    landRecoveryScale: 0.55, // s at full spend
    overheatExtraRecovery: 0.5, // s added if gauge fully drained
    refillDelayAfterLanding: 0.05, // s before instant refill
  },

  // --- Health (GAME_DESIGN §2.2) ---
  health: {
    maxHp: 1000,
    // Doubled from 100 per playtest feedback: knockdown was coming off a
    // single big hit (or one melee combo) too often. Scaling the pool
    // uniformly doubles the hit count to knock down for every weapon at
    // once, without having to re-tune each part's enduranceDamage by hand.
    maxEndurance: 200,
    enduranceRegenPerSec: 35,
    enduranceRegenDelay: 1.8, // s unhit before regen starts
    knockdownDuration: 2.2, // s base
    knockdownMashReduction: 0.12, // s shaved per mash press
    knockdownMinDuration: 0.9, // s floor with mashing
    rebirthDuration: 2.5, // s invincibility on standing
  },

  // --- Gun ---
  gun: {
    damage: 35,
    enduranceDamage: 16,
    fireInterval: 0.38, // s between shots (hold to fire)
    projectileSpeed: 32, // m/s
    homingTurnRate: 2.2, // rad/s curve toward locked target
    homingRange: 24, // beyond this, shots fly straight (soft Stage-1 range gate)
    projectileLifetime: 2.0, // s
    muzzleHeight: 1.2,
  },

  // --- Melee: shared gap-closer mechanics. Per-weapon numbers (damage,
  // enduranceDamage, hitRange, hitArcDegrees, swingActiveTime, hitRecovery,
  // whiffRecovery, knockbackSpeed) live on each MeleeWeaponPart instead,
  // since "how it swings" is what makes weapons feel different. ---
  melee: {
    // Gap-closer: triggered when target beyond closeRange, within lungeRange
    closeRange: 4,
    lungeRange: 15,
    lungeSpeed: 26, // m/s toward target
    lungeMaxDuration: 0.65, // s before auto-whiff
    lungeReachDistance: 2.6, // m: close enough -> swing
  },

  // --- Lock-on ---
  lockOn: {
    maxRange: 40, // Stage 1: basically whole arena
  },

  // --- Camera: isometric-style elevated arena view (orthographic, no
  // perspective convergence) -- Custom Robo / Virtual On inspired, pushed
  // further overhead per playtest feedback. Fixed yaw (never rotates) --
  // movement is derived from the camera's actual orientation each frame
  // (see PlayerController), so this can be retuned freely without ever
  // re-breaking WASD mapping. ---
  camera: {
    height: 24, // camera height above the floor -- steep, near-overhead
    back: 8, // distance behind the player (-z)
    lookAhead: 1.5, // look-at point this far ahead of the player (+z)
    targetBias: 0.16, // look-at slides toward the enemy by this fraction
    followLerp: 8, // per-second smoothing factor
    frustumSize: 20, // orthographic: world units visible vertically
  },

  // --- Crates (destructible cover) ---
  crate: {
    hp: 3, // gun hits to destroy
    size: 1.6,
  },

  // --- Dummy AI ---
  ai: {
    orbitRadiusMin: 8,
    orbitRadiusMax: 14,
    fireInterval: 0.9,
    jumpChancePerSec: 0.35,
    dashChancePerSec: 0.4,
    decisionInterval: 1.2, // s between strafe-direction rethinks
  },
};
