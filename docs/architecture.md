# Architecture UML

The project structure is summarized in the accompanying PlantUML class diagram (`architecture.puml`). The diagram is organized by namespace to show how gameplay, models, tools, judging, and view components interact.

Key relationships captured include:
- `Game` orchestrates note loading (`ChartLoader`), spawns `NoteView` objects via `NoteViewPool`, records input (`ChartRecorder`), judges timing (`Judge`), and stores the final `JudgementSummary` in `ResultStore`.
- `ChartLoader` parses `ChartJson` data into runtime `Chart` and `Note` objects using `Lane` and `NoteDivision` classifications.
- Judging feedback flows through `Judge`, which triggers UI (`JudgementTextPresenter`) and device lighting (`RazerChromaController`) according to `JudgementStyle` colors.
- Result presentation uses `ResultController`, `ResultJudgementRowView`, and `JudgementStyle` to format per-judgement counts from `ResultStore`.

Render the UML with PlantUML using a compatible renderer (e.g., the PlantUML VS Code extension or the `plantuml` CLI):

```bash
plantuml docs/architecture.puml
```

This produces a `architecture.png` (or SVG) illustrating the relationships described above.
