// Procedural background music -- no audio assets, a small step-sequencer
// scheduled on the Web Audio clock (the standard "lookahead scheduler"
// pattern: poll frequently via setTimeout, but schedule notes against
// AudioContext.currentTime so playback timing doesn't jitter with the
// JS event loop). Two loops: a calm hangar/draft ambient pad, and a
// driving combat loop, both built from a shared 4-bar minor-key
// progression so switching between them doesn't feel like a genre change.

import { audioCore } from "./audio";

type MusicMode = "hangar" | "combat";

const STEPS_PER_BAR = 16;
const BARS_PER_PROGRESSION = 4;
const COMBAT_BPM = 128;
const HANGAR_BPM = 84;

// i - VI - III - VII in A minor (Am - F - C - G): a common, unresolved-
// feeling driving progression that suits both a tense combat loop and,
// slowed down, a calm ambient one.
const ROOTS = [110, 87.31, 130.81, 98]; // A2, F2, C3, G2

class MusicEngine {
  private schedulerHandle: number | null = null;
  private nextNoteTime = 0;
  private step = 0;
  private bar = 0;
  private mode: MusicMode | null = null;
  private readonly lookaheadMs = 25;
  private readonly scheduleAheadSec = 0.15;

  /** Switch tracks; no-op if already playing the requested mode. */
  start(mode: MusicMode): void {
    audioCore.ensure();
    if (this.mode === mode) return;
    this.stopScheduler();
    this.mode = mode;
    this.step = 0;
    this.bar = 0;
    this.nextNoteTime = (audioCore.ctx?.currentTime ?? 0) + 0.1;
    this.tick();
  }

  stop(): void {
    this.stopScheduler();
    this.mode = null;
  }

  private stopScheduler(): void {
    if (this.schedulerHandle !== null) {
      window.clearTimeout(this.schedulerHandle);
      this.schedulerHandle = null;
    }
  }

  private tick = (): void => {
    const ctx = audioCore.ctx;
    if (!ctx || !this.mode) return;
    const bpm = this.mode === "combat" ? COMBAT_BPM : HANGAR_BPM;
    const secondsPerStep = 60 / bpm / 4; // 16th notes

    while (this.nextNoteTime < ctx.currentTime + this.scheduleAheadSec) {
      if (this.mode === "combat") {
        this.scheduleCombatStep(this.step, this.bar, this.nextNoteTime);
      } else {
        this.scheduleHangarStep(this.step, this.bar, this.nextNoteTime);
      }
      this.nextNoteTime += secondsPerStep;
      this.step += 1;
      if (this.step >= STEPS_PER_BAR) {
        this.step = 0;
        this.bar = (this.bar + 1) % BARS_PER_PROGRESSION;
      }
    }
    this.schedulerHandle = window.setTimeout(this.tick, this.lookaheadMs);
  };

  // --- Note helpers: schedule against an ABSOLUTE AudioContext time, not
  // "now" -- required for the lookahead scheduler to stay glitch-free. ---

  private tone(
    time: number,
    freq: number,
    dur: number,
    type: OscillatorType,
    vol: number,
    endFreq = freq,
  ): void {
    const ctx = audioCore.ctx;
    if (!ctx || !audioCore.musicBus) return;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, time);
    if (endFreq !== freq) {
      osc.frequency.exponentialRampToValueAtTime(Math.max(20, endFreq), time + dur);
    }
    gain.gain.setValueAtTime(0.0001, time);
    gain.gain.exponentialRampToValueAtTime(vol, time + Math.min(0.02, dur * 0.2));
    gain.gain.exponentialRampToValueAtTime(0.0001, time + dur);
    osc.connect(gain).connect(audioCore.musicBus);
    osc.start(time);
    osc.stop(time + dur + 0.05);
  }

  private noiseHit(time: number, dur: number, vol: number, filterFreq: number): void {
    const ctx = audioCore.ctx;
    if (!ctx || !audioCore.musicBus) return;
    const len = Math.max(1, Math.ceil(ctx.sampleRate * dur));
    const buffer = ctx.createBuffer(1, len, ctx.sampleRate);
    const data = buffer.getChannelData(0);
    for (let i = 0; i < len; i++) data[i] = Math.random() * 2 - 1;
    const src = ctx.createBufferSource();
    src.buffer = buffer;
    const filter = ctx.createBiquadFilter();
    filter.type = "highpass";
    filter.frequency.value = filterFreq;
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(vol, time);
    gain.gain.exponentialRampToValueAtTime(0.0001, time + dur);
    src.connect(filter).connect(gain).connect(audioCore.musicBus);
    src.start(time);
  }

  // --- Combat: 128 BPM, four-on-the-floor kick, driving bass, sparse arp ---
  private scheduleCombatStep(step: number, bar: number, time: number): void {
    const root = ROOTS[bar];

    if (step % 4 === 0) {
      this.tone(time, 130, 0.12, "sine", 0.32, 45); // kick
      this.noiseHit(time, 0.03, 0.14, 200);
    }
    const hatAccent = step % 4 === 2;
    this.noiseHit(time, 0.03, hatAccent ? 0.08 : 0.04, 7000); // hat every step

    if (step % 2 === 0) {
      this.tone(time, root, 0.16, "sawtooth", 0.14, root * 0.98); // bass pulse
    }

    // Lead fill on the odd bars only, so two of every four bars breathe
    if ((bar === 1 || bar === 3) && step % 4 === 2) {
      const third = root * Math.pow(2, 3 / 12) * 2;
      const fifth = root * Math.pow(2, 7 / 12) * 2;
      const notes = [root * 2, third, fifth, third];
      this.tone(time, notes[(step / 4) % notes.length], 0.16, "triangle", 0.1);
    }
  }

  // --- Hangar: 84 BPM, sustained detuned pad + sparse bell arpeggio ---
  private scheduleHangarStep(step: number, bar: number, time: number): void {
    const root = ROOTS[bar];

    if (step === 0) {
      this.tone(time, root, 3.6, "sine", 0.05);
      this.tone(time, root * 1.004, 3.6, "sine", 0.035); // detuned twin: width
      this.tone(time, root * Math.pow(2, 7 / 12), 3.4, "triangle", 0.022); // fifth
    }
    if (step === 4 || step === 9 || step === 13) {
      const scale = [
        root * 2,
        root * 2 * Math.pow(2, 3 / 12),
        root * 2 * Math.pow(2, 7 / 12),
      ];
      const note = scale[[4, 9, 13].indexOf(step) % scale.length];
      this.tone(time, note, 0.6, "sine", 0.055, note * 0.995);
    }
  }
}

export const music = new MusicEngine();
