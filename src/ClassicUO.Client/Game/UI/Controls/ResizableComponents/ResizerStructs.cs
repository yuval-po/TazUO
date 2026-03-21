using System;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

[Flags]
public enum ResizeEdges
{
    None = 0,
    Left = 1,
    Top = 1 << 2,
    Right = 1 << 3,
    Bottom = 1 << 4,
    All = Left | Top | Right | Bottom
}

public class ResizeEventArgs : EventArgs
{
    public int NewWidth { get; set; }
    public int NewHeight { get; set; }
}

public class ResizerProperties : MyraCommonProps
{
    public ResizeEdges Placements { get; set; } = ResizeEdges.All;
    public uint ResizeHandleRadiusPx { get; set; } = 25;
}

public class ResizeBehavior : ResizerProperties
{
    public bool Enabled { get; set; } = true;
}
