using System.Collections.Generic;

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
}
