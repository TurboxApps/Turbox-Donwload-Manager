using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IDMClone
{
    public class DownloadProgressDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // Core Window Components
        private Panel pnlTitleBar;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;

        private Panel pnlCard;
        private Panel pnlDetailsCard;
        private Label lblCardTitle;
        private SpeedometerGauge speedGauge;
        private Label lblBigSpeedBadge;

        // Metric Rows
        private Label lblFileName;
        private Label lblStatus;
        private Label lblFileSize;
        private Label lblDownloaded;
        private Label lblTransferRate;
        private Label lblStartTime;
        private Label lblElapsedTime;
        private Label lblTimeLeft;
        private Label lblResume;

        // Main Progress Bar (High-End Glowing Capsule)
        private GlowingProgressBar prgTotal;

        // Visual Stream Segments
        private Label lblSegHeader;
        private Button btnToggleSegments;
        private Panel pnlSegments;
        private List<GlowingProgressBar> _chunkProgressBars = new List<GlowingProgressBar>();
        private List<Label> _chunkLabels = new List<Label>();

        // Bottom Action Bar
        private Panel pnlBottomBar;
        private Button btnPause;
        private Button btnCancel;
        private Button btnOpenFile;
        private Button btnOpenFolder;

        // Events
        public event EventHandler PauseRequested;
        public event EventHandler ResumeRequested;
        public event EventHandler CancelRequested;

        // State Tracking
        private bool _isPaused;
        private string _targetFilePath;
        private string _fileName;
        private DateTime _startDateTime;
        private Timer _clockTimer;
        private bool _isMultiChunkMode = false;

        // User preference remembered across sessions
        public static bool IsSegmentsVisible = true;

        public DownloadProgressDialog(string fileName, string url)
        {
            _fileName = fileName;
            _targetFilePath = fileName;
            _startDateTime = DateTime.Now;

            InitializeNeonGamingUI(fileName, url);
            SetupSegments(32);
            ApplySegmentsVisibility();
            ApplyTheme();
            StartClockTimer();
        }

        private void InitializeNeonGamingUI(string fileName, string url)
        {
            this.Text = "⚡ 0.0% - " + fileName + " - Turbo X";
            this.ClientSize = new Size(740, IsSegmentsVisible ? 610 : 440);
            this.MinimumSize = new Size(680, IsSegmentsVisible ? 580 : 420);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(8, 11, 16);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            UpdateRoundedWindowRegion();
            this.Resize += (s, e) => {
                UpdateRoundedWindowRegion();
                RepositionSegmentBars();
            };

            // 1. Custom Top Title Bar (Draggable, Amber Neon Icon & Buttons)
            BuildTitleBar();

            // 2. Container Panel
            pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(8, 11, 16),
                Padding = new Padding(0)
            };
            this.Controls.Add(pnlCard);
            pnlCard.BringToFront();

            // 3. Real-Time Transfer Metrics Card
            pnlDetailsCard = new Panel
            {
                Location = new Point(16, 10),
                Size = new Size(pnlCard.Width - 32, 240),
                BackColor = Color.FromArgb(11, 15, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlDetailsCard.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlDetailsCard.Width - 1, pnlDetailsCard.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var p = new Pen(Color.FromArgb(26, 36, 52), 1.2f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            };
            pnlCard.Controls.Add(pnlDetailsCard);

            lblCardTitle = new Label
            {
                Text = "⚡ Real-Time Transfer Metrics",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 191, 36),
                Location = new Point(14, 10),
                AutoSize = true,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            pnlDetailsCard.Controls.Add(lblCardTitle);

            // Legacy badge kept for compatibility
            lblBigSpeedBadge = new Label
            {
                Text = "0 B/s",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Location = new Point(pnlDetailsCard.Width - 225, 8),
                Size = new Size(210, 26),
                TextAlign = ContentAlignment.TopRight,
                UseMnemonic = false,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlDetailsCard.Controls.Add(lblBigSpeedBadge);

            // High-End Neon Gaming Speedometer Gauge (Visual Speed Meter)
            speedGauge = new SpeedometerGauge
            {
                Location = new Point(pnlDetailsCard.Width - 215, 10),
                Size = new Size(205, 218),
                BackColor = Color.FromArgb(11, 15, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlDetailsCard.Controls.Add(speedGauge);

            // Metrics Data Rows with ample 22px spacing
            int y = 36;
            lblFileName = AddDetailRow(pnlDetailsCard, "File name:", fileName, y); y += 22;
            lblStatus = AddDetailRow(pnlDetailsCard, "Status:", "Connecting (Turbo Handshake)...", y);
            lblStatus.ForeColor = Color.FromArgb(56, 189, 248); y += 22;
            lblFileSize = AddDetailRow(pnlDetailsCard, "File size:", "Estimating...", y); y += 22;
            lblDownloaded = AddDetailRow(pnlDetailsCard, "Downloaded:", "0 B (0.0%)", y); y += 22;
            lblTransferRate = AddDetailRow(pnlDetailsCard, "Transfer rate:", "0 KB/sec", y);
            lblTransferRate.ForeColor = Color.FromArgb(251, 191, 36);
            lblTransferRate.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); y += 22;
            lblStartTime = AddDetailRow(pnlDetailsCard, "Start time:", _startDateTime.ToString("yyyy-MM-dd HH:mm:ss"), y); y += 22;
            lblElapsedTime = AddDetailRow(pnlDetailsCard, "Time elapsed:", "00:00:00", y); y += 22;
            lblTimeLeft = AddDetailRow(pnlDetailsCard, "Time left:", "--:--:--", y);
            lblTimeLeft.ForeColor = Color.FromArgb(251, 191, 36); y += 22;
            lblResume = AddDetailRow(pnlDetailsCard, "Resume support:", "Yes (Active 32-Stream)", y);
            lblResume.ForeColor = Color.FromArgb(34, 197, 94);

            // 4. Main Total Progress Bar (Glowing Capsule)
            prgTotal = new GlowingProgressBar
            {
                Location = new Point(16, 260),
                Size = new Size(pnlCard.Width - 32, 26),
                CornerRadius = 8,
                ShowText = true,
                CustomText = "0.0%",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlCard.Controls.Add(prgTotal);

            // 5. Connection segments panel with Hide / Show Toggle Button
            lblSegHeader = new Label
            {
                Text = "⚡ 32 Dynamic Parallel Stream Connections (Visual Segments):",
                Location = new Point(16, 296),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            pnlCard.Controls.Add(lblSegHeader);

            btnToggleSegments = new Button
            {
                Text = IsSegmentsVisible ? "▲ Hide Segments" : "▼ Show Segments",
                Location = new Point(pnlCard.Width - 196, 291),
                Size = new Size(180, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(203, 213, 225),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnToggleSegments.FlatAppearance.BorderColor = Color.FromArgb(33, 49, 74);
            btnToggleSegments.FlatAppearance.BorderSize = 1;
            btnToggleSegments.MouseEnter += (s, e) => {
                btnToggleSegments.ForeColor = Color.FromArgb(251, 191, 36);
                btnToggleSegments.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11);
            };
            btnToggleSegments.MouseLeave += (s, e) => {
                btnToggleSegments.ForeColor = Color.FromArgb(203, 213, 225);
                btnToggleSegments.FlatAppearance.BorderColor = Color.FromArgb(33, 49, 74);
            };
            btnToggleSegments.Click += (s, e) => {
                IsSegmentsVisible = !IsSegmentsVisible;
                ApplySegmentsVisibility();
            };
            pnlCard.Controls.Add(btnToggleSegments);

            // 6. Visual Segments Box
            pnlSegments = new Panel
            {
                Location = new Point(16, 324),
                Size = new Size(pnlCard.Width - 32, Math.Max(50, pnlCard.Height - 324 - 54)),
                BackColor = Color.FromArgb(11, 15, 23),
                AutoScroll = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlSegments.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlSegments.Width - 1, pnlSegments.Height - 1);
                using (var path = CreateRoundedRectanglePath(r, 8))
                {
                    using (var p = new Pen(Color.FromArgb(26, 36, 52), 1.2f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            };
            pnlSegments.Resize += (s, e) => RepositionSegmentBars();
            pnlCard.Controls.Add(pnlSegments);

            // 7. Dedicated Bottom Bar (Never overlaps segments)
            pnlBottomBar = new Panel
            {
                Location = new Point(16, pnlCard.Height - 48),
                Size = new Size(pnlCard.Width - 32, 38),
                BackColor = Color.FromArgb(8, 11, 16),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlCard.Controls.Add(pnlBottomBar);

            // Action Buttons
            btnPause = new Button
            {
                Text = "⏸ Pause",
                Location = new Point(0, 2),
                Size = new Size(110, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.MouseEnter += (s, e) => {
                if (!_isPaused) btnPause.BackColor = Color.FromArgb(251, 191, 36);
                else btnPause.BackColor = Color.FromArgb(56, 189, 248);
            };
            btnPause.MouseLeave += (s, e) => {
                if (!_isPaused) btnPause.BackColor = Color.FromArgb(245, 158, 11);
                else btnPause.BackColor = Color.FromArgb(14, 165, 233);
            };
            btnPause.Click += (s, e) => {
                if (btnOpenFile.Enabled) return;

                if (_isPaused)
                {
                    _isPaused = false;
                    btnPause.Text = "⏸ Pause";
                    btnPause.BackColor = Color.FromArgb(245, 158, 11);
                    btnPause.ForeColor = Color.Black;
                    btnCancel.Text = "✕ Cancel";
                    prgTotal.State = GlowingProgressBar.ProgressState.Normal;
                    lblStatus.Text = "Resuming turbo streams...";
                    lblStatus.ForeColor = Color.FromArgb(56, 189, 248);
                    lblBigSpeedBadge.Text = "RESUMING";
                    if (speedGauge != null) speedGauge.SetSpeed("RESUMING");
                    if (ResumeRequested != null) ResumeRequested(this, EventArgs.Empty);
                }
                else
                {
                    _isPaused = true;
                    btnPause.Text = "▶ Resume";
                    btnPause.BackColor = Color.FromArgb(14, 165, 233);
                    btnPause.ForeColor = Color.Black;
                    btnCancel.Text = "✕ Close";
                    prgTotal.State = GlowingProgressBar.ProgressState.Paused;
                    lblStatus.Text = "Paused";
                    lblStatus.ForeColor = Color.FromArgb(56, 189, 248);
                    lblTransferRate.Text = "0 B/s";
                    lblBigSpeedBadge.Text = "PAUSED";
                    if (speedGauge != null) speedGauge.SetSpeed("PAUSED");
                    lblTimeLeft.Text = "--:--";
                    if (PauseRequested != null) PauseRequested(this, EventArgs.Empty);
                }
            };
            pnlBottomBar.Controls.Add(btnPause);

            btnCancel = new Button
            {
                Text = "✕ Cancel",
                Location = new Point(122, 2),
                Size = new Size(105, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = Color.FromArgb(19, 26, 38),
                ForeColor = Color.FromArgb(226, 232, 240),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.MouseEnter += (s, e) => {
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);
                btnCancel.ForeColor = Color.FromArgb(248, 113, 113);
            };
            btnCancel.MouseLeave += (s, e) => {
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
                btnCancel.ForeColor = Color.FromArgb(226, 232, 240);
            };
            btnCancel.Click += (s, e) => {
                if (_isPaused || btnOpenFile.Enabled)
                {
                    this.Close();
                    return;
                }
                if (CancelRequested != null) CancelRequested(this, EventArgs.Empty);
                this.Close();
            };
            pnlBottomBar.Controls.Add(btnCancel);

            btnOpenFile = new Button
            {
                Text = "⚡ Open File",
                Location = new Point(pnlBottomBar.Width - 250, 2),
                Size = new Size(115, 34),
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(17, 24, 39),
                ForeColor = Color.FromArgb(75, 85, 99),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnOpenFile.FlatAppearance.BorderSize = 0;
            btnOpenFile.Click += (s, e) => {
                try
                {
                    string target = _targetFilePath;
                    if (string.IsNullOrEmpty(target) || !File.Exists(target))
                    {
                        string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        string candidate = Path.Combine(userDownloads, Path.GetFileName(_fileName ?? ""));
                        if (File.Exists(candidate)) target = candidate;
                    }

                    if (File.Exists(target))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = target,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show("File is still preparing or was moved:\n" + target, "IDM Turbo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open file: " + ex.Message, "IDM Turbo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            pnlBottomBar.Controls.Add(btnOpenFile);

            btnOpenFolder = new Button
            {
                Text = "📂 Open Folder",
                Location = new Point(pnlBottomBar.Width - 125, 2),
                Size = new Size(125, 34),
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = Color.FromArgb(17, 24, 39),
                ForeColor = Color.FromArgb(75, 85, 99),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.Click += (s, e) => {
                try
                {
                    string target = _targetFilePath;
                    if (File.Exists(target))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = string.Format("/select,\"{0}\"", target),
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        string folder = Path.GetDirectoryName(target);
                        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                        {
                            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        }
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = folder,
                            UseShellExecute = true
                        });
                    }
                }
                catch { }
            };
            pnlBottomBar.Controls.Add(btnOpenFolder);
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
                DrawNeonLightningBolt(g, 14, 9, 15, 16, Color.FromArgb(251, 191, 36));

                // Constrain title text width so it NEVER collides with window control buttons
                int buttonsLeft = (btnMin != null) ? btnMin.Left - 12 : (pnlTitleBar.Width - 125);
                int availWidth = Math.Max(50, buttonsLeft - 36);
                RectangleF titleRect = new RectangleF(36, 0, availWidth, pnlTitleBar.Height);

                using (var f = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var b = new SolidBrush(Color.White))
                using (var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                    LineAlignment = StringAlignment.Center
                })
                {
                    string displayTitle = !string.IsNullOrEmpty(this.Text) ? this.Text : "Download Accelerator - Turbo X";
                    g.DrawString(displayTitle, f, b, titleRect, sf);
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
            btnClose.Click += (s, e) => {
                this.Close();
            };
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

        private void UpdateRoundedWindowRegion()
        {
            try
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.Region = null;
                    return;
                }
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

        #endregion

        private void ApplySegmentsVisibility()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(ApplySegmentsVisibility));
                return;
            }

            if (IsSegmentsVisible)
            {
                btnToggleSegments.Text = "▲ Hide Segments";
                pnlSegments.Visible = true;
                lblSegHeader.Text = string.Format("⚡ {0} Dynamic Parallel Stream Connections (Visual Segments):", _chunkProgressBars.Count > 0 ? _chunkProgressBars.Count : 32);
                this.MinimumSize = new Size(680, 580);
                if (this.Height < 580) this.Height = 610;
            }
            else
            {
                btnToggleSegments.Text = "▼ Show Segments";
                pnlSegments.Visible = false;
                lblSegHeader.Text = "⚡ Parallel Streams: 32 turbo sockets active in background (Hidden)";
                this.MinimumSize = new Size(680, 420);
                this.Height = 440;
            }
        }

        private void StartClockTimer()
        {
            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                if (!_isPaused && btnOpenFile.Enabled == false)
                {
                    TimeSpan elapsed = DateTime.Now - _startDateTime;
                    lblElapsedTime.Text = string.Format("{0:00}:{1:00}:{2:00}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
                }
            };
            _clockTimer.Start();
        }

        private Label AddDetailRow(Panel panel, string title, string val, int y)
        {
            Label lblT = new Label
            {
                Text = title,
                Location = new Point(14, y),
                Size = new Size(115, 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            panel.Controls.Add(lblT);

            Label lblV = new Label
            {
                Text = val,
                Location = new Point(132, y),
                Size = new Size(Math.Max(120, panel.Width - 140 - 220), 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panel.Controls.Add(lblV);
            return lblV;
        }

        public void SetupSegments(int count)
        {
            _isMultiChunkMode = (count > 1);
            pnlSegments.Controls.Clear();
            _chunkProgressBars.Clear();
            _chunkLabels.Clear();

            int clientW = pnlSegments.ClientSize.Width > 100 ? pnlSegments.ClientSize.Width : 700;
            int cols = Math.Min(count, 8);
            if (cols < 1) cols = 1;
            int gap = 6;
            int barWidth = (clientW - 16 - (cols * gap)) / cols;
            if (barWidth < 45) barWidth = 45;

            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                int x = 8 + col * (barWidth + gap);
                int y = 6 + row * 28;

                Label lbl = new Label
                {
                    Text = string.Format("P{0}: 0%", i + 1),
                    Location = new Point(x, y),
                    Size = new Size(barWidth, 12),
                    Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    BackColor = Color.Transparent
                };
                GlowingProgressBar pb = new GlowingProgressBar
                {
                    Location = new Point(x, y + 13),
                    Size = new Size(barWidth, 8),
                    CornerRadius = 3,
                    ShowText = false
                };
                pnlSegments.Controls.Add(lbl);
                pnlSegments.Controls.Add(pb);
                _chunkLabels.Add(lbl);
                _chunkProgressBars.Add(pb);
            }
        }

        private void RepositionSegmentBars()
        {
            if (_chunkProgressBars.Count == 0) return;
            int clientW = pnlSegments.ClientSize.Width;
            if (clientW < 100) return;
            int count = _chunkProgressBars.Count;
            int cols = Math.Min(count, 8);
            if (cols < 1) cols = 1;
            int gap = 6;
            int barWidth = (clientW - 16 - (cols * gap)) / cols;
            if (barWidth < 45) barWidth = 45;

            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                int x = 8 + col * (barWidth + gap);
                int y = 6 + row * 28;

                if (i < _chunkLabels.Count)
                {
                    _chunkLabels[i].Location = new Point(x, y);
                    _chunkLabels[i].Width = barWidth;
                }
                _chunkProgressBars[i].Location = new Point(x, y + 13);
                _chunkProgressBars[i].Width = barWidth;
            }
        }

        public void ConfigureForVideoStream()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(ConfigureForVideoStream));
                return;
            }
            lblSegHeader.Text = "⚡ High-Speed Media Stream: Universal H.264 + AAC Audio (100% Playable)";
            lblStatus.Text = "Connecting & analyzing media stream...";
            lblResume.Text = "Yes (Adaptive Stream)";
        }

        public void UpdateStatusText(string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatusText(status)));
                return;
            }
            if (_isPaused || btnOpenFile.Enabled) return;
            if (!string.IsNullOrEmpty(status)) lblStatus.Text = status;
        }

        public void SetInitialProgress(double pct, long downloadedBytes, long totalBytes)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetInitialProgress(pct, downloadedBytes, totalBytes)));
                return;
            }

            pct = Math.Max(0.0, Math.Min(100.0, pct));
            prgTotal.ValueDouble = pct;

            if (totalBytes > 0)
            {
                lblFileSize.Text = FormatBytes(totalBytes);
                if (downloadedBytes > 0)
                {
                    lblDownloaded.Text = string.Format("{0} of {1} ({2:0.0}%)", FormatBytes(downloadedBytes), FormatBytes(totalBytes), pct);
                    prgTotal.CustomText = string.Format("{0:0.0}% ({1} / {2})", pct, FormatBytes(downloadedBytes), FormatBytes(totalBytes));
                }
                else
                {
                    lblDownloaded.Text = string.Format("{0:0.0}%", pct);
                    prgTotal.CustomText = string.Format("{0:0.0}%", pct);
                }
            }
            else if (downloadedBytes > 0)
            {
                lblDownloaded.Text = string.Format("{0} ({1:0.0}%)", FormatBytes(downloadedBytes), pct);
                prgTotal.CustomText = string.Format("{0:0.0}%", pct);
            }
            else
            {
                lblDownloaded.Text = string.Format("{0:0.0}%", pct);
                prgTotal.CustomText = string.Format("{0:0.0}%", pct);
            }

            lblStatus.Text = "Resuming turbo streams...";
            lblTransferRate.Text = "0 B/s";
            lblBigSpeedBadge.Text = string.Format("{0:0.0}%", pct);
            if (speedGauge != null) speedGauge.SetSpeed(string.Format("{0:0.0}%", pct));

            this.Text = string.Format("⚡ {0:0.0}% - {1} - Turbo X", pct, Path.GetFileName(_targetFilePath ?? (_fileName ?? "file")));
            if (pnlTitleBar != null) pnlTitleBar.Invalidate();

            // Pre-populate segments visually to match pct
            if (!_isMultiChunkMode && _chunkProgressBars.Count > 0)
            {
                int filled = (int)((pct / 100.0) * _chunkProgressBars.Count);
                for (int i = 0; i < _chunkProgressBars.Count; i++)
                {
                    if (i < filled)
                    {
                        _chunkProgressBars[i].Value = 100;
                        _chunkLabels[i].Text = string.Format("P{0}: 100%", i + 1);
                    }
                    else if (i == filled)
                    {
                        int partial = (int)((pct * _chunkProgressBars.Count) % 100);
                        _chunkProgressBars[i].Value = Math.Min(100, Math.Max(0, partial));
                        _chunkLabels[i].Text = string.Format("P{0}: {1}%", i + 1, partial);
                    }
                    else
                    {
                        _chunkProgressBars[i].Value = 0;
                        _chunkLabels[i].Text = string.Format("P{0}: 0%", i + 1);
                    }
                }
            }
        }

        public void SetPausedState(bool isPaused)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetPausedState(isPaused)));
                return;
            }

            _isPaused = isPaused;
            if (_isPaused)
            {
                btnPause.Text = "▶ Resume";
                btnPause.BackColor = Color.FromArgb(14, 165, 233);
                btnPause.ForeColor = Color.Black;
                btnCancel.Text = "✕ Close";
                prgTotal.State = GlowingProgressBar.ProgressState.Paused;
                lblStatus.Text = "Paused";
                lblStatus.ForeColor = Color.FromArgb(56, 189, 248);
                lblTransferRate.Text = "0 B/s";
                lblBigSpeedBadge.Text = "PAUSED";
                if (speedGauge != null) speedGauge.SetSpeed("PAUSED");
                lblTimeLeft.Text = "--:--";
            }
            else
            {
                btnPause.Text = "⏸ Pause";
                btnPause.BackColor = Color.FromArgb(245, 158, 11);
                btnPause.ForeColor = Color.Black;
                btnCancel.Text = "✕ Cancel";
                prgTotal.State = GlowingProgressBar.ProgressState.Normal;
                lblStatus.Text = "Downloading...";
                lblStatus.ForeColor = Color.FromArgb(56, 189, 248);
            }
        }

        public void UpdateProgress(double pct, long downloadedBytes, long totalBytes, string status, string speedStr, string etaStr)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateProgress(pct, downloadedBytes, totalBytes, status, speedStr, etaStr)));
                return;
            }

            if (_isPaused || btnOpenFile.Enabled) return;

            pct = Math.Max(0.0, Math.Min(100.0, pct));
            prgTotal.ValueDouble = pct;

            if (downloadedBytes > 0 && totalBytes > 0)
            {
                prgTotal.CustomText = string.Format("{0:0.0}% ({1} / {2})", pct, FormatBytes(downloadedBytes), FormatBytes(totalBytes));
            }
            else
            {
                prgTotal.CustomText = string.Format("{0:0.0}%", pct);
            }

            this.Text = string.Format("⚡ {0:0.0}% - {1} - Turbo X", pct, Path.GetFileName(_targetFilePath ?? (_fileName ?? "file")));
            if (pnlTitleBar != null) pnlTitleBar.Invalidate();

            if (!string.IsNullOrEmpty(status)) lblStatus.Text = status;
            if (totalBytes > 0) lblFileSize.Text = FormatBytes(totalBytes);

            if (downloadedBytes > 0 && totalBytes > 0)
            {
                lblDownloaded.Text = string.Format("{0} of {1} ({2:0.0}%)", FormatBytes(downloadedBytes), FormatBytes(totalBytes), pct);
            }
            else if (downloadedBytes > 0)
            {
                lblDownloaded.Text = string.Format("{0} ({1:0.0}%)", FormatBytes(downloadedBytes), pct);
            }
            else
            {
                lblDownloaded.Text = string.Format("{0:0.0}%", pct);
            }

            if (!string.IsNullOrEmpty(speedStr))
            {
                lblTransferRate.Text = speedStr;
                lblBigSpeedBadge.Text = speedStr;
                if (speedGauge != null) speedGauge.SetSpeed(speedStr);
            }
            if (!string.IsNullOrEmpty(etaStr)) lblTimeLeft.Text = etaStr;

            // Live simulate stream chunks for video downloads if not per-chunk
            if (!_isMultiChunkMode && _chunkProgressBars.Count > 0)
            {
                int filled = (int)((pct / 100.0) * _chunkProgressBars.Count);
                for (int i = 0; i < _chunkProgressBars.Count; i++)
                {
                    if (i < filled)
                    {
                        _chunkProgressBars[i].Value = 100;
                        _chunkLabels[i].Text = string.Format("P{0}: 100%", i + 1);
                    }
                    else if (i == filled)
                    {
                        int partial = (int)((pct * _chunkProgressBars.Count) % 100);
                        _chunkProgressBars[i].Value = Math.Min(100, Math.Max(0, partial));
                        _chunkLabels[i].Text = string.Format("P{0}: {1}%", i + 1, partial);
                    }
                }
            }
        }

        public void UpdateProgress(double pct, string status, string sizeStr, string speedStr, string etaStr)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateProgress(pct, status, sizeStr, speedStr, etaStr)));
                return;
            }

            if (_isPaused || btnOpenFile.Enabled) return;

            pct = Math.Max(0.0, Math.Min(100.0, pct));
            prgTotal.ValueDouble = pct;
            prgTotal.CustomText = string.Format("{0:0.0}%", pct);

            this.Text = string.Format("⚡ {0:0.0}% - {1} - Turbo X", pct, Path.GetFileName(_targetFilePath ?? (_fileName ?? "file")));
            if (pnlTitleBar != null) pnlTitleBar.Invalidate();

            if (!string.IsNullOrEmpty(status)) lblStatus.Text = status;
            if (!string.IsNullOrEmpty(sizeStr)) lblFileSize.Text = sizeStr;
            lblDownloaded.Text = string.Format("{0:0.0}%", pct);

            if (!string.IsNullOrEmpty(speedStr))
            {
                lblTransferRate.Text = speedStr;
                lblBigSpeedBadge.Text = speedStr;
                if (speedGauge != null) speedGauge.SetSpeed(speedStr);
            }
            if (!string.IsNullOrEmpty(etaStr)) lblTimeLeft.Text = etaStr;

            // Live simulate stream chunks for video downloads if not per-chunk
            if (!_isMultiChunkMode && _chunkProgressBars.Count > 0)
            {
                int filled = (int)((pct / 100.0) * _chunkProgressBars.Count);
                for (int i = 0; i < _chunkProgressBars.Count; i++)
                {
                    if (i < filled)
                    {
                        _chunkProgressBars[i].Value = 100;
                        _chunkLabels[i].Text = string.Format("P{0}: 100%", i + 1);
                    }
                    else if (i == filled)
                    {
                        int partial = (int)((pct * _chunkProgressBars.Count) % 100);
                        _chunkProgressBars[i].Value = Math.Min(100, Math.Max(0, partial));
                        _chunkLabels[i].Text = string.Format("P{0}: {1}%", i + 1, partial);
                    }
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            place = Math.Min(place, suf.Length - 1);
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return string.Format("{0:0.00} {1}", num, suf[place]);
        }

        public void UpdateChunk(int chunkIndex, double pct)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateChunk(chunkIndex, pct)));
                return;
            }

            if (_isPaused || btnOpenFile.Enabled) return;

            if (chunkIndex < _chunkProgressBars.Count)
            {
                _chunkProgressBars[chunkIndex].Value = Math.Min(100, Math.Max(0, (int)pct));
                _chunkLabels[chunkIndex].Text = string.Format("P{0}: {1:0}%", chunkIndex + 1, pct);
            }
        }

        public void MarkComplete(string targetFile)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => MarkComplete(targetFile)));
                return;
            }

            if (_isPaused) return;

            _targetFilePath = targetFile;
            if (_clockTimer != null) _clockTimer.Stop();

            prgTotal.Value = 100;
            prgTotal.State = GlowingProgressBar.ProgressState.Completed;
            prgTotal.CustomText = "100.0% Download Complete!";

            lblStatus.Text = "Download complete!";
            lblStatus.ForeColor = Color.FromArgb(34, 197, 94);
            lblTransferRate.Text = "Finished";
            lblBigSpeedBadge.Text = "100% DONE";
            if (speedGauge != null) speedGauge.SetSpeed("100% DONE");
            lblTimeLeft.Text = "00:00:00";
            btnPause.Visible = false;
            btnCancel.Text = "✕ Close";

            btnOpenFile.Enabled = true;
            btnOpenFile.BackColor = Color.FromArgb(245, 158, 11);
            btnOpenFile.ForeColor = Color.Black;
            btnOpenFile.MouseEnter += (s, e) => { if (btnOpenFile.Enabled) btnOpenFile.BackColor = Color.FromArgb(251, 191, 36); };
            btnOpenFile.MouseLeave += (s, e) => { if (btnOpenFile.Enabled) btnOpenFile.BackColor = Color.FromArgb(245, 158, 11); };

            btnOpenFolder.Enabled = true;
            btnOpenFolder.BackColor = Color.FromArgb(20, 30, 48);
            btnOpenFolder.ForeColor = Color.FromArgb(56, 189, 248);
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(30, 58, 95);
            btnOpenFolder.FlatAppearance.BorderSize = 1;
            btnOpenFolder.MouseEnter += (s, e) => { if (btnOpenFolder.Enabled) btnOpenFolder.BackColor = Color.FromArgb(28, 42, 68); };
            btnOpenFolder.MouseLeave += (s, e) => { if (btnOpenFolder.Enabled) btnOpenFolder.BackColor = Color.FromArgb(20, 30, 48); };

            this.Text = "✓ Complete - " + Path.GetFileName(_targetFilePath ?? (_fileName ?? "file")) + " - Turbo X";
            if (pnlTitleBar != null) pnlTitleBar.Invalidate();

            // Complete all 32 segments visually
            foreach (var pb in _chunkProgressBars) pb.Value = 100;
            for (int i = 0; i < _chunkLabels.Count; i++)
            {
                _chunkLabels[i].Text = string.Format("P{0}: 100%", i + 1);
            }
        }

        public void MarkFailed(string error)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => MarkFailed(error)));
                return;
            }

            if (_isPaused) return;

            if (_clockTimer != null) _clockTimer.Stop();
            lblStatus.Text = "Error: " + error;
            lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
            prgTotal.State = GlowingProgressBar.ProgressState.Failed;
            prgTotal.CustomText = "Download Failed";
            btnPause.Enabled = false;

            this.Text = "✕ Failed - " + Path.GetFileName(_targetFilePath ?? (_fileName ?? "file")) + " - Turbo X";
            if (pnlTitleBar != null) pnlTitleBar.Invalidate();
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = Color.FromArgb(8, 11, 16);
            this.ForeColor = Color.White;

            if (pnlCard != null) pnlCard.BackColor = Color.FromArgb(8, 11, 16);
            if (pnlDetailsCard != null) pnlDetailsCard.BackColor = Color.FromArgb(11, 15, 23);
            if (pnlSegments != null) pnlSegments.BackColor = Color.FromArgb(11, 15, 23);
            if (pnlBottomBar != null) pnlBottomBar.BackColor = Color.FromArgb(8, 11, 16);
            if (speedGauge != null) speedGauge.BackColor = Color.FromArgb(11, 15, 23);
        }

        #region Custom Graphics Helpers

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

        #endregion
    }

    #region Glowing Capsule Progress Bar Control

    public class GlowingProgressBar : Control
    {
        private double _value = 0;
        private string _customText = null;
        private ProgressState _state = ProgressState.Normal;
        private int _cornerRadius = 8;

        public enum ProgressState
        {
            Normal,
            Paused,
            Completed,
            Failed
        }

        public GlowingProgressBar()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            this.BackColor = Color.Transparent;
        }

        public int Value
        {
            get { return (int)Math.Round(_value); }
            set
            {
                double v = Math.Max(0.0, Math.Min(100.0, value));
                if (Math.Abs(_value - v) > 0.01)
                {
                    _value = v;
                    this.Invalidate();
                }
            }
        }

        public double ValueDouble
        {
            get { return _value; }
            set
            {
                double v = Math.Max(0.0, Math.Min(100.0, value));
                if (Math.Abs(_value - v) > 0.01)
                {
                    _value = v;
                    this.Invalidate();
                }
            }
        }

        public string CustomText
        {
            get { return _customText; }
            set
            {
                if (_customText != value)
                {
                    _customText = value;
                    this.Invalidate();
                }
            }
        }

        public ProgressState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    this.Invalidate();
                }
            }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; this.Invalidate(); }
        }

        private bool _showText = true;
        public bool ShowText
        {
            get { return _showText; }
            set { _showText = value; this.Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = this.Width;
            int h = this.Height;
            if (w <= 2 || h <= 2) return;

            Rectangle rect = new Rectangle(0, 0, w - 1, h - 1);
            int r = Math.Min(_cornerRadius, h / 2);

            // 1. Draw track background (Dark sleek slot)
            using (var path = CreateRoundedPath(rect, r))
            {
                using (var bTrack = new SolidBrush(Color.FromArgb(14, 19, 29)))
                {
                    g.FillPath(bTrack, path);
                }
                using (var pBorder = new Pen(Color.FromArgb(30, 41, 59), 1.0f))
                {
                    g.DrawPath(pBorder, path);
                }
            }

            // 2. Draw Progress Fill
            float fillW = (float)((_value / 100.0) * (w - 2));
            if (fillW > 2f)
            {
                RectangleF fillRect = new RectangleF(1, 1, fillW, h - 2);
                int fillR = Math.Min(r, (int)(fillRect.Height / 2));

                using (var fillPath = CreateRoundedPath(fillRect, fillR))
                {
                    Color c1, c2;
                    switch (_state)
                    {
                        case ProgressState.Paused:
                            c1 = Color.FromArgb(2, 132, 199);
                            c2 = Color.FromArgb(56, 189, 248);
                            break;
                        case ProgressState.Completed:
                            c1 = Color.FromArgb(16, 185, 129);
                            c2 = Color.FromArgb(52, 211, 153);
                            break;
                        case ProgressState.Failed:
                            c1 = Color.FromArgb(220, 38, 38);
                            c2 = Color.FromArgb(248, 113, 113);
                            break;
                        default: // Normal
                            c1 = Color.FromArgb(245, 158, 11);
                            c2 = Color.FromArgb(251, 191, 36);
                            break;
                    }

                    using (var grad = new LinearGradientBrush(fillRect, c1, c2, LinearGradientMode.Horizontal))
                    {
                        g.FillPath(grad, fillPath);
                    }

                    // Top highlight sheen line for 3D neon capsule feel
                    using (var pSheen = new Pen(Color.FromArgb(90, 255, 255, 255), 1.0f))
                    {
                        g.DrawLine(pSheen, fillRect.Left + fillR, fillRect.Top + 1, fillRect.Right - fillR, fillRect.Top + 1);
                    }
                }
            }

            // 3. Draw Centered Progress Text (if enabled)
            if (ShowText && h >= 16)
            {
                string txt = !string.IsNullOrEmpty(_customText) ? _customText : string.Format("{0:0.0}%", _value);
                using (var f = new Font("Segoe UI", h > 20 ? 9F : 7.5F, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(txt, f);
                    float tx = (w - sz.Width) / 2f;
                    float ty = (h - sz.Height) / 2f;

                    // Subtle dark drop-shadow for 100% text readability over any background
                    using (var bShadow = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    {
                        g.DrawString(txt, f, bShadow, tx + 1f, ty + 1f);
                    }

                    using (var bText = new SolidBrush(Color.White))
                    {
                        g.DrawString(txt, f, bText, tx, ty);
                    }
                }
            }
        }

        private static GraphicsPath CreateRoundedPath(RectangleF r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    #endregion
}
