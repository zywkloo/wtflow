# Ticket screenshots

Screenshots that document a feature/ticket go here.

## Worktrees panel (read-only MVP)

![Worktrees panel](worktree-panel-2026-06-13.png)

Generated headlessly (no display / no screen-recording permission) via the
`tests/SourceGit.Screenshots` harness — the Avalonia analog to a Storybook
snapshot. It shows governed worktrees (`master`, `feat/worktree-panel-mvp`),
a Git-only row (`docs/...`, graceful degradation), and an alarm.

```sh
dotnet run --project tests/SourceGit.Screenshots -- \
  docs/assets/tickets/worktree-panel-$(date +%F).png
```

## Naming convention

```
<area>-<short-slug>-YYYY-MM-DD.png
```

Example: `worktree-panel-2026-06-13.png`

- **Date-stamped** so a ticket can carry several iterations without overwriting.
- **Keep them small** — downscale to ~1600 px on the long edge and optimize:
  ```sh
  sips -Z 1600 raw.png --out worktree-panel-2026-06-13.png
  ```
  Aim for well under ~500 KB; PNG is fine for UI shots.

## How to capture (macOS)

The app opens a repository directly from the command line, so you can land on
the Repository view (where the Worktrees panel lives) in one step:

```sh
dotnet run --project src/SourceGit.csproj -- /path/to/a/repo/with/worktrees
```

Then capture just the window (needs Screen Recording permission for the
terminal/IDE):

```sh
# Interactive: press Space, then click the window
screencapture -o -W -x raw.png
sips -Z 1600 raw.png --out docs/assets/tickets/worktree-panel-$(date +%F).png
```

> **Note:** SourceGit is single-instance (a shared IPC lock under
> `~/Library/Application Support/SourceGit`). If `SourceGit.app` is already
> running, a dev build forwards the "open repo" request to it and exits, so quit
> the installed app first to screenshot your local build. A display-free
> alternative is rendering the control with `Avalonia.Headless` (see the PR
> discussion).
