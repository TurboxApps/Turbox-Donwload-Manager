using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDMClone
{
    public class ClipboardNotificationPopup : Form
    {
        public string DetectedUrl { get; private set; }
        public bool UserAccepted { get; private set; }

        private Label lblTitle;
        private Label lblUrl;
        private Button btnDownload;
        private Button btnClose;
        private Timer autoCloseTimer;

        public ClipboardNotificationPopup(string url)
        {
            DetectedUrl = url;
            InitializeComponent();
            ApplyTheme();
            PositionAtBottomRight();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(380, 115);
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Font = new Font("Segoe UI", 9F);

            Panel borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(borderPanel);

            lblTitle = new Label
            {
                Text = "⚡ Turbo X: Download Link Detected!",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };
            borderPanel.Controls.Add(lblTitle);

            lblUrl = new Label
            {
                Text = DetectedUrl.Length > 48 ? (DetectedUrl.Substring(0, 45) + "...") : DetectedUrl,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(14, 34),
                Size = new Size(350, 20),
                AutoEllipsis = true
            };
            borderPanel.Controls.Add(lblUrl);

            btnDownload = new Button
            {
                Text = "▶ Download Now",
                Location = new Point(175, 68),
                Size = new Size(125, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDownload.Click += (s, e) =>
            {
                UserAccepted = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            borderPanel.Controls.Add(btnDownload);

            btnClose = new Button
            {
                Text = "Ignore",
                Location = new Point(306, 68),
                Size = new Size(60, 30),
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) =>
            {
                UserAccepted = false;
                this.Close();
            };
            borderPanel.Controls.Add(btnClose);

            autoCloseTimer = new Timer { Interval = 10000 };
            autoCloseTimer.Tick += (s, e) => { autoCloseTimer.Stop(); this.Close(); };
            autoCloseTimer.Start();
        }

        private void PositionAtBottomRight()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(workingArea.Right - this.Width - 20, workingArea.Bottom - this.Height - 20);
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            lblTitle.ForeColor = t.Accent;
            lblUrl.ForeColor = t.TextSecondary;

            btnDownload.BackColor = t.Accent;
            btnDownload.ForeColor = Color.Black;
            btnDownload.FlatStyle = FlatStyle.Flat;
            btnDownload.FlatAppearance.BorderSize = 0;

            btnClose.BackColor = t.CardBg;
            btnClose.ForeColor = t.TextPrimary;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = t.Border;
        }
    }
}
