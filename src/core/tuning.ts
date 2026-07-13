// Every gameplay-feel number lives here. Stage 1's go/no-go is entirely
// about how these feel, so they're centralized for fast iteration.

export const TUNING = {
  // --- Arena ---
  arena: {
    size: 32, // square, meters
    wallHeight: 6, // invisible boundary walls
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
    maxEndurance: 100,
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

  // --- Melee ---
  melee: {
    // Gap-closer: triggered when target beyond closeRange, within lungeRange
    closeRange: 4,
    lungeRange: 15,
    lungeSpeed: 26, // m/s toward target
    lungeMaxDuration: 0.65, // s before auto-whiff
    lungeReachDistance: 2.6, // m: close enough -> swing
    swingActiveTime: 0.18, // s hitbox live
    hitRecovery: 0.45, // s after a connecting swing
    whiffRecovery: 0.95, // s punishable recovery on miss
    damage: 130,
    enduranceDamage: 55,
    hitRange: 3.0, // m swing reach
    hitArcDegrees: 70, // total arc in front of attacker
    knockbackSpeed: 10, // m/s applied to victim
  },

  // --- Lock-on ---
  lockOn: {
    maxRange: 40, // Stage 1: basically whole arena
  },

  // --- Camera: Custom Robo-style elevated view from the player's side ---
  // Fixed yaw (never rotates), so screen directions == world directions.
  camera: {
    height: 13, // camera height above the floor
    back: 14, // distance behind the player (-z)
    lookAhead: 3.5, // look-at point this far ahead of the player (+z)
    targetBias: 0.22, // look-at slides toward the enemy by this fraction
    followLerp: 8, // per-second smoothing factor
    fov: 55,
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
