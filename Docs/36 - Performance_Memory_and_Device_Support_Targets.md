**System Spec 36: Performance, Memory, and Device Support Targets**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Device targets, frame rate, simulation budgets, scalability limits, degradation rules |
| Primary goal | Ensure stable performance on low end devices and future scalability |
| Invariants referenced | INV-01, INV-02, INV-03 |

# 1. Purpose

Define performance targets and scalability limits for Unity mobile builds. The game must run on low end Android and older iPhones with stable frame rate and acceptable battery and heat behavior.

# 2. Platform and orientation

- Minimum device class includes low end Android and older iPhones.

- Primary screens use portrait orientation.

- Dungeon design and layout editing may use landscape orientation.

# 3. Frame rate targets

- Target frame rate: 30 FPS.

- Avoid stutters during tick updates by batching and decoupling UI refresh from simulation.

# 4. Simulation and UI budgeting

- Simulation updates should be batched and processed in small chunks when needed.

- UI should update key metrics at a stable cadence, not on every simulation event.

- Avoid rerendering the full dungeon view on every tick.

# 5. Scalability limits

## 5.1 MVP limits

- Max floors in MVP: 5.

- Max active monsters in MVP should be below 200.

- Floor size increases over time, with later floors physically larger than earlier floors.

## 5.2 Long term direction

- Long term max floors are effectively unbounded, limited by performance and content gating.

- Limits should be defined by configuration tables and can change over time.

# 6. Degradation rules

- If limits are exceeded, degrade simulation fidelity instead of hard blocking by default.

- Warn the player when degradation is active.

- Allow continued play when possible, but prioritize correctness for time based systems.

# 7. Telemetry hooks

- perf_sample (fps, frame_time_ms, device_class)

- tick_duration (duration_ms, workload_units)

- degradation_enabled (reason, level)

# 8. MVP constraints

- Focus on stable tick processing and minimal UI work.

- Avoid heavy pathfinding or per tile updates in the main loop.

- Keep dungeon view rendering efficient.

# 9. Open questions

None.

## 10. GD65B0C7 initial production-validation workload profile

The approved initial configuration profile is `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768`, solely owned by future `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`. These are bounded validation-safety values shared by export, pre-build, runtime load, and canonicalization—not gameplay, progression, floor-count, floor-space, schema/save, or permanent post-MVP limits.

Initial evidence is 8 top-level records, 41 nested records including six lookup keys, Floor 1's 144-tile bounds, and safely bounded diagnostics/strings. A modeled current-shape 80-floor test uses approximately 357 nested records and remains below the approved envelopes. The materialized-tile bound applies to one footprint or floor boundary, never the sum across floors or catalog; 4,096 permits only a test-scale 64 by 64 boundary and is unrelated to Floor 1 capacity 60. Neither 80 production floors nor a 64 by 64 production floor is approved.

`FloorSpatialConfiguration[]` and integer `FloorIndex` preserve floor-count extensibility without a permanent schema maximum. Later approved scale may raise configuration after workload/performance review and new evidence without inherently changing code, schema, saves, IDs, or ordering. Since the configuration is currently a Unity-imported `TextAsset`, deployment requires an updated content/application release. This profile proves bounded validator representation, not low-end mobile performance, memory residency, rendering, streaming, or frame rate. It approves no continuous rendering or per-tile simulation; device profiling remains required at its later gate.
