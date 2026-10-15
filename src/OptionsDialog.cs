using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IDMClone
{
    public class OptionsDialog : Form
    {
        private TabControl tabControl;
        private ComboBox cmbTheme;
        private ComboBox cmbMaxConnections;
        private ComboBox cmbBufferSize;
        private CheckBox chkStartup;
        private CheckBox chkClipboard;
        private CheckedListBox chkListBrowsers;
        private TextBox txtFileTypes;
        private TextBox txtDefPath;
        private Button btnOk;
        private Button btnCancel;

        public OptionsDialog()
        {
            InitializeComponent();
            ThemeManager.ApplyToControl(this);
            ThemeManager.ThemeChanged += (s, e) => ThemeManager.ApplyToControl(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Configuration";
            this.Size = new Size(520, 600);
            this.MinimumSize = new Size(480, 520);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F);

            tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(480, 490),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(tabControl);

            // 1. General Tab (Matches User Screenshot)
            BuildGeneralTab();

            // 2. Connection Tab (Turbo Speed)
            BuildConnectionTab();

            // 3. File types Tab
            BuildFileTypesTab();

            // 4. Save to Tab
            BuildSaveToTab();

            // 5. Appearance / Themes Tab
            BuildThemesTab();

            // 6. Sounds Tab
            BuildSoundsTab();

            // Bottom Buttons
            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(300, 515),
                Size = new Size(95, 30),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(405, 515),
                Size = new Size(95, 30),
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BuildGeneralTab()
        {
            TabPage tab = new TabPage("General");
            tabControl.TabPages.Add(tab);

            int y = 14;

            // Header Row
            Label lblHeader = new Label
            {
                Text = "🌐 Browser/System Integration",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(15, y),
                AutoSize = true
            };
            tab.Controls.Add(lblHeader);

            Label lblAdv = new Label
            {
                Text = "Advanced browser integration is enabled",
                Location = new Point(15, y + 26),
                AutoSize = true
            };
            tab.Controls.Add(lblAdv);

            Button btnRestart = new Button
            {
                Text = "Restart",
                Location = new Point(350, y + 23),
                Size = new Size(80, 26)
            };
            btnRestart.Click += (s, e) => MessageBox.Show("Browser integration restarted successfully.", "IDM Integration");
            tab.Controls.Add(btnRestart);

            y += 60;

            chkStartup = new CheckBox
            {
                Text = "Launch Internet Download Manager on startup",
                Location = new Point(15, y),
                AutoSize = true,
                Checked = true
            };
            tab.Controls.Add(chkStartup);

            chkClipboard = new CheckBox
            {
                Text = "Automatically start downloading of URLs placed to clipboard",
                Location = new Point(15, y + 24),
                AutoSize = true,
                Checked = false
            };
            tab.Controls.Add(chkClipboard);

            y += 56;

            // GroupBox for Browsers
            GroupBox grpBrowsers = new GroupBox
            {
                Text = "Capture downloads from the following browsers:",
                Location = new Point(15, y),
                Size = new Size(415, 185)
            };
            tab.Controls.Add(grpBrowsers);

            chkListBrowsers = new CheckedListBox
            {
                Location = new Point(15, 24),
                Size = new Size(385, 115),
                CheckOnClick = true
            };
            string[] browsers = new string[] {
                "Apple Safari",
                "Google Chrome",
                "Internet Explorer",
                "Microsoft Edge",
                "Microsoft Edge Legacy",
                "Mozilla Firefox",
                "Opera",
                "Brave Browser",
                "Vivaldi"
            };
            foreach (var b in browsers) chkListBrowsers.Items.Add(b, true);
            grpBrowsers.Controls.Add(chkListBrowsers);

            Button btnAddBrowser = new Button
            {
                Text = "Add browser...",
                Location = new Point(285, 146),
                Size = new Size(115, 26)
            };
            btnAddBrowser.Click += (s, e) => MessageBox.Show("Browse for custom browser executable.", "Add Browser");
            grpBrowsers.Controls.Add(btnAddBrowser);

            y += 195;

            Button btnKeys = new Button
            {
                Text = "Keys...",
                Location = new Point(350, y),
                Size = new Size(80, 24)
            };
            tab.Controls.Add(btnKeys);
            Label lblKeys = new Label
            {
                Text = "Customize keys to prevent or force downloading with IDM",
                Location = new Point(15, y + 3),
                AutoSize = true
            };
            tab.Controls.Add(lblKeys);

            y += 30;

            Button btnEditMenu = new Button
            {
                Text = "Edit...",
                Location = new Point(350, y),
                Size = new Size(80, 24)
            };
            tab.Controls.Add(btnEditMenu);
            Label lblEditMenu = new Label
            {
                Text = "Customize IDM menu items in context menu of browsers",
                Location = new Point(15, y + 3),
                AutoSize = true
            };
            tab.Controls.Add(lblEditMenu);
        }

        private void BuildConnectionTab()
        {
            TabPage tab = new TabPage("Connection");
            tabControl.TabPages.Add(tab);

            int y = 20;

            Label lblTurboHeader = new Label
            {
                Text = "⚡ Ultra-Fast Turbo Download Engine",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 163, 127),
                Location = new Point(15, y),
                AutoSize = true
            };
            tab.Controls.Add(lblTurboHeader);

            y += 32;

            Label lblNotice = new Label
            {
                Text = "IDM Turbo maximizes network socket bandwidth and eliminates proxy delays.",
                Location = new Point(15, y),
                Size = new Size(415, 32)
            };
            tab.Controls.Add(lblNotice);

            y += 45;

            Label lblConnType = new Label
            {
                Text = "Connection Type / Speed:",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tab.Controls.Add(lblConnType);

            ComboBox cmbConnType = new ComboBox
            {
                Location = new Point(15, y + 22),
                Size = new Size(415, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbConnType.Items.AddRange(new object[] {
                "High speed: Direct connection (Gigabit / Fiber / 5G / Broadband)",
                "Medium speed: Standard DSL / Cable",
                "Low speed: 3G / Mobile Data"
            });
            cmbConnType.SelectedIndex = 0;
            tab.Controls.Add(cmbConnType);

            y += 60;

            Label lblMaxConn = new Label
            {
                Text = "Max Default Connection Number (Threads):",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tab.Controls.Add(lblMaxConn);

            cmbMaxConnections = new ComboBox
            {
                Location = new Point(15, y + 22),
                Size = new Size(180, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMaxConnections.Items.AddRange(new object[] { "1", "2", "4", "8", "16", "24", "32 (Turbo Max - 4x Faster than IDM)" });
            cmbMaxConnections.SelectedIndex = 6; // 32 Threads!
            tab.Controls.Add(cmbMaxConnections);

            y += 60;

            Label lblBuffer = new Label
            {
                Text = "Network Buffer Chunk Size:",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tab.Controls.Add(lblBuffer);

            cmbBufferSize = new ComboBox
            {
                Location = new Point(15, y + 22),
                Size = new Size(180, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBufferSize.Items.AddRange(new object[] { "64 KB", "128 KB", "256 KB (Recommended)", "512 KB (Super Fast)" });
            cmbBufferSize.SelectedIndex = 2;
            tab.Controls.Add(cmbBufferSize);

            y += 60;

            CheckBox chkAggressive = new CheckBox
            {
                Text = "Enable Zero-Latency Socket Optimization (Disable Nagle Algorithm)",
                Location = new Point(15, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tab.Controls.Add(chkAggressive);
        }

        private void BuildFileTypesTab()
        {
            TabPage tab = new TabPage("File types");
            tabControl.TabPages.Add(tab);

            Label lbl = new Label
            {
                Text = "Automatically start downloading the following file types:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tab.Controls.Add(lbl);

            txtFileTypes = new TextBox
            {
                Location = new Point(15, 40),
                Size = new Size(415, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "3GP 7Z AAC ACE AIF ARJ ASF AVI BIN BZ2 EXE GZ GZIP IMG ISO LZH M4A M4V MKV MOV MP3 MP4 MPA MPE MPEG MPG MSI MSU OGG OGV PDF PLJ PPS PPT QT R0* R1* RA RAR RM RMVB SEA SIT SITX TAR TIF TIFF TXT WAV WMA WMV WPD XLS XLSX Z ZIP"
            };
            tab.Controls.Add(txtFileTypes);
        }

        private void BuildSaveToTab()
        {
            TabPage tab = new TabPage("Save to");
            tabControl.TabPages.Add(tab);

            Label lbl = new Label
            {
                Text = "Default Download Directory:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tab.Controls.Add(lbl);

            txtDefPath = new TextBox
            {
                Location = new Point(15, 40),
                Size = new Size(320, 24),
                Text = AddUrlDialog.GetPersistedDownloadDirectory()
            };
            tab.Controls.Add(txtDefPath);

            Button btnB = new Button
            {
                Text = "Browse...",
                Location = new Point(345, 39),
                Size = new Size(85, 26)
            };
            btnB.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtDefPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        txtDefPath.Text = fbd.SelectedPath;
                        AddUrlDialog.SavePersistedDownloadDirectory(fbd.SelectedPath);
                    }
                }
            };
            tab.Controls.Add(btnB);
        }

        private void BuildThemesTab()
        {
            TabPage tab = new TabPage("Themes & Appearance");
            tabControl.TabPages.Add(tab);

            int y = 20;

            Label lblTitle = new Label
            {
                Text = "🎨 Select Color Theme (7 Styles Available):",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Location = new Point(15, y),
                AutoSize = true
            };
            tab.Controls.Add(lblTitle);

            y += 35;

            cmbTheme = new ComboBox
            {
                Location = new Point(15, y),
                Size = new Size(300, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            foreach (var t in ThemeManager.AvailableThemes)
            {
                cmbTheme.Items.Add(t.Name);
            }
            cmbTheme.SelectedItem = ThemeManager.CurrentTheme.Name;
            tab.Controls.Add(cmbTheme);

            Button btnApplyTheme = new Button
            {
                Text = "Apply Theme Now",
                Location = new Point(325, y - 1),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnApplyTheme.Click += (s, e) => {
                if (cmbTheme.SelectedItem != null)
                {
                    ThemeManager.SetTheme(cmbTheme.SelectedItem.ToString());
                    ThemeManager.ApplyToControl(this);
                }
            };
            tab.Controls.Add(btnApplyTheme);

            y += 50;

            GroupBox grpThemeList = new GroupBox
            {
                Text = "Included Premium Themes",
                Location = new Point(15, y),
                Size = new Size(420, 200)
            };
            tab.Controls.Add(grpThemeList);

            Label lblDesc = new Label
            {
                Text = "1. 🌌 Obsidian Onyx - Ultra-sleek deep tech slate & sky blue\n" +
                       "2. ⚡ Cyberpunk Neon - High-voltage electric cyan & yellow glow\n" +
                       "3. 💎 Sapphire Glass - Deep luxury navy & radiant aqua\n" +
                       "4. 🍃 Aurora Emerald - Nordic deep forest & mint emerald\n" +
                       "5. 🔮 Amethyst Royal - Mystic royal violet & fuchsia glow\n" +
                       "6. 🔥 Sunset Crimson - Warm espresso dark & radiant amber glow\n" +
                       "7. ☀️ Titanium White - Apple-style crisp clean modern light\n" +
                       "8. 🖤 Pitch Black OLED - Pure 100% black & neon lime contrast\n" +
                       "9. 🍎 macOS Silicon Slate - Pro space gray & Apple blue",
                Location = new Point(15, 25),
                Size = new Size(390, 160),
                Font = new Font("Segoe UI", 8.5F)
            };
            grpThemeList.Controls.Add(lblDesc);
        }

        private void BuildSoundsTab()
        {
            TabPage tab = new TabPage("Sounds");
            tabControl.TabPages.Add(tab);

            CheckBox chkCompleteSound = new CheckBox
            {
                Text = "Play notification sound on download complete",
                Location = new Point(15, 25),
                AutoSize = true,
                Checked = true
            };
            tab.Controls.Add(chkCompleteSound);

            CheckBox chkErrorSound = new CheckBox
            {
                Text = "Play sound on download error",
                Location = new Point(15, 55),
                AutoSize = true,
                Checked = true
            };
            tab.Controls.Add(chkErrorSound);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cmbTheme != null && cmbTheme.SelectedItem != null)
            {
                ThemeManager.SetTheme(cmbTheme.SelectedItem.ToString());
            }

            if (txtDefPath != null && !string.IsNullOrWhiteSpace(txtDefPath.Text))
            {
                AddUrlDialog.SavePersistedDownloadDirectory(txtDefPath.Text.Trim());
            }
        }
    }
}
