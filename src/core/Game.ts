import * as THREE from "three";

export class Game {
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private camera: THREE.PerspectiveCamera;

  private constructor(canvas: HTMLCanvasElement) {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0a0a12);

    this.camera = new THREE.PerspectiveCamera(
      60,
      window.innerWidth / window.innerHeight,
      0.1,
      200,
    );
    this.camera.position.set(0, 8, 16);
    this.camera.lookAt(0, 0, 0);

    const sun = new THREE.DirectionalLight(0xffffff, 2.0);
    sun.position.set(20, 30, 10);
    sun.castShadow = true;
    this.scene.add(sun);
    this.scene.add(new THREE.AmbientLight(0x8899bb, 0.6));

    const floor = new THREE.Mesh(
      new THREE.BoxGeometry(30, 1, 30),
      new THREE.MeshStandardMaterial({ color: 0x2a2a3a }),
    );
    floor.position.y = -0.5;
    floor.receiveShadow = true;
    this.scene.add(floor);

    window.addEventListener("resize", () => {
      this.camera.aspect = window.innerWidth / window.innerHeight;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(window.innerWidth, window.innerHeight);
    });
  }

  static async create(canvas: HTMLCanvasElement): Promise<Game> {
    return new Game(canvas);
  }

  start(): void {
    this.renderer.setAnimationLoop(() => {
      this.renderer.render(this.scene, this.camera);
    });
  }
}
