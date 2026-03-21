using System.Collections.ObjectModel;
using ClassicUO.Game.UI.Controls.Resizer;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.ResizableControl;

public class ResizeProperties
{
    public bool Enabled { get; set; } = true;
    public ResizerProperties ResizerProps { get; set; } = new();
    public uint ResizeHandleRadiusPx { get; set; } = 30;
}

public class ResizablePanelProps : MyraCommonProps
{
    public ResizeProperties Resize { get; set; } = new();
}

public class ResizableControl : Container
{
    public ResizablePanelProps Props { get; }

    private MyraGrid _grid;
    private readonly Panel _contentPanel = new();

    public override ObservableCollection<Widget> Widgets => _contentPanel.Widgets;

    public ResizableControl(ResizablePanelProps props = null)
    {
        Props = props ?? new ResizablePanelProps();
        Build();
    }

    private void Build()
    {
        if (Props.Resize.Enabled)
            BuildResizable();
        else
            BuildNonResizable();

        InvalidateMeasure();
    }

    private void BuildResizable()
    {
        _grid = new MyraGrid
        {
            MinWidth = Props.MinWidth,
            MinHeight = Props.MinHeight,
            MaxWidth = Props.MaxWidth,
            MaxHeight = Props.MaxHeight
        };

        _grid.AddRow(new Proportion(ProportionType.Fill));
        _grid.AddRow(new Proportion(ProportionType.Auto));

        _grid.AddColumn(new Proportion(ProportionType.Fill));
        _grid.AddColumn(new Proportion(ProportionType.Auto));

        _grid.AddWidget(_contentPanel, 0, 0, null, 2);

        var resizerHandle = new ResizeHandle(new ResizeHandleProps
        {
            Glyph = Props.Resize.ResizerProps.GetHandleText(new ResizerAlignment()),
            Tooltip = Props.Resize.ResizerProps.Tooltip,
            FontSize = Props.Resize.ResizerProps.FontSize,
            MinWidth = Props.Resize.ResizerProps.MinWidth,
            MinHeight = Props.Resize.ResizerProps.MinHeight,
            MaxWidth = Props.Resize.ResizerProps.MaxWidth,
            MaxHeight = Props.Resize.ResizerProps.MaxHeight
        });

        resizerHandle.Resized += (_, args) =>
        {
            Width = args.NewWidth;
            Height = args.NewHeight;
        };

        _grid.AddWidget(resizerHandle, 1, 1);

        ChildrenLayout = new GridLayout();
        Children.Add(_grid);
    }

    private void BuildNonResizable()
    {
        _grid = new MyraGrid
        {
            MinWidth = Props.MinWidth,
            MinHeight = Props.MinHeight,
            MaxWidth = Props.MaxWidth,
            MaxHeight = Props.MaxHeight
        };

        _grid.AddRow(new Proportion(ProportionType.Fill));
        _grid.AddColumn(new Proportion(ProportionType.Fill));

        ChildrenLayout = new GridLayout();
        Children.Add(_grid);
    }
}
