# Tiny dev helper for wtflow on macOS. Requires a .NET 10 SDK (default: ~/.dotnet).
# See docs/dev-setup-macos.md for the full setup path.
#
#   make setup   # init the AvaloniaEdit submodule + restore packages
#   make build   # build the solution
#   make run     # build & launch the app, opening REPO (default: this repo)
#   make shot    # render the Worktrees panel to a dated PNG (headless, no display)
#
# Note: SourceGit is single-instance. Quit any running SourceGit.app before
# `make run`, or it forwards "open repo" to that instance and exits.

export DOTNET_ROOT ?= $(HOME)/.dotnet

# Resolve dotnet to an explicit path: prefer one already on PATH, otherwise the
# per-user SDK at $(DOTNET_ROOT). Using the full path avoids Make's shell-less
# recipe exec missing a PATH that only the user's shell profile sets up.
DOTNET := $(shell command -v dotnet 2>/dev/null || echo $(DOTNET_ROOT)/dotnet)

PROJECT := src/SourceGit.csproj
SLN     := SourceGit.slnx
REPO    ?= $(CURDIR)
SHOT    ?= docs/assets/tickets/worktree-panel-$(shell date +%F).png

.DEFAULT_GOAL := help
.PHONY: help setup build run shot

help:
	@echo "wtflow dev targets:"
	@echo "  make setup                       init submodule + dotnet restore"
	@echo "  make build                       dotnet build $(SLN)"
	@echo "  make run [REPO=/path/to/repo]    launch the app (default repo: this worktree)"
	@echo "  make shot [SHOT=out.png]         headless render of the Worktrees panel"

setup:
	git submodule update --init --recursive
	$(DOTNET) restore $(SLN)

build:
	$(DOTNET) build $(SLN)

run:
	$(DOTNET) run --project $(PROJECT) -- "$(REPO)"

shot:
	$(DOTNET) run --project tests/SourceGit.Screenshots -- "$(SHOT)"
