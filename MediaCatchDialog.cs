using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IDMClone
{
    public class SniffedMediaItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public string Extension { get; set; }
        public string Quality { get; set; }
    }

    public class MediaCatchDialog : Form
    {
        public SniffedMediaItem SelectedItem { get; private set; }

        private ModernListView lvMedia;
        private Button btnDownload;
        private Button btnCopy;
        private Button btnClose;
        private Label lblHeader;
        private Label lblSub;
        private List<SniffedMediaItem> _items;

        public MediaCatchDialog(List<SniffedMediaItem> items, string pageUrl)
        {
            _items = items ?? new List<SniffedMediaItem>();
            InitializeComponent(pageUrl);
            PopulateList();
            ApplyTheme();
        }

        private void InitializeComponent(string pageUrl)
        {
            this.Text = "⚡ 1DM Media Sniffer & Stream Catcher";
            this.Size = new Size(780, 520);
            this.MinimumSize = new Size(650, 400);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(16, 10, 16, 8)
            };
            this.Controls.Add(pnlTop);

            lblHeader = new Label
            {
                Text = string.Format("⚡ 1DM Sniffer: Found {0} Downloadable Media Streams", _items.Count),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(14, 8),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblHeader);

            lblSub = new Label
            {
                Text = "Detected from: " + (pageUrl.Length > 80 ? pageUrl.Substring(0, 80) + "..." : pageUrl),
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(16, 35),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblSub);

            // List View
            lvMedia = new ModernListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                Font = new Font("Segoe UI", 9.25F),
                BorderStyle = BorderStyle.FixedSingle
            };
            lvMedia.Columns.Add("Media Title / Stream Name", 320);
            lvMedia.Columns.Add("Type", 95);
            lvMedia.Columns.Add("Format", 75);
            lvMedia.Columns.Add("Quality / Type", 110);
            lvMedia.Columns.Add("Direct URL", 320);

            lvMedia.DoubleClick += (s, e) => DownloadSelected();
            this.Controls.Add(lvMedia);

            // Bottom action panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(16, 10, 16, 10)
            };
            this.Controls.Add(pnlBottom);

            btnDownload = new Button
            {
                Text = "⚡ Download Selected",
                Size = new Size(170, 34),
                Location = new Point(16, 10),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += (s, e) => DownloadSelected();
            pnlBottom.Controls.Add(btnDownload);

            btnCopy = new Button
            {
                Text = "📋 Copy Link",
                Size = new Size(110, 34),
                Location = new Point(194, 10),
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (s, e) =>
            {
                if (lvMedia.SelectedItems.Count > 0)
                {
                    SniffedMediaItem item = lvMedia.SelectedItems[0].Tag as SniffedMediaItem;
                    if (item != null)
                    {
                        Clipboard.SetText(item.Url);
                        MessageBox.Show("Media URL copied to clipboard!", "1DM Sniffer");
                    }
                }
            };
            pnlBottom.Controls.Add(btnCopy);

            btnClose = new Button
            {
                Text = "Close",
                Size = new Size(95, 34),
                Location = new Point(pnlBottom.Width - 110, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);
        }

        private void PopulateList()
        {
            lvMedia.Items.Clear();
            foreach (var itm in _items)
            {
                ListViewItem lvi = new ListViewItem(itm.Title);
                lvi.SubItems.Add(itm.Category);
                lvi.SubItems.Add(itm.Extension);
                lvi.SubItems.Add(itm.Quality ?? "Original");
                lvi.SubItems.Add(itm.Url);
                lvi.Tag = itm;

                var t = ThemeManager.CurrentTheme;
                lvi.ForeColor = t.ListText;
                lvMedia.Items.Add(lvi);
            }

            if (lvMedia.Items.Count > 0)
            {
                lvMedia.Items[0].Selected = true;
            }
        }

        private void DownloadSelected()
        {
            if (lvMedia.SelectedItems.Count == 0) return;
            SelectedItem = lvMedia.SelectedItems[0].Tag as SniffedMediaItem;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            lvMedia.BackColor = t.ListBg;
            lvMedia.ForeColor = t.ListText;

            lblHeader.ForeColor = t.Accent;
            lblSub.ForeColor = t.TextSecondary;

            btnDownload.BackColor = t.Accent;
            btnDownload.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;

            btnCopy.BackColor = t.ButtonBg;
            btnCopy.ForeColor = t.ButtonText;

            btnClose.BackColor = t.ButtonBg;
            btnClose.ForeColor = t.ButtonText;
        }
    }
}
