// Shared Web Audio plumbing for both sfx.ts and music.ts -- one
// AudioContext, one master gain, and separate sfx/music sub-buses so
// either can be balanced independently without touching the other.
// The context can only start after a user gesture (browser autoplay
// policy); ensure() is idempotent and safe to call from every gesture
// handler rather than threading a single init call through the boot chain.

class AudioCore {
  ctx: AudioContext | null = null;
  master: GainNode | null = null;
  sfxBus: GainNode | null = null;
  musicBus: GainNode | null = null;

  ensure(): void {
    if (!this.ctx) {
      this.ctx = new AudioContext();
      this.master = this.ctx.createGain();
      this.master.gain.value = 1;
      this.master.connect(this.ctx.destination);

      this.sfxBus = this.ctx.createGain();
      this.sfxBus.gain.value = 0.32;
      this.sfxBus.connect(this.master);

      this.musicBus = this.ctx.createGain();
      this.musicBus.gain.value = 0.2;
      this.musicBus.connect(this.master);
    }
    if (this.ctx.state === "suspended") void this.ctx.resume();
  }

  get running(): boolean {
    return this.ctx?.state === "running";
  }
}

export const audioCore = new AudioCore();
