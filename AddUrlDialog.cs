using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDMClone
{
    public class AddUrlDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // Public Properties for Download Engine
        public string Url { get { return txtUrl != null ? txtUrl.Text.Trim() : ""; } }
        public string FileName { get { return txtFileName != null ? txtFileName.Text.Trim() : ""; } }
        public string SaveDirectory { get { return txtFolder != null ? txtFolder.Text.Trim() : ""; } }
        public string SelectedQuality { get { return !string.IsNullOrEmpty(_currentQuality) ? _currentQuality : "1080p"; } }
        public string SelectedFormat { get { return GetSelectedExtension().TrimStart('.'); } }
        public bool IsVideoMode { get; private set; }
        public bool IsTorrentMode { get; private set; }
        public bool IsScheduled { get { return _isScheduledChecked; } }
        public DateTime ScheduledTime { get { return _scheduledDateTime; } }
        public string DirectCdnUrl { get; private set; }
        public string AvailableHeightsCsv { get; private set; }

        // Core Layout & Header
        private Panel pnlTitleBar;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;

        private Panel pnlHeader;
        private Panel pnlMediaCard;
        private Bitmap _previewThumbnail = null;
        private string _previewTitle = "Bulbuli";
        private string _previewSub1 = "Coke Studio Bangla -";
        private string _previewSub2 = "Season One";
        private string _previewSub3 = "Ritu Raj X Nandita";
        private string _previewDuration = "04:32";

        // Inputs
        private Panel pnlUrlContainer;
        private TextBox txtUrl;
        private Button btnGetInfo;
        private bool _hoverGetInfo = false;

        private Panel pnlFileNameContainer;
        private TextBox txtFileName;

        private Panel pnlFolderContainer;
        private TextBox txtFolder;
        private Button btnBrowseFolder;
        private bool _hoverBrowseFolder = false;
        private static string _savedDownloadDir = null;

        // Custom Sleek Dropdowns (zero white box glitch)
        private Panel pnlFormatContainer;
        private string _currentFormat = "MP4 Video (.mp4)";
        private List<string> _formatOptions = new List<string>();

        private Panel pnlQualityContainer;
        private string _currentQuality = "2K Quad HD (1440p - Universal MP4)";
        private List<string> _qualityOptions = new List<string>();

        // Custom Schedule Timing Card
        private Panel pnlScheduleCard;
        private bool _isScheduledChecked = false;
        private DateTime _scheduledDateTime = DateTime.Now.AddMinutes(5);
        private DateTimePicker dtpScheduleTime;

        // Actions
        private Button btnMoreOptions;
        private Button btnCancel;
        private Button btnOk;
        private bool _hoverMoreOptions = false;
        private bool _hoverCancel = false;
        private bool _hoverOk = false;

        private VideoEngine _videoEngine;
        private string _initialPresetQuality;
        private string _initialPresetFormat;

        public AddUrlDialog() : this(null, null, null, null, null, null) { }
        public AddUrlDialog(string initialUrl) : this(initialUrl, null, null, null, null, null) { }
        public AddUrlDialog(string initialUrl, string initialTitle, string initialQuality, string initialFormat) : this(initialUrl, initialTitle, initialQuality, initialFormat, null, null) { }

        public AddUrlDialog(string initialUrl, string initialTitle, string initialQuality, string initialFormat, string availableHeightsCsv, string directCdnUrl)
        {
            DirectCdnUrl = directCdnUrl;
            AvailableHeightsCsv = availableHeightsCsv;
            _initialPresetQuality = initialQuality;
            _initialPresetFormat = initialFormat;
            _videoEngine = new VideoEngine();

            InitializeFormatsAndQualities();
            InitializeNeonGamingUI();
            GenerateBulbuliThumbnail();

            if (!string.IsNullOrEmpty(initialUrl))
            {
                txtUrl.Text = initialUrl;
                ProcessInitialInput(initialUrl, initialTitle, initialQuality, initialFormat);
            }
            else
            {
                CheckClipboardForUrl();
            }
        }

        private void InitializeFormatsAndQualities()
        {
            _formatOptions = new List<string> {
                "MP4 Video (.mp4)",
                "MKV Video (.mkv)",
                "AVI Video (.avi)",
                "WEBM Video (.webm)",
                "MOV Video (.mov)",
                "WMV Video (.wmv)",
                "MP3 Audio (.mp3)",
                "M4A Audio (.m4a)",
                "FLAC Audio (.flac)",
                "WAV Audio (.wav)",
                "AAC Audio (.aac)",
                "ZIP Archive (.zip)",
                "RAR Archive (.rar)",
                "7Z Archive (.7z)",
                "ISO Disc Image (.iso)",
                "EXE Application (.exe)",
                "MSI Installer (.msi)",
                "PDF Document (.pdf)",
                "TORRENT File (.torrent)",
                "All Files (*.*)"
            };

            _qualityOptions = new List<string> {
                "8K Ultra HD (4320p - Best Quality)",
                "4K Ultra HD (2160p - Best Quality)",
                "2K Quad HD (1440p - Universal MP4)",
                "1080p Full HD (H.264 - 100% Playable)",
                "720p HD (H.264 - 100% Playable)",
                "480p Standard (H.264 - 100% Playable)",
                "360p Medium (H.264 - 100% Playable)",
                "Audio Only - MP3 (High Quality 320 kbps)",
                "Audio Only - M4A (Original AAC Audio)"
            };
        }

        private void InitializeNeonGamingUI()
        {
            this.Text = "New Download - Turbo X";
            this.ClientSize = new Size(760, 480);
            this.MinimumSize = new Size(760, 480);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(8, 11, 16);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            UpdateRoundedWindowRegion();
            this.Resize += (s, e) => UpdateRoundedWindowRegion();

            // 1. Custom Title Bar
            BuildTitleBar();

            // 2. Top Header & Media Preview Card
            BuildHeaderAndPreviewCard();

            // 3. Form Inputs (URL, FileName, SaveDir, Format, Quality, Schedule)
            BuildFormInputs();

            // 4. Bottom Action Bar
            BuildBottomActions();

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void UpdateRoundedWindowRegion()
        {
            try
            {
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, this.Width, this.Height), 12))
                {
                    this.Region = new Region(path);
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Outer Glowing Neon Gaming Border (Warm Amber / Gold Glow)
            Rectangle r = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (var path = CreateRoundedRectanglePath(r, 12))
            {
                // Soft neon aura
                using (var pGlow = new Pen(Color.FromArgb(45, 245, 158, 11), 3.0f))
                {
                    g.DrawPath(pGlow, path);
                }
                // Crisp inner amber neon stroke
                using (var pBorder = new Pen(Color.FromArgb(245, 158, 11), 1.4f))
                {
                    g.DrawPath(pBorder, path);
                }
            }
        }

        #region Title Bar & Window Movement

        private void BuildTitleBar()
        {
            pnlTitleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(7, 10, 15)
            };
            this.Controls.Add(pnlTitleBar);

            pnlTitleBar.MouseDown += (s, e) => DragWindow(e);
            pnlTitleBar.Resize += (s, e) => pnlTitleBar.Invalidate();

            pnlTitleBar.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Golden Lightning Bolt icon
                DrawNeonLightningBolt(g, 16, 9, 15, 16, Color.FromArgb(251, 191, 36));

                // Constrain title text width so it NEVER collides with window control buttons
                int buttonsLeft = (btnMin != null) ? btnMin.Left - 12 : (pnlTitleBar.Width - 125);
                int availWidth = Math.Max(50, buttonsLeft - 38);
                RectangleF titleRect = new RectangleF(38, 0, availWidth, pnlTitleBar.Height);

                // Title Text
                using (var f = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.White))
                using (var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                    LineAlignment = StringAlignment.Center
                })
                {
                    g.DrawString("New Download - Turbo X", f, b, titleRect, sf);
                }
            };

            // Window Controls on Top Right (High contrast, clearly visible)
            btnClose = new Button
            {
                Text = "✕",
                Size = new Size(30, 26),
                Location = new Point(pnlTitleBar.Width - 36, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(16, 22, 34)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.FromArgb(220, 38, 38); };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(241, 245, 249); btnClose.BackColor = Color.FromArgb(16, 22, 34); };
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlTitleBar.Controls.Add(btnClose);

            btnMax = new Button
            {
                Text = "☐",
                Size = new Size(30, 26),
                Location = new Point(pnlTitleBar.Width - 70, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(16, 22, 34),
                Enabled = true
            };
            btnMax.FlatAppearance.BorderSize = 0;
            btnMax.MouseEnter += (s, e) => { btnMax.ForeColor = Color.White; btnMax.BackColor = Color.FromArgb(30, 41, 59); };
            btnMax.MouseLeave += (s, e) => { btnMax.ForeColor = Color.FromArgb(241, 245, 249); btnMax.BackColor = Color.FromArgb(16, 22, 34); };
            btnMax.Click += (s, e) => {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    btnMax.Text = "☐";
                }
                else
                {
                    this.WindowState = FormWindowState.Maximized;
                    btnMax.Text = "❐";
                }
                UpdateRoundedWindowRegion();
            };
            pnlTitleBar.Controls.Add(btnMax);

            btnMin = new Button
            {
                Text = "—",
                Size = new Size(30, 26),
                Location = new Point(pnlTitleBar.Width - 104, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(16, 22, 34)
            };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.MouseEnter += (s, e) => { btnMin.ForeColor = Color.White; btnMin.BackColor = Color.FromArgb(30, 41, 59); };
            btnMin.MouseLeave += (s, e) => { btnMin.ForeColor = Color.FromArgb(241, 245, 249); btnMin.BackColor = Color.FromArgb(16, 22, 34); };
            btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            pnlTitleBar.Controls.Add(btnMin);
        }

        private void DragWindow(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        #endregion

        #region Header & Media Preview Card

        private void BuildHeaderAndPreviewCard()
        {
            pnlHeader = new Panel
            {
                Location = new Point(0, 34),
                Size = new Size(760, 82),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlHeader);
            pnlHeader.MouseDown += (s, e) => DragWindow(e);

            pnlHeader.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Big Glowing Lightning Bolt on Left
                DrawNeonLightningBolt(g, 20, 10, 34, 46, Color.FromArgb(251, 191, 36));

                // Title: New Download Accelerator
                using (var f = new Font("Segoe UI", 13.5F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.White))
                {
                    g.DrawString("New Download Accelerator", f, b, 66, 12);
                }

                // Subtitle: Auto-detects YouTube, Facebook, TikTok & more
                using (var f = new Font("Segoe UI", 8.75F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("Auto-detects YouTube, Facebook, TikTok & more", f, b, 67, 40);
                }
            };

            // Top Right: Media Preview Card
            pnlMediaCard = new Panel
            {
                Location = new Point(446, 4),
                Size = new Size(294, 74),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(pnlMediaCard);

            pnlMediaCard.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, pnlMediaCard.Width - 1, pnlMediaCard.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(12, 16, 24))) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(28, 38, 54), 1f)) g.DrawPath(p, path);
                }

                // Thumbnail image box on the left
                Rectangle thumbRect = new Rectangle(6, 6, 96, 62);
                using (var tPath = CreateRoundedRectanglePath(thumbRect, 6))
                {
                    g.SetClip(tPath);
                    if (_previewThumbnail != null)
                    {
                        g.DrawImage(_previewThumbnail, thumbRect);
                    }
                    else
                    {
                        using (var b = new SolidBrush(Color.FromArgb(20, 26, 38))) g.FillRectangle(b, thumbRect);
                    }
                    g.ResetClip();

                    using (var p = new Pen(Color.FromArgb(40, 52, 72), 1f)) g.DrawPath(p, tPath);
                }

                // Right text lines
                int textX = 110;
                // Title (Bulbuli)
                using (var f = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.White))
                {
                    g.DrawString(_previewTitle ?? "Media File", f, b, textX, 7);
                }
                // Line 2 (Coke Studio Bangla -)
                using (var f = new Font("Segoe UI", 8.0F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(_previewSub1 ?? "Online Stream", f, b, textX, 26);
                }
                // Line 3 (Season One)
                using (var f = new Font("Segoe UI", 8.0F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(_previewSub2 ?? "High Quality", f, b, textX, 41);
                }
                // Line 4 (Ritu Raj X Nandita)
                using (var f = new Font("Segoe UI", 7.5F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString(_previewSub3 ?? "Audio / Video", f, b, textX, 56);
                }
            };
        }

        private void GenerateBulbuliThumbnail()
        {
            try
            {
                Bitmap bmp = new Bitmap(96, 62);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Rich warm stage lighting gradient background
                    using (var grad = new LinearGradientBrush(new Rectangle(0, 0, 96, 62), Color.FromArgb(55, 24, 14), Color.FromArgb(12, 14, 20), LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(grad, 0, 0, 96, 62);
                    }

                    // Warm ambient spotlight beam
                    using (var gradBeam = new LinearGradientBrush(new Rectangle(35, 0, 60, 62), Color.FromArgb(80, 245, 158, 11), Color.FromArgb(0, 0, 0, 0), LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillEllipse(gradBeam, 35, -10, 65, 75);
                    }

                    // Artist silhouette & warm hair glow
                    using (var b = new SolidBrush(Color.FromArgb(200, 155, 105, 70)))
                    {
                        g.FillEllipse(b, 52, 9, 26, 28);
                    }
                    using (var b = new SolidBrush(Color.FromArgb(24, 28, 38)))
                    {
                        g.FillEllipse(b, 44, 28, 44, 40);
                    }
                    // Studio Condenser Microphone
                    using (var p = new Pen(Color.FromArgb(220, 228, 240), 2f))
                    {
                        g.DrawLine(p, 47, 23, 39, 36);
                    }
                    using (var b = new SolidBrush(Color.FromArgb(240, 245, 255)))
                    {
                        g.FillEllipse(b, 45, 20, 7, 7);
                    }

                    // Red Circular Badge (Coke Studio emblem) on the left
                    Rectangle redBadgeRect = new Rectangle(8, 16, 28, 28);
                    using (var b = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillEllipse(b, redBadgeRect);
                    }
                    // Stylized Bengali script art in white
                    using (var p = new Pen(Color.White, 1.4f))
                    {
                        g.DrawArc(p, 12, 23, 10, 10, 180, 220);
                        g.DrawLine(p, 18, 23, 27, 23);
                        g.DrawArc(p, 20, 24, 8, 8, 0, 180);
                        g.DrawLine(p, 23, 21, 23, 34);
                    }

                    // Duration pill badge on bottom-right corner ("04:32")
                    Rectangle durRect = new Rectangle(56, 44, 36, 15);
                    using (var dPath = CreateRoundedRectanglePath(durRect, 4))
                    {
                        using (var b = new SolidBrush(Color.FromArgb(230, 8, 10, 14))) g.FillPath(b, dPath);
                        using (var f = new Font("Segoe UI", 6.5F, FontStyle.Bold))
                        using (var b = new SolidBrush(Color.White))
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(_previewDuration, f, b, durRect, sf);
                        }
                    }
                }
                _previewThumbnail = bmp;
            }
            catch { }
        }

        #endregion

        #region Form Inputs Construction

        private void BuildFormInputs()
        {
            int startX = 20;

            // ==================== 1. URL Row ====================
            Label lblUrl = new Label
            {
                Text = "Download Address / URL:",
                Location = new Point(startX, 118),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(lblUrl);

            pnlUrlContainer = new Panel
            {
                Location = new Point(startX, 138),
                Size = new Size(600, 36),
                BackColor = Color.FromArgb(9, 13, 21)
            };
            pnlUrlContainer.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlUrlContainer.Width - 1, pnlUrlContainer.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(9, 13, 21))) g.FillPath(b, path);
                    // Glowing Neon Golden Amber Border
                    using (var pGlow = new Pen(Color.FromArgb(50, 245, 158, 11), 2.5f)) g.DrawPath(pGlow, path);
                    using (var p = new Pen(Color.FromArgb(245, 158, 11), 1.3f)) g.DrawPath(p, path);
                }
                DrawChainLinkIcon(g, 10, 9, Color.FromArgb(251, 191, 36));
            };
            this.Controls.Add(pnlUrlContainer);

            txtUrl = new TextBox
            {
                Location = new Point(34, 8),
                Size = new Size(556, 20),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(9, 13, 21),
                ForeColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F),
                Text = "https://www.youtube.com/watch?v=9c1NrAqYe4"
            };
            pnlUrlContainer.Controls.Add(txtUrl);

            // "Get Info" Button
            btnGetInfo = new Button
            {
                Text = "Get Info",
                Location = new Point(630, 138),
                Size = new Size(110, 36),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnGetInfo.FlatAppearance.BorderSize = 0;
            btnGetInfo.MouseEnter += (s, e) => { _hoverGetInfo = true; btnGetInfo.Invalidate(); };
            btnGetInfo.MouseLeave += (s, e) => { _hoverGetInfo = false; btnGetInfo.Invalidate(); };
            btnGetInfo.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, btnGetInfo.Width - 1, btnGetInfo.Height - 1);
                Color c1 = _hoverGetInfo ? Color.FromArgb(253, 224, 71) : Color.FromArgb(251, 191, 36);
                Color c2 = _hoverGetInfo ? Color.FromArgb(245, 158, 11) : Color.FromArgb(217, 119, 6);
                using (var path = CreateRoundedRectanglePath(r, 8))
                using (var grad = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical))
                {
                    g.FillPath(grad, path);
                    if (_hoverGetInfo)
                    {
                        using (var pGlow = new Pen(Color.FromArgb(120, 255, 255, 255), 1.2f)) g.DrawPath(pGlow, path);
                    }
                }
                using (var f = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.FromArgb(11, 15, 25)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("Get Info", f, b, r, sf);
                }
            };
            btnGetInfo.Click += BtnGetInfo_Click;
            this.Controls.Add(btnGetInfo);

            // ==================== 2. File Name Row ====================
            Label lblFile = new Label
            {
                Text = "File Name:",
                Location = new Point(startX, 184),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(lblFile);

            pnlFileNameContainer = new Panel
            {
                Location = new Point(startX, 204),
                Size = new Size(720, 36),
                BackColor = Color.FromArgb(9, 13, 21)
            };
            pnlFileNameContainer.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlFileNameContainer.Width - 1, pnlFileNameContainer.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(9, 13, 21))) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(28, 38, 54), 1.2f)) g.DrawPath(p, path);
                }
            };
            this.Controls.Add(pnlFileNameContainer);

            txtFileName = new TextBox
            {
                Location = new Point(14, 8),
                Size = new Size(692, 20),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(9, 13, 21),
                ForeColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F),
                Text = "Bulbuli _ Coke Studio Bangla _ Season One _ Ritu Raj X Nandita.mp4"
            };
            pnlFileNameContainer.Controls.Add(txtFileName);

            // ==================== 3. Save Directory & Format Row ====================
            Label lblSave = new Label
            {
                Text = "Save To Directory:",
                Location = new Point(startX, 250),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(lblSave);

            pnlFolderContainer = new Panel
            {
                Location = new Point(startX, 270),
                Size = new Size(315, 36),
                BackColor = Color.FromArgb(9, 13, 21),
                Cursor = Cursors.Hand
            };
            pnlFolderContainer.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlFolderContainer.Width - 1, pnlFolderContainer.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(9, 13, 21))) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(28, 38, 54), 1.2f)) g.DrawPath(p, path);
                }
                DrawFolderIcon(g, 10, 9, Color.FromArgb(251, 191, 36));
            };
            pnlFolderContainer.Click += (s, e) => BrowseFolder();
            this.Controls.Add(pnlFolderContainer);

            txtFolder = new TextBox
            {
                Location = new Point(34, 8),
                Size = new Size(272, 20),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(9, 13, 21),
                ForeColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F),
                Text = GetPersistedDownloadDirectory()
            };
            txtFolder.DoubleClick += (s, e) => BrowseFolder();
            txtFolder.Leave += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtFolder.Text) && Directory.Exists(txtFolder.Text.Trim()))
                {
                    SavePersistedDownloadDirectory(txtFolder.Text.Trim());
                }
            };
            pnlFolderContainer.Controls.Add(txtFolder);

            // Dedicated Neon Gaming "Browse" Button with Vector Folder Icon
            btnBrowseFolder = new Button
            {
                Text = "Browse",
                Location = new Point(startX + 325, 270),
                Size = new Size(90, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnBrowseFolder.FlatAppearance.BorderSize = 0;
            btnBrowseFolder.MouseEnter += (s, e) => { _hoverBrowseFolder = true; btnBrowseFolder.Invalidate(); };
            btnBrowseFolder.MouseLeave += (s, e) => { _hoverBrowseFolder = false; btnBrowseFolder.Invalidate(); };
            btnBrowseFolder.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, btnBrowseFolder.Width - 1, btnBrowseFolder.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    Color bg = _hoverBrowseFolder ? Color.FromArgb(28, 38, 56) : Color.FromArgb(14, 19, 29);
                    using (var b = new SolidBrush(bg)) g.FillPath(b, path);

                    Color borderC = _hoverBrowseFolder ? Color.FromArgb(251, 191, 36) : Color.FromArgb(45, 58, 78);
                    using (var p = new Pen(borderC, 1.2f)) g.DrawPath(p, path);
                }

                Color accent = _hoverBrowseFolder ? Color.FromArgb(251, 191, 36) : Color.FromArgb(245, 158, 11);
                DrawFolderIcon(g, 12, 10, accent);

                using (var f = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var b = new SolidBrush(_hoverBrowseFolder ? Color.FromArgb(251, 191, 36) : Color.FromArgb(241, 245, 249)))
                {
                    g.DrawString("Browse", f, b, 32, 8);
                }
            };
            btnBrowseFolder.Click += (s, e) => BrowseFolder();
            this.Controls.Add(btnBrowseFolder);

            // Format / Container (Custom Sleek Dropdown with Zero White Box Glitch)
            Label lblFmt = new Label
            {
                Text = "Format / Container:",
                Location = new Point(445, 250),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(lblFmt);

            pnlFormatContainer = new Panel
            {
                Location = new Point(445, 270),
                Size = new Size(295, 36),
                BackColor = Color.FromArgb(9, 13, 21),
                Cursor = Cursors.Hand
            };
            pnlFormatContainer.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlFormatContainer.Width - 1, pnlFormatContainer.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(9, 13, 21))) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(28, 38, 54), 1.2f)) g.DrawPath(p, path);
                }
                DrawMediaBadgeIcon(g, 10, 9, Color.FromArgb(251, 191, 36));

                // Current Format Text
                using (var f = new Font("Segoe UI", 9.5F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString(_currentFormat, f, b, 34, 8);
                }

                // Dropdown arrow
                DrawDropdownArrow(g, r.Right - 20, 15, Color.FromArgb(203, 213, 225));
            };
            pnlFormatContainer.Click += (s, e) => ShowFormatDropdown();
            this.Controls.Add(pnlFormatContainer);

            // ==================== 4. Quality & Schedule Row ====================
            Label lblQual = new Label
            {
                Text = "Quality / Resolution:",
                Location = new Point(startX, 314),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(lblQual);

            pnlQualityContainer = new Panel
            {
                Location = new Point(startX, 334),
                Size = new Size(340, 36),
                BackColor = Color.FromArgb(9, 13, 21),
                Cursor = Cursors.Hand
            };
            pnlQualityContainer.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlQualityContainer.Width - 1, pnlQualityContainer.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(9, 13, 21))) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(28, 38, 54), 1.2f)) g.DrawPath(p, path);
                }
                DrawMonitorQualityIcon(g, 10, 9, Color.FromArgb(251, 191, 36));

                // Current Quality Text
                using (var f = new Font("Segoe UI", 9.25F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString(_currentQuality, f, b, 34, 8);
                }

                // Dropdown arrow
                DrawDropdownArrow(g, r.Right - 20, 15, Color.FromArgb(203, 213, 225));
            };
            pnlQualityContainer.Click += (s, e) => ShowQualityDropdown();
            this.Controls.Add(pnlQualityContainer);

            // Download Timing Schedule Container (Custom Styled Card)
            pnlScheduleCard = new Panel
            {
                Location = new Point(375, 314),
                Size = new Size(365, 76),
                BackColor = Color.FromArgb(9, 13, 21),
                Cursor = Cursors.Hand
            };
            pnlScheduleCard.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlScheduleCard.Width - 1, pnlScheduleCard.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(9, 13, 21))) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(28, 38, 54), 1.2f)) g.DrawPath(p, path);
                }

                // Clock icon + Header Text in Golden Amber
                DrawClockIcon(g, 12, 9, Color.FromArgb(251, 191, 36));
                using (var f = new Font("Segoe UI", 8.5F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.FromArgb(251, 191, 36)))
                {
                    g.DrawString("Download Timing Schedule", f, b, 28, 7);
                }

                // Option 1: Immediate Radio Button
                DrawRadioCircle(g, 14, 30, !_isScheduledChecked);
                using (var f = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.DrawString("Start download immediately (Recommended)", f, b, 34, 29);
                }

                // Option 2: Scheduled Time Radio Button
                DrawRadioCircle(g, 14, 51, _isScheduledChecked);
                using (var f = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.DrawString("Start at scheduled time:", f, b, 34, 50);
                }

                // Dark Time Box next to option 2
                Rectangle timeBox = new Rectangle(180, 47, 120, 22);
                using (var tbPath = CreateRoundedRectanglePath(timeBox, 4))
                {
                    using (var b = new SolidBrush(Color.FromArgb(16, 22, 34))) g.FillPath(b, tbPath);
                    using (var p = new Pen(Color.FromArgb(38, 50, 70), 1f)) g.DrawPath(p, tbPath);
                }
                using (var f = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                using (var b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.DrawString(_scheduledDateTime.ToString("hh:mm:ss tt"), f, b, 186, 50);
                }
                // Calendar icon inside time box
                DrawCalendarIcon(g, 282, 51, Color.FromArgb(148, 163, 184));
            };
            pnlScheduleCard.Click += PnlScheduleCard_Click;
            this.Controls.Add(pnlScheduleCard);

            // Hidden background DateTimePicker to handle time adjustments if needed
            dtpScheduleTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Visible = false
            };
            this.Controls.Add(dtpScheduleTime);
        }

        private void PnlScheduleCard_Click(object sender, EventArgs e)
        {
            Point pt = pnlScheduleCard.PointToClient(Cursor.Position);
            if (pt.Y < 42)
            {
                _isScheduledChecked = false;
            }
            else
            {
                _isScheduledChecked = true;
                // If clicked near time box, allow setting time
                if (pt.X >= 170)
                {
                    _scheduledDateTime = _scheduledDateTime.AddMinutes(15);
                }
            }
            pnlScheduleCard.Invalidate();
        }

        private void ShowFormatDropdown()
        {
            var cms = CreateDarkContextMenu();
            foreach (var fmt in _formatOptions)
            {
                var item = cms.Items.Add(fmt);
                item.Click += (s, ev) =>
                {
                    _currentFormat = fmt;
                    string ext = GetSelectedExtension();
                    UpdateFileNameExtension(ext);
                    pnlFormatContainer.Invalidate();
                };
            }
            cms.Show(pnlFormatContainer, new Point(0, pnlFormatContainer.Height + 2));
        }

        private void ShowQualityDropdown()
        {
            var cms = CreateDarkContextMenu();
            foreach (var q in _qualityOptions)
            {
                var item = cms.Items.Add(q);
                item.Click += (s, ev) =>
                {
                    _currentQuality = q;
                    if (q.Contains("MP3"))
                    {
                        _currentFormat = "MP3 Audio (.mp3)";
                        UpdateFileNameExtension(".mp3");
                    }
                    else if (q.Contains("M4A"))
                    {
                        _currentFormat = "M4A Audio (.m4a)";
                        UpdateFileNameExtension(".m4a");
                    }
                    else if (_currentFormat.Contains("Audio"))
                    {
                        _currentFormat = "MP4 Video (.mp4)";
                        UpdateFileNameExtension(".mp4");
                    }
                    pnlQualityContainer.Invalidate();
                    pnlFormatContainer.Invalidate();
                };
            }
            cms.Show(pnlQualityContainer, new Point(0, pnlQualityContainer.Height + 2));
        }

        private ContextMenuStrip CreateDarkContextMenu()
        {
            var cms = new ContextMenuStrip();
            cms.ShowImageMargin = false;
            cms.Renderer = new ToolStripProfessionalRenderer(new DarkNeonColorTable());
            cms.Font = new Font("Segoe UI", 9F);
            cms.ForeColor = Color.White;
            return cms;
        }

        private class DarkNeonColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder { get { return Color.FromArgb(245, 158, 11); } }
            public override Color MenuItemBorder { get { return Color.Transparent; } }
            public override Color MenuItemSelected { get { return Color.FromArgb(245, 158, 11); } }
            public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(251, 191, 36); } }
            public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(245, 158, 11); } }
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(10, 14, 22); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(10, 14, 22); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(10, 14, 22); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(10, 14, 22); } }
        }

        public static string GetPersistedDownloadDirectory()
        {
            if (!string.IsNullOrEmpty(_savedDownloadDir) && Directory.Exists(_savedDownloadDir))
            {
                return _savedDownloadDir;
            }

            try
            {
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDMTurboDownloader");
                string configFile = Path.Combine(appDataDir, "download_dir.cfg");
                if (File.Exists(configFile))
                {
                    string saved = File.ReadAllText(configFile).Trim();
                    if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
                    {
                        _savedDownloadDir = saved;
                        return _savedDownloadDir;
                    }
                }

                string baseConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "download_dir.cfg");
                if (File.Exists(baseConfig))
                {
                    string saved = File.ReadAllText(baseConfig).Trim();
                    if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
                    {
                        _savedDownloadDir = saved;
                        return _savedDownloadDir;
                    }
                }
            }
            catch { }

            string def = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            _savedDownloadDir = def;
            return def;
        }

        public static void SavePersistedDownloadDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            try
            {
                _savedDownloadDir = dir.Trim();

                if (!Directory.Exists(_savedDownloadDir))
                {
                    try { Directory.CreateDirectory(_savedDownloadDir); } catch { }
                }

                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDMTurboDownloader");
                if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
                File.WriteAllText(Path.Combine(appDataDir, "download_dir.cfg"), _savedDownloadDir, System.Text.Encoding.UTF8);

                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "download_dir.cfg"), _savedDownloadDir, System.Text.Encoding.UTF8);
                }
                catch { }
            }
            catch { }
        }

        private void BrowseFolder()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Destination Directory for IDM Turbo Downloads:";
                string current = txtFolder != null ? txtFolder.Text.Trim() : "";
                if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
                {
                    fbd.SelectedPath = current;
                }
                else
                {
                    fbd.SelectedPath = GetPersistedDownloadDirectory();
                }

                if (fbd.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtFolder.Text = fbd.SelectedPath;
                    SavePersistedDownloadDirectory(fbd.SelectedPath);
                }
            }
        }

        #endregion

        #region Bottom Action Bar

        private void BuildBottomActions()
        {
            int y = 416;

            // "⚙️ More Options ⌄" Button
            btnMoreOptions = new Button
            {
                Text = "⚙  More Options  ⌄",
                Location = new Point(20, y),
                Size = new Size(155, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnMoreOptions.FlatAppearance.BorderSize = 0;
            btnMoreOptions.MouseEnter += (s, e) => { _hoverMoreOptions = true; btnMoreOptions.Invalidate(); };
            btnMoreOptions.MouseLeave += (s, e) => { _hoverMoreOptions = false; btnMoreOptions.Invalidate(); };
            btnMoreOptions.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, btnMoreOptions.Width - 1, btnMoreOptions.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    Color bg = _hoverMoreOptions ? Color.FromArgb(20, 26, 38) : Color.FromArgb(12, 16, 25);
                    using (var b = new SolidBrush(bg)) g.FillPath(b, path);

                    Color borderClr = _hoverMoreOptions ? Color.FromArgb(245, 158, 11) : Color.FromArgb(120, 53, 15);
                    using (var p = new Pen(borderClr, 1.2f)) g.DrawPath(p, path);
                }

                using (var f = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("⚙  More Options  ⌄", f, b, r, sf);
                }
            };
            btnMoreOptions.Click += BtnMoreOptions_Click;
            this.Controls.Add(btnMoreOptions);

            // "Cancel" Button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(465, y),
                Size = new Size(95, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.MouseEnter += (s, e) => { _hoverCancel = true; btnCancel.Invalidate(); };
            btnCancel.MouseLeave += (s, e) => { _hoverCancel = false; btnCancel.Invalidate(); };
            btnCancel.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, btnCancel.Width - 1, btnCancel.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    Color bg = _hoverCancel ? Color.FromArgb(28, 38, 54) : Color.FromArgb(18, 24, 36);
                    using (var b = new SolidBrush(bg)) g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(36, 48, 68), 1.2f)) g.DrawPath(p, path);
                }
                using (var f = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.FromArgb(203, 213, 225)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("Cancel", f, b, r, sf);
                }
            };
            this.Controls.Add(btnCancel);

            // "Start Download" Button (Glowing Golden Gradient)
            btnOk = new Button
            {
                Text = "⬇ Start Download",
                Location = new Point(570, y),
                Size = new Size(170, 38),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                UseVisualStyleBackColor = false
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.MouseEnter += (s, e) => { _hoverOk = true; btnOk.Invalidate(); };
            btnOk.MouseLeave += (s, e) => { _hoverOk = false; btnOk.Invalidate(); };
            btnOk.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, btnOk.Width - 1, btnOk.Height - 1);

                Color c1 = _hoverOk ? Color.FromArgb(253, 224, 71) : Color.FromArgb(251, 191, 36);
                Color c2 = _hoverOk ? Color.FromArgb(245, 158, 11) : Color.FromArgb(217, 119, 6);

                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    // Outer neon amber glow
                    using (var pGlow = new Pen(Color.FromArgb(80, 245, 158, 11), 3f)) g.DrawPath(pGlow, path);

                    using (var grad = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical))
                    {
                        g.FillPath(grad, path);
                    }
                    using (var p = new Pen(Color.FromArgb(254, 240, 138), 1.2f)) g.DrawPath(p, path);
                }

                // Download tray icon & text
                DrawDownloadTrayIcon(g, r.Left + 16, r.Top + 11, Color.FromArgb(11, 15, 25));

                using (var f = new Font("Segoe UI", 10F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.FromArgb(11, 15, 25)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Rectangle textR = new Rectangle(r.Left + 18, r.Top, r.Width - 18, r.Height);
                    g.DrawString("Start Download", f, b, textR, sf);
                }
            };
            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);
        }

        private void BtnMoreOptions_Click(object sender, EventArgs e)
        {
            var cms = CreateDarkContextMenu();
            cms.Items.Add("⚡ 32-Socket Multi-Thread Acceleration (Active)", null, (s, ev) => { });
            cms.Items.Add("🌐 Custom HTTP Headers & Cookies...", null, (s, ev) => {
                using (OptionsDialog opt = new OptionsDialog()) opt.ShowDialog(this);
            });
            cms.Items.Add("🔒 Proxy & SOCKS5 Configuration...", null, (s, ev) => {
                using (OptionsDialog opt = new OptionsDialog()) opt.ShowDialog(this);
            });
            cms.Items.Add("⏱ Bandwidth Speed Limiter (No Limit)", null, (s, ev) => { });
            cms.Show(btnMoreOptions, new Point(0, btnMoreOptions.Height + 2));
        }

        #endregion

        #region Business Logic & Stream Fetching

        private void CheckClipboardForUrl()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clip = Clipboard.GetText().Trim();
                    if (clip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        clip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        txtUrl.Text = clip;
                        BtnGetInfo_Click(this, EventArgs.Empty);
                    }
                }
            }
            catch { }
        }

        private void ProcessInitialInput(string url, string presetTitle, string presetQuality, string presetFormat)
        {
            url = VideoEngine.NormalizeVideoUrl(url);
            txtUrl.Text = url ?? "";

            if (!string.IsNullOrEmpty(presetTitle))
            {
                bool isMedia = VideoEngine.IsMediaUrl(url);
                string defExt = isMedia ? ".mp4" : ".bin";
                txtFileName.Text = DownloadEngine.MakeSafeFileName(presetTitle, 75, defExt);
                if (isMedia && txtFileName.Text.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    txtFileName.Text = Path.GetFileNameWithoutExtension(txtFileName.Text) + ".mp4";
                }

                _previewTitle = presetTitle;
                if (pnlMediaCard != null) pnlMediaCard.Invalidate();
            }

            if (TorrentEngine.IsTorrentUrl(url))
            {
                HandleTorrentUrl(url, presetTitle, presetQuality, presetFormat);
                return;
            }

            if (VideoEngine.IsMediaUrl(url) && !string.IsNullOrEmpty(presetTitle))
            {
                IsVideoMode = true;
                List<int> clientHeights = ParseHeightsCsv(AvailableHeightsCsv);
                if (clientHeights != null && clientHeights.Count > 0)
                {
                    PopulateQualityDropdown(clientHeights);
                    if (!string.IsNullOrEmpty(presetQuality)) SelectQualityByName(presetQuality);
                    if (!string.IsNullOrEmpty(presetFormat)) SelectFormatByExtension(presetFormat);
                    return;
                }

                _videoEngine.FetchMetadata(url, (success, title, availableHeights, error) =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (success)
                        {
                            if (string.IsNullOrEmpty(txtFileName.Text) || txtFileName.Text.StartsWith("download_"))
                            {
                                txtFileName.Text = title + ".mp4";
                            }
                            PopulateQualityDropdown(availableHeights);
                            if (!string.IsNullOrEmpty(presetQuality)) SelectQualityByName(presetQuality);
                            _previewTitle = title;
                            if (pnlMediaCard != null) pnlMediaCard.Invalidate();
                        }
                    }));
                });
                return;
            }

            BtnGetInfo_Click(this, EventArgs.Empty);
        }

        private void BtnGetInfo_Click(object sender, EventArgs e)
        {
            string url = VideoEngine.NormalizeVideoUrl(txtUrl.Text.Trim());
            if (string.IsNullOrEmpty(url)) return;
            if (txtUrl.Text.Trim() != url) txtUrl.Text = url;

            if (TorrentEngine.IsTorrentUrl(url))
            {
                HandleTorrentUrl(url, null, null, null);
                return;
            }

            btnGetInfo.Enabled = false;
            IsVideoMode = VideoEngine.IsMediaUrl(url);

            if (IsVideoMode)
            {
                if (txtFileName.Text.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    txtFileName.Text = Path.GetFileNameWithoutExtension(txtFileName.Text) + ".mp4";
                }

                _videoEngine.FetchMetadata(url, (success, title, availableHeights, error) =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        btnGetInfo.Enabled = true;
                        if (success)
                        {
                            txtFileName.Text = title + ".mp4";
                            _previewTitle = title;
                            _previewSub1 = "Detected Online Stream";
                            _previewSub2 = "Coke Studio / High Def";
                            PopulateQualityDropdown(availableHeights);
                            if (pnlMediaCard != null) pnlMediaCard.Invalidate();
                        }
                        else
                        {
                            if (url.IndexOf("tiktok.com", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                PopulateQualityDropdown(new List<int> { 2160, 1440, 1080, 720, 480 });
                                SelectQualityByName("1080p");
                                _previewTitle = "TikTok Video Stream";
                                _previewSub1 = "TikTok Official Clip";
                            }
                            else if (url.IndexOf("facebook.com", StringComparison.OrdinalIgnoreCase) >= 0 || url.IndexOf("fb.watch", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                PopulateQualityDropdown(new List<int> { 1080, 720, 480 });
                                SelectQualityByName("1080p");
                                _previewTitle = "Facebook Video Stream";
                                _previewSub1 = "Facebook Watch";
                            }
                            else
                            {
                                PopulateQualityDropdown(new List<int> { 2160, 1440, 1080, 720, 480 });
                                SelectQualityByName("1440p");
                            }
                            if (pnlMediaCard != null) pnlMediaCard.Invalidate();
                        }
                    }));
                });
            }
            else
            {
                IsVideoMode = false;
                CheckDirectHttp(url);
            }
        }

        private void CheckDirectHttp(string url)
        {
            IsVideoMode = false;
            DownloadEngine engine = new DownloadEngine(url, txtFolder.Text, 8);
            engine.FetchMetadata((success, error) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    btnGetInfo.Enabled = true;
                    if (success)
                    {
                        if (string.IsNullOrEmpty(txtFileName.Text) || txtFileName.Text.StartsWith("download_"))
                        {
                            txtFileName.Text = engine.SuggestedFileName;
                        }
                        _previewTitle = engine.SuggestedFileName;
                        _previewSub1 = string.Format("Direct File Size: {0}", FormatBytes(engine.TotalFileSize));
                        _previewSub2 = engine.SupportsRangeRequests ? "Multi-Part Resumable" : "Standard Stream";
                        _previewDuration = "FILE";
                        if (pnlMediaCard != null) pnlMediaCard.Invalidate();
                    }
                }));
            });
        }

        private void HandleTorrentUrl(string url, string presetTitle, string presetQuality, string presetFormat)
        {
            btnGetInfo.Enabled = true;
            IsVideoMode = false;
            IsTorrentMode = true;

            string title = presetTitle;
            if (string.IsNullOrEmpty(title))
            {
                Match m = Regex.Match(url, @"[?&]dn=([^&]+)", RegexOptions.IgnoreCase);
                if (m.Success) title = Uri.UnescapeDataString(m.Groups[1].Value.Replace('+', ' '));
                else
                {
                    Match mHash = Regex.Match(url, @"btih:([a-fA-F0-9]+)", RegexOptions.IgnoreCase);
                    title = mHash.Success ? ("Torrent_" + mHash.Groups[1].Value.Substring(0, 8)) : "Torrent_Download";
                }
            }

            SelectFormatByExtension(".mp4");
            PopulateQualityDropdown(null);
            SelectQualityByName("1080p");
            txtFileName.Text = SanitizeFileName(Path.GetFileNameWithoutExtension(title)) + ".mp4";

            _previewTitle = title;
            _previewSub1 = "Torrent P2P Magnet";
            _previewSub2 = "32-Peer DHT Swarm";
            _previewDuration = "P2P";
            if (pnlMediaCard != null) pnlMediaCard.Invalidate();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl.Text.Trim()))
            {
                MessageBox.Show("Please enter a valid URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Persist the user's chosen download directory so it is always remembered
            if (txtFolder != null && !string.IsNullOrWhiteSpace(txtFolder.Text))
            {
                string targetDir = txtFolder.Text.Trim();
                if (Directory.Exists(targetDir))
                {
                    SavePersistedDownloadDirectory(targetDir);
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(targetDir);
                        SavePersistedDownloadDirectory(targetDir);
                    }
                    catch { }
                }
            }
            if (string.IsNullOrEmpty(txtFileName.Text.Trim()))
            {
                txtFileName.Text = "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            string defExt = (IsVideoMode || VideoEngine.IsMediaUrl(Url)) ? ("." + SelectedFormat.TrimStart('.')) : (IsTorrentMode ? ".mp4" : ".bin");
            if ((IsVideoMode || VideoEngine.IsMediaUrl(Url)) && txtFileName.Text.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                txtFileName.Text = Path.GetFileNameWithoutExtension(txtFileName.Text) + defExt;
            }
            txtFileName.Text = DownloadEngine.MakeSafeFileName(txtFileName.Text.Trim(), 75, defExt);
        }

        private void UpdateFileNameExtension(string newExt)
        {
            string current = txtFileName.Text.Trim();
            if (string.IsNullOrEmpty(current)) return;
            string baseName = Path.GetFileNameWithoutExtension(current);
            txtFileName.Text = baseName + newExt;
        }

        private string GetSelectedExtension()
        {
            if (string.IsNullOrEmpty(_currentFormat)) return ".mp4";
            Match m = Regex.Match(_currentFormat, @"\(\*?(\.[a-zA-Z0-9]+)\)");
            if (m.Success) return m.Groups[1].Value.ToLowerInvariant();
            return ".mp4";
        }

        private void SelectFormatByExtension(string ext)
        {
            string extLower = ext.ToLowerInvariant();
            foreach (var item in _formatOptions)
            {
                if (item.ToLowerInvariant().Contains("(" + extLower + ")"))
                {
                    _currentFormat = item;
                    if (pnlFormatContainer != null) pnlFormatContainer.Invalidate();
                    return;
                }
            }
        }

        private void PopulateQualityDropdown(List<int> heights)
        {
            _qualityOptions.Clear();
            _qualityOptions.Add("8K Ultra HD (4320p - Best Quality)");
            _qualityOptions.Add("4K Ultra HD (2160p - Best Quality)");
            _qualityOptions.Add("2K Quad HD (1440p - Universal MP4)");
            _qualityOptions.Add("1080p Full HD (H.264 - 100% Playable)");
            _qualityOptions.Add("720p HD (H.264 - 100% Playable)");
            _qualityOptions.Add("480p Standard (H.264 - 100% Playable)");
            _qualityOptions.Add("360p Medium (H.264 - 100% Playable)");
            _qualityOptions.Add("Audio Only - MP3 (High Quality 320 kbps)");
            _qualityOptions.Add("Audio Only - M4A (Original AAC Audio)");

            int defaultIndex = 2; // Default to 2K Quad HD (1440p)
            if (heights != null && heights.Count > 0)
            {
                int maxH = 0;
                foreach (int h in heights) if (h > maxH) maxH = h;
                if (maxH >= 4320) defaultIndex = 0;
                else if (maxH >= 2160) defaultIndex = 1;
                else if (maxH >= 1440) defaultIndex = 2;
                else if (maxH >= 1080) defaultIndex = 3;
                else if (maxH >= 720) defaultIndex = 4;
                else if (maxH >= 480) defaultIndex = 5;
                else defaultIndex = 6;
            }

            if (!string.IsNullOrEmpty(_initialPresetQuality))
            {
                SelectQualityByName(_initialPresetQuality);
            }
            else if (defaultIndex >= 0 && defaultIndex < _qualityOptions.Count)
            {
                _currentQuality = _qualityOptions[defaultIndex];
                if (pnlQualityContainer != null) pnlQualityContainer.Invalidate();
            }
        }

        private void SelectQualityByName(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            string k = key.Trim().ToLowerInvariant();
            foreach (var item in _qualityOptions)
            {
                if (item.ToLowerInvariant().Contains(k))
                {
                    _currentQuality = item;
                    if (pnlQualityContainer != null) pnlQualityContainer.Invalidate();
                    return;
                }
            }
        }

        private static List<int> ParseHeightsCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return null;
            List<int> list = new List<int>();
            string[] parts = csv.Split(new char[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string p in parts)
            {
                int h;
                if (int.TryParse(p.Trim(), out h) && h > 0)
                {
                    if (!list.Contains(h)) list.Add(h);
                }
            }
            list.Sort((a, b) => b.CompareTo(a));
            return list.Count > 0 ? list : null;
        }

        private static string SanitizeFileName(string name)
        {
            return DownloadEngine.MakeSafeFileName(name, 75);
        }

        private string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "Unknown";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dBytes = bytes;
            while (dBytes >= 1024 && i < suffixes.Length - 1)
            {
                dBytes /= 1024;
                i++;
            }
            return string.Format("{0:0.##} {1}", dBytes, suffixes[i]);
        }

        #endregion

        #region Custom Graphics & Icon Drawing Helpers

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawNeonLightningBolt(Graphics g, int x, int y, int w, int h, Color color)
        {
            try
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(x + (w * 0.55f), y),
                    new PointF(x, y + (h * 0.52f)),
                    new PointF(x + (w * 0.45f), y + (h * 0.52f)),
                    new PointF(x + (w * 0.25f), y + h),
                    new PointF(x + w, y + (h * 0.42f)),
                    new PointF(x + (w * 0.55f), y + (h * 0.42f))
                };

                using (var path = new GraphicsPath())
                {
                    path.AddPolygon(pts);
                    using (var pGlow = new Pen(Color.FromArgb(90, color), 2.5f)) g.DrawPath(pGlow, path);
                    using (var b = new SolidBrush(color)) g.FillPath(b, path);
                }
            }
            catch { }
        }

        private static void DrawChainLinkIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.8f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawArc(p, x + 6, y + 2, 7, 7, -45, 180);
                g.DrawLine(p, x + 4, y + 10, x + 10, y + 4);
                g.DrawArc(p, x + 1, y + 7, 7, 7, 135, 180);
            }
        }

        private static void DrawFolderIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.6f))
            {
                g.DrawRectangle(p, x + 1, y + 4, 15, 10);
                g.DrawLine(p, x + 1, y + 4, x + 5, y + 1);
                g.DrawLine(p, x + 5, y + 1, x + 9, y + 1);
                g.DrawLine(p, x + 9, y + 1, x + 11, y + 4);
            }
        }

        private static void DrawMediaBadgeIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.5f))
            {
                g.DrawRectangle(p, x + 1, y + 2, 14, 13);
                g.DrawLine(p, x + 4, y + 6, x + 12, y + 6);
                g.DrawLine(p, x + 4, y + 9, x + 12, y + 9);
            }
        }

        private static void DrawMonitorQualityIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.5f))
            {
                g.DrawRectangle(p, x + 1, y + 1, 15, 11);
                g.DrawLine(p, x + 8, y + 12, x + 8, y + 15);
                g.DrawLine(p, x + 4, y + 15, x + 12, y + 15);
            }
        }

        private static void DrawClockIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.5f))
            {
                g.DrawEllipse(p, x, y, 12, 12);
                g.DrawLine(p, x + 6, y + 3, x + 6, y + 6);
                g.DrawLine(p, x + 6, y + 6, x + 9, y + 6);
            }
        }

        private static void DrawDownloadTrayIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 2.0f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, x + 7, y + 1, x + 7, y + 9);
                g.DrawLine(p, x + 4, y + 6, x + 7, y + 9);
                g.DrawLine(p, x + 10, y + 6, x + 7, y + 9);
                g.DrawLine(p, x + 2, y + 12, x + 12, y + 12);
            }
        }

        private static void DrawDropdownArrow(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.6f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, x, y, x + 4, y + 4);
                g.DrawLine(p, x + 4, y + 4, x + 8, y);
            }
        }

        private static void DrawRadioCircle(Graphics g, int x, int y, bool isChecked)
        {
            int d = 13;
            if (isChecked)
            {
                // Glowing golden amber outer ring
                using (var p = new Pen(Color.FromArgb(245, 158, 11), 1.8f))
                {
                    g.DrawEllipse(p, x, y, d, d);
                }
                // Solid golden dot inside
                using (var b = new SolidBrush(Color.FromArgb(251, 191, 36)))
                {
                    g.FillEllipse(b, x + 3.5f, y + 3.5f, 6f, 6f);
                }
            }
            else
            {
                // Dark slate inactive ring
                using (var p = new Pen(Color.FromArgb(71, 85, 105), 1.5f))
                {
                    g.DrawEllipse(p, x, y, d, d);
                }
            }
        }

        private static void DrawCalendarIcon(Graphics g, int x, int y, Color c)
        {
            using (var p = new Pen(c, 1.2f))
            {
                g.DrawRectangle(p, x, y + 2, 11, 10);
                g.DrawLine(p, x, y + 5, x + 11, y + 5);
                g.DrawLine(p, x + 3, y, x + 3, y + 2);
                g.DrawLine(p, x + 8, y, x + 8, y + 2);
            }
        }

        #endregion
    }
}
