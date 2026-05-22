using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class ConfirmationModal : MyraControl
{
    private readonly Action<bool> _onClose;
    private readonly string _confirmButtonLabel;
    private readonly string _cancelButtonLabel;

    public ConfirmationModal(
        string title,
        string question,
        Action<bool> onClose,
        string confirmButtonLabel = null,
        string cancelButtonLabel = null
    ) : this(
        title,
        GetContentByText(question),
        onClose,
        confirmButtonLabel,
        cancelButtonLabel
    )
    {
    }

    public ConfirmationModal(
        string title,
        Widget modalContent,
        Action<bool> onClose,
        string confirmButtonLabel = null,
        string cancelButtonLabel = null
    ) : base(title ?? "Confirm Action")
    {
        ArgumentNullException.ThrowIfNull(onClose);
        _onClose = onClose;
        _confirmButtonLabel = confirmButtonLabel;
        _cancelButtonLabel = cancelButtonLabel;

        ConfigureRootWindow();

        var content = new VerticalStackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Widgets.Add(modalContent);
        content.Widgets.Add(GetButtonGrid());

        _rootWindow.Content = content;
    }

    private void ConfigureRootWindow()
    {
        _rootWindow.TitlePanel.HorizontalAlignment = HorizontalAlignment.Center;
        _rootWindow.TitlePanel.VerticalAlignment = VerticalAlignment.Center;
        _rootWindow.TitlePanel.MinWidth = 300;
        _rootWindow.TitleLabelAlignment = HorizontalAlignment.Center;

        _rootWindow.CloseButton.Visible = false;
        _rootWindow.Props.Minimizable = false;
        _rootWindow.Props.Resize.Enabled = false;
    }

    private Grid GetButtonGrid()
    {
        var buttonGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };

        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var closeButton = new MyraButton(_cancelButtonLabel ?? "Cancel", () =>
        {
            _rootWindow.Close();
            _onClose(false);
        });

        buttonGrid.Widgets.Add(closeButton);
        Grid.SetColumn(closeButton, 0);

        var spacer = new MyraSpacer(1, 4);
        buttonGrid.Widgets.Add(spacer);
        Grid.SetColumn(spacer, 1);

        var confirmButton = new MyraButton(_confirmButtonLabel ?? "Confirm", () =>
        {
            _rootWindow.Close();
            _onClose(true);
        })
        {
            BorderThickness = new Thickness(0, 0, 0, 3),
            Border = new SolidBrush(new Color(185, 20, 60, 120)),
            Background = new SolidBrush(new Color(220, 20, 60, 150)),
            OverBackground = new SolidBrush(new Color(240, 20, 60, 50))
        };

        buttonGrid.Widgets.Add(confirmButton);
        Grid.SetColumn(confirmButton, 2);

        return buttonGrid;
    }

    private static MyraLabel GetContentByText(string question) =>
        new(question ?? "Are you sure you wish to continue?", MyraLabel.TextStyle.H2)
        {
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(2, 4, 2, 4)
        };
}
