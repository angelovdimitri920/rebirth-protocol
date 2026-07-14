// Keyboard + mouse + gamepad (Xbox layout via the standard Gamepad API
// mapping). The camera rotates to keep both fighters framed, but movement
// is derived from the camera's actual orientation each frame (see
// PlayerController), so it can't drift out of sync -- mouse is buttons
// only, no mouse-look/pointer-lock.
//
// Gamepad mapping (HOLOSSEUM_REFERENCE.md "Robo Controller Layout"), L/R
// read off the Xbox triggers: Stick=Move, A=Jump (double-tap while
// airborne also triggers the chassis's air-dash/mobility move, same as
// X=Dash), B=Fire Gun/Melee, L=Fire Pod, R=Fire Bomb/Block Shield,
// X=Dash, Y=Switch Targets. Since gun/melee and bomb/shield are
// mutually-exclusive loadout choices here, B and R each do double duty
// for whichever half of the pair is actually equipped -- the underlying
// combat systems already no-op on the unequipped half, same as LMB/RMB do
// on keyboard. Holding L or R (once the pod is deployed / while aiming a
// bomb) also redirects the stick to steer the pod's launch direction or
// nudge the bomb reticule (PlayerController), instead of moving.
//
// Gamepad button -> action (standard mapping indices):
//   Left stick     move (analog, direction only -- no analog speed model);
//                  redirected to aim-steering while L/R is held, see above
//   D-pad          menu navigation (hangar/pause), merged onto Arrow keys
//   A (0)          jump/hover, mash to recover from knockdown; menu confirm
//   B (1)          right arm: fire gun (held) / swing melee (pressed)
//   X (2)          dash
//   Y (3)          switch targets / lock-on toggle
//   LT (6)         fire pod (deploy/recall on press; hold to steer aim)
//   RT (7)         left arm: throw bomb (held to aim -- hold to steer,
//                  release to fire) / hold shield (held)
//   Start (9)      pause
//
// Gamepad buttons are merged into the same key-code space the keyboard
// uses ("Space", "ShiftLeft", etc.) so every consumer (PlayerController,
// Game's pause listener, menu navigation) reads gamepad and keyboard
// through one API without caring which produced the input.

import { sfx } from "./sfx";

const BUTTON_TO_CODE: [index: number, code: string][] = [
  [0, "Space"],
  [2, "ShiftLeft"],
  [3, "Tab"],
  [6, "KeyE"],
  [7, "KeyQ"],
  [9, "KeyP"],
  [12, "ArrowUp"],
  [13, "ArrowDown"],
  [14, "ArrowLeft"],
  [15, "ArrowRight"],
];
const BUTTON_RIGHT_ARM = 1; // B: fire gun (held) / swing melee (pressed)
const STICK_DEADZONE = 0.2;
const DOUBLE_TAP_WINDOW_MS = 300;

export class Input {
  private keys = new Set<string>();
  private pressed = new Set<string>(); // cleared each frame: edge-triggered
  private doubleTap = new Set<string>(); // cleared each frame: edge-triggered
  private lastPressTime = new Map<string, number>();
  private mouseFire = false;
  private padFire = false;
  private padHeld = new Set<string>();
  private padPrevPressed = new Map<number, boolean>();
  meleePressed = false;
  gamepadConnected = false;
  /** Left-stick direction this frame, or null if centered/no pad. x/z map
   *  directly onto the screen-right/screen-forward axes in PlayerController;
   *  magnitude is already clamped to [0,1]. */
  stickMoveDir: { x: number; z: number } | null = null;

  get fireHeld(): boolean {
    return this.mouseFire || this.padFire;
  }

  /** Records a fresh press of `code`, detecting a double-tap gesture
   *  (A+A: jump-again-while-airborne triggers the chassis air-dash). */
  private registerPress(code: string): void {
    this.pressed.add(code);
    const now = performance.now();
    const last = this.lastPressTime.get(code) ?? -Infinity;
    if (now - last <= DOUBLE_TAP_WINDOW_MS) this.doubleTap.add(code);
    this.lastPressTime.set(code, now);
  }

  constructor(canvas: HTMLCanvasElement) {
    window.addEventListener("keydown", (e) => {
      if (!this.keys.has(e.code)) this.registerPress(e.code);
      this.keys.add(e.code);
    });
    window.addEventListener("keyup", (e) => this.keys.delete(e.code));
    window.addEventListener("blur", () => {
      this.keys.clear();
      this.mouseFire = false;
    });

    canvas.addEventListener("mousedown", (e) => {
      sfx.ensure(); // user gesture: keeps the AudioContext unlocked
      if (e.button === 0) this.mouseFire = true;
      if (e.button === 2) this.meleePressed = true;
    });
    window.addEventListener("mouseup", (e) => {
      if (e.button === 0) this.mouseFire = false;
    });
    canvas.addEventListener("contextmenu", (e) => e.preventDefault());

    window.addEventListener("gamepadconnected", () => {
      this.gamepadConnected = true;
    });
    window.addEventListener("gamepaddisconnected", () => {
      this.gamepadConnected = false;
      this.padHeld.clear();
      this.padFire = false;
      this.stickMoveDir = null;
    });
  }

  held(code: string): boolean {
    return this.keys.has(code) || this.padHeld.has(code);
  }

  /** True only on the frame the key went down. */
  justPressed(code: string): boolean {
    return this.pressed.has(code);
  }

  /** True only on the frame a second press of `code` lands within
   *  DOUBLE_TAP_WINDOW_MS of the previous one. */
  wasDoubleTapped(code: string): boolean {
    return this.doubleTap.has(code);
  }

  /** Poll the gamepad. Call once per rendered frame (not per fixed step) so
   *  held/edge state stays stable across however many sim steps run this
   *  frame -- mirrors how the event-driven keyboard state behaves. */
  poll(): void {
    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    const gp = pads[0];
    if (!gp) {
      this.padHeld.clear();
      this.padFire = false;
      this.stickMoveDir = null;
      return;
    }

    const lx = gp.axes[0] ?? 0;
    const ly = gp.axes[1] ?? 0;
    const mag = Math.hypot(lx, ly);
    this.stickMoveDir = mag > STICK_DEADZONE ? { x: lx, z: -ly } : null;

    this.padHeld.clear();
    for (const [index, code] of BUTTON_TO_CODE) {
      const btn = gp.buttons[index];
      const isDown = !!btn?.pressed;
      const wasDown = this.padPrevPressed.get(index) ?? false;
      if (isDown) {
        this.padHeld.add(code);
        if (!wasDown) this.registerPress(code);
      }
      this.padPrevPressed.set(index, isDown);
    }

    const bDown = !!gp.buttons[BUTTON_RIGHT_ARM]?.pressed;
    const bWasDown = this.padPrevPressed.get(BUTTON_RIGHT_ARM) ?? false;
    if (bDown) sfx.ensure();
    this.padFire = bDown; // gun: fire while held
    if (bDown && !bWasDown) this.meleePressed = true; // melee: swing on press
    this.padPrevPressed.set(BUTTON_RIGHT_ARM, bDown);
  }

  /** Call once per rendered frame, after all systems have read input. */
  endFrame(): void {
    this.pressed.clear();
    this.doubleTap.clear();
    this.meleePressed = false;
  }
}
