using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace IDMClone
{
    public class AboutDialog : Form
    {
        private Panel pnlCard;
        private Label lblTitle;
        private Label lblVersion;
        private Label lblDeveloper;
        private Label lblSubtitle;
        private GroupBox grpFeatures;
        private Label lblFeatures;
        private GroupBox grpSupport;
        private Button btnTelegram;
        private Button btnFacebook;
        private Button btnOk;

        public AboutDialog()
        {
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "About IDM Turbo Downloader";
            this.Size = new Size(500, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);
            this.ShowIcon = false;

            pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            this.Controls.Add(pnlCard);

            int y = 16;

            // App Title
            lblTitle = new Label
            {
                Text = "⚡ IDM Turbo Downloader",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(440, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblTitle);
            y += 34;

            // Version
            lblVersion = new Label
            {
                Text = "Version 6.43 (Build 32 - Unlimited Turbo)",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Location = new Point(20, y),
                Size = new Size(440, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblVersion);
            y += 24;

            // Developer Credit
            lblDeveloper = new Label
            {
                Text = "💻 Developed by Abir",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(440, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblDeveloper);
            y += 28;

            lblSubtitle = new Label
            {
                Text = "High-Speed Multi-Threaded Download Accelerator & Media Engine",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Location = new Point(20, y),
                Size = new Size(440, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblSubtitle);
            y += 28;

            // Features Box
            grpFeatures = new GroupBox
            {
                Text = "Core Capabilities",
                Location = new Point(24, y),
                Size = new Size(436, 120),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            pnlCard.Controls.Add(grpFeatures);

            lblFeatures = new Label
            {
                Text = "• 32-Thread Parallel Turbo Acceleration Engine\n" +
                       "• Universal H.264 Playable Video Downloader (Any Site)\n" +
                       "• Automatic Clipboard URL Detection & Instant Prompt\n" +
                       "• High-Contrast Professional Themes & High-DPI Display Support\n" +
                       "• Smart Download Scheduler & Auto-Shutdown Support",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Location = new Point(16, 22),
                Size = new Size(405, 90)
            };
            grpFeatures.Controls.Add(lblFeatures);
            y += 130;

            // Support Box (Telegram and Facebook buttons ONLY - No @abrno34 text!)
            grpSupport = new GroupBox
            {
                Text = "Official Support & Community",
                Location = new Point(24, y),
                Size = new Size(436, 75),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            pnlCard.Controls.Add(grpSupport);

            btnTelegram = new Button
            {
                Text = "✈️ Telegram",
                Location = new Point(30, 25),
                Size = new Size(170, 36),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTelegram.Click += (s, e) => OpenLink("https://t.me/abrno34");
            grpSupport.Controls.Add(btnTelegram);

            btnFacebook = new Button
            {
                Text = "📘 Facebook",
                Location = new Point(236, 25),
                Size = new Size(170, 36),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFacebook.Click += (s, e) => OpenLink("https://facebook.com/abrno34");
            grpSupport.Controls.Add(btnFacebook);
            y += 85;

            // OK Button
            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(190, y),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) => this.Close();
            pnlCard.Controls.Add(btnOk);

            this.AcceptButton = btnOk;
        }

        private void OpenLink(string url)
        {
            try { Process.Start(url); } catch { }
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            pnlCard.BackColor = t.WindowBg;
            lblTitle.ForeColor = t.Accent;
            lblVersion.ForeColor = t.TextSecondary;
            lblDeveloper.ForeColor = t.AccentSecondary;
            lblSubtitle.ForeColor = t.TextSecondary;

            grpFeatures.BackColor = t.CardBg;
            grpFeatures.ForeColor = t.Accent;
            lblFeatures.ForeColor = t.TextPrimary;

            grpSupport.BackColor = t.CardBg;
            grpSupport.ForeColor = t.Accent;

            btnTelegram.BackColor = Color.FromArgb(14, 165, 233);
            btnTelegram.ForeColor = Color.White;
            btnTelegram.FlatStyle = FlatStyle.Flat;
            btnTelegram.FlatAppearance.BorderSize = 0;

            btnFacebook.BackColor = Color.FromArgb(59, 130, 246);
            btnFacebook.ForeColor = Color.White;
            btnFacebook.FlatStyle = FlatStyle.Flat;
            btnFacebook.FlatAppearance.BorderSize = 0;

            btnOk.BackColor = t.Accent;
            btnOk.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
        }
    }
}
