# Project Structure Guidelines

This document defines architecture and placement guidelines only.

- For setup, local run, and command usage, see [CONTRIBUTING.md](../CONTRIBUTING.md).
- Do not add command snippets to this file.

## Purpose of each project

## `src/Components`

A reusable React component library that can be published to npm and reused by `src/Client` and `src/Electron/src/Renderer`.

This project contains Storybook/Vitest-based component tests and usage documentation.

## `src/Client`

The main browser application (React + Elmish). Used for [swate.nfdi4plants.org](https://swate.nfdi4plants.org/), ARCitect v1 integration, and the Excel add-in.

Reuses code from `src/Components`.

## `src/Server`

The .NET backend application for centralized APIs and database access for Swate applications.

This project must not be Fable-compatible.

## `src/Shared`

Shared, Fable-compatible .NET code used by Client and Server as the ground truth for shared types and client-server contracts.

## `src/Electron`

Electron application stack rebuilt via Fable.

### `src/Electron/src/Main`

Node.js main process code for filesystem and other native functionality.

### `src/Electron/src/Renderer`

React renderer process code for Electron UI.

Reuses code from `src/Components`. If UI logic is needed in both Client and Renderer, extract it into `src/Components` (or another shared isolated module) instead of importing from `src/Client`.

### `src/Electron/src/Preload`

Node.js preload process code. Exposes native functionality to renderer via context bridge and registers IPC channels.

### `src/Electron/src/Swate.Electron.Shared`

Shared types/utilities for main-renderer-preload communication.

For example: IPC base types are defined here and implemented in preload, main, and renderer.

## Additional dependency rules

- `src/Client` must not be referenced by any Electron project.
- Browser UI concerns must not be moved into `src/Server`.
- Electron process-specific code must stay process-specific:
	- `Main` contains Node/Electron main APIs.
	- `Preload` contains bridge and IPC registration code.
	- `Renderer` contains UI and renderer-safe integrations.

## Guidelines for isolated React components (`src/Components`)

Components in `src/Components` are expected to be reusable and application-agnostic.

- **PascalCase file and component names**: `MyComponent.tsx` for the component "MyComponent" and `MyComponent.stories.tsx` for its Storybook tests. This also includes subcomponent names (These can be exported from the same file or split into separate files if they are large enough to warrant it).
- **No app-state coupling**: no direct dependency on Client Elmish model/update logic.
- **No app-side effects**: do not call app-specific services directly; pass handlers via props.
- **Stable public API**: prefer explicit props and callbacks over hidden global state.
- **Presentation-first**: keep components focused on rendering and local UI interaction.
- **Portable behavior**: component behavior must work in both browser and Electron renderer contexts.
- **Local stories/tests**: test files must be colocated and named `<Component>.stories.tsx`.

### Placement rule of thumb

- If code is reusable UI without app context -> put it in `src/Components`.
- If code requires Client application state/workflow -> put it in `src/Client`.
- If code is contract/type shared between backend and frontend -> put it in `src/Shared`.
- If code is Electron process or IPC specific -> put it in the corresponding Electron project.
