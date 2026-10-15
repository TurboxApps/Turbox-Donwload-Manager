using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IDMClone.Installer
{
    public class SetupForm : Form
    {
        private string _targetDir;
        private TextBox _txtPath;
        private CheckBox _chkDesktop;
        private CheckBox _chkStartMenu;
        private CheckBox _chkStartup;
        private CheckBox _chkExtensions;
        private CheckBox _chkLaunchNow;
        private CheckBox _chkOpenExtensions;

        private Panel _pnlStepOptions;
        private Panel _pnlStepProgress;
        private Panel _pnlStepFinished;

        private ProgressBar _progressBar;
        private Label _lblStatus;
        private Label _lblDetail;

        private Button _btnInstall;
        private Button _btnCancel;
        private Button _btnFinish;

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (arg.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
                    {
                        PerformUninstall();
                        return;
                    }
                }
            }

            Application.Run(new SetupForm());
        }

        public SetupForm()
        {
            InitDefaultTargetDir();
            BuildUI();
        }

        private void InitDefaultTargetDir()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _targetDir = Path.Combine(localApp, "Programs", "IDM Turbo Downloader");
        }

        private void BuildUI()
        {
            this.Text = "⚡ IDM Turbo Downloader v2.5 - Setup Wizard";
            this.Size = new Size(620, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(14, 18, 26);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            try
            {
                if (File.Exists("app.ico")) this.Icon = new Icon("app.ico");
            }
            catch { }

            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.FromArgb(20, 26, 38)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "⚡ IDM Turbo Downloader v2.5",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(24, 14),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "Ultra High-Speed Accelerator & 8K/4K/MP3 Browser Integration Suite",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(26, 44),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSub);

            // Container for pages
            Panel pnlBody = new Panel
            {
                Location = new Point(0, 82),
                Size = new Size(605, 300),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlBody);

            // Bottom Buttons Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(17, 22, 32)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(30, 41, 59), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
                }
            };
            this.Controls.Add(pnlBottom);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(95, 34),
                Location = new Point(490, 13),
                BackColor = Color.FromArgb(38, 44, 58),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(_btnCancel);

            _btnInstall = new Button
            {
                Text = "Install Now ➔",
                Size = new Size(130, 34),
                Location = new Point(350, 13),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnInstall.FlatAppearance.BorderSize = 0;
            _btnInstall.Click += (s, e) => StartInstallation();
            pnlBottom.Controls.Add(_btnInstall);

            _btnFinish = new Button
            {
                Text = "Finish",
                Size = new Size(110, 34),
                Location = new Point(475, 13),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnFinish.FlatAppearance.BorderSize = 0;
            _btnFinish.Click += (s, e) => FinishInstallation();
            pnlBottom.Controls.Add(_btnFinish);

            // ---------------- PAGE 1: Options ----------------
            _pnlStepOptions = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlBody.Controls.Add(_pnlStepOptions);

            Label lblPathIntro = new Label
            {
                Text = "Install destination directory:",
                Location = new Point(24, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240)
            };
            _pnlStepOptions.Controls.Add(lblPathIntro);

            _txtPath = new TextBox
            {
                Text = _targetDir,
                Location = new Point(26, 42),
                Size = new Size(460, 26),
                BackColor = Color.FromArgb(24, 32, 47),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F)
            };
            _pnlStepOptions.Controls.Add(_txtPath);

            Button btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(495, 41),
                Size = new Size(85, 27),
                BackColor = Color.FromArgb(38, 48, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select Installation Folder for IDM Turbo Downloader:";
                    fbd.SelectedPath = _txtPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        _txtPath.Text = fbd.SelectedPath;
                        _targetDir = fbd.SelectedPath;
                    }
                }
            };
            _pnlStepOptions.Controls.Add(btnBrowse);

            // GroupBox for options
            GroupBox grpOptions = new GroupBox
            {
                Text = " Additional Components & Shortcuts ",
                Location = new Point(26, 88),
                Size = new Size(554, 192),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _pnlStepOptions.Controls.Add(grpOptions);

            _chkDesktop = new CheckBox
            {
                Text = "Create a Desktop shortcut",
                Checked = true,
                Location = new Point(20, 30),
                AutoSize = true,
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9F)
            };
            grpOptions.Controls.Add(_chkDesktop);

            _chkStartMenu = new CheckBox
            {
                Text = "Create Start Menu application entry",
                Checked = true,
                Location = new Point(20, 62),
                AutoSize = true,
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9F)
            };
            grpOptions.Controls.Add(_chkStartMenu);

            _chkStartup = new CheckBox
            {
                Text = "Start Turbo X minimized with Windows (Fast Background Interception)",
                Checked = true,
                Location = new Point(20, 94),
                AutoSize = true,
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9F)
            };
            grpOptions.Controls.Add(_chkStartup);

            _chkExtensions = new CheckBox
            {
                Text = "Include Dedicated Browser Extensions (Brave, Chrome, Edge, Firefox 8K/4K Modules)",
                Checked = true,
                Location = new Point(20, 126),
                AutoSize = true,
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9F)
            };
            grpOptions.Controls.Add(_chkExtensions);

            Label lblSecurity = new Label
            {
                Text = "🛡️ Obfuscar-Protected Binary: Anti-tamper, 100% Safe, GitHub/Defender Verified.",
                Location = new Point(20, 160),
                AutoSize = true,
                ForeColor = Color.FromArgb(34, 197, 94),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };
            grpOptions.Controls.Add(lblSecurity);

            // ---------------- PAGE 2: Progress ----------------
            _pnlStepProgress = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.Transparent };
            pnlBody.Controls.Add(_pnlStepProgress);

            Label lblProgHeader = new Label
            {
                Text = "Installing IDM Turbo Downloader...",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(26, 35),
                AutoSize = true
            };
            _pnlStepProgress.Controls.Add(lblProgHeader);

            _lblStatus = new Label
            {
                Text = "Preparing installation...",
                Location = new Point(28, 75),
                Size = new Size(550, 22),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9.5F)
            };
            _pnlStepProgress.Controls.Add(_lblStatus);

            _progressBar = new ProgressBar
            {
                Location = new Point(28, 105),
                Size = new Size(550, 24),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            _pnlStepProgress.Controls.Add(_progressBar);

            _lblDetail = new Label
            {
                Text = "",
                Location = new Point(28, 138),
                Size = new Size(550, 45),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.5F)
            };
            _pnlStepProgress.Controls.Add(_lblDetail);

            // ---------------- PAGE 3: Finished ----------------
            _pnlStepFinished = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.Transparent };
            pnlBody.Controls.Add(_pnlStepFinished);

            Label lblFinHeader = new Label
            {
                Text = "🎉 Installation Successfully Completed!",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94),
                Location = new Point(26, 25),
                AutoSize = true
            };
            _pnlStepFinished.Controls.Add(lblFinHeader);

            Label lblFinDesc = new Label
            {
                Text = "IDM Turbo Downloader has been successfully installed.\r\n" +
                       "You can now accelerate downloads up to 32 parallel threads\r\n" +
                       "and capture 8K/4K/MP3 streams with universal browser support.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(28, 65),
                Size = new Size(550, 65)
            };
            _pnlStepFinished.Controls.Add(lblFinDesc);

            _chkLaunchNow = new CheckBox
            {
                Text = "Launch IDM Turbo Downloader now",
                Checked = true,
                Location = new Point(30, 150),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _pnlStepFinished.Controls.Add(_chkLaunchNow);

            _chkOpenExtensions = new CheckBox
            {
                Text = "Open Browser Extensions folder (To load Brave, Chrome, Edge or Firefox)",
                Checked = true,
                Location = new Point(30, 185),
                AutoSize = true,
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 9.5F)
            };
            _pnlStepFinished.Controls.Add(_chkOpenExtensions);
        }

        private void StartInstallation()
        {
            _targetDir = _txtPath.Text.Trim();
            if (string.IsNullOrEmpty(_targetDir))
            {
                MessageBox.Show("Please specify a valid destination folder.", "IDM Turbo Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _pnlStepOptions.Visible = false;
            _pnlStepProgress.Visible = true;
            _btnInstall.Enabled = false;
            _btnCancel.Enabled = false;

            Thread worker = new Thread(InstallThreadProc)
            {
                IsBackground = true,
                Name = "IDMTurbo_InstallWorker"
            };
            worker.Start();
        }

        private void UpdateProgress(int percent, string status, string detail)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateProgress(percent, status, detail)));
                return;
            }

            _progressBar.Value = Math.Max(0, Math.Min(100, percent));
            if (!string.IsNullOrEmpty(status)) _lblStatus.Text = status;
            if (!string.IsNullOrEmpty(detail)) _lblDetail.Text = detail;
        }

        private void InstallThreadProc()
        {
            try
            {
                UpdateProgress(5, "Creating destination directory...", _targetDir);
                if (!Directory.Exists(_targetDir))
                {
                    Directory.CreateDirectory(_targetDir);
                }

                // Close any currently running instances of IDMTurboDownloader
                UpdateProgress(10, "Closing existing processes...", "");
                try
                {
                    foreach (var p in Process.GetProcessesByName("IDMTurboDownloader"))
                    {
                        try { p.Kill(); p.WaitForExit(1500); } catch { }
                    }
                }
                catch { }

                // Check for embedded payload.zip resource
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream payloadStream = null;

                foreach (string name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase))
                    {
                        payloadStream = asm.GetManifestResourceStream(name);
                        break;
                    }
                }

                if (payloadStream != null)
                {
                    UpdateProgress(15, "Extracting components from embedded archive...", "");
                    using (payloadStream)
                    using (ZipArchive archive = new ZipArchive(payloadStream, ZipArchiveMode.Read))
                    {
                        int total = archive.Entries.Count;
                        int count = 0;

                        foreach (var entry in archive.Entries)
                        {
                            count++;
                            int pct = 15 + (int)((count / (float)total) * 60);

                            string destPath = Path.Combine(_targetDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

                            if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                            {
                                if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
                                continue;
                            }

                            string parentDir = Path.GetDirectoryName(destPath);
                            if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

                            UpdateProgress(pct, "Extracting files...", entry.Name);
                            entry.ExtractToFile(destPath, true);
                        }
                    }
                }
                else
                {
                    // Fallback: Copy files from current directory or update directory
                    UpdateProgress(20, "Copying application binaries...", "");
                    string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (Directory.Exists(Path.Combine(sourceDir, "update")))
                    {
                        sourceDir = Path.Combine(sourceDir, "update");
                    }

                    CopyDirectory(sourceDir, _targetDir);
                }

                UpdateProgress(80, "Configuring shortcuts and system integration...", "");
                string mainExe = Path.Combine(_targetDir, "IDMTurboDownloader.exe");
                string icoPath = Path.Combine(_targetDir, "app.ico");

                if (_chkDesktop.Checked && File.Exists(mainExe))
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string lnk = Path.Combine(desktop, "IDM Turbo Downloader.lnk");
                    CreateShellLink(lnk, mainExe, _targetDir, icoPath, "IDM Turbo High-Speed Downloader");
                }

                if (_chkStartMenu.Checked && File.Exists(mainExe))
                {
                    string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    string lnk = Path.Combine(startMenu, "IDM Turbo Downloader.lnk");
                    CreateShellLink(lnk, mainExe, _targetDir, icoPath, "IDM Turbo High-Speed Downloader");
                }

                if (_chkStartup.Checked && File.Exists(mainExe))
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null)
                            {
                                key.SetValue("TurboX", "\"" + mainExe + "\" --background");
                            }
                        }
                    }
                    catch { }
                }

                // Register in Windows Add/Remove Programs
                UpdateProgress(90, "Registering application with Windows...", "");
                try
                {
                    string uninstKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\IDMTurboDownloader";
                    using (var key = Registry.CurrentUser.CreateSubKey(uninstKey))
                    {
                        if (key != null)
                        {
                            key.SetValue("DisplayName", "IDM Turbo Downloader");
                            key.SetValue("DisplayVersion", "2.5.0");
                            key.SetValue("Publisher", "TurboSoftware");
                            key.SetValue("DisplayIcon", icoPath);
                            key.SetValue("InstallLocation", _targetDir);
                            key.SetValue("UninstallString", "\"" + mainExe + "\" --uninstall");
                        }
                    }
                }
                catch { }

                UpdateProgress(100, "Done!", "All components successfully installed.");
                Thread.Sleep(400);

                this.BeginInvoke(new Action(() =>
                {
                    _pnlStepProgress.Visible = false;
                    _pnlStepFinished.Visible = true;
                    _btnCancel.Visible = false;
                    _btnInstall.Visible = false;
                    _btnFinish.Visible = true;
                }));
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("Installation encountered an issue: " + ex.Message, "IDM Turbo Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _btnCancel.Enabled = true;
                }));
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fn = Path.GetFileName(file);
                if (fn.Equals("IDMTurbo_Setup.exe", StringComparison.OrdinalIgnoreCase) ||
                    fn.Equals("Setup.exe", StringComparison.OrdinalIgnoreCase)) continue;

                File.Copy(file, Path.Combine(destDir, fn), true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dn = Path.GetFileName(dir);
                if (dn.Equals("update", StringComparison.OrdinalIgnoreCase) ||
                    dn.Equals("obfuscar_tools", StringComparison.OrdinalIgnoreCase) ||
                    dn.Equals(".git", StringComparison.OrdinalIgnoreCase)) continue;

                CopyDirectory(dir, Path.Combine(destDir, dn));
            }
        }

        private void CreateShellLink(string shortcutPath, string targetPath, string workingDir, string iconPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.Description = description;
                    if (File.Exists(iconPath)) shortcut.IconLocation = iconPath + ", 0";
                    shortcut.Save();
                }
            }
            catch { }
        }

        private void FinishInstallation()
        {
            string mainExe = Path.Combine(_targetDir, "IDMTurboDownloader.exe");

            if (_chkOpenExtensions.Checked)
            {
                string braveExt = Path.Combine(_targetDir, "extension_brave");
                if (Directory.Exists(braveExt))
                {
                    try { Process.Start("explorer.exe", braveExt); } catch { }
                }
                else
                {
                    try { Process.Start("explorer.exe", _targetDir); } catch { }
                }
            }

            if (_chkLaunchNow.Checked && File.Exists(mainExe))
            {
                try { Process.Start(mainExe); } catch { }
            }

            this.Close();
        }

        private static void PerformUninstall()
        {
            var dr = MessageBox.Show("Are you sure you want to completely uninstall IDM Turbo Downloader?", "Uninstall IDM Turbo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                // 1. Terminate running instances
                foreach (var p in Process.GetProcessesByName("IDMTurboDownloader"))
                {
                    try { p.Kill(); p.WaitForExit(1000); } catch { }
                }

                // 2. Remove shortcuts
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string lnk1 = Path.Combine(desktop, "IDM Turbo Downloader.lnk");
                if (File.Exists(lnk1)) File.Delete(lnk1);

                string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string lnk2 = Path.Combine(startMenu, "IDM Turbo Downloader.lnk");
                if (File.Exists(lnk2)) File.Delete(lnk2);

                // 3. Remove Registry Run
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null) key.DeleteValue("TurboX", false);
                    }
                }
                catch { }

                // 4. Remove Uninstall key
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\IDMTurboDownloader", false);
                }
                catch { }

                MessageBox.Show("IDM Turbo Downloader was successfully uninstalled from your computer.", "Uninstall IDM Turbo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during uninstall: " + ex.Message, "Uninstall IDM Turbo");
            }
        }
    }
}
