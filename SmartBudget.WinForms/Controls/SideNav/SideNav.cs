using SmartBudget.WinForms.Controls.SideNav;
using System.ComponentModel;

namespace SmartBudget.WinForms.Controls.SideNav;

public sealed partial class SideNav : UserControl
{
    private readonly FlowLayoutPanel _stack;
    private readonly Panel _rightBorder;

    private readonly Dictionary<string, SideNavItemView> _itemViews = new();

    private UiTheme _theme = new();

    public event EventHandler<SideNavSelectionChangedEventArgs>? SelectedChanged;
    public event EventHandler<SideNavItemInvokedEventArgs>? ItemInvoked;
    public string? SelectedId { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public UiTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value ?? new UiTheme();
            ApplyTheme(_theme);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ItemHeight { get; set; } = 40;

    public SideNav()
    {
        DoubleBuffered = true;
        BackColor = _theme.Background;
        Width = 210;
        Dock = DockStyle.Left;

        _rightBorder = new Panel
        {
            Dock = DockStyle.Right,
            Width = 1,
            BackColor = _theme.BorderRight
        };

        _stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 8, 0, 8),
            BackColor = _theme.Background
        };

        //_stack.BackColor = _theme.Background;     // <-- NOT Transparent

        Controls.Add(_stack);
        Controls.Add(_rightBorder);
    }

    public void SetEntries(IEnumerable<SideNavEntry> entries, string? selectId = null)
    {
        SuspendLayout();
        _stack.SuspendLayout();

        _stack.Controls.Clear();
        _itemViews.Clear();

        foreach (var entry in entries)
        {
            if (entry is SideNavHeader header)
            {
                _stack.Controls.Add(BuildHeader(header.Text));
                continue;
            }

            if (entry is SideNavItem item)
            {
                var row = new SideNavItemView
                {
                    Width = _stack.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2,
                    Height = ItemHeight,
                    Margin = new Padding(0),
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };

                row.Bind(item, _theme);
                row.ItemClicked += (_, __) => {
                    // Always raise invoked
                    ItemInvoked?.Invoke(this, new SideNavItemInvokedEventArgs(item));

                    // Only keep “selected highlight” for the things you want to behave like nav items
                    // (or make this configurable per item)
                    //if (item.ActionKind is SideNavActionKind.Navigate or SideNavActionKind.SelectAccount)
                        Select(item.Id, raiseEvent: true);


                };

                _stack.Controls.Add(row);
                _itemViews[item.Id] = row;
            }
        }

        _stack.ResumeLayout();
        ResumeLayout();

        ApplyTheme(_theme);
        if (selectId is not null) Select(selectId, raiseEvent: false);
        else if (_itemViews.Count > 0) Select(_itemViews.Keys.First(), raiseEvent: false);
    }

    public void Select(string id, bool raiseEvent = true)
    {
        if (SelectedId == id) return;
        if (!_itemViews.TryGetValue(id, out var newlySelected)) return;

        if (SelectedId is not null && _itemViews.TryGetValue(SelectedId, out var prev))
            prev.SetSelected(false);

        SelectedId = id;
        newlySelected.SetSelected(true);

        if (raiseEvent)
            SelectedChanged?.Invoke(this, new SideNavSelectionChangedEventArgs(id));
    }

    public void SetBadge(string id, int? badge)
    {
        if (_itemViews.TryGetValue(id, out var view))
            view.SetBadge(badge);
    }

    private Control BuildHeader(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Height = 28,
            Width = _stack.ClientSize.Width - 2,
            Padding = new Padding(12, 8, 0, 0),
            ForeColor = _theme.HeaderText,
            Font = _theme.HeaderFont,
            Margin = new Padding(0, 10, 0, 2),
            BackColor = _theme.Background,     // <-- NOT Transparent
        };
    }

    public void ApplyTheme(UiTheme _theme)
    {
        BackColor = _theme.Background;
        _stack.BackColor = _theme.Background;
        _rightBorder.BackColor = _theme.BorderRight;

        foreach (Control c in _stack.Controls)
        {
            if (c is SideNavItemView item)
                item.Bind(item.Item, _theme); // rebind to refresh colors/fonts
            else if (c is Label header)
            {
                header.ForeColor = _theme.HeaderText;
                header.Font = _theme.HeaderFont;
            }
        }

        Invalidate();
    }
}
