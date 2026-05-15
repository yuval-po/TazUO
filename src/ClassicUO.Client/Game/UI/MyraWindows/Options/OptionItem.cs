#nullable enable

using System;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal class OptionItem : ContentControl
{
    private readonly SingleItemLayout<Widget> _layout;
    private string? _tags;
    private readonly Func<Widget> _createWidget;
    private readonly string _searchText;
    private readonly bool _skipSearch;

    public override Widget Content
    {
        get => _layout.Child ?? CreateOrUpdateChild();
        set => _layout.Child = value;
    }

    public OptionItem(
        string searchText,
        Func<Widget> createWidget,
        string? tags = null,
        bool skipSearch = false
    )
    {
        _searchText = searchText;
        _createWidget = createWidget;
        _skipSearch = skipSearch;
        _tags = tags;
        _layout = new SingleItemLayout<Widget>(this);
        ChildrenLayout = _layout;
    }

    public bool MatchesSearch(string text)
    {
        if (_skipSearch)
            return false;

        if (_searchText.Contains(text, StringComparison.OrdinalIgnoreCase))
            return true;

        return _tags.NotNullNotEmpty() && _tags!.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    public OptionItem SetTags(string tags)
    {
        _tags = tags;
        return this;
    }

    private Widget CreateOrUpdateChild()
    {
        _layout.Child ??= _createWidget();
        _layout.Child.Enabled = Enabled; // Make sure enablement is propagated to the child.
        return _layout.Child;
    }

    protected override Point InternalMeasure(Point availableSize)
    {
        CreateOrUpdateChild();
        return base.InternalMeasure(availableSize);
    }

    public override void InternalRender(RenderContext context)
    {
        CreateOrUpdateChild();
        base.InternalRender(context);
    }
}
