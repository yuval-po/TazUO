using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    /// <summary>
    /// Myra-based replacement for the legacy grid highlight menu. Renders every highlight
    /// configuration of the current profile as a <see cref="Rulebase{TRule}"/> table with inline
    /// enable/rename/color/properties editing plus the rulebase's own add/delete/reorder toolbar,
    /// alongside outer toolbar buttons to import, export and edit the shared property lists.
    /// </summary>
    internal class GridHighlightMenu : MyraControl
    {
        private readonly World _world;
        private Rulebase<GridHighlightData> _rulebase;

        public GridHighlightMenu(World world) : base(TazLang.Get("gridhighlight_settings_title"))
        {
            _world = world;
            Build();
            CenterInViewPort();
        }

        public static void Open(World world)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridHighlightMenu { IsDisposed: false } w)
                {
                    w.BringOnTop();
                    return;
                }
            }

            UIManager.Add(new GridHighlightMenu(world));
        }

        private void Build()
        {
            var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

            root.Widgets.Add(new MyraLabel(TazLang.Get("gridhighlight_settings_desc"), MyraLabel.TextStyle.P) { Width = 400 });
            root.Widgets.Add(BuildToolbar());
            root.Widgets.Add(new ScrollViewer { MaxHeight = 400, Content = BuildRulebase() });

            SetRootContent(root);
        }

        private HorizontalStackPanel BuildToolbar()
        {
            var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            toolbar.Widgets.Add(new MyraButton(TazLang.Get("gridhighlight_export"), () => ExportGridHighlightSettings(_world)));
            toolbar.Widgets.Add(new MyraButton(TazLang.Get("gridhighlight_import"), () => ImportGridHighlightSettings(_world)));
            toolbar.Widgets.Add(new MyraButton(TazLang.Get("gridhighlight_configs"), () => GridHighlightConfig.Show(_world)));

            return toolbar;
        }

        private Rulebase<GridHighlightData> BuildRulebase()
        {
            _rulebase = new Rulebase<GridHighlightData>
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            _rulebase.Columns.AddRange(GridHighlightRulebaseColumns.Get());

            List<GridHighlightSetupEntry> highlights = GridHighlightsConfig.Current.Highlights;
            for (int i = 0; i < highlights.Count; i++)
                _rulebase.Rules.Add(new GridHighlightData(highlights[i]) { Order = (uint)i });

            _rulebase.RuleCrud += OnRuleCrud;
            _rulebase.Reordered += OnReordered;

            return _rulebase;
        }

        private static void OnRuleCrud(object sender, RuleCrudEventArgs<GridHighlightData> args)
        {
            switch (args.Event)
            {
                case RuleCrudEventType.Create:
                    GridHighlightsConfig.Current.Highlights.Add(args.Rule.Entry);
                    break;
                case RuleCrudEventType.Delete:
                    GridHighlightsConfig.Current.Highlights.Remove(args.Rule.Entry);
                    break;
            }

            GridHighlightsConfig.Current.Save();
            GridHighlightData.RecheckMatchStatus();
        }

        private void OnReordered(object sender, RulebaseOrderChangedEventArgs<GridHighlightData> args)
        {
            GridHighlightsConfig.Current.Highlights.Clear();
            GridHighlightsConfig.Current.Highlights.AddRange(_rulebase.Rules.Select(r => r.Entry));
            GridHighlightsConfig.Current.Save();
            GridHighlightData.RecheckMatchStatus();
        }

        internal void SaveAndUpdate()
        {
            GridHighlightsConfig.Current.Save();
            GridHighlightData.RecheckMatchStatus();
        }

        private static void ExportGridHighlightSettings(World world)
        {
            List<GridHighlightSetupEntry> data = GridHighlightsConfig.Current.Highlights;

            RunFileDialog(world, true, TazLang.Get("gridhighlight_export_dialog"), file =>
            {
                if (Directory.Exists(file))
                {
                    // If the path is a directory, append default filename
                    file = Path.Combine(file, "highlights.json");
                }
                else if (!Path.HasExtension(file))
                {
                    // If it's not a directory and has no extension, assume they meant a file name
                    file += ".json";
                }

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
                GameActions.Print(world, TazLang.Get("gridhighlight_export_success", [file]));
            });
        }

        private static void ImportGridHighlightSettings(World world) => RunFileDialog(world, false, TazLang.Get("gridhighlight_import_dialog"), file =>
        {
            try
            {
                if (!File.Exists(file))
                    return;

                string json = File.ReadAllText(file);
                List<GridHighlightSetupEntry> imported = JsonSerializer.Deserialize<List<GridHighlightSetupEntry>>(json);

                if (imported == null)
                    return;

                GridHighlightsConfig.Current.Highlights.AddRange(imported);
                GridHighlightsConfig.Current.Save();

                foreach (IGui gump in UIManager.Gumps)
                {
                    if (gump is not GridHighlightMenu w || w.IsDisposed)
                        continue;

                    foreach (GridHighlightSetupEntry entry in imported)
                        w._rulebase.Rules.Add(new GridHighlightData(entry));

                    for (int i = 0; i < w._rulebase.Rules.Count; i++)
                        w._rulebase.Rules[i].Order = (uint)i;

                    break;
                }

                GridHighlightData.RecheckMatchStatus();
                GameActions.Print(world, TazLang.Get("gridhighlight_import_success", [file]));
            }
            catch (Exception ex)
            {
                GameActions.Print(world, TazLang.Get("gridhighlight_import_error"), Constants.HUE_ERROR);
                Log.Error(ex.ToString());
            }
        });

        private static void RunFileDialog(World world, bool save, string title, Action<string> onResult) => FileSelector.ShowFileBrowser(world, save ? FileSelectorType.Directory : FileSelectorType.File, null, save ? null : ["*.json"], onResult, title);
    }
}
