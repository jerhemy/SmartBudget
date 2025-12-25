using SmartBudget.Domain;
using System.ComponentModel;
using System.Globalization;

namespace SmartBudget.WinForms.Controls.Calendar;

public sealed partial class CalendarDayCell : UserControl
{
    private sealed record LineHit(Rectangle Rect, CalendarTransactionLine Line);

    private CalendarTransactionLine[] _lines = Array.Empty<CalendarTransactionLine>();

    private Cursor? _dragCursor;
    private Cursor? _originalCursor;

    // Line hit-test rectangles for current paint
    private readonly List<Rectangle> _lineRects = new();

    private int _selectedLineIndex = -1;
    private int _hoverLineIndex = -1;
    private int _mouseDownLineIndex = -1;
    private Point _mouseDownPoint;
    private bool _isDragging;
    private bool _startedDrag;
    public DateOnly Date { get; private set; }
    private bool _inDisplayedMonth;

    public bool InDisplayedMonth => _inDisplayedMonth;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsToday { get; set; }


    private double _startingAmount = 0;

    // NEW: running account balance for this day (in dollars, not cents)
    private decimal _accountBalanceForDay;

    public event EventHandler<CalendarLineDroppedEventArgs>? LineDropped;
    public event EventHandler<CalendarTransactionClickedEventArgs>? TransactionClicked;
    public event EventHandler<CalendarDayClickedEventArgs>? DayClicked;

    public CalendarDayCell()
    {
        InitializeComponent();

        // Important for drop target behavior
        AllowDrop = true;

        // Helps reduce flicker when you redraw frequently
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        MouseDown += OnCellMouseDown;
        MouseMove += OnCellMouseMove;
        MouseUp += OnCellMouseUp;

        DragEnter += OnCellDragEnter;
        DragOver += OnCellDragOver;
        DragDrop += OnCellDragDrop;

        HookChildInput(this);
    }

    private UiTheme? _theme;

    public void ApplyTheme(UiTheme theme)
    {
        _theme = theme;
        BackColor = theme.PanelBack; // the area behind the card
        ForeColor = theme.TextPrimary;
        Invalidate();
    }

    #region Set Data

    public void SetDate(DateOnly date, bool inDisplayedMonth, bool isToday)
    {
        Date = date;
        _inDisplayedMonth = inDisplayedMonth;
        IsToday = isToday;
        Invalidate();
    }

    #endregion

    // NEW: called by CalendarView after it computes the running balance for this day
    public void SetAccountBalanceForDay(decimal balanceForDay)
    {
        _accountBalanceForDay = balanceForDay;
        Invalidate();
    }

    private void CalendarDayCell_GiveFeedback(object? sender, GiveFeedbackEventArgs e)
    {
        if (_dragCursor is null) return;

        e.UseDefaultCursors = false;
        Cursor.Current = _dragCursor;
    }


    public bool TryRemoveLineAt(int index, out CalendarTransactionLine? removed)
    {
        removed = null;
        if (index < 0 || index >= _lines.Length) return false;

        removed = _lines[index];
        _lines = _lines.Where((_, i) => i != index).ToArray();
        Invalidate();
        return true;
    }

    public void AddLine(CalendarTransactionLine line, int? insertIndex = null)
    {
        if (insertIndex is null || insertIndex < 0 || insertIndex > _lines.Length)
        {
            _lines = _lines.Append(line).ToArray();
        }
        else
        {
            var list = _lines.ToList();
            list.Insert(insertIndex.Value, line);
            _lines = list.ToArray();
        }

        Invalidate();
    }

    #region Mouse Events

    private void OnCellMouseDown(object? sender, MouseEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"MouseDown cell={Date} at {e.Location}");
        if (e.Button != MouseButtons.Left) return;

        _mouseDownPoint = e.Location;
        _mouseDownLineIndex = HitTestLineIndex(e.Location);

        _isDragging = false;
        _startedDrag = false;
    }

    private void OnCellMouseMove(object? sender, MouseEventArgs e)
    {
        // Hover behavior (no buttons pressed)
        if (e.Button == MouseButtons.None)
        {
            var hit = HitTestLineIndex(e.Location);
            if (hit != _hoverLineIndex)
            {
                _hoverLineIndex = hit;
                Cursor = _hoverLineIndex >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate(); // so hover highlight can repaint
            }
            return;
        }

        // Drag behavior
        if (e.Button != MouseButtons.Left) return;
        if (_mouseDownLineIndex < 0) return;
        if (_isDragging) return;

        // Only start dragging after small movement threshold (prevents accidental drags)
        var dragSize = SystemInformation.DragSize;
        var dragRect = new Rectangle(
            _mouseDownPoint.X - dragSize.Width / 2,
            _mouseDownPoint.Y - dragSize.Height / 2,
            dragSize.Width,
            dragSize.Height);

        if (dragRect.Contains(e.Location)) return;

        _isDragging = true;
        _startedDrag = true;

        var line = _lines[_mouseDownLineIndex];

        var payload = new CalendarDragData(
            TransactionId: line.TransactionId,
            SourceDate: Date,
            SourceIndex: _mouseDownLineIndex);

        var dataObj = new DataObject();
        dataObj.SetData(typeof(CalendarDragData), payload);
        dataObj.SetData(typeof(CalendarTransactionLine), line);

        // ✅ show “ghost” cursor
        _originalCursor = Cursor.Current;
        var dragText = $"{line.Title}  {line.Amount:+0.##;-0.##;0}";
        _dragCursor?.Dispose();
        _dragCursor = CreateDragCursor(dragText, Font);

        GiveFeedback += CalendarDayCell_GiveFeedback;

        DoDragDrop(dataObj, DragDropEffects.Move);

        // ✅ cleanup
        GiveFeedback -= CalendarDayCell_GiveFeedback;

        _dragCursor?.Dispose();
        _dragCursor = null;
    }

    private void OnCellMouseUp(object? sender, MouseEventArgs e)
    {
        try
        {
            if (e.Button == MouseButtons.Left && !_startedDrag)
            {
                var hitIndex = HitTestLineIndex(e.Location);

                if (hitIndex >= 0 && hitIndex < _lines.Length)
                {
                    // single-click selects the line
                    SetSelectedLineIndex(hitIndex);

                    var line = _lines[hitIndex];

                    // raise event so the host can populate an edit form
                    //TransactionClicked?.Invoke(
                    //    this,
                    //    new CalendarTransactionClickedEventArgs(
                    //        date: Date,
                    //        lineIndex: hitIndex,
                    //        clickCount: e.Clicks,
                    //        line: line));

                    TransactionClicked?.Invoke(
                        this,
                        new CalendarTransactionClickedEventArgs(
                            date: DateOnly.FromDateTime(Date.ToDateTime(TimeOnly.MinValue)),
                            transactionId: line.TransactionId,
                            title: line.Title ?? string.Empty,
                            memo: line.Memo ?? string.Empty,
                            amount: line.Amount));
                    //MessageBox.Show($"Transaction clicked: {line.Title} ({(long)(line.Amount * 100)} cents)");
                }
                else
                {
                    // clicked on empty space in the day cell
                    ClearSelection();

                    DayClicked?.Invoke(this, new CalendarDayClickedEventArgs(Date, e.Clicks));
                }
            }
        }
        finally
        {
            _mouseDownLineIndex = -1;
            _isDragging = false;
            _startedDrag = false;
        }
    }

    private void OnCellDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = CanAcceptDrag(e) ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void OnCellDragOver(object? sender, DragEventArgs e)
    {
        e.Effect = CanAcceptDrag(e) ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void OnCellDragDrop(object? sender, DragEventArgs e)
    {
        if (!CanAcceptDrag(e)) return;

        var payload = (CalendarDragData)e.Data!.GetData(typeof(CalendarDragData))!;
        var line = (CalendarTransactionLine)e.Data!.GetData(typeof(CalendarTransactionLine))!;

        // Let CalendarView coordinate removal from source + add to target + persistence.
        LineDropped?.Invoke(this, new CalendarLineDroppedEventArgs(payload, line, targetDate: Date));
    }

    #endregion

    private static bool CanAcceptDrag(DragEventArgs e)
        => e.Data?.GetDataPresent(typeof(CalendarDragData)) == true
           && e.Data.GetDataPresent(typeof(CalendarTransactionLine)) == true;

    private int HitTestLineIndex(Point p)
    {
        // _lineRects is rebuilt during paint; if you hit before first paint, no hit
        for (var i = 0; i < _lineRects.Count; i++)
        {
            if (_lineRects[i].Contains(p)) return i;
        }
        return -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        _lineRects.Clear();
        if (_theme is null) return;

        // Layout constants (tune to your UI)
        const int headerHeight = 18;
        const int footerHeight = 18;
        const int lineHeight = 18;
        const int padding = 4;

        var theme = _theme;

        // Footer rect (inside the card area; we’ll compute later)
        // var footerRect = ...

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        // --- Background behind the card (matches SideNav container) ---
        using (var bg = new SolidBrush(theme.PanelBack))
            e.Graphics.FillRectangle(bg, ClientRectangle);

        // --- Card rect (like a SideNav item) ---
        // Give a little inset so borders don’t double-up visually in the grid.
        const int cardInset = 3;
        var cardRect = new Rectangle(
            x: cardInset,
            y: cardInset,
            width: Width - cardInset * 2,
            height: Height - cardInset * 2);

        // Card fill: normal, dimmed if not in displayed month
        var cardFill = InDisplayedMonth ? theme.CardBack : theme.CardBackDim;

        // Subtle “today” wash (like active highlight in SideNav)
        if (IsToday)
            cardFill = Blend(cardFill, Color.FromArgb(40, theme.Accent)); // alpha drives subtlety

        using (var cardBrush = new SolidBrush(cardFill))
            e.Graphics.FillRectangle(cardBrush, cardRect);

        // Text colors
        var textColor = InDisplayedMonth ? theme.TextPrimary : theme.TextMuted;

        // --- Header: day number ---
        using (var headerBrush = new SolidBrush(textColor))
        {
            var dayText = Date.Day.ToString(CultureInfo.InvariantCulture);
            var dayRect = new Rectangle(cardRect.X + padding, cardRect.Y + 1, cardRect.Width - padding * 2, headerHeight - 2);
            e.Graphics.DrawString(dayText, Font, headerBrush, dayRect);
        }

        // Lines area
        var contentTop = cardRect.Y + headerHeight + padding;
        var contentBottom = cardRect.Bottom - footerHeight - padding;
        var y = contentTop;

        for (var i = 0; i < _lines.Length; i++)
        {
            if (y + lineHeight > contentBottom) break;

            var line = _lines[i];

            var rowRect = new Rectangle(cardRect.X + padding, y, cardRect.Width - padding * 2, lineHeight);

            // Selection / hover background (match SideNav hover/selected)
            if (i == _selectedLineIndex)
            {
                using var sel = new SolidBrush(theme.SelectedBack);
                e.Graphics.FillRectangle(sel, rowRect);
            }
            else if (i == _hoverLineIndex)
            {
                using var hov = new SolidBrush(theme.HoverBack);
                e.Graphics.FillRectangle(hov, rowRect);
            }

            _lineRects.Add(rowRect);

            // Amount text + reserve space on the right so title can ellipsis correctly
            var amount = line.Amount / 100;
            var amountText = amount.ToString("C", CultureInfo.CurrentCulture);

            var amountSize = TextRenderer.MeasureText(
                e.Graphics,
                amountText,
                Font,
                new Size(int.MaxValue, lineHeight),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            const int gap = 20; // spacing between title and amount
            var amountWidth = amountSize.Width + 5;

            var amountRect = new Rectangle(
                x: rowRect.Right - amountWidth,
                y: rowRect.Y,
                width: amountWidth,
                height: rowRect.Height);

            var titleRect = new Rectangle(
                x: rowRect.X,
                y: rowRect.Y,
                width: Math.Max(0, rowRect.Width - amountWidth - gap),
                height: rowRect.Height);

            // Amount color by sign (from theme)
            var amtColor =
                line.Amount < 0 ? theme.Negative :
                line.Amount > 0 ? theme.Positive :
                textColor;

            // Title
            TextRenderer.DrawText(
                e.Graphics,
                line.Title ?? string.Empty,
                Font,
                titleRect,
                textColor,
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter);

            // Amount (right aligned)
            TextRenderer.DrawText(
                e.Graphics,
                amountText,
                Font,
                amountRect,
                amtColor,
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.Right |
                TextFormatFlags.VerticalCenter);

            y += lineHeight;
        }

        // Footer: running account balance for the day (right aligned)
        var footerRect = new Rectangle(cardRect.X, cardRect.Bottom - footerHeight, cardRect.Width, footerHeight);

        var balText = _startingAmount.ToString("C", CultureInfo.CurrentCulture);

        var balColor =
            _startingAmount < 0 ? theme.Negative :
            _startingAmount > 0 ? theme.Positive :
            textColor;

        TextRenderer.DrawText(
            e.Graphics,
            balText,
            Font,
            footerRect,
            balColor,
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.Right |
            TextFormatFlags.VerticalCenter);

        // Card border (today = accent border, otherwise normal border)
        var borderColor = IsToday ? theme.Accent : theme.Border;
        var borderWidth = IsToday ? 2f : 1f;

        using (var pen = new Pen(borderColor, borderWidth) { Alignment = System.Drawing.Drawing2D.PenAlignment.Inset })
            e.Graphics.DrawRectangle(pen, cardRect);

        static Color Blend(Color baseColor, Color overlay)
        {
            var a = overlay.A / 255f;

            int r = (int)(baseColor.R * (1 - a) + overlay.R * a);
            int g = (int)(baseColor.G * (1 - a) + overlay.G * a);
            int b = (int)(baseColor.B * (1 - a) + overlay.B * a);

            return Color.FromArgb(255, r, g, b);
        }
    }

    private void HookChildInput(Control root)
    {
        foreach (Control c in root.Controls)
        {
            // Make child act like the cell for drag/drop purposes
            c.AllowDrop = true;

            // Forward mouse events so dragging can START even if user clicks a child control
            c.MouseDown += ForwardMouseDown;
            c.MouseMove += ForwardMouseMove;
            c.MouseUp += ForwardMouseUp;

            // Forward drag events so dropping WORKS even if the mouse is over a child control
            c.DragEnter += ForwardDragEnter;
            c.DragOver += ForwardDragOver;
            c.DragDrop += ForwardDragDrop;

            if (c.HasChildren)
                HookChildInput(c);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static Cursor CreateDragCursor(string text, Font font)
    {
        using var tmp = new Bitmap(1, 1);
        using var g0 = Graphics.FromImage(tmp);
        var size = g0.MeasureString(text, font);

        var width = Math.Min(360, (int)Math.Ceiling(size.Width) + 20);
        var height = Math.Max(26, (int)Math.Ceiling(size.Height) + 10);

        var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        g.Clear(Color.Transparent);

        var rect = new Rectangle(0, 0, width - 1, height - 1);
        using var bg = new SolidBrush(Color.FromArgb(210, 30, 30, 30));
        using var border = new Pen(Color.FromArgb(230, 255, 255, 255));
        g.FillRectangle(bg, rect);
        g.DrawRectangle(border, rect);

        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(text, font, textBrush, new RectangleF(10, 4, width - 20, height - 8));

        // IMPORTANT: do NOT DestroyIcon(hIcon) here; Cursor uses the handle.
        var hIcon = bmp.GetHicon();

        // Cursor will own the handle until disposed. We'll dispose cursor after drag.
        return new Cursor(hIcon);
    }

    #region Forward Mouse Events

    private void ForwardMouseDown(object? sender, MouseEventArgs e)
    {
        // Translate child coords to cell coords
        var child = (Control)sender!;
        var p = PointToClient(child.PointToScreen(e.Location));

        OnCellMouseDown(this, new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta));
    }

    private void ForwardMouseMove(object? sender, MouseEventArgs e)
    {
        var child = (Control)sender!;
        var p = PointToClient(child.PointToScreen(e.Location));

        OnCellMouseMove(this, new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta));
    }

    private void ForwardMouseUp(object? sender, MouseEventArgs e)
    {
        var child = (Control)sender!;
        var p = PointToClient(child.PointToScreen(e.Location));

        OnCellMouseUp(this, new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta));
    }

    private void ForwardDragEnter(object? sender, DragEventArgs e) => OnCellDragEnter(this, e);
    private void ForwardDragOver(object? sender, DragEventArgs e) => OnCellDragOver(this, e);
    private void ForwardDragDrop(object? sender, DragEventArgs e) => OnCellDragDrop(this, e);

    #endregion

    private static void DrawEllipsizedText(Graphics g, string text, Font font, Color color, Rectangle rect, TextFormatFlags extraFlags = TextFormatFlags.Left)
    {
        var flags =
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.VerticalCenter |
            extraFlags;

        TextRenderer.DrawText(g, text ?? string.Empty, font, rect, color, flags);
    }

    // Expose selection (optional, but handy)
    private readonly List<LineHit> _lineHits = new();
    private long? _selectedTransactionId;



    public int SelectedLineIndex => _selectedLineIndex;

    public void ClearSelection()
    {
        if (_selectedLineIndex == -1) return;
        _selectedLineIndex = -1;
        Invalidate();
    }

    public void SetSelectedLineIndex(int index)
    {
        if (_selectedLineIndex == index) return;
        _selectedLineIndex = index;
        Invalidate();
    }

    public void SetSelectedTransaction(long? transactionId)
    {
        if (_selectedTransactionId == transactionId) return;
        _selectedTransactionId = transactionId;
        Invalidate();
    }

    // Call this wherever you set _lines (or inside your existing Bind method)

    public void SetLines(CalendarTransactionLine[] lines, double startingAmount = 0)
    {
        _lines = lines ?? Array.Empty<CalendarTransactionLine>();
        _startingAmount = startingAmount;
        RebuildLineRects();
        Invalidate();
    }

    private void RebuildLineRects()
    {
        _lineRects.Clear();

        const int headerHeight = 18;
        const int footerHeight = 18;
        const int lineHeight = 18;
        const int padding = 4;

        var contentTop = headerHeight + padding;
        var contentBottom = Height - footerHeight - padding;

        var y = contentTop;
        for (var i = 0; i < _lines.Length; i++)
        {
            if (y + lineHeight > contentBottom) break;

            _lineRects.Add(new Rectangle(padding, y, Math.Max(0, Width - padding * 2), lineHeight));
            y += lineHeight;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var hit = HitTestLine(e.Location);
        Cursor = hit is null ? Cursors.Default : Cursors.Hand;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
            return;

        var hit = HitTestLine(e.Location);
        if (hit is not null)
        {
            SetSelectedTransaction(hit.Line.TransactionId);

            double amountCents = (double)(hit.Line.Amount/100);
            TransactionClicked?.Invoke(
                this,
                new CalendarTransactionClickedEventArgs(
                    date: DateOnly.FromDateTime(Date.ToDateTime(TimeOnly.MinValue)),
                    transactionId: hit.Line.TransactionId,
                    title: hit.Line.Title ?? string.Empty,
                    memo: hit.Line.Memo ?? string.Empty,
                    amount: amountCents));
            //MessageBox.Show($"Transaction clicked: {hit.Line.TransactionId} {hit.Line.Title} ({amountCents})");

        }
        else
        {
            // clicking empty space in the day cell = "day click"
            ClearSelection();

            //DayClicked?.Invoke(
            //    this,
            //    new CalendarDayClickedEventArgs(
            //        DateOnly.FromDateTime(Date.ToDateTime(TimeOnly.MinValue))));
            //MessageBox.Show($"Day clicked: {Date}");
        }
    }

    private LineHit? HitTestLine(Point p)
    {
        var idx = HitTestLineIndexFallback(p);
        if (idx < 0 || idx >= _lines.Length) return null;

        // Build a rect consistent with layout (for selection highlight etc.)
        var rect = GetLineRect(idx);
        return new LineHit(rect, _lines[idx]);
    }

    private int HitTestLineIndexFallback(Point p)
    {
        const int headerHeight = 18;
        const int footerHeight = 18;
        const int lineHeight = 18;
        const int padding = 4;

        var contentTop = headerHeight + padding;
        var contentBottom = Height - footerHeight - padding;

        if (p.Y < contentTop || p.Y >= contentBottom) return -1;
        if (p.X < padding || p.X >= Width - padding) return -1;

        var index = (p.Y - contentTop) / lineHeight;

        var maxVisible = Math.Min(
            _lines.Length,
            Math.Max(0, (contentBottom - contentTop) / lineHeight));

        if (index < 0 || index >= maxVisible) return -1;

        return index;
    }

    private Rectangle GetLineRect(int index)
    {
        const int headerHeight = 18;
        const int footerHeight = 18;
        const int lineHeight = 18;
        const int padding = 4;

        var y = (headerHeight + padding) + (index * lineHeight);
        return new Rectangle(padding, y, Math.Max(0, Width - padding * 2), lineHeight);
    }
}

public sealed class CalendarLineDroppedEventArgs : EventArgs
{
    public CalendarLineDroppedEventArgs(CalendarDragData dragData, CalendarTransactionLine line, DateOnly targetDate)
    {
        DragData = dragData;
        Line = line;
        TargetDate = targetDate;
    }

    public CalendarDragData DragData { get; }
    public CalendarTransactionLine Line { get; }
    public DateOnly TargetDate { get; }
}

public sealed class CalendarTransactionClickedEventArgs : EventArgs
{
    public CalendarTransactionClickedEventArgs(DateOnly date, long transactionId, string title, string memo, double amount)
    {
        Date = date;
        TransactionId = transactionId;
        Title = title;
        Memo = memo;
        Amount = amount;
    }

    public DateOnly Date { get; }
    public long TransactionId { get; }
    public double Amount { get; }
    public string Title { get; }
    public string Memo { get; }
}

public sealed class CalendarDayClickedEventArgs : EventArgs
{
    public CalendarDayClickedEventArgs(DateOnly date, int clickCount)
    {
        Date = date;
        ClickCount = clickCount;
    }

    public DateOnly Date { get; }
    public int ClickCount { get; }
}