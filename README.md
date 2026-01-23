# StepPlayer

This repository is a personal research project for studying rhythm game mechanics
and real-time feedback systems.

This repository focuses on **technical exploration**, not recreating any
commercial game or assets.

---

## What this project explores

- Rhythm game timing & judgement systems
- Audio synchronization
- UI / feedback experiments
- Optional hardware feedback via Razer Chroma

---

## Razer Chroma (Optional)

This project includes **experimental integration** with Razer Chroma devices
using the Unity SDK.

- Lighting reacts to judgement results only
- Fully optional (game works without Chroma)
- Static colors, event-based feedback

### Emulator support

You can test Chroma output **without physical hardware** using:

https://github.com/WyvrnOfficial/ChromaEmulator

This is recommended for development, testing, and CI environments.

---

## Songs & Charts

Song data is **not included** in this repository.

All audio, images, and charts are managed via Google Drive:

https://drive.google.com/drive/folders/1ss71t_okXAC2pxeLzp0iz-gmEiCKAEvz

Expected structure:

```
Songs/
<Song Name>/
<Song Name>.sm
<Song Name>.ogg (or .mp3)
<Song Name>.png
```

---

## License

⚠️ This repository uses **multiple licenses**.

### Source code (CC0)

All files under:

```
Assets/_Project/
```

are released under **CC0 1.0 Universal**
→ free to use, modify, and distribute without restriction.

### Other files

Everything outside `Assets/_Project/` is **not covered by CC0**
and follows its own license or copyright.

---

## Disclaimer

This project is provided **as-is**, without warranty.

---

## Purpose

This repository is a **sandbox for learning and experimentation**.

- Understanding > completeness
- Prototyping > polish
- Flexibility > fixed design
