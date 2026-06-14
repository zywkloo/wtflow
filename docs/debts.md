# Technical debt

A living ledger of known gaps between this fork's code and the `wtcraft`
machine protocol it targets. Each entry records the symptom, the root cause,
and the fix direction. Protocol references point at the `wtcraft` repository
(`docs/protocol/`), which is authoritative for the wire contract.

The headline debt today is not a missing feature — it is that the Worktrees
panel renders **fixture data**, not real worktree state. D1–D6 are the concrete
steps to close that gap.

---

## D1 — Worktrees panel renders fixture data, not real state

- **Symptom:** the stage / role / verification / alarms shown in the panel are
  hardcoded; they do not reflect the actual `.worktree-task.md` contracts or
  any running session.
- **Root:** [FixtureWtcraftClient.cs](src/Models/FixtureWtcraftClient.cs) loads
  an embedded JSON resource (`Resources/Fixtures/wtcraft-worktrees.json`). The
  panel is written against `IWtcraftClient` precisely so a real client can
  replace it.
- **Fix:** add a `CliWtcraftClient` that shells out to `wtcraft status --json`
  for task + git facts, reads `.worktree-session.json` for runtime facts, and
  computes alarms. See D2–D4 for the shape work that has to land first, and D6
  for the binary prerequisite.

## D2 — `WtcraftSnapshot` wrapper shape does not match the wire format

- **Symptom:** [WtcraftState.cs](src/Models/WtcraftState.cs) deserializes into
  `{ SchemaVersion, Worktrees[] }`.
- **Root:** the real `wtcraft status --json` success shape is a **bare JSON
  array** of worktree objects — there is no top-level wrapper and no schema
  field. The wrapper was invented for the fixture. (Machine Protocol v1,
  `status --json`: "Success shape: JSON array of worktree objects.")
- **Fix:** deserialize a bare array; drop the `SchemaVersion` wrapper from the
  model. Move version gating to D5.

## D3 — `WtcraftWorktreeState` field mismatch

- **Symptom:** the model's fields are a lossy, renamed subset of the wire
  fields.
- **Root:** the model predates the stabilized `status --json` field set.

| `status --json` field | `WtcraftWorktreeState` | Note |
| --- | --- | --- |
| `worktree` | `Path` | rename |
| `branch` | `Branch` | ok |
| `stage` | `Stage` | ok |
| `role` | `Role` | ok |
| `agent` | `Agent` | ok |
| `verify_result` + `verified` | `Verification` | two fields collapsed into one |
| `repo_root` | — | missing |
| `zombie` | — | missing (registry dir deleted) |
| `locked` | — | missing |
| `contracted` | — | missing (no `.worktree-task.md`) |
| `task_file` | — | missing |
| `status` (legacy) | — | missing (fallback when `stage` absent) |
| `priority` | — | missing |
| `created` | — | missing |
| `base` | — | missing |

- **Fix:** align the model to the Machine Protocol v1 `status --json` field set.
  Keep `verify_result` and `verified` distinct.

## D4 — `Alarms` and `SessionState` do not come from `status --json`

- **Symptom:** the model carries `Alarms[]` and `SessionState` as if they were
  part of one snapshot source. They are not in `status --json`.
- **Root:** `status --json` reports only task-contract + git facts. The other
  two come from different places:
  - `SessionState` ← `.worktree-session.json` (Session Model v1) — a separate,
    launcher-owned, machine-local file.
  - `Alarms` ← the **observer reconcile** of task facts × session facts × git
    facts, defined by the Session Model v1 "Reconciliation with task state"
    table and the Task State Machine v1 "Observer alarms" table.
- **Fix direction:**
  - Read `.worktree-session.json` directly. It is a versioned, documented
    schema — parsing it is not output-scraping.
  - **Alarm computation belongs in the core, long-term.** The target is a
    core-owned reconcile command (proposed `wtcraft observe --json`, emitting
    `alarms[]`) so every client stays a thin renderer and the reconcile tables
    never drift between clients. Until that ships, the GUI computes alarms
    locally as an **explicit temporary stand-in** against the v1 tables, marked
    for deletion once the core command exists. Do not treat the local
    computation as the architecture.

## D5 — Version gating uses a fictional field

- **Symptom:** [FixtureWtcraftClient.cs](src/Models/FixtureWtcraftClient.cs)
  gates on `SchemaVersion == 1`.
- **Root:** `status --json` has no version field by design (bare array, kept
  stable for back-compat). Capability discovery is `wtcraft capabilities --json`
  (`protocol_version`, per-command `json` flags, `success_shape`,
  `supports_repo_option`, `exit_codes`) plus `wtcraft --version`.
- **Fix:** probe `capabilities --json` once; gate on `protocol_version` and the
  per-command `json` capability instead of a wrapper field.

## D6 — Installed `wtcraft` binary is stale

- **Symptom:** `~/.local/bin/wtcraft` ignores `--json` (prints the human table)
  and still emits the legacy `status: ready` instead of `stage:`.
- **Root:** it predates the machine-protocol-v1 build (wtcraft PR #33) and the
  version/doctor branch.
- **Fix:** reinstall the protocol-v1 build before `CliWtcraftClient` can work.
  Note: even the new build currently prints `--version` / `cli_version` as
  `unknown` (its `read_cli_version` finds no package version) — a separate
  upstream gap to track, not a blocker for `status --json`.

---

## Note — live updates without a daemon

Closing D1 does not require server-push. Keep the **compute** in the core
(one-shot `status --json` / future `observe --json`) and put the **trigger** on
the .NET side: a `FileSystemWatcher` on each worktree's `.worktree-task.md` and
`.worktree-session.json` mtimes re-fetches only when something changed. That
yields near-live UX with no `wtcraft` daemon. Server-Sent Events / streaming is
a later optimization for the extracted Rust core (wtcraft ADR-006), not a
prerequisite here.
