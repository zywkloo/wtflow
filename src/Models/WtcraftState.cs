using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SourceGit.Models
{
    /// <summary>
    /// Optional governance state for a single worktree, sourced from wtcraft.
    /// Every field is optional; consumers must degrade gracefully when a value
    /// is empty so the panel keeps working on plain Git repositories.
    /// </summary>
    public class WtcraftWorktreeState
    {
        // Match key. A state is paired with a Git worktree by branch name first,
        // falling back to the worktree directory name in Path.
        public string Branch { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public string Stage { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Agent { get; set; } = string.Empty;
        public string Verification { get; set; } = string.Empty;
        public List<string> Alarms { get; set; } = [];

        // Placeholder only for the MVP: the real one-session-per-worktree state
        // is owned by an external agent runtime, not this app.
        public string SessionState { get; set; } = string.Empty;
    }

    /// <summary>
    /// A versioned snapshot of governance state for a repository's worktrees.
    /// SchemaVersion lets the client reject payloads it does not understand
    /// instead of mis-rendering them.
    /// </summary>
    public class WtcraftSnapshot
    {
        public int SchemaVersion { get; set; } = 0;
        public List<WtcraftWorktreeState> Worktrees { get; set; } = [];
    }

    /// <summary>
    /// One worktree row from `wtcraft status --json` (machine protocol v1). This
    /// is the raw wire shape — the command emits a bare JSON array of these — with
    /// snake_case field names. <see cref="CliWtcraftClient"/> maps it into
    /// <see cref="WtcraftWorktreeState"/>.
    ///
    /// Alarms and session state are intentionally absent: status --json reports
    /// only task-contract + git facts. Reconciled alarms await `observe --json`
    /// and per-worktree session state lives in `.worktree-session.json`.
    /// </summary>
    public class WtcraftCliWorktree
    {
        [JsonPropertyName("repo_root")] public string RepoRoot { get; set; }
        [JsonPropertyName("worktree")] public string Worktree { get; set; }
        [JsonPropertyName("branch")] public string Branch { get; set; }
        [JsonPropertyName("zombie")] public bool Zombie { get; set; }
        [JsonPropertyName("locked")] public bool Locked { get; set; }
        [JsonPropertyName("contracted")] public bool Contracted { get; set; }
        [JsonPropertyName("task_file")] public string TaskFile { get; set; }
        [JsonPropertyName("stage")] public string Stage { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("agent")] public string Agent { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("priority")] public string Priority { get; set; }
        [JsonPropertyName("created")] public string Created { get; set; }
        [JsonPropertyName("base")] public string Base { get; set; }
        [JsonPropertyName("verify_result")] public string VerifyResult { get; set; }
        [JsonPropertyName("verified")] public string Verified { get; set; }
    }
}
