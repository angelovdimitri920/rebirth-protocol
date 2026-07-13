import { Game } from "./core/Game";

const canvas = document.getElementById("game-canvas") as HTMLCanvasElement;
const game = await Game.create(canvas);
game.start();

// Debug handle for console-driven testing (harmless in production)
(window as unknown as { game: Game }).game = game;
