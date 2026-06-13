# Local macOS development & Claude Code onboarding

This is the deterministic setup path for building `wtflow` (a SourceGit fork)
on macOS, and the onboarding reference a fresh Claude Code session in this
repo should follow before touching GUI code.

> [!TIP]
> If you just want the short version, run the helper and follow what it tells
> you:
>
> ```sh
> bash scripts/dev-setup-macos.sh
> ```

## 1. What this repo requires

| Requirement      | Value                                   | Source of truth                     |
|------------------|-----------------------------------------|-------------------------------------|
| .NET SDK         | **10.0.x** (.NET 10)                     | [`global.json`](../global.json), CI |
| Target framework | `net10.0`                               | [`src/SourceGit.csproj`](../src/SourceGit.csproj) |
| macOS            | **13.0+** (Ventura or newer)            | `SupportedOSPlatformVersion` in csproj |
| Git              | **>= 2.25.1**                           | upstream SourceGit requirement      |
| Architecture     | Apple Silicon (`osx-arm64`) or Intel (`osx-x64`) | — |

The SDK version is pinned in [`global.json`](../global.json):

```json
{
  "sdk": {
    "version": "10.0.0",
    "rollForward": "latestMajor",
    "allowPrerelease": false
  }
}
```

This means: install a **.NET 10** SDK. `rollForward: latestMajor` lets the
build use the newest installed SDK if exactly `10.0.0` isn't present, but to
match CI (which provisions `10.0.x` via `actions/setup-dotnet`) you should
keep a .NET 10 SDK installed.

## 2. Install the .NET 10 SDK

Pick one option. After installing, jump to [section 3](#3-verify-the-toolchain)
to confirm.

### Option A — Official install script (user-local, no `sudo`) — recommended

Installs to `~/.dotnet` and never touches system directories, so it's easy to
remove later (`rm -rf ~/.dotnet`).

```sh
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0
```

Then add it to your `PATH` (and persist it in your shell profile):

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"

# Persist for zsh (default on modern macOS):
echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.zshrc
echo 'export PATH="$DOTNET_ROOT:$PATH"' >> ~/.zshrc
```

### Option B — Homebrew

```sh
brew install --cask dotnet-sdk
```

The cask tracks the latest .NET SDK. Verify it resolves to a 10.x SDK with
`dotnet --list-sdks` ([section 3](#3-verify-the-toolchain)); if Homebrew only
offers a newer major, prefer Option A pinned to `--channel 10.0`.

### Option C — Microsoft `.pkg` installer

Download the **.NET 10 SDK** for **macOS / Arm64** (or x64 on Intel) from
<https://dotnet.microsoft.com/en-us/download/dotnet/10.0> and run the `.pkg`.
This installs to `/usr/local/share/dotnet` and puts `dotnet` on `PATH`.

## 3. Verify the toolchain

```sh
dotnet --version       # should print 10.0.x
dotnet --list-sdks     # at least one 10.0.x entry
git --version          # >= 2.25.1
```

## 4. Initialize submodules

This repo vendors `AvaloniaEdit` as a Git submodule
([`.gitmodules`](../.gitmodules) → `depends/AvaloniaEdit`). The main project
references it, so the build **fails without it**. Initialize once per clone /
worktree:

```sh
git submodule update --init --recursive
```

`scripts/dev-setup-macos.sh` does this for you.

## 5. Build & run

From the repo root:

```sh
# One-shot environment check + restore:
bash scripts/dev-setup-macos.sh

# Or the individual commands:
dotnet restore SourceGit.slnx
dotnet build SourceGit.slnx
dotnet run --project src/SourceGit.csproj
```

If `nuget.org` isn't configured as a restore source, add it once:

```sh
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Running the app on macOS

`wtflow` is an [Avalonia](https://avaloniaui.net/) desktop app, so `dotnet run`
launches a native window:

```sh
dotnet run --project src/SourceGit.csproj
```

- A Debug build is the default and is what you want for local iteration. AOT /
  trimming only kick in for `-c Release` (see [section 6](#6-common-failure-modes)).
- On first run the app creates its data directory at
  `~/Library/Application Support/SourceGit` (settings, avatars, crash logs).
- To pass a repository at launch:
  `dotnet run --project src/SourceGit.csproj -- /path/to/repo`.

### Environment configuration

| Variable | Purpose | Suggested value |
|----------|---------|-----------------|
| `DOTNET_ROOT` | Where the SDK lives (Option A install) | `$HOME/.dotnet` |
| `PATH` | Must include `$DOTNET_ROOT` | `$DOTNET_ROOT:$PATH` |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | Disable .NET CLI telemetry | `1` (optional) |
| `DOTNET_NOLOGO` | Hide the first-run banner | `1` (optional) |

The first `dotnet restore`/`run` also offers to install an ASP.NET Core HTTPS
dev certificate — harmless and not required for this app; you can ignore the
prompt or `dotnet dev-certs https --trust` if you want it gone.

## 6. Common failure modes

| Symptom | Cause | Fix |
|---------|-------|-----|
| `dotnet: command not found` | SDK not installed or not on `PATH` | Do [section 2](#2-install-the-net-10-sdk); for the script install, re-check `DOTNET_ROOT`/`PATH` |
| `A compatible .NET SDK was not found` / `global.json` error | No .NET 10 SDK present | Install `--channel 10.0` (Option A) |
| Restore/build error about `AvaloniaEdit` / `depends/AvaloniaEdit/...csproj` not found | Submodule not initialized | `git submodule update --init --recursive` ([section 4](#4-initialize-submodules)) |
| `Unable to load the service index for source ...` during restore | `nuget.org` source missing or offline | Add the source (see [section 5](#5-build--run)); check network/proxy |
| AOT / trimming errors on `dotnet build` | AOT is **Release-only** | Use a Debug build for local dev (`dotnet build SourceGit.slnx`, default config is Debug) |
| Slow first build | NuGet download + first compile | Expected; subsequent builds are incremental |

## 7. Verified environment (2026-06-13)

This setup path was run end-to-end on this machine (macOS 26.5, Apple Silicon):

- **.NET SDK `10.0.301` installed** to `~/.dotnet` via the Option A script
  ([section 2](#2-install-the-net-10-sdk)). `dotnet --version` → `10.0.301`,
  which satisfies the `10.0.x` pin in `global.json`.
- **Submodule `depends/AvaloniaEdit` initialized** ([section 4](#4-initialize-submodules)).
- `dotnet restore SourceGit.slnx` → **succeeded** (3 projects).
- `dotnet build SourceGit.slnx` → **succeeded, 0 warnings, 0 errors**.
- `bash scripts/dev-setup-macos.sh` → **all green**.

> [!NOTE]
> The SDK was installed to the per-user `~/.dotnet` (no `sudo`). It is only on
> `PATH` for shells that export it — add the two lines from
> [section 2, Option A](#2-install-the-net-10-sdk) to your `~/.zshrc` so new
> terminals (and Claude Code sessions) pick it up.
