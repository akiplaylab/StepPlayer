# StepPlayer

## Overview

This repository is a **personal research project** focused on studying and recreating gameplay mechanics.

The purpose of this project is **technical and educational research**, including:
- rhythm game mechanics
- timing and judgement systems
- UI/UX experiments
- audio synchronization techniques

This repository does **not aim to reproduce the original game**, assets, or commercial experience, but rather to explore rhythm-game design and implementation as an individual study project.

## Hardware Integration (Razer Chroma)

This project includes **optional experimental integration** with
Razer Chroma devices using the Razer Chroma SDK for Unity.

This integration provides **simple, event-driven lighting feedback**
linked directly to gameplay judgements.

### Current Implementation

- Chroma SDK is initialized and shut down safely at runtime
- Lighting is triggered **only on judgement events**
- Judgement results are mapped to colors and applied immediately
- Uses **static color output** (no animations or per-key effects)
- Supports the following device categories:
  - Keyboard
  - Mouse
  - Mousepad
  - Headset
  - Chroma Link devices

Lighting control is implemented in a dedicated controller class and
invoked from the judgement system.

### Design Characteristics

- Hardware integration is **optional**
  - The game remains fully playable without Chroma devices
- The Chroma SDK is accessed directly from Unity (C#)
- Feedback is **event-based**, not timeline- or BPM-driven
- Lighting is treated as **supplementary feedback**, not required for gameplay

### Development Status

- This feature is **experimental**
- Behavior may change as the judgement and feedback systems evolve
- Currently focused on correctness and stability rather than visual complexity

## Songs and Charts

Song data (audio files, images, and charts) are **not included** in this repository.

All song-related files are managed externally via **Google Drive** to support flexible and asynchronous collaboration, especially for music and chart creation.

### Google Drive (Source of Truth)

https://drive.google.com/drive/folders/1ss71t_okXAC2pxeLzp0iz-gmEiCKAEvz

The folder structure in Google Drive matches the local project structure exactly:

```
Songs/
<Song Name>/
<Song Name>.sm
<Song Name>.ogg (or .mp3)
<Song Name>.png
```

## License

### Source Code
All source code in this repository is released under the CC0 1.0 Universal license.
