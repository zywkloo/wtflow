using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace SourceGit.Models
{
    /// <summary>
    /// <see cref="IWtcraftClient"/> that shells out to the real <c>wtcraft</c> CLI
    /// (<c>wtcraft status --json</c>, machine protocol v1) and maps its bare-array
    /// output into a <see cref="WtcraftSnapshot"/>.
    ///
    /// Any failure degrades to <c>null</c> so the panel falls back to Git-native
    /// worktree facts: wtcraft missing, a build too old to support <c>--json</c>
    /// (it prints a human table instead), a non-zero exit, or unparseable output.
    ///
    /// Alarms and session state are not populated here — <c>status --json</c>
    /// reports only task-contract and git facts. Reconciled alarms await
    /// <c>observe --json</c>; session state lives in <c>.worktree-session.json</c>.
    /// </summary>
    public class CliWtcraftClient : IWtcraftClient
    {
        public async Task<WtcraftSnapshot> GetSnapshotAsync(string repoPath)
        {
            if (string.IsNullOrEmpty(repoPath))
                return null;

            try
            {
                var starter = new ProcessStartInfo
                {
                    // Resolve `wtcraft` on PATH. Unix-first; on Windows this path
                    // is absent and Process.Start throws, caught below as a
                    // graceful null. A configurable binary path is a follow-up.
                    FileName = "/usr/bin/env",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = repoPath,
                };
                starter.ArgumentList.Add("wtcraft");
                starter.ArgumentList.Add("status");
                starter.ArgumentList.Add("--json");
                starter.ArgumentList.Add("--repo");
                starter.ArgumentList.Add(repoPath);

                using var proc = Process.Start(starter);
                if (proc == null)
                    return null;

                var stdout = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await proc.WaitForExitAsync().ConfigureAwait(false);

                if (proc.ExitCode != 0)
                    return null;

                // Guard against an old wtcraft that ignores --json and prints a
                // human table: real machine output is a JSON array.
                var head = stdout?.TrimStart();
                if (string.IsNullOrEmpty(head) || head[0] != '[')
                    return null;

                var rows = JsonSerializer.Deserialize(stdout, JsonCodeGen.Default.ListWtcraftCliWorktree);
                if (rows == null)
                    return null;

                var snapshot = new WtcraftSnapshot { SchemaVersion = 1 };
                foreach (var r in rows)
                {
                    snapshot.Worktrees.Add(new WtcraftWorktreeState
                    {
                        Branch = r.Branch ?? string.Empty,
                        Path = r.Worktree ?? string.Empty,
                        Stage = r.Stage ?? string.Empty,
                        Role = r.Role ?? string.Empty,
                        Agent = r.Agent ?? string.Empty,
                        Verification = r.VerifyResult ?? string.Empty,
                        // Alarms / SessionState deferred: status --json carries neither.
                    });
                }

                return snapshot;
            }
            catch
            {
                // wtcraft absent, not executable, timed out, or output unparseable.
                return null;
            }
        }
    }
}
