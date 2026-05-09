using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public record struct VisualContainerProps()
{
    public Orientation Orientation { get; init; } = Orientation.Vertical;
    public string LabelText { get; init; } = null;
}

public class VisualContainer : Container
{
    public VisualContainer(VisualContainerProps props, params Widget[] widgets)
    {
        if (!string.IsNullOrWhiteSpace(props.LabelText))
        {
            Children.Add(new MyraLabel(props.LabelText, MyraLabel.TextStyle.P));
            Children.Add(new MyraSpacer(0, 2));
        }

        if (widgets?.Length > 0)
            Add(widgets);

        Padding = new Thickness(4, 6, 4, 14);
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

    public override void InternalRender(RenderContext context)
    {
        int a = 0;
        base.InternalRender(context);
    }

    protected override Point InternalMeasure(Point availableSize)
    {
        int a = 0;
        return base.InternalMeasure(availableSize);
    }

    protected override void InternalArrange()
    {
        int a = 0;
        base.InternalArrange();
    }
}
