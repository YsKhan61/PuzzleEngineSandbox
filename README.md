# Puzzle Engine Sandbox
A modular, DOTS-accelerated puzzle simulation framework for Unity.

[![Unity](https://img.shields.io/badge/Unity-6000.0.62f1_LTS-black)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)]()

---

## Overview

Puzzle Engine Sandbox is a highly modular and data-driven puzzle simulation framework built in Unity using DOTS (ECS + Burst + Jobs), ScriptableObjects, deterministic rule systems, and custom editor tooling.

It demonstrates production-quality engineering practices suitable for gameplay and tech programming roles. The architecture is extendable, high-performance, designer-friendly, and built with clean separation between data, simulation, and tools.

---

## Key Features

### 1. Grid-Based Puzzle Simulation
- 2D grid of any size
- Tiles represented as lightweight data structures
- Supports merging, combining, leveling, and state transitions
- Fully deterministic behavior for reproducibility

### 2. Scriptable Rule System
- All interactions are defined via ScriptableObjects
- Merge rules
- Combine rules
- State rules (wet, hot, frozen, cracked, etc.)
- Time-based state transitions
- Designers can create new tile types and rules without code changes

### 3. DOTS-Powered Simulation
- Burst-compiled ECS systems
- Parallel neighbor scanning
- Interaction detection
- Rule execution
- State propagation
- Efficient performance on large grids

### 4. Undo and Replay System
- Snapshot-based undo
- Frame-accurate deterministic replay
- Essential for debugging puzzle behavior and chain reactions

### 5. Custom Unity Editor Tools
- Puzzle Grid Editor:
  - Paint tiles onto a grid
  - Define initial puzzle layouts
  - Save as PuzzleAsset

- Rule Editor:
  - Create interaction rules visually
  - Test rules on a sample grid

- Simulation Debugger:
  - Step-through simulation
  - Display triggered rules
  - Highlight tile changes

---

## Architecture Overview

The project is built around a clean, scalable architecture.

### Core Runtime Components
- `PuzzleManager`
- `GridModel` (pure data)
- `TileData` structs
- `InteractionRuleSO`
- DOTS systems:
  - NeighborScanSystem
  - InteractionSystem
  - MergeSystem
  - StateUpdateSystem

### Architectural Goals
- Predictable and deterministic simulation
- Centralized data model
- DOTS backend for heavy logic
- Editor tooling for content creation
- Strong separation of concerns
- Maintainability and testability

More details in:

```
/Documentation/architecture.md
/Documentation/rule-engine.md
/Documentation/puzzle-editor-design.md
```

---

## Project Structure

```
/PuzzleEngineSandbox
│
├── Project/                  → Unity project
├── Documentation/            → Technical design docs
└── .github/workflows/        → CI/CD workflows
```

### Inside Unity:

```
Assets/
    PuzzleEngine/
        Runtime/
            Core/
            Rules/
            DOTS/
            Simulation/
        Editor/
            GridEditor/
            RuleEditor/
            Debugger/
```

---

## Tech Stack

- Unity 6000.0.61f1 LTS
- C# (SOLID, clean architecture)
- Unity DOTS (Entities, Burst, Jobs)
- ScriptableObjects for content-driven rules
- Custom Unity Editor tooling
- Unity Test Framework
- GitHub Actions (Unity WebGL Builder)

---

## Demo (Coming Soon)

A public WebGL build and preview GIFs will be added after the engine reaches Milestone 3.

Planned media:
- Tile merging showcase
- Combine and chain reaction demo
- Step-through debugger preview
- Rule editor UI
- WebGL demo link via GitHub Pages

---

## Roadmap

### Milestone 1 — Core Engine
- Grid model and initialization
- TileData and TileType definitions
- Basic merge/combine rules

### Milestone 2 — DOTS Simulation
- Entity conversion
- Neighbor scanning system
- Interaction system
- Merge and state update systems

### Milestone 3 — Editor Tools
- Grid painter
- Rule editor
- Simulation debugger
- Puzzle asset workflow

### Milestone 4 — Public Release
- WebGL CI/CD build
- Trailer and screenshots
- README and documentation polish

---

## Purpose

Puzzle Engine Sandbox highlights engineering capabilities that align with the development culture of studios such as Metacore, Supercell, Rovio, Next Games, and Fingersoft.

It showcases:
- System architecture
- Optimized simulation
- Data-driven pipelines
- Tool development
- Deterministic behavior
- Production-level project structure

---

## License

MIT License

---

## Contact

**Yash Sadhukhan**  
Unity Gameplay Programmer  
Email: yskhan61@gmail.com  
LinkedIn / Portfolio [Click here](https://www.linkedin.com/in/yskhan61/)
