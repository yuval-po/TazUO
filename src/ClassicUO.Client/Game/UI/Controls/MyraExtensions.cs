using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls;

public static class MyraExtensions
{
    // These are actually implemented by Myra but are private for some reason
    extension(ScrollViewer scrollViewer)
    {
        public int HorizontalScrollbarHeight()
        {
            int result = 0;
            if (scrollViewer.HorizontalScrollBackground != null)
                result = scrollViewer.HorizontalScrollBackground.Size.Y;

            if (scrollViewer.HorizontalScrollKnob != null && scrollViewer.HorizontalScrollKnob.Size.Y > result)
                result = scrollViewer.HorizontalScrollKnob.Size.Y;

            return result;
        }

        public int VerticalScrollbarWidth()
        {
            int result = 0;
            if (scrollViewer.VerticalScrollBackground != null)
                result = scrollViewer.VerticalScrollBackground.Size.X;

            if (scrollViewer.VerticalScrollKnob != null && scrollViewer.VerticalScrollKnob.Size.X > result)
                result = scrollViewer.VerticalScrollKnob.Size.X;

            return result;
        }
    }
}
