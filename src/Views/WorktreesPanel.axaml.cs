using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class WorktreesPanel : UserControl
    {
        public WorktreesPanel()
        {
            InitializeComponent();
        }

        private ViewModels.Repository FindRepo() =>
            this.FindAncestorOfType<Repository>()?.DataContext as ViewModels.Repository;

        private void OnWorktreeDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Control { DataContext: ViewModels.WorktreePanelItem item })
                FindRepo()?.OpenWorktree(item.SourceWorktree);
            e.Handled = true;
        }

        private void OnWorktreeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            if (sender is not Control { DataContext: ViewModels.WorktreePanelItem item } ctrl)
            {
                e.Handled = true;
                return;
            }

            var repo = FindRepo();
            if (repo == null)
            {
                e.Handled = true;
                return;
            }

            var wt = item.SourceWorktree;
            var menu = new ContextMenu();

            var open = new MenuItem();
            open.Header = App.Text("Worktree.Open");
            open.Icon = this.CreateMenuIcon("Icons.Folder.Open");
            open.Click += (_, ev) => { repo.OpenWorktree(wt); ev.Handled = true; };
            menu.Items.Add(open);
            menu.Items.Add(new MenuItem { Header = "-" });

            if (wt.IsLocked)
            {
                var unlock = new MenuItem();
                unlock.Header = App.Text("Worktree.Unlock");
                unlock.Icon = this.CreateMenuIcon("Icons.Unlock");
                unlock.Click += async (_, ev) => { await repo.UnlockWorktreeAsync(wt); ev.Handled = true; };
                menu.Items.Add(unlock);
            }
            else
            {
                var loc = new MenuItem();
                loc.Header = App.Text("Worktree.Lock");
                loc.Icon = this.CreateMenuIcon("Icons.Lock");
                loc.IsEnabled = !wt.IsMain;
                loc.Click += async (_, ev) => { await repo.LockWorktreeAsync(wt); ev.Handled = true; };
                menu.Items.Add(loc);
            }

            var remove = new MenuItem();
            remove.Header = App.Text("Worktree.Remove");
            remove.Icon = this.CreateMenuIcon("Icons.Clear");
            remove.IsEnabled = !wt.IsCurrent && !wt.IsMain;
            remove.Click += (_, ev) =>
            {
                if (repo.CanCreatePopup())
                    repo.ShowPopup(new ViewModels.RemoveWorktree(repo, wt));
                ev.Handled = true;
            };
            menu.Items.Add(remove);

            var copy = new MenuItem();
            copy.Header = App.Text("Worktree.CopyPath");
            copy.Icon = this.CreateMenuIcon("Icons.Copy");
            copy.Click += async (_, ev) => { await this.CopyTextAsync(wt.FullPath); ev.Handled = true; };
            menu.Items.Add(new MenuItem { Header = "-" });
            menu.Items.Add(copy);

            menu.Open(ctrl);
            e.Handled = true;
        }

        private void OnAddWorktree(object sender, RoutedEventArgs e)
        {
            FindRepo()?.AddWorktree();
            e.Handled = true;
        }

        private async void OnPruneWorktrees(object sender, RoutedEventArgs e)
        {
            var repo = FindRepo();
            if (repo != null)
                await repo.PruneWorktreesAsync();
            e.Handled = true;
        }

        private void OnHidePanel(object sender, RoutedEventArgs e)
        {
            var repo = FindRepo();
            if (repo != null)
                repo.IsWorktreesPanelVisible = false;
            e.Handled = true;
        }
    }
}
