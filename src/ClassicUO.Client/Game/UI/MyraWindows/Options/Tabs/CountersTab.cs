using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class CountersTab
{
    internal static OptionItem GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;

        return new OptionItem(
            "counters",
            () => new WrapPanel().AddRange(
                new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(
                            () => profile.CounterBarEnabled,
                            b =>
                            {
                                profile.CounterBarEnabled = b;
                                CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

                                if (b)
                                {
                                    if (counterGump != null)
                                        counterGump.IsEnabled = counterGump.IsVisible = true;
                                    else
                                        UIManager.Add(counterGump = new CounterBarGump(World.Instance, 200, 200));
                                }
                                else
                                    counterGump?.IsEnabled = counterGump.IsVisible = false;

                                counterGump?.SetLayout(
                                    profile.CounterBarCellSize,
                                    profile.CounterBarRows,
                                    profile.CounterBarColumns
                                );
                            }
                        ),
                        counterLang.EnableCounters
                    ),
                    GetAbbreviationGroup(),
                    GetHighlightGroup(),
                    OptionsFactory.CreateSpacer(),
                    GetLayoutGroup()
                )
            )
        );
    }

    private static CheckBoxGroup GetAbbreviationGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;

        return new CheckBoxGroup(
            new PropertyBinder(
                new Accessor<bool>(() => profile.CounterBarDisplayAbbreviatedAmount),
                counterLang.AbbreviatedValues
            ),
            new LabeledIntegerInput(
                counterLang.AbbreviateIfAmountExceeds,
                new Accessor<int>(() => profile.CounterBarAbbreviatedAmount)
            ) { InputBoxWidth = StyleConstantsDefaults.NUMERIC_INPUT_BOX_WIDTH, MinValue = 999, MaxValue = 999999999 }
        );
    }

    private static VisualContainer GetHighlightGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;

        return new VisualContainer(
            new VisualContainerProps { LabelText = counterLang.SectionHighlightingLabel },
            OptionsFactory.CreateCheckboxOption(
                counterLang.HighlightItemsOnUse,
                new Accessor<bool>(() => profile.CounterBarHighlightOnUse)
            ),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.CounterBarHighlightOnAmount),
                    counterLang.HighlightRedWhenAmountIsLow
                ),
                new LabeledIntegerInput(
                    counterLang.HighlightRedIfAmountIsBelow,
                    new Accessor<int>(() => profile.CounterBarHighlightAmount)
                ) { InputBoxWidth = StyleConstantsDefaults.NUMERIC_INPUT_BOX_WIDTH, MinValue = 1, MaxValue = 60000 }
            )
        );
    }

    private static VisualContainer GetLayoutGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;

        return
            new VisualContainer(
                new VisualContainerProps { LabelText = counterLang.CounterLayout },
                OptionsFactory.CreateSliderOption(
                    counterLang.GridSize,
                    30,
                    80,
                    profile.CounterBarCellSize,
                    v =>
                    {
                        profile.CounterBarCellSize = (int)v;
                        UIManager.GetGump<CounterBarGump>()
                            ?.SetLayout(
                                profile.CounterBarCellSize,
                                profile.CounterBarRows,
                                profile.CounterBarColumns
                            );
                    }
                ),
                new LabeledIntegerInput(
                    counterLang.Rows,
                    profile.CounterBarRows,
                    v =>
                    {
                        profile.CounterBarRows = v;
                        UIManager.GetGump<CounterBarGump>()
                            ?.SetLayout(
                                profile.CounterBarCellSize,
                                profile.CounterBarRows,
                                profile.CounterBarColumns
                            );
                    }
                ) { InputBoxWidth = StyleConstantsDefaults.NUMERIC_INPUT_BOX_WIDTH, MinValue = 1, MaxValue = 30 },
                new LabeledIntegerInput(
                    counterLang.Columns,
                    profile.CounterBarColumns,
                    v =>
                    {
                        profile.CounterBarColumns = v;
                        UIManager.GetGump<CounterBarGump>()
                            ?.SetLayout(
                                profile.CounterBarCellSize,
                                profile.CounterBarRows,
                                profile.CounterBarColumns
                            );
                    }
                ) { InputBoxWidth = StyleConstantsDefaults.NUMERIC_INPUT_BOX_WIDTH, MinValue = 1, MaxValue = 30 }
            );
    }
}
