# Project-T Development Guidelines

This file contains workspace-scoped rules and instructions for Antigravity agents operating in the `Project-T` codebase.

---

## 1. Project Stack
* **Engine**: Unity 3D
* **Networking**: Photon Fusion (Host/Server-Client architecture)
* **UI**: Unity UI & TextMeshPro (TMP)

---

## 2. Coding Style & Conventions (C#)
* **Public Fields/Properties**: Use `PascalCase`.
* **Private/Protected Fields**: Use camelCase prefixed with an underscore (e.g., `_myField`).
* **Attributes**: Always place attributes on a separate line above the target declaration. Use `[SerializeField]` for private fields exposed to the Inspector.

---

## 3. Photon Fusion Rules
* **Networked State**: 
  * Networked properties must use the auto-implemented property syntax: `[Networked] public type PropertyName { get; set; }`.
  * Do not initialize networked properties inline. Use `Spawned()` or spawner callbacks (`beforeSpawnCallback`) for initialization.
* **Authority**:
  * Only modify `[Networked]` state on the **State Authority** (Server/Host).
  * Use `Object.HasStateAuthority` or `Runner.IsServer` checks before mutating state.
* **RPCs**:
  * Use `[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]` for client requests to change state on the server.
  * Use `[Rpc(RpcSources.StateAuthority, RpcTargets.All)]` for broadcasting visual/transient events.
* **Visual Updates**: Use `[OnChangedRender(nameof(OnChangedCallback))]` on networked properties to handle local visual updates immediately when state changes.
* **Lobby / Matchmaking**:
  * When starting or locking a match, modify session flags: `Runner.SessionInfo.IsOpen = false` and `Runner.SessionInfo.IsVisible = false`.
  * Filter session lists in the lobby UI using `session.IsVisible` and `session.IsOpen`.
