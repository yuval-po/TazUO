using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Input;

public class MouseLeftButtonClickStateChangedEventArgs(bool previous, bool current) : EventArgs
{
    public readonly bool Previous = previous;
    public readonly bool Current = current;
}

public class MouseMovedEventArgs(Point previous, Point current) : EventArgs
{
    public readonly Point Previous = previous;
    public readonly Point Current = current;
}
