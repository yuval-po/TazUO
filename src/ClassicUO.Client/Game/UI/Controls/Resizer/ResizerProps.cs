using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.Resizer;

public record struct ResizerAlignment(HorizontalAlignment Horizontal, VerticalAlignment Vertical);

public class ResizerProperties
{
    public Dictionary<ResizerAlignment, string> HandleTexts { get; set; } = new();

    public string Tooltip { get; set; } = Language.Instance.UiCommons.DragToResize;
    public ResizerAlignment[] Placements { get; set; } = [new(HorizontalAlignment.Right, VerticalAlignment.Bottom)];
    public int FontSize { get; set; } = 30;

    public int MinWidth { get; set; } = StyleConstantsDefaults.WINDOW_MIN_WIDTH;
    public int MinHeight { get; set; } = StyleConstantsDefaults.WINDOW_MIN_HEIGHT;

    public int MaxWidth { get; set; } = StyleConstantsDefaults.WINDOW_MAX_WIDTH;
    public int MaxHeight { get; set; } = StyleConstantsDefaults.WINDOW_MAX_HEIGHT;

    public string GetHandleText(ResizerAlignment alignment)
    {
        if (HandleTexts.TryGetValue(alignment, out string text))
            return text;

        return alignment switch
        {
            { Horizontal: HorizontalAlignment.Right, Vertical: VerticalAlignment.Top } => StyleConstantsDefaults.TOP_RIGHT_HANDLE_TEXT,
            { Horizontal: HorizontalAlignment.Right, Vertical: VerticalAlignment.Bottom } => StyleConstantsDefaults.BOTTOM_RIGHT_HANDLE_TEXT,
            { Horizontal: HorizontalAlignment.Left, Vertical: VerticalAlignment.Bottom } => StyleConstantsDefaults.BOTTOM_LEFT_HANDLE_TEXT,
            { Horizontal: HorizontalAlignment.Left, Vertical: VerticalAlignment.Top } => StyleConstantsDefaults.TOP_LEFT_HANDLE_TEXT,
            _ => "<>"
        };
    }
}
