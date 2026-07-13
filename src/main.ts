import { Game } from "./core/Game";

const canvas = document.getElementById("game-canvas") as HTMLCanvasElement;
const game = await Game.create(canvas);
game.start();
