using System;
using System.Text.Json;

using Avalonia.Platform;

namespace SourceGit.Models
{
    /// <summary>
    /// <see cref="IWtcraftClient"/> backed by a checked-in JSON fixture embedded
    /// as an Avalonia resource. This keeps the panel demonstrable without
    /// depending on an unreleased local wtcraft protocol, and exercises the same
    /// graceful-degradation paths the real client will need.
    /// </summary>
    public class FixtureWtcraftClient : IWtcraftClient
    {
        public const int SupportedSchemaVersion = 1;

        public static readonly Uri FixtureUri =
            new("avares://SourceGit/Resources/Fixtures/wtcraft-worktrees.json");

        public bool IsAvailable => _snapshot != null;

        public FixtureWtcraftClient()
        {
            _snapshot = TryLoad();
        }

        public WtcraftSnapshot GetSnapshot(string repoPath)
        {
            // The fixture is repo-agnostic; a real client would key off repoPath.
            _ = repoPath;
            return _snapshot;
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
