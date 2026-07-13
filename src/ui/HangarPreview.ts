import * as THREE from "three";
import { buildRoboMesh, type RoboMeshParts } from "../robo/RoboMesh";
import type { Loadout } from "../parts/parts";

// Live 3D turntable preview for the hangar: rebuilds the robo mesh from
// scratch every time the loadout changes, using the exact same builder the
// real game uses, so what you see here is what you fight with.

export class HangarPreview {
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private camera: THREE.PerspectiveCamera;
  private current: RoboMeshParts | null = null;
  private rafId: number | null = null;
  private spin = 0;

  constructor(canvas: HTMLCanvasElement) {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(36, 1, 0.1, 20);
    this.camera.position.set(0.4, 0.15, 3.3);
    this.camera.lookAt(0, 0.0, 0);

    const key = new THREE.DirectionalLight(0xffffff, 2.6);
    key.position.set(3, 4, 4);
    this.scene.add(key);
    this.scene.add(new THREE.AmbientLight(0x8fa0d0, 1.1));
    const rim = new THREE.DirectionalLight(0x4a6cff, 1.2);
    rim.position.set(-3, 1.5, -3);
    this.scene.add(rim);

    const platform = new THREE.Mesh(
      new THREE.CircleGeometry(1.4, 32),
      new THREE.MeshStandardMaterial({ color: 0x1a1f38, roughness: 0.7 }),
    );
    platform.rotation.x = -Math.PI / 2;
    platform.position.y = -0.92;
    this.scene.add(platform);
    const ring = new THREE.Mesh(
      new THREE.RingGeometry(1.35, 1.42, 48),
      new THREE.MeshBasicMaterial({ color: 0x3355ff, side: THREE.DoubleSide }),
    );
    ring.rotation.x = -Math.PI / 2;
    ring.position.y = -0.91;
    this.scene.add(ring);

    this.resize();
    // ResizeObserver, not a window "resize" listener + requestAnimationFrame:
    // the canvas can report 0 height on the tick it's first inserted, before
    // the flex layout has settled, and rAF isn't a reliable place to retry
    // (suspended entirely on a backgrounded tab). ResizeObserver fires the
    // moment the element's real box actually changes, covering both cases.
    new ResizeObserver(() => this.resize()).observe(canvas);
  }

  resize(): void {
    const canvas = this.renderer.domElement;
    const w = canvas.clientWidth || 1;
    const h = canvas.clientHeight || 1;
    this.renderer.setSize(w, h, false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  }

  rebuild(loadout: Loadout): void {
    if (this.current) {
      this.scene.remove(this.current.root);
      disposeGroup(this.current.root);
      for (const m of this.current.materials) m.dispose();
    }
    const parts = buildRoboMesh(loadout, 0x5577aa, 0x33e0ff);
    parts.root.position.set(0, 0, 0);
    parts.root.rotation.y = this.spin;
    this.scene.add(parts.root);
    this.current = parts;
  }

  start(): void {
    const loop = () => {
      this.spin += 0.006;
      if (this.current) this.current.root.rotation.y = this.spin;
      this.renderer.render(this.scene, this.camera);
      this.rafId = requestAnimationFrame(loop);
    };
    loop();
  }

  stop(): void {
    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
    this.rafId = null;
  }

  /** Testing/debug hook: force one frame when rAF is suspended. */
  renderOnce(): void {
    this.renderer.render(this.scene, this.camera);
  }
}

function disposeGroup(obj: THREE.Object3D): void {
  obj.traverse((child) => {
    if (child instanceof THREE.Mesh) {
      child.geometry.dispose();
    }
  });
}
