using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class ContainersTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            lang.LabelContainers,
            () => OptionTabCommons.StyledWrapPanel(
                GetStandardContainerSection(),
                GetGridContainerSection()
            )
        );
    }

    private static VisualContainer GetStandardContainerSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;

        var container = new VisualContainer(
            new VisualContainerProps { LabelText = containerLang.LabelOriginalContainers },
            GetNormalContainerCheckboxesSection(),
            GetNormalContainersScalingSection(),
            new MyraButton(
                containerLang.RebuildContainersTxt,
                () => World.Instance.ContainerManager.BuildContainerFile(true)
            )
        );

        if (Client.Game.UO.Version >= ClientVersion.CV_706000)
            container.Add(
                OptionsFactory.CreateCheckboxOption(
                    containerLang.UseLargeContainerGumps,
                    new Accessor<bool>(() => profile.UseLargeContainerGumps)
                )
            );

        if (Client.Game.UO.Version >= ClientVersion.CV_705301)
            container.Add(
                OptionsFactory.CreateComboBox(
                    containerLang.CharacterBackpackStyle,
                    profile.BackpackStyle,
                    [
                        containerLang.BackpackOpt_Default,
                        containerLang.BackpackOpt_Suede,
                        containerLang.BackpackOpt_PolarBear,
                        containerLang.BackpackOpt_GhoulSkin
                    ],
                    i => profile.BackpackStyle = i
                )
            );

        return container;
    }

    private static WrapPanel GetNormalContainerCheckboxesSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;

        return OptionTabCommons.StyledWrapPanel(
            OptionsFactory.CreateCheckboxOption(
                containerLang.DoubleClickToLootItemsInsideContainers,
                new Accessor<bool>(() => profile.DoubleClickToLootInsideContainers)
            ),
            OptionsFactory.CreateCheckboxOption(
                containerLang.RelativeDragAndDropItemsInContainers,
                new Accessor<bool>(() => profile.RelativeDragAndDropItems)
            ),
            OptionsFactory.CreateCheckboxOption(
                containerLang.HighlightContainerOnGroundWhenMouseIsOverAContainerGump,
                new Accessor<bool>(() => profile.HighlightContainerWhenSelected)
            ),
            OptionsFactory.CreateCheckboxOption(
                containerLang.RecolorContainerGumpByWithContainerHue,
                new Accessor<bool>(() => profile.HueContainerGumps)
            ),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.OverrideContainerLocation),
                    containerLang.OverrideContainerGumpLocations
                ),
                OptionsFactory.CreateComboBox(
                    containerLang.OverridePosition,
                    profile.OverrideContainerLocationSetting,
                    [
                        containerLang.PositionOpt_NearContainer,
                        containerLang.PositionOpt_TopRight,
                        containerLang.PositionOpt_LastDraggedPosition,
                        containerLang.RememberEachContainer
                    ],
                    i => profile.OverrideContainerLocationSetting = i
                )
            )
        );
    }

    private static VisualContainer GetNormalContainersScalingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;

        return new VisualContainer(
            new VisualContainerProps { LabelText = containerLang.ContainerScale },
            OptionsFactory.CreateSliderOption(
                containerLang.ContainerScale,
                Constants.MIN_CONTAINER_SIZE_PERC,
                Constants.MAX_CONTAINER_SIZE_PERC,
                profile.ContainersScale,
                i =>
                {
                    profile.ContainersScale = (byte)i;
                    UIManager.ContainerScale = (byte)i / 100f;
                    UIManager.ForEach<ContainerGump>(c => c.RequestUpdateContents());
                }
            ),
            OptionsFactory.CreateCheckboxOption(
                containerLang.AlsoScaleItems,
                new Accessor<bool>(() => profile.ScaleItemsInsideContainers)
            )
        );
    }

    private static VisualContainer GetGridContainerSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;

        return new VisualContainer(
            new VisualContainerProps { LabelText = containerLang.LabelGridContainers },
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.UseGridLayoutContainerGumps),
                    tuoLang.EnableGridContainers
                ),
                OptionsFactory.CreateCheckboxOption(
                    tuoLang.GridContainersDefaultToOldStyleView,
                    new Accessor<bool>(() => profile.GridContainersDefaultToOldStyleView)
                ),
                OptionsFactory.CreateSliderOption(
                    tuoLang.GridContainerScale,
                    50,
                    200,
                    profile.GridContainersScale,
                    i => profile.GridContainersScale = (byte)i
                ),
                OptionsFactory.CreateCheckboxOption(
                    tuoLang.AlsoScaleItems,
                    new Accessor<bool>(() => profile.GridContainerScaleItems)
                ),
                OptionsFactory.CreateSliderOption(
                    tuoLang.GridItemBorderOpacity,
                    0,
                    100,
                    profile.GridBorderAlpha,
                    i =>
                    {
                        profile.GridBorderAlpha = (byte)i;
                        GridContainer.GridItem.StaticGridContainerSettingUpdated();
                    }
                ),
                OptionsFactory.CreateHuePicker(
                    tuoLang.BorderColor,
                    profile.GridBorderHue,
                    h =>
                    {
                        profile.GridBorderHue = h;
                        GridContainer.GridItem.StaticGridContainerSettingUpdated();
                    }
                ),
                OptionsFactory.CreateSliderOption(
                    tuoLang.ContainerOpacity,
                    0,
                    100,
                    profile.ContainerOpacity,
                    i =>
                    {
                        profile.ContainerOpacity = (byte)i;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                OptionsFactory.CreateHuePicker(
                    tuoLang.BackgroundColor,
                    profile.AltGridContainerBackgroundHue,
                    h =>
                    {
                        profile.AltGridContainerBackgroundHue = h;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                OptionsFactory.CreateCheckboxOption(
                    tuoLang.UseContainersHue,
                    new Accessor<bool>(
                        () => profile.Grid_UseContainerHue,
                        b =>
                        {
                            profile.Grid_UseContainerHue = b;
                            GridContainer.UpdateAllGridContainers();
                        }
                    )
                )
            )
        );
    }
}
