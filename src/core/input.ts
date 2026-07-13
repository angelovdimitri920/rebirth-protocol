// Keyboard + mouse state. Pointer lock is requested on first click so the
// mouse can drive the camera without leaving the window.

export class Input {
  private keys = new Set<string>();
  private pressed = new Set<string>(); // cleared each frame: edge-triggered
  mouseDx = 0;
  mouseDy = 0;
  fireHeld = false;
  meleePressed = false;

  constructor(canvas: HTMLCanvasElement) {
    window.addEventListener("keydown", (e) => {
      if (!this.keys.has(e.code)) this.pressed.add(e.code);
      this.keys.add(e.code);
    });
    window.addEventListener("keyup", (e) => this.keys.delete(e.code));
    window.addEventListener("blur", () => this.keys.clear());

    canvas.addEventListener("click", () => {
      if (document.pointerLockElement !== canvas) canvas.requestPointerLock();
    });
    window.addEventListener("mousemove", (e) => {
      if (document.pointerLockElement) {
        this.mouseDx += e.movementX;
        this.mouseDy += e.movementY;
      }
    });
    window.addEventListener("mousedown", (e) => {
      if (!document.pointerLockElement) return;
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
    this.mouseDx = 0;
    this.mouseDy = 0;
  }
}
