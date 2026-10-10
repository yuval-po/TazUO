using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     A dialog that presents read-only content and is dismissed by right-clicking it. The window has
///     no close button and cannot be minimized or resized.
///     <para>
///         Takes ownership of the content widget: it is attached to this window's desktop and detached
///         when the modal is disposed, so the same widget cannot back two live modals at once. A
///         dismissed modal is disposed and cannot be shown again - callers that re-open on demand must
///         construct a new instance, checking <see cref="MyraControl.IsDisposed" /> on any cached one.
///     </para>
/// </summary>
public class InfoModal : MyraControl
{
    /// <param name="title">Window title bar text.</param>
    /// <param name="modalContent">Widget rendered as the dialog body.</param>
    public InfoModal(string title, Widget modalContent) : base(title)
    {
        ConfigureRootWindow();
        SetRootContent(GetContent(modalContent));
    }

    /// <summary>
    ///     Applies modal-specific window settings: centered title, fixed minimum width, hidden close
    ///     button, and disabled minimize/resize.
    /// </summary>
    private void ConfigureRootWindow()
    {
        _rootWindow.TitlePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _rootWindow.TitlePanel.VerticalAlignment = VerticalAlignment.Center;
        _rootWindow.TitlePanel.MinWidth = 300;
        _rootWindow.TitleLabelAlignment = HorizontalAlignment.Center;

        _rootWindow.CloseButton.Visible = false;
        _rootWindow.Props.Minimizable = false;
        _rootWindow.Props.Resize.Enabled = false;
    }

    /// <summary>
    ///     Assembles the full dialog content: the caller-supplied body widget stacked above the
    ///     confirm/cancel button row.
    /// </summary>
    private static VerticalStackPanel GetContent(Widget modalContent)
    {
        var content = new VerticalStackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Widgets.Add(modalContent);
        return content;
    }

    /// <summary>
    ///     Registers the modal with <see cref="UIManager" /> and centers it on screen.
    /// </summary>
    /// <remarks>
    ///     Visibility of a <see cref="MyraControl" /> is owned by <see cref="UIManager" />, which drives
    ///     its update and draw passes - Myra's own <c>Window.Show</c>/<c>ShowModal</c> must not be used
    ///     here, as the root window is already this control's desktop root and showing it again appends a
    ///     duplicate desktop entry. Call once per instance: a second call would duplicate the
    ///     <see cref="UIManager" /> registration instead.
    /// </remarks>
    public void Show(Point? placement)
    {
        if (placement != null)
            SetPosition(placement.Value.X, placement.Value.Y);
        else
            CenterInScreen();

        UIManager.Add(this);
    }
}
