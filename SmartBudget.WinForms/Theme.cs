using SmartBudget.WinForms.Controls.SideNav;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms;

public sealed class UiTheme
{
    public Color Primary = Color.White;

    public Color Secondary = Color.Black;
    // --- Surfaces / layout ---
    public Color AppBack { get; init; }            // main app background
    public Color PanelBack { get; init; }          // sidenav & general panels
    public Color PanelBorder { get; init; }        // panel separators / right border

    // Card-like surfaces (calendar cells, content panels, etc.)
    public Color CardBack { get; init; }
    public Color CardBackDim { get; init; }        // out-of-month day
    public Color CardBorder { get; init; }

    // --- Interactive states (used by both nav items and calendar rows) ---
    public Color HoverBack { get; init; }
    public Color SelectedBack { get; init; }

    // --- Text ---
    public Color TextPrimary { get; init; }
    public Color TextMuted { get; init; }
    public Color HeaderText { get; init; }

    // --- Accent / semantics ---
    public Color Accent { get; init; }             // selection stripe / today
    public Color Positive { get; init; }
    public Color Negative { get; init; }

    // --- Optional badge ---
    public Color BadgeBack { get; init; }
    public Color BadgeText { get; init; }

    // --- Fonts (optional but helpful) ---
    public Font ItemFont { get; init; } = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F);
    public Font HeaderFont { get; init; } = new Font(SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F), FontStyle.Bold);

    public Color Background { get; set; } = Color.FromArgb(22, 37, 53);
    public Color BorderRight { get; set; } = Color.FromArgb(35, 55, 75);
    public Color Border { get; set; } = Color.FromArgb(35, 55, 75);

    // Adapter from your current SideNavTheme
    public static UiTheme FromSideNavTheme(SideNavTheme t)
    {
        // CardBack: pick something that reads like a “tile” on top of Background
        // If your Background is dark, ItemHoverBack is a good “raised” fill.
        return new UiTheme
        {
            AppBack = t.Background,
            PanelBack = t.Background,
            PanelBorder = t.BorderRight,

            CardBack = t.ItemHoverBack,                  // slightly lighter tile
            CardBackDim = Blend(t.Background, Color.FromArgb(35, t.BorderRight)), // subtle dim
            CardBorder = t.BorderRight,

            HoverBack = t.ItemHoverBack,
            SelectedBack = t.ItemSelectedBack,

            TextPrimary = t.TextNormal,
            TextMuted = t.TextDisabled,
            HeaderText = t.HeaderText,

            Accent = t.Accent,
            Positive = Color.FromArgb(0, 140, 0),
            Negative = Color.FromArgb(200, 0, 0),

            BadgeBack = t.BadgeBack,
            BadgeText = t.BadgeText,

            ItemFont = t.ItemFont,
            HeaderFont = t.HeaderFont
        };

        static Color Blend(Color baseColor, Color overlay)
        {
            var a = overlay.A / 255f;

            int r = (int)(baseColor.R * (1 - a) + overlay.R * a);
            int g = (int)(baseColor.G * (1 - a) + overlay.G * a);
            int b = (int)(baseColor.B * (1 - a) + overlay.B * a);

            return Color.FromArgb(255, r, g, b);
        }
    }
}