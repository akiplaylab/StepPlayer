# Dance Dance Revolution

## Overview

This repository is a **personal research project** focused on studying and recreating gameplay mechanics inspired by *Dance Dance Revolution* (DDR).

The purpose of this project is **technical and educational research**, including:
- rhythm game mechanics
- timing and judgement systems
- UI/UX experiments
- audio synchronization techniques

This project is **not affiliated with, endorsed by, or connected to the official *Dance Dance Revolution* series**.
All trademarks, product names, and references to *Dance Dance Revolution* belong to their respective owners and are used here **for descriptive and research purposes only**.

This repository does **not aim to reproduce the original game**, assets, or commercial experience, but rather to explore rhythm-game design and implementation as an individual study project.

## License

### Source Code
All source code in this repository is released under the CC0 1.0 Universal license.

### Music Assets

- `Dance Mood`
  - Source: Pixabay
  - License: Pixabay Content License

- `Starry Stella`
  - Original composition by a collaborator
  - Released under a CC0-equivalent license
  - Attribution is appreciated but not required

## Testing

This project splits tests between the .NET domain logic and Unity integration checks:

- **.NET domain logic**: run with `dotnet test tests/DDR.Domain.Tests/DDR.Domain.Tests.csproj`.
- **Unity integration**: run EditMode tests via the Unity Test Runner (or the Unity CI workflow).

The .NET tests cover the core scoring logic, while Unity tests only verify the thin adapter layer.
