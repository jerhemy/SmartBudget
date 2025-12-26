using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Controls.FileDrop;

public sealed partial class FileDropZone : UserControl
{
    private readonly CardPanel _card;
    private readonly DropPanel _drop;

    private readonly Label _lblTitle;
    private readonly Label _lblIcon;
    private readonly Label _lblDragDrop;
    private readonly Label _lblOr;
    private readonly LinkLabel _lnkBrowse;
    private readonly Label _lblSupports;

    private string _titleText = "Upload your file :";
    private string _dragDropText = "Drag & Drop";
    private string _browseText = "browse";
    private string _supportsText = "";

    private string[] _allowedExtensions = Array.Empty<string>();
    private readonly Dictionary<string, Action<string>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    private bool IsInDesigner =>
        LicenseManager.UsageMode == LicenseUsageMode.Designtime || (Site?.DesignMode ?? false);

    public FileDropZone()
    {
        DoubleBuffered = true;
        TabStop = true;
        AllowDrop = true;

        // Background behind the card (soft blue like your reference image)
        BackColor = Color.FromArgb(214, 230, 248);

        _card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(20),
            Padding = new Padding(18),
            CardBackColor = Color.White,
            ShadowColor = Color.FromArgb(30, 0, 0, 0),
            CornerRadius = 16,
            ShadowOffsetY = 6
        };

        _lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.Transparent
        };

        _drop = new DropPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0),
            Padding = new Padding(12),
            BackColor = Color.White,
            CornerRadius = 10,
            BorderColor = Color.FromArgb(215, 225, 238),
            BorderHoverColor = Color.FromArgb(160, 180, 210)
        };

        // Center stack inside drop panel
        var center = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent
        };
        center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 40));   // top spacer
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));  // icon
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // Drag & Drop
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));  // or + browse
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));  // supports
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 60));   // bottom spacer

        _lblIcon = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(60, 120, 220)
        };

        // Use a “document/import” feel, not image upload:
        // Try a glyph font; fall back to plain text if unavailable.
        SetIconGlyphSafe("Segoe Fluent Icons", "\uE8A5"); // document-ish on many systems
        // If that glyph doesn’t render on your system, you can set IconText to "⇩" or "📄" at runtime.

        _lblDragDrop = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 60, 60),
            BackColor = Color.Transparent
        };

        // Or + browse line
        var orBrowse = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.None
        };

        _lblOr = new Label
        {
            AutoSize = true,
            Text = "or ",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(120, 120, 120),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 3, 0, 0)
        };

        _lnkBrowse = new LinkLabel
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            LinkColor = Color.FromArgb(60, 120, 220),
            ActiveLinkColor = Color.FromArgb(40, 100, 200),
            VisitedLinkColor = Color.FromArgb(60, 120, 220),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 3, 0, 0)
        };
        _lnkBrowse.LinkClicked += (_, __) => BrowseForFile();

        orBrowse.Controls.Add(_lblOr);
        orBrowse.Controls.Add(_lnkBrowse);

        // Center the FlowLayoutPanel by putting it inside another panel
        var orBrowseHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        orBrowseHost.Controls.Add(orBrowse);
        orBrowse.SizeChanged += (_, __) =>
        {
            orBrowse.Left = (orBrowseHost.Width - orBrowse.Width) / 2;
            orBrowse.Top = (orBrowseHost.Height - orBrowse.Height) / 2;
        };
        orBrowseHost.SizeChanged += (_, __) =>
        {
            orBrowse.Left = (orBrowseHost.Width - orBrowse.Width) / 2;
            orBrowse.Top = (orBrowseHost.Height - orBrowse.Height) / 2;
        };

        _lblSupports = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(140, 140, 140),
            BackColor = Color.Transparent
        };

        center.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 0);
        center.Controls.Add(_lblIcon, 0, 1);
        center.Controls.Add(_lblDragDrop, 0, 2);
        center.Controls.Add(orBrowseHost, 0, 3);
        center.Controls.Add(_lblSupports, 0, 4);
        center.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 5);

        _drop.Controls.Add(center);

        _card.Controls.Add(_drop);
        _card.Controls.Add(_lblTitle);

        // Outer padding using a container panel
        var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(26, 18, 26, 18), BackColor = BackColor };
        outer.Controls.Add(_card);
        Controls.Add(outer);

        // Apply texts AFTER controls exist (designer-safe)
        ApplyAllText();

        // Drag/drop behavior (safe in designer too)
        DragEnter += (_, e) => SetDragEffect(e);
        DragOver += (_, e) => SetDragEffect(e);
        DragLeave += (_, __) => { _drop.IsHover = false; _drop.Invalidate(); };
        DragDrop += (_, e) =>
        {
            _drop.IsHover = false;
            _drop.Invalidate();
            HandleFiles(GetDroppedFiles(e));
        };

        // Click anywhere in the drop zone to browse (but not in designer)
        _drop.MouseUp += (_, __) => BrowseForFile();
        foreach (Control c in _drop.Controls)
            c.MouseUp += (_, __) => BrowseForFile();
        _lblIcon.MouseUp += (_, __) => BrowseForFile();
        _lblDragDrop.MouseUp += (_, __) => BrowseForFile();
        _lblSupports.MouseUp += (_, __) => BrowseForFile();

        // Keyboard
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                BrowseForFile();
            }
        };
    }

    // ---------------- Public API ----------------

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool AllowMultipleFiles { get; set; } = false;

    [Category("Behavior")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] AllowedExtensions
    {
        get => _allowedExtensions;
        set
        {
            _allowedExtensions = value ?? Array.Empty<string>();
            NormalizeAllowedExtensions();

            // If SupportsText wasn't explicitly set, auto-generate it
            if (string.IsNullOrWhiteSpace(_supportsText))
                _supportsText = BuildSupportsText(_allowedExtensions);

            ApplyAllText();
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TitleText
    {
        get => _titleText;
        set { _titleText = value ?? ""; ApplyAllText(); }
    }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DragDropText
    {
        get => _dragDropText;
        set { _dragDropText = value ?? ""; ApplyAllText(); }
    }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BrowseText
    {
        get => _browseText;
        set { _browseText = value ?? ""; ApplyAllText(); }
    }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SupportsText
    {
        get => _supportsText;
        set { _supportsText = value ?? ""; ApplyAllText(); }
    }

    /// <summary>Set a simple icon text if you don’t want glyph fonts. Example: "⇩" or "📄"</summary>
    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconText
    {
        get => _lblIcon.Text;
        set { _lblIcon.Text = value ?? ""; }
    }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color IconColor
    {
        get => _lblIcon.ForeColor;
        set { _lblIcon.ForeColor = value; _lblIcon.Invalidate(); }
    }

    public event EventHandler<FileDroppedEventArgs>? FileDropped;
    public event EventHandler<string>? FileRejected;

    public void RegisterHandler(string extension, Action<string> handler)
    {
        if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("Extension required.", nameof(extension));
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        _handlers[NormalizeExt(extension)] = handler;
    }

    // ---------------- Internals ----------------

    private void ApplyAllText()
    {
        // Always null-safe (even though these exist, this makes it resilient to future edits)
        if (_lblTitle is not null) _lblTitle.Text = _titleText;
        if (_lblDragDrop is not null) _lblDragDrop.Text = _dragDropText;
        if (_lnkBrowse is not null) _lnkBrowse.Text = _browseText;
        if (_lblSupports is not null) _lblSupports.Text = _supportsText;
    }

    private void SetIconGlyphSafe(string fontFamily, string glyph)
    {
        try
        {
            _lblIcon.Font = new Font(fontFamily, 34f, FontStyle.Regular);
            _lblIcon.Text = glyph;
        }
        catch
        {
            // Fallback that will never crash designer
            _lblIcon.Font = new Font("Segoe UI", 28f, FontStyle.Regular);
            _lblIcon.Text = "⇩"; // non-image-upload feel
        }
    }

    private void SetDragEffect(DragEventArgs e)
    {
        if (e.Data is not null && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Copy;
            _drop.IsHover = true;
            _drop.Invalidate();
        }
        else
        {
            e.Effect = DragDropEffects.None;
            _drop.IsHover = false;
            _drop.Invalidate();
        }
    }

    private static IReadOnlyList<string> GetDroppedFiles(DragEventArgs e)
    {
        var arr = e.Data?.GetData(DataFormats.FileDrop) as string[];
        return arr?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? Array.Empty<string>();
    }

    private void BrowseForFile()
    {
        if (IsInDesigner) return;

        using var dlg = new OpenFileDialog
        {
            Multiselect = AllowMultipleFiles,
            Title = "Choose a file"
        };

        if (_allowedExtensions.Length > 0)
        {
            var patterns = string.Join(";", _allowedExtensions.Select(x => "*" + x));
            dlg.Filter = $"Supported Files ({patterns})|{patterns}|All Files (*.*)|*.*";
        }
        else
        {
            dlg.Filter = "All Files (*.*)|*.*";
        }

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        HandleFiles(dlg.FileNames ?? Array.Empty<string>());
    }

    private void HandleFiles(IReadOnlyList<string> files)
    {
        if (files.Count == 0) return;

        if (!AllowMultipleFiles && files.Count > 1)
        {
            FileRejected?.Invoke(this, "Only one file is allowed.");
            return;
        }

        var valid = new List<string>(files.Count);
        foreach (var f in files)
        {
            if (!File.Exists(f))
            {
                FileRejected?.Invoke(this, $"File not found: {f}");
                continue;
            }

            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (!IsAllowed(ext))
            {
                FileRejected?.Invoke(this, $"File type not allowed: {ext}");
                continue;
            }

            valid.Add(f);
        }

        if (valid.Count == 0) return;

        var args = new FileDroppedEventArgs(valid);
        FileDropped?.Invoke(this, args);

        if (!string.IsNullOrWhiteSpace(args.Extension) && _handlers.TryGetValue(args.Extension, out var handler))
        {
            try { handler(args.FilePath); }
            catch (Exception ex) { FileRejected?.Invoke(this, $"Handler failed: {ex.Message}"); }
        }
    }

    private bool IsAllowed(string ext)
    {
        if (_allowedExtensions.Length == 0) return true;
        ext = NormalizeExt(ext);
        return _allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private void NormalizeAllowedExtensions()
    {
        for (int i = 0; i < _allowedExtensions.Length; i++)
            _allowedExtensions[i] = NormalizeExt(_allowedExtensions[i]);
    }

    private static string NormalizeExt(string ext)
    {
        ext = (ext ?? "").Trim();
        if (ext.Length == 0) return "";
        if (!ext.StartsWith(".")) ext = "." + ext;
        return ext.ToLowerInvariant();
    }

    private static string BuildSupportsText(string[] exts)
    {
        if (exts is null || exts.Length == 0) return "";
        var nice = exts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().TrimStart('.').ToUpperInvariant());
        return "Supports: " + string.Join(", ", nice);
    }

    // ---------------- Nested paint panels ----------------

    private sealed class CardPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CardBackColor { get; set; } = Color.White;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ShadowColor { get; set; } = Color.FromArgb(30, 0, 0, 0);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 16;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ShadowOffsetY { get; set; } = 6;

        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var card = ClientRectangle;
            card.Inflate(-2, -2);

            var shadow = card;
            shadow.Offset(0, ShadowOffsetY);

            using (var sb = new SolidBrush(ShadowColor))
                FillRounded(e.Graphics, sb, shadow, CornerRadius);

            using (var cb = new SolidBrush(CardBackColor))
                FillRounded(e.Graphics, cb, card, CornerRadius);
        }
    }

    private sealed class DropPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsHover { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = Color.FromArgb(215, 225, 238);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderHoverColor { get; set; } = Color.FromArgb(160, 180, 210);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        public DropPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var r = ClientRectangle;
            r.Inflate(-1, -1);

            // background already via BackColor, but if transparent parents ever happen:
            using (var bg = new SolidBrush(BackColor))
                FillRounded(e.Graphics, bg, r, CornerRadius);

            var border = IsHover ? BorderHoverColor : BorderColor;
            using var pen = new Pen(border, 1f) { DashStyle = DashStyle.Dash };
            DrawRounded(e.Graphics, pen, r, CornerRadius);
        }
    }

    private static void FillRounded(Graphics g, Brush b, Rectangle r, int radius)
    {
        using var path = RoundedPath(r, radius);
        g.FillPath(b, path);
    }

    private static void DrawRounded(Graphics g, Pen p, Rectangle r, int radius)
    {
        using var path = RoundedPath(r, radius);
        g.DrawPath(p, path);
    }

    private static GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
