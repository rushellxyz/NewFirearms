# AGENTS.md

## Project overview

This repository contains a BepInEx mod for Casualties: Unknown split across two assemblies and a bundled asset pack:

- `NewFirearms/` — main mod logic, item registration, JSON loading, gameplay patches, and compatibility shims.
- `GunMinigame/` — minigame subsystem and its standalone/API-only support path.
- `Ship/` — built-in gun and magazine JSON data plus runtime asset resources.

The project is meant to stay functional even when optional integrations are not installed. The main plugin and the minigame must degrade gracefully when optional mods are absent.

## Golden rules

- Rewrite old code freely when it is wrong, brittle, or hard to maintain.
- Keep the mod working without optional dependencies such as Together/KrokMP, GunsawGenetics, or other compatibility layers being present.
- Prefer clean, direct APIs over reflection-heavy hacks. Use Harmony/AccessTools only where necessary and keep patches explicit and readable.
- Multiplayer rules are host-authorized. Follow the existing Together/KrokMP pattern rather than introducing client-authorized behavior.
- Preserve compatibility with older JSON configs and older multiplayer data unless a change explicitly requires breaking it.
- The GunMinigame API is still evolving. It is acceptable to break internal API compatibility as long as the shipped runtime remains coherent and the latest minigame assembly stays aligned with the latest main plugin release.

## Architecture and compatibility expectations

- Treat the project as a plugin ecosystem, not a monolithic app: the main mod and the minigame are intentionally split.
- Keep optional integrations behind capability checks, not hard runtime requirements.
- Do not assume optional mod assemblies are always present; guard `PluginInfos`, imports, and method access appropriately.
- When targeting third-party game APIs, prefer stable binding patterns and minimal invasive patching.
- Keep JSON-driven content and runtime registration backward-compatible when adding fields or changing parsing behavior.

## Coding conventions

- Prefer small, explicit helper methods over giant patch files.
- Keep feature flags and runtime detection centralized.
- Validate optional integrations with null-safe logic and clear fallback behavior.
- Avoid broad, magical patches. Prefer targeted Harmony patches that are easy to reason about.
- Keep failure modes observable: missing optional support should log clearly and continue without crashing the game.

## Build and validation notes

- This repo references game assemblies and mod dependencies from the local game install, so build validation depends on the environment where those files exist.
- Do not introduce new hard dependencies on optional packages, helper libraries, or platform-specific assumptions.
- When changing mod logic, favor minimal, focused edits that maintain the existing runtime contract and compatibility story.

## Working style for future changes

- Keep code readable and maintainable even if it is a hacky mod project.
- Prefer explicit fallback behavior over silent assumptions.
- When adjusting compatibility shims, keep the project running in API-only mode, standalone mode, and bundled-plugin mode without regressions.
- If a change makes older configs or multiplayer data invalid, document the break and make the migration path deliberate rather than accidental.
