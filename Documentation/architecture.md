# Architecture Overview

## Core Concepts
The Puzzle Engine Sandbox is structured into three main layers:

1. **Data Layer**
   - GridModel
   - TileData
   - ScriptableObjects for tile types and rules

2. **Runtime Simulation Layer**
   - PuzzleManager orchestrates the simulation
   - DOTS systems:
     - NeighborScanSystem
     - InteractionSystem
     - MergeSystem
     - StateUpdateSystem

3. **Editor Tooling Layer**
   - Grid Editor
   - Rule Editor
   - Simulation Debugger

The engine is deterministic and designed for scalability.
