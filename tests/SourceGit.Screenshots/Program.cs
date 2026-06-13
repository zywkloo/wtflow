using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Styling;
using Avalonia.Threading;

namespace SourceGit.Screenshots
{
    // Renders the Worktrees panel to a PNG with no display, using the real
    // SourceGit App resources (themes/locale/icons) and the checked-in wtcraft
    // fixture, so the row mix (governed + Git-only) matches the running app.
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var outPath = args.Length > 0 ? args[0] : "worktree-panel.png";

            BuildAvaloniaApp().SetupWithoutStarting();

            // Deterministic shot: English + dark theme.
            App.SetLocale("en_US");
            if (Application.Current != null)
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;

            var view = new Views.WorktreesPanel { DataContext = BuildSampleViewModel() };
            var window = new Window
            {
                SystemDecorations = SystemDecorations.None,
                Width = 340,
                Height = 660,
                Content = view,
            };
            window.Show();

            // Pump layout + a render pass, then capture.
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();

            var frame = window.CaptureRenderedFrame();
            if (frame == null)
            {
                Console.Error.WriteLine("CaptureRenderedFrame returned null");
                return 1;
            }

            using var fs = File.Create(outPath);
            frame.Save(fs);
            Console.WriteLine($"Saved {outPath} ({frame.PixelSize.Width}x{frame.PixelSize.Height})");
            return 0;
        }

        private static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                .WithInterFont()
                .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" });

            builder.ConfigureFonts(manager =>
            {
                manager.AddFontCollection(new EmbeddedFontCollection(
                    new Uri("fonts:SourceGit", UriKind.Absolute),
                    new Uri("avares://SourceGit/Resources/Fonts", UriKind.Absolute)));
            });

            return builder;
        }

        private static ViewModels.WorktreesPanel BuildSampleViewModel()
        {
            const string repo = "/Users/dev/wtflow";
            var backends = new List<Models.Worktree>
            {
                new() { FullPath = repo, Branch = "refs/heads/master", Head = "ac1ab06e9f1d2c3b4a5d6e7f8091a2b3c4d5e6f7" },
                new() { FullPath = repo + "/worktrees/docs/claude-code-onboarding", Branch = "refs/heads/docs/claude-code-onboarding", Head = "1b3a9109aabbccddeeff00112233445566778899" },
                new() { FullPath = repo + "/worktrees/feat/worktree-panel-mvp", Branch = "refs/heads/feat/worktree-panel-mvp", Head = "8a167a17ffeeddccbbaa99887766554433221100" },
            };

            var worktrees = ViewModels.Worktree.Build(repo, backends);
            var panel = new ViewModels.WorktreesPanel(new Models.FixtureWtcraftClient());
            panel.Update(repo, worktrees);
            return panel;
        }
    }
}
