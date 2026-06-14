using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class WorktreePanelItem
    {
        public Worktree SourceWorktree { get; }

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
            SourceWorktree = wt;

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

        public Repository Repository => _repo;

        public WorktreesPanel(Models.IWtcraftClient wtcraft, Repository repo)
        {
            _wtcraft = wtcraft;
            _repo = repo;
        }

        // Runs off the UI thread: may shell out to the wtcraft CLI. Never throws.
        public async Task<Models.WtcraftSnapshot> FetchSnapshotAsync(string repoPath)
        {
            if (_wtcraft == null)
                return null;

            try
            {
                return await _wtcraft.GetSnapshotAsync(repoPath).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        // Applies an already-fetched snapshot. Must run on the UI thread (mutates
        // the observable Items collection).
        public void Update(Models.WtcraftSnapshot snapshot, IReadOnlyList<Worktree> worktrees)
        {
            IsWtcraftAvailable = snapshot != null;

            Items.Clear();
            if (worktrees != null)
            {
                foreach (var wt in worktrees)
                    Items.Add(new WorktreePanelItem(wt, MatchState(snapshot, wt)));
            }

            IsEmpty = Items.Count == 0;
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
        private readonly Repository _repo;
        private bool _isWtcraftAvailable = false;
        private bool _isEmpty = true;
    }
}
