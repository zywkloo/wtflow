using System;
using System.Text.Json;
using System.Threading.Tasks;

using Avalonia.Platform;

namespace SourceGit.Models
{
    /// <summary>
    /// <see cref="IWtcraftClient"/> backed by a checked-in JSON fixture embedded
    /// as an Avalonia resource. Kept as a test/demo fallback; production wiring
    /// uses <see cref="CliWtcraftClient"/>.
    /// </summary>
    public class FixtureWtcraftClient : IWtcraftClient
    {
        public const int SupportedSchemaVersion = 1;

        public static readonly Uri FixtureUri =
            new("avares://SourceGit/Resources/Fixtures/wtcraft-worktrees.json");

        public FixtureWtcraftClient()
        {
            _snapshot = TryLoad();
        }

        public Task<WtcraftSnapshot> GetSnapshotAsync(string repoPath)
        {
            // The fixture is repo-agnostic; a real client keys off repoPath.
            _ = repoPath;
            return Task.FromResult(_snapshot);
        }

        private static WtcraftSnapshot TryLoad()
        {
            try
            {
                if (!AssetLoader.Exists(FixtureUri))
                    return null;

                using var stream = AssetLoader.Open(FixtureUri);
                var snapshot = JsonSerializer.Deserialize(stream, JsonCodeGen.Default.WtcraftSnapshot);

                // Reject anything we do not understand instead of mis-rendering.
                if (snapshot == null || snapshot.SchemaVersion != SupportedSchemaVersion)
                    return null;

                return snapshot;
            }
            catch
            {
                // Any IO/parse failure degrades to "unavailable".
                return null;
            }
        }

        private readonly WtcraftSnapshot _snapshot;
    }
}
