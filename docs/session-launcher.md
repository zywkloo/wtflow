# Session launcher (GUI)

> Status: **design, not yet built.** The Worktrees panel ships today as a
> read-only view (see [debts.md](debts.md) D1). The launch surface described
> here is the planned next capability. This document is the GUI-side design; the
> cross-client *contract* it implements lives in the `wtcraft` repository
> (`docs/protocol/`, `docs/adr/005-interactive-first-session-launch.md`,
> `docs/backlogs/session-runtime-protocol.md`).

## Why this lives here

`wtcraft` owns the contract: the session sidecar schema, the legal launch
modes, and the non-goals. This fork owns the *experience*: what the panel looks
like, how it shells out, which terminal it spawns, and how it reflects live
state. Keeping the UX here prevents the protocol docs from absorbing
GUI-specific detail that no other client shares.

## Boundary: launcher, not terminal

The launch surface is a docked panel — a VS Code-style bottom pane — that
**launches and monitors** an agent session. It is not a terminal emulator. The
agent TUI (Claude Code, Codex, Antigravity, …) opens in an **external**
terminal (Ghostty / Terminal.app / a new tab). The moment the docked pane hosts
the live TUI it has become an embedded emulator, which is an explicit non-goal
(wtcraft `docs/sourcegitfork/session-state.md`, ADR-005).

Placement is a free choice (bottom pane vs top-of-list); hosting the TUI is not.

## Surface

A single launcher row/pane with:

- **prompt box** — freeform task description
- **agent picker** — Claude Code / Codex / Antigravity / other CLI
- **launch-mode toggle** — `interactive` (ships first) | `headless` (later)
- **start button** — runs the flow below
- **status row** — live per-worktree state and alarms, sourced from
  `wtcraft status --json` (and the future `observe --json`)

## Start-session flow

Interactive path (v1):

1. User fills prompt, picks an agent, leaves mode on `interactive`, hits start.
2. GUI runs `wtcraft new <type/name>` (with `--repo` targeting), which creates
   the worktree and seeds `.worktree-task.md` with `stage`/`role`/`agent`
   backfilled. The GUI does **not** hand-write the task file — `wtcraft` owns
   the template.
3. GUI writes `.worktree-session.json` for the new worktree. The launcher is the
   **single writer** of this file (Session Model v1); it uses a temp file plus
   atomic rename so readers never see partial JSON.
4. GUI spawns an **external terminal** running the chosen agent CLI in the
   worktree directory, and records the terminal/session handle into the sidecar.
5. GUI monitors via `wtcraft status --json` + the sidecar, offering focus / stop
   / retry without owning the terminal.

Planner-lite init (deferred, v2): before step 4, a short headless call can turn
the freeform prompt into structured Scope / Off-limits / Steps inside
`.worktree-task.md`, then the interactive TUI takes over. Gating needs no vendor
hooks — watch the task-file mtime/content plus the headless exit code. This puts
a headless dependency in the first flow, which ADR-005 defers.

## Launch modes

- **interactive** (v1) — external TUI, human drives, GUI observes. The default.
- **headless / full-auto** (later) — one long `/loop` or `/goal` in auto mode
  until it hits an alarm, then parks for human review; the GUI surfaces only
  acceptance/verification, never keystrokes.

Both modes report through the same task contract + sidecar, so the toggle swaps
the runner, not the observer.

## Live updates without a daemon

Keep the compute in the core (`wtcraft status --json`, future `observe --json`)
and put the trigger on the .NET side: a `FileSystemWatcher` on each worktree's
`.worktree-task.md` and `.worktree-session.json` mtimes re-fetches only when
something changed. Near-live UX, no `wtcraft` daemon.

Phasing:

- **Now (v1):** one-shot `observe --json` triggered by `FileSystemWatcher`.
  Pull, but reactive — every state change that goes *through* the sidecar (the
  launcher writing `.worktree-session.json`) is a file write, so the watcher
  catches it near-instantly.
- **Later (Rust core):** the same `observe --json` *schema* delivered by push
  (SSE/stream), discovered via `capabilities --json`. The GUI swaps transport,
  not its data model — the interim is not throwaway.

The one gap the interim cannot cover is an *un-mediated* process death (a hard
crash where the launcher never wrote `exit_code`); the next poll's PID /
start-time check catches it with latency. If the GUI wants "just changed"
toasts before Rust lands, it can diff successive snapshots client-side. Neither
gap is a correctness issue — only freshness — so revisiting them at the Rust
refactor is a safe deferral, not a known defect.

## Non-goals (GUI side)

- no terminal emulation (the TUI is external)
- not the canonical store for agent conversation logs
- no process supervision beyond launch / focus / stop / retry
- no client-local reconcile logic as the long-term home — alarms are the core's
  job via `observe --json`; any interim local computation is a temporary
  stand-in (see [debts.md](debts.md) D4)

## Implements (wtcraft contract)

- Session Model v1 — `docs/protocol/session-model-v1.md` (sidecar schema, states,
  liveness, reconciliation)
- Task State Machine v1 — `docs/protocol/task-state-machine-v1.md`
- Machine Protocol v1 — `docs/protocol/machine-protocol-v1.md`
  (`status`/`observe --json`, targeting, capabilities)
- ADR-005 — interactive-first session launch
- `docs/backlogs/session-runtime-protocol.md` — launch modes, Planner-lite
