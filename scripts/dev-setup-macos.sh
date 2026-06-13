#!/usr/bin/env bash
#
# dev-setup-macos.sh — deterministic local setup check for wtflow on macOS.
#
# What it does:
#   1. Reads the required .NET SDK version from global.json.
#   2. Checks the local `dotnet` install and warns on version mismatch.
#   3. Initializes the AvaloniaEdit submodule (needed to build).
#   4. Runs `dotnet restore`.
#
# It is safe to re-run. See docs/dev-setup-macos.md for the full guide.

set -euo pipefail

# Resolve repo root from this script's location so it works from anywhere.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

info()  { printf '  %s\n' "$*"; }
ok()    { printf '\033[0;32m[ ok ]\033[0m %s\n' "$*"; }
warn()  { printf '\033[1;33m[warn]\033[0m %s\n' "$*"; }
err()   { printf '\033[0;31m[fail]\033[0m %s\n' "$*" >&2; }
step()  { printf '\n==> %s\n' "$*"; }

DOC="docs/dev-setup-macos.md"

# --- 1. Required SDK version from global.json --------------------------------
step "Reading required .NET SDK from global.json"
REQUIRED_VERSION=""
if [ -f global.json ]; then
  REQUIRED_VERSION="$(grep -Eo '"version"[[:space:]]*:[[:space:]]*"[0-9]+\.[0-9]+\.[0-9]+"' global.json \
    | grep -Eo '[0-9]+\.[0-9]+\.[0-9]+' | head -1 || true)"
fi
if [ -z "$REQUIRED_VERSION" ]; then
  warn "Could not parse a version from global.json; assuming .NET 10."
  REQUIRED_VERSION="10.0.0"
fi
REQUIRED_MAJOR="${REQUIRED_VERSION%%.*}"
ok "Repo requires .NET SDK ${REQUIRED_MAJOR}.x (global.json pins ${REQUIRED_VERSION})."

# --- 2. Local dotnet -----------------------------------------------------------
step "Checking local dotnet"
if ! command -v dotnet >/dev/null 2>&1; then
  err "dotnet not found on PATH."
  info "Install the .NET ${REQUIRED_MAJOR} SDK, then re-run this script."
  info "Quickest path (user-local, no sudo):"
  info "  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh"
  info "  chmod +x /tmp/dotnet-install.sh && /tmp/dotnet-install.sh --channel ${REQUIRED_MAJOR}.0"
  info "  export DOTNET_ROOT=\"\$HOME/.dotnet\" && export PATH=\"\$DOTNET_ROOT:\$PATH\""
  info "Full guide: ${DOC}"
  exit 1
fi

INSTALLED_VERSION="$(dotnet --version 2>/dev/null || echo 'unknown')"
INSTALLED_MAJOR="${INSTALLED_VERSION%%.*}"
ok "Found dotnet ${INSTALLED_VERSION} ($(command -v dotnet))."

info "Installed SDKs:"
dotnet --list-sdks | sed 's/^/    /'

if dotnet --list-sdks | grep -q "^${REQUIRED_MAJOR}\."; then
  ok "A .NET ${REQUIRED_MAJOR}.x SDK is installed."
else
  warn "No .NET ${REQUIRED_MAJOR}.x SDK found. The build pins ${REQUIRED_MAJOR}.x via global.json."
  warn "Install it with: dotnet-install.sh --channel ${REQUIRED_MAJOR}.0  (see ${DOC})"
fi

if [ "$INSTALLED_MAJOR" != "$REQUIRED_MAJOR" ]; then
  warn "Default 'dotnet --version' is ${INSTALLED_VERSION}; repo expects ${REQUIRED_MAJOR}.x."
fi

# --- 3. Submodules -------------------------------------------------------------
step "Ensuring submodules are initialized"
if [ -f .gitmodules ]; then
  if [ -f depends/AvaloniaEdit/src/AvaloniaEdit/AvaloniaEdit.csproj ]; then
    ok "Submodule depends/AvaloniaEdit already present."
  else
    info "Initializing submodules (git submodule update --init --recursive)..."
    if git submodule update --init --recursive; then
      ok "Submodules initialized."
    else
      warn "Submodule init failed (network?). Build will fail until depends/AvaloniaEdit exists."
    fi
  fi
else
  info "No .gitmodules; skipping."
fi

# --- 4. Restore ----------------------------------------------------------------
step "Restoring NuGet packages"
RESTORE_TARGET="SourceGit.slnx"
[ -f "$RESTORE_TARGET" ] || RESTORE_TARGET=""
info "Running: dotnet restore ${RESTORE_TARGET}"
if dotnet restore ${RESTORE_TARGET}; then
  ok "Restore complete."
else
  err "dotnet restore failed. See output above and ${DOC} (section 6)."
  exit 1
fi

step "Done"
ok "Environment looks ready. Next: dotnet build SourceGit.slnx"
