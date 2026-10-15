using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IDMClone
{
    public class ColorPalette
    {
        public string Name { get; set; }
        public bool IsDark { get; set; }
        public Color WindowBg { get; set; }
        public Color PanelBg { get; set; }
        public Color CardBg { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color Accent { get; set; }
        public Color AccentSecondary { get; set; }
        public Color ToolbarBg { get; set; }
        public Color Border { get; set; }
        public Color ListBg { get; set; }
        public Color ListText { get; set; }
        public Color ButtonBg { get; set; }
        public Color ButtonText { get; set; }
        public Color Highlight { get; set; }
        public Color HighlightText { get; set; }
    }

    public class ModernColorTable : ProfessionalColorTable
    {
        private ColorPalette _palette;

        public ModernColorTable(ColorPalette palette)
        {
            _palette = palette;
            base.UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground { get { return _palette.CardBg; } }
        public override Color ImageMarginGradientBegin { get { return _palette.PanelBg; } }
        public override Color ImageMarginGradientMiddle { get { return _palette.PanelBg; } }
        public override Color ImageMarginGradientEnd { get { return _palette.PanelBg; } }
        public override Color MenuBorder { get { return _palette.Border; } }
        public override Color MenuItemBorder { get { return _palette.Accent; } }
        public override Color MenuItemSelected { get { return _palette.Highlight; } }
        public override Color MenuItemSelectedGradientBegin { get { return _palette.Highlight; } }
        public override Color MenuItemSelectedGradientEnd { get { return _palette.Highlight; } }
        public override Color MenuItemPressedGradientBegin { get { return _palette.PanelBg; } }
        public override Color MenuItemPressedGradientEnd { get { return _palette.PanelBg; } }
        public override Color MenuStripGradientBegin { get { return _palette.ToolbarBg; } }
        public override Color MenuStripGradientEnd { get { return _palette.ToolbarBg; } }
        public override Color ToolStripGradientBegin { get { return _palette.ToolbarBg; } }
        public override Color ToolStripGradientMiddle { get { return _palette.ToolbarBg; } }
        public override Color ToolStripGradientEnd { get { return _palette.ToolbarBg; } }
        public override Color ToolStripBorder { get { return _palette.Border; } }
        public override Color StatusStripGradientBegin { get { return _palette.ToolbarBg; } }
        public override Color StatusStripGradientEnd { get { return _palette.ToolbarBg; } }
        public override Color SeparatorDark { get { return _palette.Border; } }
        public override Color SeparatorLight { get { return _palette.Border; } }
        public override Color CheckBackground { get { return _palette.Highlight; } }
        public override Color CheckSelectedBackground { get { return _palette.Highlight; } }
        public override Color CheckPressedBackground { get { return _palette.Highlight; } }
        public override Color ButtonSelectedHighlight { get { return _palette.Highlight; } }
        public override Color ButtonPressedHighlight { get { return _palette.PanelBg; } }
        public override Color ButtonCheckedHighlight { get { return _palette.Highlight; } }
        public override Color GripDark { get { return _palette.Border; } }
        public override Color GripLight { get { return _palette.PanelBg; } }
    }

    public class ModernThemeRenderer : ToolStripProfessionalRenderer
    {
        private ColorPalette _palette;

        public ModernThemeRenderer(ColorPalette palette) : base(new ModernColorTable(palette))
        {
            _palette = palette;
            this.RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                e.TextColor = _palette.HighlightText;
            }
            else
            {
                e.TextColor = _palette.TextPrimary;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = _palette.Accent;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                Rectangle rc = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
                using (SolidBrush b = new SolidBrush(_palette.Highlight))
                {
                    e.Graphics.FillRectangle(b, rc);
                }
                using (Pen p = new Pen(_palette.Accent))
                {
                    e.Graphics.DrawRectangle(p, rc);
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(_palette.Border))
            {
                e.Graphics.DrawLine(p, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // Clean high-contrast custom check rendering
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(e.ImageRectangle.X + 2, e.ImageRectangle.Y + 2, e.ImageRectangle.Width - 4, e.ImageRectangle.Height - 4);
            using (SolidBrush b = new SolidBrush(_palette.Accent))
            {
                g.FillEllipse(b, rect.X + 3, rect.Y + 3, 8, 8);
            }
        }
    }

    public static class ThemeManager
    {
        public static List<ColorPalette> AvailableThemes = new List<ColorPalette>();
        public static ColorPalette CurrentTheme { get; private set; }

        public static event EventHandler ThemeChanged;

        static ThemeManager()
        {
            InitializeThemes();
            // Default: Obsidian Onyx (Ultra-readable high contrast dark)
            CurrentTheme = AvailableThemes[0];
            ApplyRenderer();
        }

        private static void InitializeThemes()
        {
            // 1. Obsidian Onyx (High-contrast slate dark, crisp pure white text)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "🌌 Obsidian Onyx (Modern Dark)",
                IsDark = true,
                WindowBg = Color.FromArgb(15, 23, 42),       // Slate 900
                PanelBg = Color.FromArgb(30, 41, 59),        // Slate 800
                CardBg = Color.FromArgb(30, 41, 59),
                TextPrimary = Color.FromArgb(255, 255, 255), // Pure White
                TextSecondary = Color.FromArgb(148, 163, 184),
                Accent = Color.FromArgb(56, 189, 248),       // Sky Blue Glow
                AccentSecondary = Color.FromArgb(129, 140, 248),
                ToolbarBg = Color.FromArgb(20, 29, 47),
                Border = Color.FromArgb(51, 65, 85),
                ListBg = Color.FromArgb(15, 23, 42),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(30, 41, 59),
                ButtonText = Color.FromArgb(255, 255, 255),
                Highlight = Color.FromArgb(37, 99, 235),
                HighlightText = Color.White
            });

            // 2. Cyberpunk Neon (Pure jet black cards, electric neon cyan & yellow)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "⚡ Cyberpunk Neon (Vibrant Glow)",
                IsDark = true,
                WindowBg = Color.FromArgb(10, 10, 15),
                PanelBg = Color.FromArgb(18, 18, 28),
                CardBg = Color.FromArgb(25, 25, 40),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(255, 0, 128),
                Accent = Color.FromArgb(0, 245, 255),       // Electric Cyan
                AccentSecondary = Color.FromArgb(255, 230, 0),
                ToolbarBg = Color.FromArgb(15, 15, 24),
                Border = Color.FromArgb(0, 245, 255),
                ListBg = Color.FromArgb(12, 12, 20),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(35, 20, 50),
                ButtonText = Color.FromArgb(0, 245, 255),
                Highlight = Color.FromArgb(120, 20, 80),
                HighlightText = Color.FromArgb(0, 245, 255)
            });

            // 3. Sapphire Glass (Deep navy luxury, glowing aqua blue)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "💎 Sapphire Glass (Luxury Navy)",
                IsDark = true,
                WindowBg = Color.FromArgb(10, 25, 47),
                PanelBg = Color.FromArgb(17, 34, 64),
                CardBg = Color.FromArgb(23, 42, 77),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(148, 163, 184),
                Accent = Color.FromArgb(100, 255, 218),      // Glowing Aqua
                AccentSecondary = Color.FromArgb(56, 189, 248),
                ToolbarBg = Color.FromArgb(13, 29, 54),
                Border = Color.FromArgb(35, 53, 84),
                ListBg = Color.FromArgb(10, 25, 47),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(23, 42, 77),
                ButtonText = Color.FromArgb(100, 255, 218),
                Highlight = Color.FromArgb(30, 64, 120),
                HighlightText = Color.White
            });

            // 4. Aurora Emerald (Deep pine forest, radiant mint emerald)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "🍃 Aurora Emerald (Forest Mint)",
                IsDark = true,
                WindowBg = Color.FromArgb(6, 26, 20),
                PanelBg = Color.FromArgb(13, 43, 33),
                CardBg = Color.FromArgb(20, 58, 45),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(110, 231, 183),
                Accent = Color.FromArgb(52, 211, 153),      // Mint
                AccentSecondary = Color.FromArgb(16, 185, 129),
                ToolbarBg = Color.FromArgb(10, 35, 27),
                Border = Color.FromArgb(27, 77, 60),
                ListBg = Color.FromArgb(6, 26, 20),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(20, 58, 45),
                ButtonText = Color.FromArgb(167, 243, 208),
                Highlight = Color.FromArgb(10, 90, 65),
                HighlightText = Color.White
            });

            // 5. Amethyst Royal (Deep royal obsidian, vivid fuchsia & violet)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "🔮 Amethyst Royal (Mystic Violet)",
                IsDark = true,
                WindowBg = Color.FromArgb(20, 15, 38),
                PanelBg = Color.FromArgb(33, 24, 61),
                CardBg = Color.FromArgb(46, 34, 84),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(216, 180, 254),
                Accent = Color.FromArgb(192, 132, 252),      // Bright Amethyst
                AccentSecondary = Color.FromArgb(244, 114, 182),
                ToolbarBg = Color.FromArgb(26, 19, 49),
                Border = Color.FromArgb(60, 44, 107),
                ListBg = Color.FromArgb(20, 15, 38),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(46, 34, 84),
                ButtonText = Color.FromArgb(255, 255, 255),
                Highlight = Color.FromArgb(90, 35, 160),
                HighlightText = Color.White
            });

            // 6. Sunset Crimson (Espresso charcoal, warm glowing amber)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "🔥 Sunset Crimson (Warm Fire)",
                IsDark = true,
                WindowBg = Color.FromArgb(28, 16, 16),
                PanelBg = Color.FromArgb(45, 24, 24),
                CardBg = Color.FromArgb(64, 32, 32),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(253, 186, 116),
                Accent = Color.FromArgb(251, 146, 60),       // Warm Flame
                AccentSecondary = Color.FromArgb(248, 113, 113),
                ToolbarBg = Color.FromArgb(36, 19, 19),
                Border = Color.FromArgb(85, 38, 38),
                ListBg = Color.FromArgb(28, 16, 16),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(64, 32, 32),
                ButtonText = Color.FromArgb(254, 215, 170),
                Highlight = Color.FromArgb(140, 50, 20),
                HighlightText = Color.White
            });

            // 7. Titanium White (Apple-style clean minimalist light)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "☀️ Titanium White (Clean Modern Light)",
                IsDark = false,
                WindowBg = Color.FromArgb(248, 250, 252),    // Clean Off-White
                PanelBg = Color.FromArgb(255, 255, 255),     // Crisp White
                CardBg = Color.FromArgb(241, 245, 249),
                TextPrimary = Color.FromArgb(15, 23, 42),     // Slate Black 100% sharp
                TextSecondary = Color.FromArgb(71, 85, 105),
                Accent = Color.FromArgb(2, 132, 199),        // Electric Blue
                AccentSecondary = Color.FromArgb(16, 185, 129),
                ToolbarBg = Color.FromArgb(241, 245, 249),
                Border = Color.FromArgb(203, 213, 225),
                ListBg = Color.FromArgb(255, 255, 255),
                ListText = Color.FromArgb(15, 23, 42),
                ButtonBg = Color.FromArgb(226, 232, 240),
                ButtonText = Color.FromArgb(15, 23, 42),
                Highlight = Color.FromArgb(199, 210, 254),
                HighlightText = Color.FromArgb(15, 23, 42)
            });

            // 8. Pitch Black OLED (100% Pitch Black, Vivid Neon Green)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "🖤 Pitch Black OLED (Pure High Contrast)",
                IsDark = true,
                WindowBg = Color.FromArgb(0, 0, 0),
                PanelBg = Color.FromArgb(14, 14, 14),
                CardBg = Color.FromArgb(24, 24, 24),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(170, 170, 170),
                Accent = Color.FromArgb(0, 255, 102),       // Neon Lime
                AccentSecondary = Color.FromArgb(0, 204, 255),
                ToolbarBg = Color.FromArgb(10, 10, 10),
                Border = Color.FromArgb(45, 45, 45),
                ListBg = Color.FromArgb(0, 0, 0),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(26, 26, 26),
                ButtonText = Color.FromArgb(0, 255, 102),
                Highlight = Color.FromArgb(0, 80, 35),
                HighlightText = Color.White
            });

            // 9. macOS Silicon Slate (Pro Space Gray, Apple Blue)
            AvailableThemes.Add(new ColorPalette
            {
                Name = "🍎 macOS Silicon Slate (Pro Space Gray)",
                IsDark = true,
                WindowBg = Color.FromArgb(28, 30, 36),
                PanelBg = Color.FromArgb(38, 41, 49),
                CardBg = Color.FromArgb(48, 52, 63),
                TextPrimary = Color.FromArgb(255, 255, 255),
                TextSecondary = Color.FromArgb(160, 165, 180),
                Accent = Color.FromArgb(10, 132, 255),       // Apple Pro Blue
                AccentSecondary = Color.FromArgb(48, 209, 88),
                ToolbarBg = Color.FromArgb(32, 34, 41),
                Border = Color.FromArgb(60, 64, 76),
                ListBg = Color.FromArgb(24, 26, 31),
                ListText = Color.FromArgb(255, 255, 255),
                ButtonBg = Color.FromArgb(48, 52, 63),
                ButtonText = Color.FromArgb(255, 255, 255),
                Highlight = Color.FromArgb(20, 70, 135),
                HighlightText = Color.White
            });
        }

        public static void SetTheme(string themeName)
        {
            foreach (var t in AvailableThemes)
            {
                if (t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains(themeName))
                {
                    CurrentTheme = t;
                    ApplyRenderer();
                    if (ThemeChanged != null) ThemeChanged(null, EventArgs.Empty);
                    break;
                }
            }
        }

        private static void ApplyRenderer()
        {
            try
            {
                ToolStripManager.Renderer = new ModernThemeRenderer(CurrentTheme);
            }
            catch { }
        }

        public static void ApplyToControl(Control ctrl)
        {
            if (ctrl == null) return;
            var t = CurrentTheme;

            if (ctrl is Form)
            {
                ctrl.BackColor = t.WindowBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is MenuStrip || ctrl is ContextMenuStrip)
            {
                ctrl.BackColor = t.ToolbarBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is ToolStrip)
            {
                ctrl.BackColor = t.ToolbarBg;
                ctrl.ForeColor = t.TextPrimary;
                foreach (ToolStripItem item in ((ToolStrip)ctrl).Items)
                {
                    item.ForeColor = t.TextPrimary;
                }
            }
            else if (ctrl is StatusStrip)
            {
                ctrl.BackColor = t.ToolbarBg;
                ctrl.ForeColor = t.TextPrimary;
                foreach (ToolStripItem item in ((StatusStrip)ctrl).Items)
                {
                    item.ForeColor = t.TextPrimary;
                }
            }
            else if (ctrl is TreeView)
            {
                ctrl.BackColor = t.PanelBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is ListView)
            {
                ctrl.BackColor = t.ListBg;
                ctrl.ForeColor = t.ListText;
            }
            else if (ctrl is TabControl)
            {
                StyleTabControl((TabControl)ctrl);
            }
            else if (ctrl is TabPage)
            {
                TabPage tp = (TabPage)ctrl;
                tp.UseVisualStyleBackColor = false;
                tp.BackColor = t.PanelBg;
                tp.ForeColor = t.TextPrimary;
            }
            else if (ctrl is CheckBox || ctrl is RadioButton)
            {
                ctrl.BackColor = (ctrl.Parent != null) ? ctrl.Parent.BackColor : t.PanelBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is NumericUpDown || ctrl is DateTimePicker)
            {
                ctrl.BackColor = t.CardBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is GroupBox)
            {
                ctrl.ForeColor = t.Accent;
                ctrl.BackColor = (ctrl.Parent != null) ? ctrl.Parent.BackColor : t.PanelBg;
            }
            else if (ctrl is CheckedListBox || ctrl is ListBox)
            {
                ctrl.BackColor = t.CardBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is TextBox || ctrl is ComboBox)
            {
                ctrl.BackColor = t.CardBg;
                ctrl.ForeColor = t.TextPrimary;
            }
            else if (ctrl is Button)
            {
                Button btn = (Button)ctrl;
                if (btn.BackColor == t.Accent)
                {
                    btn.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
                }
                else
                {
                    btn.BackColor = t.ButtonBg;
                    btn.ForeColor = t.ButtonText;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = t.Border;
                }
            }
            else if (ctrl is Label)
            {
                ctrl.BackColor = (ctrl.Parent != null) ? ctrl.Parent.BackColor : Color.Transparent;
                if (ctrl.ForeColor != t.Accent && ctrl.ForeColor != t.AccentSecondary)
                {
                    ctrl.ForeColor = t.TextPrimary;
                }
            }

            foreach (Control child in ctrl.Controls)
            {
                ApplyToControl(child);
            }
        }

        public static void StyleTabControl(TabControl tc)
        {
            if (tc == null) return;
            var t = CurrentTheme;

            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
            tc.SizeMode = TabSizeMode.Normal;
            tc.Padding = new Point(14, 6);
            tc.ItemSize = new Size(0, 28);

            tc.DrawItem -= TabControl_DrawItem;
            tc.DrawItem += TabControl_DrawItem;

            tc.SelectedIndexChanged -= TabControl_SelectedIndexChanged;
            tc.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            foreach (TabPage page in tc.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = t.PanelBg;
                page.ForeColor = t.TextPrimary;
                page.Paint -= TabPage_Paint;
                page.Paint += TabPage_Paint;
            }
            tc.Invalidate();
        }

        private static void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc != null) tc.Invalidate();
        }

        private static void TabPage_Paint(object sender, PaintEventArgs e)
        {
            TabPage tp = sender as TabPage;
            if (tp != null)
            {
                using (SolidBrush b = new SolidBrush(CurrentTheme.PanelBg))
                {
                    e.Graphics.FillRectangle(b, tp.ClientRectangle);
                }
            }
        }

        private static void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc == null || e.Index >= tc.TabPages.Count) return;

            Graphics g = e.Graphics;
            TabPage page = tc.TabPages[e.Index];
            bool isSelected = (tc.SelectedIndex == e.Index);
            var t = CurrentTheme;
            Rectangle tabRect = tc.GetTabRect(e.Index);

            // Tab header background
            Color bgTab = isSelected 
                ? t.PanelBg 
                : (t.IsDark ? Color.FromArgb(24, 32, 47) : Color.FromArgb(235, 238, 244));

            using (SolidBrush b = new SolidBrush(bgTab))
            {
                g.FillRectangle(b, tabRect);
            }

            if (isSelected)
            {
                using (Pen p = new Pen(t.Accent, 3))
                {
                    g.DrawLine(p, tabRect.Left, tabRect.Top + 1, tabRect.Right, tabRect.Top + 1);
                }
            }
            else
            {
                using (Pen p = new Pen(t.Border, 1))
                {
                    g.DrawRectangle(p, tabRect.Left, tabRect.Top, tabRect.Width - 1, tabRect.Height - 1);
                }
            }

            // Tab label text
            Color textColor = isSelected
                ? t.Accent
                : (t.IsDark ? Color.FromArgb(215, 225, 240) : Color.FromArgb(60, 65, 75));

            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                using (Font tabFont = new Font("Segoe UI", 9F, isSelected ? FontStyle.Bold : FontStyle.Regular))
                {
                    g.DrawString(page.Text, tabFont, textBrush, tabRect, sf);
                }
            }
        }
    }
}
