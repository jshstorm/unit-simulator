# Development Infrastructure Rules

This document outlines the development guidelines and architectural decisions for maintaining the Unit Simulator and extending it with GUI tools.

## Overview

The Unit Simulator is designed as a core simulation engine that separates simulation logic from visualization. This separation allows for flexible integration with various front-end tools, including GUI applications and external visualization systems.

## Architecture Principles

### 1. Separation of Concerns

The simulator follows a clear separation between:

- **Simulation Logic (Core)**: The `SimulatorCore` handles all simulation computations, unit behaviors, and state management.
- **Frame Data Generation**: Each frame produces a JSON representation of the simulation state, independent of rendering.
- **Rendering**: The `Renderer` class is responsible for generating visual output (images) from frame data.

This separation enables:
- Running simulations without generating images (headless mode)
- Replaying simulations from saved frame data
- Integrating with external visualization tools
- Debugging and analyzing simulation behavior through JSON data

### 2. Core Simulator Components

#### SimulatorCore
The central component that manages the simulation loop and state:
- Maintains simulation state (units, waves, etc.)
- Generates frame data at each step
- Supports callbacks for extensions and integrations
- Allows loading state from JSON and resuming simulation
- Enables runtime state injection (modifying unit states dynamically)

#### FrameData
Data structures representing a complete simulation frame:
- Frame number and wave information
- Unit states (positions, health, targets, etc.)
- Relationships between units (attack targets, slots, etc.)
- Serializable to JSON for storage and replay

#### Callbacks Interface
Extension point for integrating external systems:
- `OnFrameGenerated`: Called after each frame is computed
- `OnSimulationComplete`: Called when simulation finishes
- `OnStateChanged`: Called when simulation state is modified
- `OnUnitEvent`: Called for significant unit events (attack, death, etc.)

### 3. Frame Data vs Rendering

**Frame Data Generation** (JSON):
- Contains all simulation state for a given frame
- Lightweight and fast to generate
- Used for:
  - Debugging and analysis
  - State persistence and replay
  - External tool integration
  - Simulation validation

**Image Rendering**:
- Generates visual PNG frames from simulation state
- More resource-intensive
- Optional - can be disabled for performance
- Useful for:
  - Video generation
  - Visual debugging
  - Presentation/demonstration

## Extension Points

### Loading and Resuming Simulation

The simulator supports loading a specific frame from JSON and resuming from that point:

```csharp
// Load simulation state from a saved frame
var frameData = FrameData.LoadFromJsonFile("output/debug/frame_0100.json");

// Create simulator and set state
var simulator = new SimulatorCore();
simulator.LoadState(frameData);

// Resume simulation from this point
simulator.Run(callbacks);
```

### Injecting State Changes

External tools can modify simulation state at runtime:

```csharp
// Get a unit by ID and modify its state
simulator.ModifyUnit(unitId, unit => {
    unit.Position = newPosition;
    unit.HP = newHP;
});

// Inject a new enemy unit
simulator.InjectUnit(newUnit);

// Remove a unit
simulator.RemoveUnit(unitId);
```

### GUI Integration

The `GuiIntegration` class provides a placeholder for connecting external GUI tools:

```csharp
// Register GUI callbacks
var gui = new GuiIntegration();
gui.OnFrameRequest = (frameNumber) => simulator.GetFrameData(frameNumber);
gui.OnStateModification = (changes) => simulator.ApplyStateChanges(changes);
gui.OnPlaybackControl = (action) => simulator.HandlePlaybackAction(action);
```

## Development Guidelines

### Adding New Features

1. **Core Logic**: Add new simulation logic to `SimulatorCore` or relevant behavior classes.
2. **Frame Data**: Update `FrameData` models if new state needs to be captured.
3. **Callbacks**: Add new callback types if external systems need to react to new events.
4. **Documentation**: Update this document with new extension points.

### Testing

- Test simulation logic independently of rendering.
- Use frame data JSON for regression testing.
- Validate that state loading/resuming produces consistent results.

### Performance Considerations

- Frame data generation should be lightweight.
- Expensive operations (like rendering) should be optional.
- Consider batching callbacks to reduce overhead.

## File Structure

```
UnitSimulator/
├── SimulatorCore.cs       # Core simulation engine
├── FrameData.cs           # Frame data models and serialization
├── ISimulatorCallbacks.cs # Callback interfaces for extensions
├── GuiIntegration.cs      # GUI integration placeholder
├── Unit.cs                # Unit model and behavior
├── SquadBehavior.cs       # Friendly squad AI
├── EnemyBehavior.cs       # Enemy AI
├── AvoidanceSystem.cs     # Collision avoidance
├── WaveManager.cs         # Wave spawning and management
├── Renderer.cs            # Image generation (separate from core)
└── Constants.cs           # Configuration constants
```

## Version History

- Initial version: Core simulator refactoring, frame data separation, callback system, GUI integration placeholder.
