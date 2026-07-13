import { Game } from "./core/Game";
import { showHangar } from "./ui/Hangar";

const canvas = document.getElementById("game-canvas") as HTMLCanvasElement;
const loadout = await showHangar();
const game = await Game.create(canvas, loadout);
game.start();

// Debug handle for console-driven testing (harmless in production)
(window as unknown as { game: Game }).game = game;
