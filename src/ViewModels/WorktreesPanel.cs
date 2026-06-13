using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// One row in the Worktrees panel: Git-native facts plus the optional
    /// wtcraft governance state. Read-only by design for the MVP.
    /// </summary>
    public class WorktreePanelItem
    {
        public string Name { get; }
        public string Branch { get; }
        public string Location { get; }
        public string ShortHead { get; }
        public bool IsCurrent { get; }
        public bool IsMain { get; }
        public bool IsLocked { get; }

        public bool HasGovernance { get; }
        public string Stage { get; }
        public string Role { get; }
        public string Agent { get; }
        public string Verification { get; }
        public string SessionState { get; }
        public IReadOnlyList<string> Alarms { get; }
        public bool HasAlarms => Alarms.Count > 0;

        public WorktreePanelItem(Worktree wt, Models.WtcraftWorktreeState state)
        {
            Name = wt.Name;
            Branch = wt.Branch;
            Location = string.IsNullOrEmpty(wt.DisplayPath) ? wt.FullPath : wt.DisplayPath;
            ShortHead = string.IsNullOrEmpty(wt.Head) ? string.Empty :
                wt.Head.Length > 8 ? wt.Head.Substring(0, 8) : wt.Head;
            IsCurrent = wt.IsCurrent;
            IsMain = wt.IsMain;
            IsLocked = wt.IsLocked;

            HasGovernance = state != null;
            Stage = state?.Stage ?? string.Empty;
            Role = state?.Role ?? string.Empty;
            Agent = state?.Agent ?? string.Empty;
            Verification = state?.Verification ?? string.Empty;
            SessionState = state?.SessionState ?? string.Empty;
            Alarms = state?.Alarms ?? [];
        }
    }

    /// <summary>
    /// View-model for the default-open right-side Worktrees panel. It merges the
    /// repository's Git-native worktrees with an optional wtcraft snapshot and
    /// degrades gracefully (Git-only) when wtcraft is unavailable.
    /// </summary>
    public class WorktreesPanel : ObservableObject
    {
        public ObservableCollection<WorktreePanelItem> Items { get; } = [];

        public bool IsWtcraftAvailable
        {
            get => _isWtcraftAvailable;
            private set => SetProperty(ref _isWtcraftAvailable, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            private set => SetProperty(ref _isEmpty, value);
        }

        public WorktreesPanel(Models.IWtcraftClient wtcraft)
        {
            _wtcraft = wtcraft;
        }

        /// <summary>
        /// Rebuilds the panel rows from the latest Git worktree list. Safe to
        /// call on every refresh; must run on the UI thread.
        /// </summary>
        public void Update(string repoPath, IReadOnlyList<Worktree> worktrees)
        {
            var snapshot = TryGetSnapshot(repoPath);
            IsWtcraftAvailable = snapshot != null;

            Items.Clear();
            if (worktrees != null)
            {
                foreach (var wt in worktrees)
                    Items.Add(new WorktreePanelItem(wt, MatchState(snapshot, wt)));
            }

            IsEmpty = Items.Count == 0;
        }

        private Models.WtcraftSnapshot TryGetSnapshot(string repoPath)
        {
            try
            {
                // A client should not throw, but never trust that across a boundary.
                return _wtcraft?.GetSnapshot(repoPath);
            }
            catch
            {
                return null;
            }
        }

        private static Models.WtcraftWorktreeState MatchState(Models.WtcraftSnapshot snapshot, Worktree wt)
        {
            if (snapshot?.Worktrees == null)
                return null;

            foreach (var state in snapshot.Worktrees)
            {
                if (!string.IsNullOrEmpty(state.Branch) &&
                    string.Equals(state.Branch, wt.Branch, StringComparison.Ordinal))
                    return state;

                if (!string.IsNullOrEmpty(state.Path) &&
                    wt.FullPath.EndsWith(state.Path, StringComparison.Ordinal))
                    return state;
            }

            return null;
        }

        private readonly Models.IWtcraftClient _wtcraft;
        private bool _isWtcraftAvailable = false;
        private bool _isEmpty = true;
    }
}
