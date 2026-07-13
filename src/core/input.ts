// Keyboard + mouse buttons. The camera never rotates (Custom Robo-style
// fixed view), so there's no mouse-look and no pointer lock — the mouse
// only fires (LMB) and melees (RMB).

import { sfx } from "./sfx";

export class Input {
  private keys = new Set<string>();
  private pressed = new Set<string>(); // cleared each frame: edge-triggered
  fireHeld = false;
  meleePressed = false;

  constructor(canvas: HTMLCanvasElement) {
    window.addEventListener("keydown", (e) => {
      if (!this.keys.has(e.code)) this.pressed.add(e.code);
      this.keys.add(e.code);
    });
    window.addEventListener("keyup", (e) => this.keys.delete(e.code));
    window.addEventListener("blur", () => {
      this.keys.clear();
      this.fireHeld = false;
    });

    canvas.addEventListener("mousedown", (e) => {
      sfx.ensure(); // user gesture: keeps the AudioContext unlocked
      if (e.button === 0) this.fireHeld = true;
      if (e.button === 2) this.meleePressed = true;
    });
    window.addEventListener("mouseup", (e) => {
      if (e.button === 0) this.fireHeld = false;
    });
    canvas.addEventListener("contextmenu", (e) => e.preventDefault());
  }

  held(code: string): boolean {
    return this.keys.has(code);
  }

  /** True only on the frame the key went down. */
  justPressed(code: string): boolean {
    return this.pressed.has(code);
  }

  /** Call once per rendered frame, after all systems have read input. */
  endFrame(): void {
    this.pressed.clear();
    this.meleePressed = false;
  }
}
