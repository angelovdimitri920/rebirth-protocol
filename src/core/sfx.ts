// Synthesized sound effects via Web Audio -- no audio assets, everything is
// oscillators and filtered noise. Module singleton: combat systems call
// sfx.shot() etc. directly. Routes through the shared audioCore's sfxBus
// so it mixes independently of background music.

import { audioCore } from "./audio";

class Sfx {
  /** Call from any user-gesture handler; safe to call repeatedly. */
  ensure(): void {
    audioCore.ensure();
  }

  private tone(
    freq: number,
    endFreq: number,
    dur: number,
    type: OscillatorType,
    vol: number,
    delay = 0,
  ): void {
    const ctx = audioCore.ctx;
    if (!ctx || !audioCore.sfxBus || !audioCore.running) return;
    const t0 = ctx.currentTime + delay;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, t0);
    osc.frequency.exponentialRampToValueAtTime(Math.max(20, endFreq), t0 + dur);
    gain.gain.setValueAtTime(vol, t0);
    gain.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    osc.connect(gain).connect(audioCore.sfxBus);
    osc.start(t0);
    osc.stop(t0 + dur + 0.02);
  }

  private noise(dur: number, vol: number, filterFreq: number, delay = 0): void {
    const ctx = audioCore.ctx;
    if (!ctx || !audioCore.sfxBus || !audioCore.running) return;
    const t0 = ctx.currentTime + delay;
    const len = Math.ceil(ctx.sampleRate * dur);
    const buffer = ctx.createBuffer(1, len, ctx.sampleRate);
    const data = buffer.getChannelData(0);
    for (let i = 0; i < len; i++) data[i] = Math.random() * 2 - 1;
    const src = ctx.createBufferSource();
    src.buffer = buffer;
    const filter = ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = filterFreq;
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(vol, t0);
    gain.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    src.connect(filter).connect(gain).connect(audioCore.sfxBus);
    src.start(t0);
  }

  // --- Combat ---
  shot(): void {
    this.tone(920, 240, 0.09, "square", 0.14);
  }
  podShot(): void {
    this.tone(1400, 700, 0.05, "square", 0.07);
  }
  hit(): void {
    this.noise(0.08, 0.2, 2400);
    this.tone(300, 90, 0.08, "triangle", 0.16);
  }
  shielded(): void {
    this.tone(520, 480, 0.1, "sine", 0.18);
  }
  meleeSwing(): void {
    this.noise(0.12, 0.12, 1200);
  }
  meleeHit(): void {
    this.noise(0.1, 0.25, 3000);
    this.tone(180, 60, 0.14, "sawtooth", 0.2);
  }
  clash(): void {
    this.tone(1800, 1200, 0.12, "square", 0.16);
    this.noise(0.15, 0.18, 5000);
  }
  explosion(): void {
    this.noise(0.5, 0.32, 900);
    this.tone(120, 35, 0.45, "sine", 0.3);
  }
  bombThrow(): void {
    this.tone(300, 700, 0.22, "sine", 0.09);
  }
  dash(): void {
    this.noise(0.14, 0.1, 1800);
    this.tone(200, 600, 0.12, "sine", 0.08);
  }
  thrust(): void {
    this.tone(140, 320, 0.22, "sine", 0.08);
    this.noise(0.18, 0.06, 900);
  }
  knockdown(): void {
    this.tone(400, 60, 0.4, "sawtooth", 0.22);
    this.noise(0.3, 0.18, 700);
  }
  rebirth(): void {
    this.tone(300, 900, 0.3, "sine", 0.16);
    this.tone(450, 1350, 0.3, "sine", 0.1, 0.05);
  }
  guardBreak(): void {
    this.tone(900, 100, 0.3, "square", 0.2);
    this.noise(0.25, 0.2, 4000);
  }
  eliminate(): void {
    this.noise(0.4, 0.28, 1600);
    this.tone(500, 40, 0.5, "sawtooth", 0.24);
    this.tone(700, 50, 0.55, "sawtooth", 0.16, 0.06);
  }
  overheat(): void {
    this.tone(500, 120, 0.35, "sawtooth", 0.18);
    this.noise(0.25, 0.1, 500);
  }
  land(): void {
    this.noise(0.06, 0.12, 500);
  }
  mashTick(): void {
    this.tone(900, 900, 0.03, "square", 0.06);
  }
  crateBreak(): void {
    this.noise(0.12, 0.22, 1400);
    this.tone(150, 60, 0.1, "triangle", 0.12);
  }
  podToggle(deployed: boolean): void {
    this.tone(deployed ? 500 : 750, deployed ? 900 : 400, 0.14, "triangle", 0.12);
  }
  hazardSizzle(): void {
    this.noise(0.25, 0.08, 3000);
  }

  // --- UI / run ---
  uiClick(): void {
    this.tone(700, 500, 0.05, "square", 0.08);
  }
  lockToggle(on: boolean): void {
    this.tone(on ? 700 : 500, on ? 1000 : 350, 0.08, "sine", 0.12);
  }
  pickup(): void {
    this.tone(660, 990, 0.1, "sine", 0.14);
    this.tone(990, 1320, 0.12, "sine", 0.12, 0.08);
  }
  draftPick(): void {
    this.tone(440, 880, 0.16, "triangle", 0.16);
  }
  victory(): void {
    for (const [i, f] of [523, 659, 784, 1047].entries())
      this.tone(f, f, 0.22, "triangle", 0.16, i * 0.12);
  }
  defeat(): void {
    for (const [i, f] of [392, 330, 262, 196].entries())
      this.tone(f, f * 0.94, 0.3, "sawtooth", 0.13, i * 0.15);
  }
}

export const sfx = new Sfx();
