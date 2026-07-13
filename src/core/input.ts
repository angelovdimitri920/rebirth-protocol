// Keyboard + mouse + gamepad (Xbox layout via the standard Gamepad API
// mapping). The camera never rotates (Custom Robo-style fixed view), so
// there's no mouse-look and no pointer lock -- mouse is buttons only.
//
// Gamepad button -> action (standard mapping indices):
//   Left stick     move (analog, direction only -- no analog speed model)
//   D-pad          menu navigation (hangar/pause), merged onto Arrow keys
//   A (0)          jump/hover, mash to recover from knockdown; menu confirm
//   B (1)          dash
//   X (2)          bomb / hold to raise shield
//   Y (3)          pod deploy/recall
//   LB (4)         lock-on toggle
//   RB (5)         melee
//   RT (7)         gun (held)
//   Start (9)      pause
//
// Gamepad buttons are merged into the same key-code space the keyboard
// uses ("Space", "ShiftLeft", etc.) so every consumer (PlayerController,
// Game's pause listener, menu navigation) reads gamepad and keyboard
// through one API without caring which produced the input.

import { sfx } from "./sfx";

const BUTTON_TO_CODE: [index: number, code: string][] = [
  [0, "Space"],
  [1, "ShiftLeft"],
  [2, "KeyQ"],
  [3, "KeyE"],
  [4, "Tab"],
  [9, "KeyP"],
  [12, "ArrowUp"],
  [13, "ArrowDown"],
  [14, "ArrowLeft"],
  [15, "ArrowRight"],
];
const BUTTON_MELEE = 5;
const BUTTON_FIRE = 7;
const STICK_DEADZONE = 0.2;

export class Input {
  private keys = new Set<string>();
  private pressed = new Set<string>(); // cleared each frame: edge-triggered
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

  constructor(canvas: HTMLCanvasElement) {
    window.addEventListener("keydown", (e) => {
      if (!this.keys.has(e.code)) this.pressed.add(e.code);
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
        if (!wasDown) this.pressed.add(code);
      }
      this.padPrevPressed.set(index, isDown);
    }

    const rtDown = !!gp.buttons[BUTTON_FIRE]?.pressed;
    if (rtDown) sfx.ensure();
    this.padFire = rtDown;

    const rbDown = !!gp.buttons[BUTTON_MELEE]?.pressed;
    const rbWasDown = this.padPrevPressed.get(BUTTON_MELEE) ?? false;
    if (rbDown && !rbWasDown) {
      sfx.ensure();
      this.meleePressed = true;
    }
    this.padPrevPressed.set(BUTTON_MELEE, rbDown);
  }

  /** Call once per rendered frame, after all systems have read input. */
  endFrame(): void {
    this.pressed.clear();
    this.meleePressed = false;
  }
}
