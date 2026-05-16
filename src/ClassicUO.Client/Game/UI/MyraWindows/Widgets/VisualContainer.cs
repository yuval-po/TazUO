using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public record struct VisualContainerProps()
{
    public Orientation Orientation { get; init; } = Orientation.Vertical;
    public string LabelText { get; init; } = null;
    public string LabelLink { get; init; } = null;
}

public class VisualContainer : Container
{
    public VisualContainer(VisualContainerProps props, params Widget[] widgets)
    {
        if (!string.IsNullOrWhiteSpace(props.LabelText))
        {
            if (string.IsNullOrWhiteSpace(props.LabelLink))
                Children.Add(new MyraLabel(props.LabelText, MyraLabel.TextStyle.P));
            else
                Children.Add(new LinkLabel(props.LabelText, props.LabelLink, MyraLabel.TextStyle.P));
            Children.Add(new MyraSpacer(0, 2));
        }

        if (widgets?.Length > 0)
            Add(widgets);

        Margin = new Thickness(4);
        Padding = new Thickness(4, 6, 4, 12);
        Background = new SolidBrush(new Color(0, 0, 0, 25));
        Border = new SolidBrush(new Color(0, 0, 0, 75));
        BorderThickness = new Thickness(2);
        ChildrenLayout = new StackPanelLayout(props.Orientation) { Spacing = MyraStyle.STANDARD_SPACING };
    }

    public void Add(params Widget[] widgets)
    {
        foreach (Widget widget in widgets)
            Children.Add(widget);
    }
}
