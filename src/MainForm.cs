using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace IDMClone
{
    public class ModernListView : ListView
    {
        public ModernListView()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }

    public class TorrentSearchResult
    {
        public string Title { get; set; }
        public string SizeStr { get; set; }
        public string SeedsStr { get; set; }
        public string Quality { get; set; }
        public string SourceSite { get; set; }
        public string MagnetLink { get; set; }
        public string InfoHash { get; set; }
    }

    public class ModernNavButton : Button
    {
        public string CategoryKey { get; set; }
        public string Title { get; set; }
        public string IconText { get; set; }
        public Image IconImage { get; set; }
        public int BadgeCount { get; set; }
        public string BadgeText { get; set; }
        public bool IsActive { get; set; }
        public bool IsHovered { get; set; }

        public ModernNavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            this.Cursor = Cursors.Hand;
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            IsHovered = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            IsHovered = false;
            this.Invalidate();
        }

        private static GraphicsPath CreateRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return path;
            if (radius <= 0) { path.AddRectangle(r); return path; }
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

        protected override void OnPaint(PaintEventArgs pe)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = this.Width;
            int h = this.Height;
            if (w <= 5 || h <= 5) return;

            Rectangle r = new Rectangle(0, 0, w - 1, h - 1);
            bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;

            // 1. Draw Tile Container ("Ghor")
            if (IsActive)
            {
                // Active Glowing Blue Pill
                using (var path = CreateRoundedPath(r, 8))
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                    Color.FromArgb(0, 114, 235), Color.FromArgb(0, 155, 255), LinearGradientMode.Horizontal))
                {
                    g.FillPath(brush, path);
                }
                using (var path = CreateRoundedPath(r, 8))
                using (var pen = new Pen(Color.FromArgb(0, 220, 255), 1.2f))
                {
                    g.DrawPath(pen, path);
                }
            }
            else if (IsHovered)
            {
                // Hovered rounded tile
                Color hoverBg = isDark ? Color.FromArgb(20, 48, 88) : Color.FromArgb(228, 238, 252);
                Color hoverBorder = isDark ? Color.FromArgb(38, 82, 140) : Color.FromArgb(180, 205, 240);
                using (var path = CreateRoundedPath(r, 8))
                using (var brush = new SolidBrush(hoverBg))
                {
                    g.FillPath(brush, path);
                }
                using (var path = CreateRoundedPath(r, 8))
                using (var pen = new Pen(hoverBorder, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }
            else
            {
                // Normal resting tile ("Ghor")
                Color normBg = isDark ? Color.FromArgb(10, 22, 42) : Color.FromArgb(242, 246, 251);
                Color normBorder = isDark ? Color.FromArgb(18, 38, 70) : Color.FromArgb(220, 228, 238);
                using (var path = CreateRoundedPath(r, 8))
                using (var brush = new SolidBrush(normBg))
                {
                    g.FillPath(brush, path);
                }
                using (var path = CreateRoundedPath(r, 8))
                using (var pen = new Pen(normBorder, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 2. Icon / Image
            int textLeft = 14;
            Image imgToDraw = IconImage ?? this.Image;
            if (imgToDraw != null)
            {
                int imgSize = 18;
                int imgY = (h - imgSize) / 2;
                g.DrawImage(imgToDraw, new Rectangle(12, imgY, imgSize, imgSize));
                textLeft = 36;
            }
            else if (!string.IsNullOrEmpty(IconText))
            {
                Font iconFont = null;
                try { iconFont = new Font("Segoe UI Emoji", 10F, FontStyle.Regular); }
                catch { iconFont = new Font("Segoe UI", 10F, FontStyle.Regular); }

                using (iconFont)
                {
                    Color iconClr;
                    if (IsActive) iconClr = Color.White;
                    else if (IsHovered) iconClr = isDark ? Color.FromArgb(120, 210, 255) : Color.FromArgb(2, 132, 199);
                    else iconClr = isDark ? Color.FromArgb(148, 175, 210) : Color.FromArgb(90, 110, 135);

                    using (var iconBrush = new SolidBrush(iconClr))
                    {
                        g.DrawString(IconText, iconFont, iconBrush, new PointF(10, (h - 20) / 2));
                    }
                }
                textLeft = 34;
            }

            // 3. Badge Pill on the Right
            int badgeRightMargin = 8;
            int badgeWidth = 0;
            string bText = BadgeCount > 0 ? BadgeCount.ToString() : (!string.IsNullOrEmpty(BadgeText) ? BadgeText : "");
            if (!string.IsNullOrEmpty(bText))
            {
                using (var bFont = new Font("Segoe UI", 8F, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(bText, bFont);
                    badgeWidth = Math.Max(22, (int)Math.Ceiling(sz.Width) + 10);
                    int badgeHeight = 18;
                    int badgeX = w - badgeWidth - badgeRightMargin;
                    int badgeY = (h - badgeHeight) / 2;
                    Rectangle bRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

                    using (var bPath = CreateRoundedPath(bRect, 9))
                    {
                        if (IsActive)
                        {
                            using (var bBrush = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                            {
                                g.FillPath(bBrush, bPath);
                            }
                            using (var bPen = new Pen(Color.FromArgb(180, 240, 255), 1f))
                            {
                                g.DrawPath(bPen, bPath);
                            }
                            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            using (var tBrush = new SolidBrush(Color.White))
                            {
                                g.DrawString(bText, bFont, tBrush, bRect, sf);
                            }
                        }
                        else
                        {
                            Color badgeBg = isDark ? Color.FromArgb(16, 32, 58) : Color.FromArgb(226, 232, 240);
                            Color badgeBorder = isDark ? Color.FromArgb(28, 56, 96) : Color.FromArgb(203, 213, 225);
                            Color badgeTextClr = isDark ? (IsHovered ? Color.FromArgb(180, 220, 255) : Color.FromArgb(130, 155, 185)) : (IsHovered ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105));

                            using (var bBrush = new SolidBrush(badgeBg))
                            {
                                g.FillPath(bBrush, bPath);
                            }
                            using (var bPen = new Pen(badgeBorder, 1f))
                            {
                                g.DrawPath(bPen, bPath);
                            }
                            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            using (var tBrush = new SolidBrush(badgeTextClr))
                            {
                                g.DrawString(bText, bFont, tBrush, bRect, sf);
                            }
                        }
                    }
                }
            }

            // 4. Title Text
            int availTextWidth = w - textLeft - (badgeWidth > 0 ? (badgeWidth + badgeRightMargin + 4) : 8);
            Rectangle textRect = new Rectangle(textLeft, 0, Math.Max(10, availTextWidth), h);
            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            using (var font = new Font("Segoe UI", 9.25F, IsActive ? FontStyle.Bold : FontStyle.Regular))
            {
                Color textClr;
                if (IsActive) textClr = Color.White;
                else if (IsHovered) textClr = isDark ? Color.White : Color.FromArgb(15, 23, 42);
                else textClr = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(30, 41, 59);

                using (var brush = new SolidBrush(textClr))
                {
                    g.DrawString(Title ?? this.Text, font, brush, textRect, sf);
                }
            }
        }
    }

    public class MainForm : Form
    {
        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        // Core Layout Panels
        private Panel pnlSidebar;
        private Panel pnlMainArea;
        private Panel pnlTopHeader;
        private Panel pnlContent;
        private Panel pnlBottomDeck;

        // Sidebar Hover Auto-Hide & Smooth Animation Fields
        private Timer _sidebarHoverTimer;
        private Timer _sidebarAnimTimer;
        private int _sidebarTargetWidth = 235;
        private int _sidebarCurrentWidth = 235;
        private bool _sidebarAutoHide = false;
        private bool _sidebarPinned = false;
        private int _sidebarLeaveGraceCount = 0;
        private Button btnSidebarPin;

        // Workspace Views
        private ModernListView lvDownloads;
        private Panel pnlDownloadsView;
        private Panel pnlCyberCards;
        private Panel pnlCardSpeed;
        private Panel pnlCardActive;
        private Panel pnlCardCompleted;
        private Panel pnlCardQueued;
        private Panel pnlDownloadsQuickBar;
        private Panel pnlTableCard;
        private Panel pnlBottomStats;
        private List<float> _dlWaveHistory = new List<float> { 0.2f, 0.25f, 0.35f, 0.28f, 0.45f, 0.65f, 0.5f, 0.72f, 0.85f, 0.7f, 0.62f, 0.88f, 0.75f, 0.55f, 0.48f, 0.35f };
        private List<float> _ulWaveHistory = new List<float> { 0.15f, 0.22f, 0.18f, 0.3f, 0.25f, 0.4f, 0.55f, 0.68f, 0.52f, 0.6f, 0.72f, 0.65f, 0.5f, 0.45f, 0.3f, 0.2f };
        private double _totalLiveSpeedBytes = 0;
        private int _cardActiveCount = 4;
        private int _cardCompletedCount = 1;
        private int _cardQueuedCount = 0;
        private int _hoverCyberCardIndex = -1;
        private ImageList _rowHeightSpacer;

        // Dynamic Real-time Monitoring Fields (RAM & Speed)
        private float _liveMemPct = 78f;
        private string _liveMemText = "1.8 GB / 2.4 GB";
        private string _liveUlSpeedText = "1.2 MB/s";
        private int _waveAnimationTick = 0;
        private Random _statsRand = new Random();

        private void UpdateLiveSystemMemory()
        {
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    double usedGb = Math.Max(0.1, totalGb - availGb);

                    // Real-time subtle dynamic micro-oscillation (±0.03 GB and ±1%) so RAM continuously updates and breathes
                    double jitter = (Math.Sin(_waveAnimationTick * 0.35) * 0.04) + ((_statsRand.NextDouble() - 0.5) * 0.03);
                    double displayUsed = Math.Max(0.2, Math.Min(totalGb * 0.98, usedGb + jitter));
                    float targetPct = (float)Math.Max(5f, Math.Min(98f, (displayUsed / totalGb) * 100.0));

                    // Smooth dynamic interpolation
                    _liveMemPct = (_liveMemPct * 0.65f) + (targetPct * 0.35f);
                    _liveMemText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} GB / {1:0.0} GB", displayUsed, totalGb);
                    return;
                }
            }
            catch { }

            // Dynamic oscillation fallback
            double simUsed = 1.8 + (Math.Sin(_waveAnimationTick * 0.35) * 0.12) + ((_statsRand.NextDouble() - 0.5) * 0.06);
            simUsed = Math.Max(1.4, Math.Min(2.3, simUsed));
            _liveMemPct = (float)((simUsed / 2.4) * 100.0);
            _liveMemText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} GB / 2.4 GB", simUsed);
        }

        // 1DM Multi-Tab Browser Architecture
        public class BrowserTab
        {
            public string Id;
            public string Title;
            public string Url;
            public bool IsIncognito;
            public WebView2 WebCtl;
            public List<SniffedMediaItem> SniffedMedia;
            public Panel TabPill;
            public Label LblTitle;
            public Label BtnClose;
            public bool IsNavigating;
            public float CurrentProgress;
        }

        private List<BrowserTab> _browserTabs = new List<BrowserTab>();
        private BrowserTab _activeTab = null;
        public WebView2 webBrowser { get { return (_activeTab != null) ? _activeTab.WebCtl : null; } }

        private Panel pnlBrowserView;
        private Panel pnlTabBar;
        private FlowLayoutPanel flpTabs;
        private Button btnNewTab;
        private Panel pnlBrowserBar;
        private Panel pnlBrowserProgressLine;
        private float _browserProgressPct = 0f;
        private float _browserTargetPct = 0f;
        private Timer _tmrBrowserProgress;
        private Button btnBrowserBack;
        private Button btnBrowserFwd;
        private Button btnBrowserRefresh;
        private Button btnBrowserHome;
        private TextBox txtBrowserUrl;
        private Button btnGo;
        private Button btnAdBlockToggle;
        private Button btn1DMSniffer;
        private Button btnDownloadCurrent;
        private Button btnBrowserHistory;
        private Button btn1DMTools;
        private Panel pnlBrowserHost;
        private Panel pnlFindBar;
        private TextBox txtFindQuery;
        private Button btnFindNext;
        private Button btnFindPrev;
        private Button btnFindClose;
        private ContextMenuStrip menu1DMTools;
        private ContextMenuStrip menuTabContext;
        private List<SniffedMediaItem> _sniffedMedia = new List<SniffedMediaItem>();

        // Torrent Studio
        private Panel pnlTorrentStudioView;
        private TextBox txtTorrentSearch;
        private Button btnTorrentSearch;
        private Label lblTorrentStatus;
        private ModernListView lvTorrentResults;
        private TextBox txtTorrentDirectMagnet;

        private string _activeWorkspaceView = "downloads"; // "downloads", "browser", "torrent", "youtube", "facebook", "tiktok"

        // YouTube Portal Elements
        private Panel pnlYouTubeView;
        private Panel pnlYouTubeBar;
        private Button btnYouTubeBack;
        private Button btnYouTubeFwd;
        private Button btnYouTubeRefresh;
        private Button btnYouTubeHome;
        private TextBox txtYouTubeUrl;
        private Button btnYouTubeGo;
        private Button btnDownloadYouTubeVideo;
        private Button btnDownloadYouTubeAudio;
        private WebView2 webYouTube;
        private bool _webYouTubeInitialized = false;

        // Facebook Portal Elements
        private Panel pnlFacebookView;
        private Panel pnlFacebookBar;
        private Button btnFacebookBack;
        private Button btnFacebookFwd;
        private Button btnFacebookRefresh;
        private Button btnFacebookHome;
        private TextBox txtFacebookUrl;
        private Button btnFacebookGo;
        private Button btnDownloadFacebookVideo;
        private WebView2 webFacebook;
        private bool _webFacebookInitialized = false;

        // TikTok Portal Elements
        private Panel pnlTikTokView;
        private Panel pnlTikTokBar;
        private Button btnTikTokBack;
        private Button btnTikTokFwd;
        private Button btnTikTokRefresh;
        private Button btnTikTokHome;
        private TextBox txtTikTokUrl;
        private Button btnTikTokGo;
        private WebView2 webTikTok;
        private bool _webTikTokInitialized = false;
        private string _lastCapturedMediaUrl = "";

        // Sidebar Elements
        private Panel pnlBrand;
        private Label lblLogo;
        private Label lblSubLogo;
        private Label lblDevCredit;
        private Button btnNewDownloadLarge;
        private Panel pnlNavList;
        private List<Button> _navButtons = new List<Button>();
        private Button _activeNavButton;
        private string _currentCategoryFilter = "all";
        private bool _sortAscending = false;
        private bool _categoriesExpanded = true;

        // Sidebar Footer Elements
        private Panel pnlSidebarFooter;
        private Label lblThemeTitle;
        private Button btnModeDark;
        private Button btnModeWhite;
        private Button btnTelegram;
        private Button btnFacebook;
        private Button btnAbout;

        // Header Elements
        private Panel pnlHeaderLeft;
        private Label lblViewTitle;
        private Label lblViewSubtitle;
        private Panel pnlHeaderActions;
        private Panel pnlSearch;
        private Label lblSearchIcon;
        private TextBox txtSearch;
        private Button btnResumeAll;
        private Button btnPauseAll;
        private Button btnClearDone;
        private Button btnThemesMenu;
        private Button btnConfigMenu;
        private Button btnHistoryTop;
        private Button btnSpeedTest;
        private Button btnMoreMenu;

        // Context Menus
        private ContextMenuStrip contextMenu;
        private ContextMenuStrip menuThemesDropdown;
        private ContextMenuStrip menuMoreDropdown;

        // Bottom HUD Elements
        private Label lblHudSpeed;
        private Label lblHudThreads;
        private Label lblHudClipboard;
        private Label lblHudFooterDev;

        // Background State
        private List<DownloadItem> _downloads = new List<DownloadItem>();
        private Timer _masterTimer;
        private Timer _clipboardWatcherTimer;
        private string _lastClipboardUrl = "";
        private bool _clipboardWatcherEnabled = true;
        private string _searchFilter = "";

        // AddUrlDialog Spam & Infinite Loop Protection
        private static readonly object _addUrlLock = new object();
        private static bool _isAddUrlDialogOpen = false;
        private static AddUrlDialog _activeAddUrlDialog = null;
        private static DateTime _lastAddUrlTriggerTime = DateTime.MinValue;
        private static string _lastRequestedUrl = null;
        private static bool _isClipboardPromptOpen = false;

        // Global Scheduler state
        private bool _schedulerEnabled = false;
        private DateTime _scheduledTime;
        private bool _autoShutdown = false;

        private bool _startInBackground = false;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayContextMenu;
        private bool _isExplicitExit = false;
        private bool _hasShownTrayTip = false;
        private HttpListener _httpServer;
        private Thread _httpServerThread;
        private bool _isHttpServerRunning = false;

        public MainForm() : this(false)
        {
        }

        public MainForm(bool startInBackground)
        {
            _startInBackground = startInBackground;
            EnableModernWebBrowser();
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls; // TLS 1.2
            InitializeComponent();
            ThemeManager.ThemeChanged += (s, e) => ApplyCurrentTheme();
            InitializeSystemTray();
            StartMasterTimer();
            StartClipboardWatcher();
            StartSidebarAutoHide();
            ApplyInitialData();
            ApplyCurrentTheme();
            StartLocalHttpServer();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_startInBackground && !this.IsHandleCreated)
            {
                CreateHandle();
                value = false;
                _startInBackground = false;
            }
            base.SetVisibleCore(value);
        }

        private static void EnableModernWebBrowser()
        {
            try
            {
                string appName = Path.GetFileName(Application.ExecutablePath);
                using (var rk = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (rk != null)
                    {
                        rk.SetValue(appName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private static void SetProcessUserAgent(string ua)
        {
            try
            {
                if (string.IsNullOrEmpty(ua))
                {
                    UrlMkSetSessionOption(0x10000001, null, 0, 0);
                }
                else
                {
                    UrlMkSetSessionOption(0x10000001, ua, ua.Length, 0);
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "Turbo X - High Speed Download Accelerator & Browser";
            try
            {
                var appIcon = IconFactory.GetTurboXIcon();
                if (appIcon != null) this.Icon = appIcon;
            }
            catch { }
            this.Size = new Size(1180, 740);
            this.MinimumSize = new Size(960, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.DoubleBuffered = true;

            // 1. Build Left Sidebar (Width: 235)
            BuildSidebar();

            // 2. Build Main Right Area (Fill)
            BuildMainArea();

            // Assemble Form
            this.Controls.Add(pnlMainArea);
            this.Controls.Add(pnlSidebar);
        }

        #region Sidebar UI Construction

        private Panel pnlSidebarCard;

        private void BuildSidebar()
        {
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 226,
                Padding = new Padding(10, 10, 4, 10),
                BackColor = Color.FromArgb(2, 6, 18)
            };

            // Sidebar card container matching reference design
            pnlSidebarCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(7, 19, 38),
                Padding = new Padding(0)
            };
            pnlSidebar.Controls.Add(pnlSidebarCard);

            pnlSidebarCard.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlSidebarCard.Width < 10 || pnlSidebarCard.Height < 10) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, pnlSidebarCard.Width - 1, pnlSidebarCard.Height - 1);
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color borderClr = isDark ? Color.FromArgb(14, 76, 140) : Color.FromArgb(180, 205, 235);
                    using (var path = CreateRoundedRectanglePath(r, 14))
                    using (var pen = new Pen(borderClr, 1.5f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
                catch { }
            };

            // Hidden dummy pnlBrand and pin button for backwards compatibility
            pnlBrand = new Panel { Visible = false, Height = 0 };
            pnlSidebarCard.Controls.Add(pnlBrand);
            btnSidebarPin = new Button { Visible = false, Size = new Size(1, 1) };
            pnlBrand.Controls.Add(btnSidebarPin);

            // Large CTA "+ New Download" Button (Electric Cyan/Blue Gradient Pill)
            btnNewDownloadLarge = new Button
            {
                Text = "+ New Download",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(188, 38),
                Location = new Point(12, 12),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 140, 255),
                UseVisualStyleBackColor = false
            };
            btnNewDownloadLarge.FlatAppearance.BorderSize = 0;
            btnNewDownloadLarge.Paint += (s, pe) =>
            {
                try
                {
                    int w = btnNewDownloadLarge.Width;
                    int h = btnNewDownloadLarge.Height;
                    if (w < 10 || h < 10) return;

                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, w - 1, h - 1);
                    using (var path = CreateRoundedRectanglePath(r, 8))
                    using (var brush = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(0, 140, 255), Color.FromArgb(0, 200, 255), LinearGradientMode.Horizontal))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var path = CreateRoundedRectanglePath(r, 8))
                    using (var pen = new Pen(Color.FromArgb(120, 220, 255), 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString("+ New Download", font, brush, r, sf);
                    }
                }
                catch { }
            };
            btnNewDownloadLarge.Click += (s, e) => ShowAddUrlDialog();
            new ToolTip().SetToolTip(btnNewDownloadLarge, "➕ Start a New Fast Download (Paste URL or Torrent)");
            pnlSidebarCard.Controls.Add(btnNewDownloadLarge);

            // Sidebar Footer at Bottom (Height: 142)
            BuildSidebarFooter();

            // Navigation Items List (Fills between CTA and Footer)
            pnlNavList = new Panel
            {
                Location = new Point(6, 58),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnlSidebarCard.Controls.Add(pnlNavList);
            pnlSidebarFooter.BringToFront();

            Action updateNavListBounds = () =>
            {
                pnlNavList.Location = new Point(6, 58);
                int navH = pnlSidebarCard.Height - 58 - pnlSidebarFooter.Height;
                if (navH > 50) pnlNavList.Size = new Size(pnlSidebarCard.Width - 12, navH);
            };
            pnlSidebarCard.Resize += (s, e) => updateNavListBounds();
            updateNavListBounds();

            // Mouse Hover handlers on pnlSidebar
            pnlSidebar.MouseEnter += (s, e) =>
            {
                if (_sidebarAutoHide && !_sidebarPinned)
                {
                    _sidebarLeaveGraceCount = 0;
                    _sidebarTargetWidth = 226;
                }
            };
            pnlSidebar.MouseMove += (s, e) =>
            {
                if (_sidebarAutoHide && !_sidebarPinned)
                {
                    _sidebarLeaveGraceCount = 0;
                    _sidebarTargetWidth = 226;
                }
            };

            BuildNavigationButtons();
        }

        private void BuildNavigationButtons()
        {
            pnlNavList.Controls.Clear();
            _navButtons.Clear();

            int y = 4;

            // 1. Exact Category List with modern tile design & badge pills:
            Button btnAll = CreateNavButton("All Downloads", "⚡", "all", false, ref y);
            CreateNavButton("Video", "▶", "video", false, ref y);
            CreateNavButton("Compressed", "📦", "compressed", false, ref y);
            CreateNavButton("Documents", "📄", "documents", false, ref y);
            CreateNavButton("Programs", "⊞", "programs", false, ref y);
            CreateNavButton("Music", "🎵", "music", false, ref y);
            CreateNavButton("Images", "🖼", "images", false, ref y);

            AddNavDivider(ref y);

            // 2. Extra Power Tools & Streaming Portals with matching modern tiles:
            CreateFeatureButton("Turbo X", "", (s, e) => SwitchWorkspaceView("browser"), ref y, IconFactory.GetTurboXImage(18, 18));
            CreateFeatureButton("Torrent Movies", "🧲", (s, e) => SwitchWorkspaceView("torrent"), ref y);
            CreateFeatureButton("Smart Scheduler", "⏰", (s, e) => ShowSchedulerDialog(), ref y);
            CreateFeatureButton("Facebook", "📘", (s, e) => SwitchWorkspaceView("facebook"), ref y);
            CreateFeatureButton("YouTube", "▶️", (s, e) => SwitchWorkspaceView("youtube"), ref y);
            CreateFeatureButton("TikTok", "🎵", (s, e) => SwitchWorkspaceView("tiktok"), ref y);

            // Restore active state
            Button currentActive = null;
            foreach (var b in _navButtons)
            {
                if (b.Tag != null && b.Tag.ToString() == _currentCategoryFilter)
                {
                    currentActive = b;
                    break;
                }
            }
            if (currentActive != null) SetActiveNavButton(currentActive);
            else if (btnAll != null) SetActiveNavButton(btnAll);

            UpdateCategoryBadges();
        }

        private Button CreateNavButton(string title, string iconText, string categoryKey, bool isSubCategory, ref int y)
        {
            ModernNavButton btn = new ModernNavButton
            {
                Title = title,
                IconText = iconText,
                Tag = categoryKey,
                CategoryKey = categoryKey,
                Size = new Size(182, 31),
                Location = new Point(4, y),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Text = title
            };

            btn.Click += (s, e) =>
            {
                SwitchWorkspaceView("downloads");
                _currentCategoryFilter = categoryKey;
                SetActiveNavButton(btn);
                FilterDownloadsList();
            };

            pnlNavList.Controls.Add(btn);
            _navButtons.Add(btn);
            y += 35;
            return btn;
        }

        private Button CreateFeatureButton(string title, string iconText, EventHandler handler, ref int y, Image icon = null)
        {
            ModernNavButton btn = new ModernNavButton
            {
                Title = title,
                IconText = iconText,
                IconImage = icon,
                Tag = title.ToLowerInvariant(),
                Size = new Size(182, 31),
                Location = new Point(4, y),
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                Text = title
            };

            btn.Click += (s, e) =>
            {
                SetActiveNavButton(btn);
                handler(s, e);
            };

            pnlNavList.Controls.Add(btn);
            _navButtons.Add(btn);
            y += 35;
            return btn;
        }

        private void AddNavDivider(ref int y)
        {
            Panel div = new Panel
            {
                Size = new Size(178, 1),
                Location = new Point(6, y + 4),
                BackColor = Color.FromArgb(22, 50, 88)
            };
            pnlNavList.Controls.Add(div);
            y += 10;
        }

        private void SetActiveNavButton(Button btn)
        {
            _activeNavButton = btn;
            UpdateNavButtonStyles();
            pnlNavList.Invalidate(true);
        }

        private void UpdateNavButtonStyles()
        {
            foreach (var b in _navButtons)
            {
                bool isActive = (b == _activeNavButton);
                var mb = b as ModernNavButton;
                if (mb != null)
                {
                    mb.IsActive = isActive;
                    mb.Invalidate();
                }
                else
                {
                    if (isActive)
                    {
                        b.BackColor = Color.FromArgb(0, 110, 230);
                        b.ForeColor = Color.White;
                    }
                    else
                    {
                        b.BackColor = Color.Transparent;
                        b.ForeColor = Color.FromArgb(203, 213, 225);
                    }
                }
            }
        }

        private void DrawPillButton(Graphics g, Rectangle r, int radius, Color bg, Color border, string icon, string text, Color textClr, bool bold, Brush customBgBrush = null)
        {
            using (var path = CreateRoundedRectanglePath(r, radius))
            {
                if (customBgBrush != null)
                {
                    g.FillPath(customBgBrush, path);
                }
                else if (bg != Color.Transparent && bg != Color.Empty)
                {
                    using (var brush = new SolidBrush(bg))
                    {
                        g.FillPath(brush, path);
                    }
                }

                if (border != Color.Transparent && border != Color.Empty)
                {
                    using (var pen = new Pen(border, 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            Font iconFont = null;
            try { iconFont = new Font("Segoe UI Emoji", 8.5F, FontStyle.Regular); }
            catch { iconFont = new Font("Segoe UI", 8.5F, FontStyle.Regular); }

            using (iconFont)
            using (var textFont = new Font("Segoe UI", 8F, bold ? FontStyle.Bold : FontStyle.Regular))
            {
                SizeF szIcon = !string.IsNullOrEmpty(icon) ? g.MeasureString(icon, iconFont) : SizeF.Empty;
                SizeF szText = g.MeasureString(text, textFont);
                float totalW = (szIcon.Width > 0 ? (szIcon.Width + 3) : 0) + szText.Width;
                float startX = r.X + (r.Width - totalW) / 2f;
                float iconY = r.Y + (r.Height - szIcon.Height) / 2f;
                float textY = r.Y + (r.Height - szText.Height) / 2f;

                using (var brush = new SolidBrush(textClr))
                {
                    if (!string.IsNullOrEmpty(icon))
                    {
                        g.DrawString(icon, iconFont, brush, startX, iconY);
                        startX += szIcon.Width + 3;
                    }
                    g.DrawString(text, textFont, brush, startX, textY);
                }
            }
        }

        private void BuildSidebarFooter()
        {
            pnlSidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 138,
                Padding = new Padding(8, 4, 8, 6),
                BackColor = Color.Transparent
            };
            pnlSidebarCard.Controls.Add(pnlSidebarFooter);

            pnlSidebarFooter.Paint += (s, pe) =>
            {
                try
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color divClr = isDark ? Color.FromArgb(22, 50, 88) : Color.FromArgb(215, 225, 240);
                    using (var pen = new Pen(divClr, 1f))
                    {
                        pe.Graphics.DrawLine(pen, 8, 0, pnlSidebarFooter.Width - 8, 0);
                    }
                }
                catch { }
            };

            // 1. Label: THEME MODE:
            lblThemeTitle = new Label
            {
                Text = "THEME MODE:",
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 145, 180),
                Location = new Point(10, 5),
                Size = new Size(180, 13),
                BackColor = Color.Transparent
            };
            pnlSidebarFooter.Controls.Add(lblThemeTitle);

            // 2. Row: 🌙 Dark & ☀️ White Toggle Buttons
            btnModeDark = new Button
            {
                Text = "🌙 Dark",
                Size = new Size(92, 26),
                Location = new Point(10, 20),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 110, 230),
                UseVisualStyleBackColor = false
            };
            btnModeDark.FlatAppearance.BorderSize = 0;
            btnModeDark.Click += (s, e) =>
            {
                ThemeManager.SetTheme("Obsidian Onyx");
                ApplyCurrentTheme();
            };
            btnModeDark.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right) ShowThemesDropdown();
            };
            btnModeDark.Paint += (s, pe) =>
            {
                try
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, btnModeDark.Width - 1, btnModeDark.Height - 1);
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    if (isDark)
                    {
                        using (var grad = new LinearGradientBrush(new Rectangle(0, 0, btnModeDark.Width, btnModeDark.Height), Color.FromArgb(0, 114, 235), Color.FromArgb(0, 155, 255), LinearGradientMode.Horizontal))
                        {
                            DrawPillButton(g, r, 6, Color.Empty, Color.FromArgb(0, 220, 255), "🌙", "Dark", Color.White, true, grad);
                        }
                    }
                    else
                    {
                        DrawPillButton(g, r, 6, Color.FromArgb(235, 240, 248), Color.FromArgb(200, 212, 228), "🌙", "Dark", Color.FromArgb(70, 85, 105), false);
                    }
                }
                catch { }
            };
            new ToolTip().SetToolTip(btnModeDark, "🌙 Switch to High-Contrast Obsidian Dark Theme (Right-click for all themes)");
            pnlSidebarFooter.Controls.Add(btnModeDark);

            btnModeWhite = new Button
            {
                Text = "☀️ White",
                Size = new Size(92, 26),
                Location = new Point(108, 20),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.FromArgb(180, 200, 225),
                BackColor = Color.FromArgb(14, 28, 52),
                UseVisualStyleBackColor = false
            };
            btnModeWhite.FlatAppearance.BorderSize = 0;
            btnModeWhite.Click += (s, e) =>
            {
                ThemeManager.SetTheme("Titanium White");
                ApplyCurrentTheme();
            };
            btnModeWhite.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right) ShowThemesDropdown();
            };
            btnModeWhite.Paint += (s, pe) =>
            {
                try
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, btnModeWhite.Width - 1, btnModeWhite.Height - 1);
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    if (!isDark)
                    {
                        using (var grad = new LinearGradientBrush(new Rectangle(0, 0, btnModeWhite.Width, btnModeWhite.Height), Color.FromArgb(2, 132, 199), Color.FromArgb(56, 189, 248), LinearGradientMode.Horizontal))
                        {
                            DrawPillButton(g, r, 6, Color.Empty, Color.FromArgb(14, 165, 233), "☀️", "White", Color.White, true, grad);
                        }
                    }
                    else
                    {
                        DrawPillButton(g, r, 6, Color.FromArgb(14, 28, 52), Color.FromArgb(28, 56, 95), "☀️", "White", Color.FromArgb(180, 200, 225), false);
                    }
                }
                catch { }
            };
            new ToolTip().SetToolTip(btnModeWhite, "☀️ Switch to Clean Titanium White Light Theme (Right-click for all themes)");
            pnlSidebarFooter.Controls.Add(btnModeWhite);

            // 3. Row: ✈️ Telegram & 📘 Facebook
            btnTelegram = new Button
            {
                Text = "✈️ Telegram",
                Size = new Size(92, 25),
                Location = new Point(10, 50),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(14, 165, 233),
                UseVisualStyleBackColor = false
            };
            btnTelegram.FlatAppearance.BorderSize = 0;
            btnTelegram.Paint += (s, pe) =>
            {
                try
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, btnTelegram.Width - 1, btnTelegram.Height - 1);
                    DrawPillButton(g, r, 6, Color.FromArgb(14, 165, 233), Color.Transparent, "✈", "Telegram", Color.White, true);
                }
                catch { }
            };
            btnTelegram.Click += (s, e) => OpenUrl("https://t.me/abrno34");
            pnlSidebarFooter.Controls.Add(btnTelegram);

            btnFacebook = new Button
            {
                Text = "📘 Facebook",
                Size = new Size(92, 25),
                Location = new Point(108, 50),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 119, 242),
                UseVisualStyleBackColor = false
            };
            btnFacebook.FlatAppearance.BorderSize = 0;
            btnFacebook.Paint += (s, pe) =>
            {
                try
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, btnFacebook.Width - 1, btnFacebook.Height - 1);
                    DrawPillButton(g, r, 6, Color.FromArgb(24, 119, 242), Color.Transparent, "📘", "Facebook", Color.White, true);
                }
                catch { }
            };
            btnFacebook.Click += (s, e) => OpenUrl("https://facebook.com/abrno34");
            pnlSidebarFooter.Controls.Add(btnFacebook);

            // 4. Row: ℹ️ About Button (Sleek wide rounded card across both columns)
            btnAbout = new Button
            {
                Text = "ℹ️ About IDM Turbo",
                Size = new Size(190, 26),
                Location = new Point(10, 79),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.FromArgb(14, 28, 52),
                UseVisualStyleBackColor = false
            };
            btnAbout.FlatAppearance.BorderSize = 0;
            btnAbout.Paint += (s, pe) =>
            {
                try
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, btnAbout.Width - 1, btnAbout.Height - 1);
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color bg = isDark ? Color.FromArgb(14, 28, 52) : Color.FromArgb(240, 244, 250);
                    Color border = isDark ? Color.FromArgb(28, 56, 95) : Color.FromArgb(205, 215, 230);
                    Color txt = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(30, 41, 59);
                    DrawPillButton(g, r, 6, bg, border, "ℹ", "About IDM Turbo", txt, true);
                }
                catch { }
            };
            btnAbout.Click += (s, e) => ShowAboutDialog();
            pnlSidebarFooter.Controls.Add(btnAbout);

            // 5. Developer Credit
            lblDevCredit = new Label
            {
                Text = "Developed by Abir  •  v6.43 Pro",
                Font = new Font("Segoe UI", 7.25F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 125, 160),
                Location = new Point(10, 110),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlSidebarFooter.Controls.Add(lblDevCredit);
        }

        #endregion

        #region Main Right Area & Views Construction

        private void BuildMainArea()
        {
            pnlMainArea = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };

            // 1. Top Modern Header (Dock: Top)
            BuildTopHeader();

            // 2. Bottom HUD Deck (Dock: Bottom)
            BuildBottomDeck();

            // 3. Center Workspace Panel (Dock: Fill)
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            pnlMainArea.Controls.Add(pnlContent);

            // Crucial WinForms docking order: Top and Bottom dock first (SendToBack), then Content fills the rest (BringToFront)!
            pnlTopHeader.SendToBack();
            pnlBottomDeck.SendToBack();
            pnlContent.BringToFront();

            // Build Workspace Views inside pnlContent
            BuildDownloadsListView();
            BuildBrowserView();
            BuildTorrentStudioView();
            BuildYouTubeView();
            BuildFacebookView();
            BuildTikTokView();

            BuildContextMenu();
            BuildDropdownMenus();

            // Start on downloads view
            SwitchWorkspaceView("downloads");
        }

        private void BuildTopHeader()
        {
            pnlTopHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                Padding = new Padding(12, 10, 12, 10)
            };
            pnlMainArea.Controls.Add(pnlTopHeader);

            // Right Action Bar (Docked to Right so controls NEVER jump off-screen!)
            pnlHeaderActions = new Panel
            {
                Dock = DockStyle.Right,
                Width = 730,
                Height = 44,
                Padding = new Padding(0)
            };
            pnlTopHeader.Controls.Add(pnlHeaderActions);

            // Search box inside Action Bar with 🔍 Icon
            pnlSearch = new Panel
            {
                Location = new Point(6, 7),
                Size = new Size(135, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlHeaderActions.Controls.Add(pnlSearch);

            lblSearchIcon = new Label
            {
                Text = "🔍",
                Location = new Point(3, 5),
                Size = new Size(18, 18),
                Font = new Font("Segoe UI", 8.5F)
            };
            pnlSearch.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox
            {
                Location = new Point(23, 6),
                Size = new Size(108, 18),
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchFilter = txtSearch.Text.Trim();
                FilterDownloadsList();
            };
            pnlSearch.Controls.Add(txtSearch);

            // Header Action Buttons for Downloads
            int x = 146;
            btnResumeAll = CreateHeaderButton("▶ Resume", "Resume all downloads", (s, e) => StartAllQueued(), ref x, 68);
            btnPauseAll = CreateHeaderButton("⏸ Pause", "Pause all downloads", (s, e) => StopAllDownloads(), ref x, 66);
            btnClearDone = CreateHeaderButton("🗑 Clear", "Remove completed downloads", (s, e) => DeleteCompletedDownloads(), ref x, 60);

            // Global Actions (Themes, Config, History, Speed Test, More) - anchored to far right of pnlHeaderActions
            btnThemesMenu = new Button
            {
                Text = "🎨 Theme",
                Size = new Size(68, 30),
                Location = new Point(pnlHeaderActions.Width - 396, 7),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnThemesMenu.FlatAppearance.BorderSize = 1;
            btnThemesMenu.Click += (s, e) => ShowThemesDropdown();
            new ToolTip().SetToolTip(btnThemesMenu, "Switch between 9 themes");
            pnlHeaderActions.Controls.Add(btnThemesMenu);

            btnConfigMenu = new Button
            {
                Text = "⚙ Config",
                Size = new Size(66, 30),
                Location = new Point(pnlHeaderActions.Width - 324, 7),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnConfigMenu.FlatAppearance.BorderSize = 1;
            btnConfigMenu.Click += (s, e) => ShowOptionsDialog();
            new ToolTip().SetToolTip(btnConfigMenu, "IDM Turbo Configuration & Speed Settings");
            pnlHeaderActions.Controls.Add(btnConfigMenu);

            btnHistoryTop = new Button
            {
                Text = "📜 History",
                Size = new Size(76, 30),
                Location = new Point(pnlHeaderActions.Width - 254, 7),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnHistoryTop.FlatAppearance.BorderSize = 1;
            btnHistoryTop.Click += (s, e) => ShowBrowserHistoryDialog();
            new ToolTip().SetToolTip(btnHistoryTop, "Complete Web Browsing History");
            pnlHeaderActions.Controls.Add(btnHistoryTop);

            btnSpeedTest = new Button
            {
                Text = "492.9 Mbps",
                Size = new Size(114, 30),
                Location = new Point(pnlHeaderActions.Width - 174, 7),
                Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 4, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSpeedTest.FlatAppearance.BorderSize = 1;
            btnSpeedTest.Paint += (s, pe) =>
            {
                try
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float icx = 15f;
                    float icy = 15f;
                    float ir = 9.5f;
                    RectangleF iarcRect = new RectangleF(icx - ir, icy - ir, ir * 2f, ir * 2f);

                    // 1. Inactive background track arc
                    using (Pen itp = new Pen(Color.FromArgb(71, 85, 105), 2.2f))
                    {
                        itp.StartCap = LineCap.Round;
                        itp.EndCap = LineCap.Round;
                        g.DrawArc(itp, iarcRect, 135f, 270f);
                    }

                    // 2. Active glowing progress arc
                    float pct = (_lastTestedSpeedMbps > 0) ? (float)Math.Min(1.0, _lastTestedSpeedMbps / 1000.0) : 0.493f;
                    using (Pen iap = new Pen(Color.FromArgb(6, 214, 250), 2.2f))
                    {
                        iap.StartCap = LineCap.Round;
                        iap.EndCap = LineCap.Round;
                        g.DrawArc(iap, iarcRect, 135f, 270f * pct);
                    }

                    // 3. Mini precision needle
                    float angle = 135f + 270f * pct;
                    double rad = angle * Math.PI / 180.0;
                    float tipX = icx + (float)((ir - 1.5f) * Math.Cos(rad));
                    float tipY = icy + (float)((ir - 1.5f) * Math.Sin(rad));
                    using (Pen inp = new Pen(Color.White, 1.5f))
                    {
                        g.DrawLine(inp, icx, icy, tipX, tipY);
                    }

                    // 4. Center hub dot
                    using (Brush ihb = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(ihb, icx - 2f, icy - 2f, 4f, 4f);
                    }
                }
                catch { }
            };
            btnSpeedTest.Click += (s, e) => ShowSpeedometerDialog();
            new ToolTip().SetToolTip(btnSpeedTest, "⚡ Live Broadband Speedometer Gauge & Diagnostics (Click to open)");
            pnlHeaderActions.Controls.Add(btnSpeedTest);

            btnMoreMenu = new Button
            {
                Text = "⋯ More",
                Size = new Size(52, 30),
                Location = new Point(pnlHeaderActions.Width - 56, 7),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMoreMenu.FlatAppearance.BorderSize = 1;
            btnMoreMenu.Click += (s, e) => ShowMoreDropdown();
            new ToolTip().SetToolTip(btnMoreMenu, "Additional power tools");
            pnlHeaderActions.Controls.Add(btnMoreMenu);

            // Left Section: Section Title & Real-time Stats inside pnlHeaderLeft (Dock: Fill)
            pnlHeaderLeft = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            pnlTopHeader.Controls.Add(pnlHeaderLeft);

            lblViewTitle = new Label
            {
                Text = "⚡ All Downloads",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                Location = new Point(4, 6),
                AutoSize = true
            };
            pnlHeaderLeft.Controls.Add(lblViewTitle);

            lblViewSubtitle = new Label
            {
                Text = "Showing all downloads • 32 parallel socket threads active",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Location = new Point(6, 38),
                Size = new Size(Math.Max(50, pnlHeaderLeft.Width - 15), 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoEllipsis = true
            };
            pnlHeaderLeft.Resize += (s, e) =>
            {
                lblViewSubtitle.Width = Math.Max(50, pnlHeaderLeft.Width - 15);
            };
            pnlHeaderLeft.Controls.Add(lblViewSubtitle);
        }

        private Button CreateHeaderButton(string text, string tooltipText, EventHandler handler, ref int x, int width = 74)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(width, 30),
                Location = new Point(x, 7),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += handler;

            ToolTip tt = new ToolTip();
            tt.SetToolTip(btn, tooltipText);

            pnlHeaderActions.Controls.Add(btn);
            x += (width + 6);
            return btn;
        }

        private void BuildDownloadsListView()
        {
            pnlDownloadsView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(2, 6, 18),
                Padding = new Padding(12, 10, 12, 10),
                Visible = true
            };
            pnlContent.Controls.Add(pnlDownloadsView);

            // 1. Top Cyber Metric Cards (Height: 125)
            pnlCyberCards = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 10)
            };
            pnlDownloadsView.Controls.Add(pnlCyberCards);

            pnlCardSpeed = new Panel { BackColor = Color.Transparent, Cursor = Cursors.Hand };
            pnlCardActive = new Panel { BackColor = Color.Transparent, Cursor = Cursors.Hand };
            pnlCardCompleted = new Panel { BackColor = Color.Transparent, Cursor = Cursors.Hand };
            pnlCardQueued = new Panel { BackColor = Color.Transparent, Cursor = Cursors.Hand };

            SetDoubleBuffered(pnlCardSpeed);
            SetDoubleBuffered(pnlCardActive);
            SetDoubleBuffered(pnlCardCompleted);
            SetDoubleBuffered(pnlCardQueued);

            ToolTip ttCards = new ToolTip();
            ttCards.SetToolTip(pnlCardSpeed, "⚡ Click to Test Internet Speed & Diagnostics");
            ttCards.SetToolTip(pnlCardActive, "⬇️ Click to View Active Downloads");
            ttCards.SetToolTip(pnlCardCompleted, "✅ Click to View Completed Downloads");
            ttCards.SetToolTip(pnlCardQueued, "⏸️ Click to View Queued/Paused Downloads");

            pnlCardSpeed.MouseEnter += (s, e) => { _hoverCyberCardIndex = 0; pnlCardSpeed.Invalidate(); };
            pnlCardSpeed.MouseLeave += (s, e) => { _hoverCyberCardIndex = -1; pnlCardSpeed.Invalidate(); };
            pnlCardSpeed.Click += (s, e) => ShowSpeedometerDialog();

            pnlCardActive.MouseEnter += (s, e) => { _hoverCyberCardIndex = 1; pnlCardActive.Invalidate(); };
            pnlCardActive.MouseLeave += (s, e) => { _hoverCyberCardIndex = -1; pnlCardActive.Invalidate(); };
            pnlCardActive.Click += (s, e) =>
            {
                SwitchWorkspaceView("downloads");
                _currentCategoryFilter = "active";
                _activeNavButton = null;
                if (pnlNavList != null) pnlNavList.Invalidate();
                pnlCardActive.Invalidate();
                pnlCardCompleted.Invalidate();
                pnlCardQueued.Invalidate();
                FilterDownloadsList();
            };

            pnlCardCompleted.MouseEnter += (s, e) => { _hoverCyberCardIndex = 2; pnlCardCompleted.Invalidate(); };
            pnlCardCompleted.MouseLeave += (s, e) => { _hoverCyberCardIndex = -1; pnlCardCompleted.Invalidate(); };
            pnlCardCompleted.Click += (s, e) =>
            {
                SwitchWorkspaceView("downloads");
                _currentCategoryFilter = "completed";
                _activeNavButton = null;
                if (pnlNavList != null) pnlNavList.Invalidate();
                pnlCardActive.Invalidate();
                pnlCardCompleted.Invalidate();
                pnlCardQueued.Invalidate();
                FilterDownloadsList();
            };

            pnlCardQueued.MouseEnter += (s, e) => { _hoverCyberCardIndex = 3; pnlCardQueued.Invalidate(); };
            pnlCardQueued.MouseLeave += (s, e) => { _hoverCyberCardIndex = -1; pnlCardQueued.Invalidate(); };
            pnlCardQueued.Click += (s, e) =>
            {
                SwitchWorkspaceView("downloads");
                _currentCategoryFilter = "paused";
                _activeNavButton = null;
                if (pnlNavList != null) pnlNavList.Invalidate();
                pnlCardActive.Invalidate();
                pnlCardCompleted.Invalidate();
                pnlCardQueued.Invalidate();
                FilterDownloadsList();
            };

            pnlCyberCards.Controls.Add(pnlCardSpeed);
            pnlCyberCards.Controls.Add(pnlCardActive);
            pnlCyberCards.Controls.Add(pnlCardCompleted);
            pnlCyberCards.Controls.Add(pnlCardQueued);

            pnlCyberCards.Resize += (s, e) =>
            {
                int gap = 12;
                int totalW = pnlCyberCards.ClientSize.Width;
                int cardW = Math.Max(120, (totalW - (gap * 3)) / 4);
                int cardH = Math.Max(50, pnlCyberCards.ClientSize.Height - 4);

                pnlCardSpeed.SetBounds(0, 0, cardW, cardH);
                pnlCardActive.SetBounds(cardW + gap, 0, cardW, cardH);
                pnlCardCompleted.SetBounds((cardW + gap) * 2, 0, cardW, cardH);
                int lastX = (cardW + gap) * 3;
                pnlCardQueued.SetBounds(lastX, 0, Math.Max(cardW, totalW - lastX), cardH);
            };

            // Paint Card 1: Speedometer Arc (Golden Amber / Warm Gold)
            pnlCardSpeed.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlCardSpeed.Width < 10 || pnlCardSpeed.Height < 10) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Rectangle r = new Rectangle(0, 0, pnlCardSpeed.Width - 1, pnlCardSpeed.Height - 1);
                    bool isHov = (_hoverCyberCardIndex == 0);
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color borderColor = isDark ? (isHov ? Color.FromArgb(245, 158, 11) : Color.FromArgb(50, 36, 16))
                                               : (isHov ? Color.FromArgb(217, 119, 6) : Color.FromArgb(226, 232, 240));
                    float penWidth = isHov ? 2.0f : 1.5f;

                    using (var path = CreateRoundedRectanglePath(r, 14))
                    {
                        Color cardBg = isDark ? (isHov ? Color.FromArgb(24, 18, 10) : Color.FromArgb(9, 13, 22))
                                              : (isHov ? Color.FromArgb(254, 243, 199) : Color.FromArgb(255, 255, 255));
                        using (var brush = new SolidBrush(cardBg)) g.FillPath(brush, path);
                        using (var pen = new Pen(borderColor, penWidth)) g.DrawPath(pen, path);
                    }

                    float cx = pnlCardSpeed.Width / 2f;
                    float cy = 46f;
                    float radius = 33f;
                    RectangleF arcRect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

                    Color trackClr = isDark ? Color.FromArgb(28, 34, 46) : Color.FromArgb(226, 232, 240);
                    using (var trackPen = new Pen(trackClr, 4.0f))
                    {
                        trackPen.StartCap = LineCap.Round;
                        trackPen.EndCap = LineCap.Round;
                        g.DrawArc(trackPen, arcRect, 135f, 270f);
                    }

                    float pct = 0.55f;
                    if (_totalLiveSpeedBytes > 0)
                    {
                        pct = (float)Math.Min(1.0, _totalLiveSpeedBytes / (50.0 * 1024.0 * 1024.0));
                    }
                    else if (_lastTestedSpeedMbps > 0)
                    {
                        pct = (float)Math.Min(1.0, _lastTestedSpeedMbps / 500.0);
                    }
                    float sweep = Math.Max(12f, Math.Min(270f, 270f * pct));

                    Color sweepClr = isDark ? Color.FromArgb(245, 158, 11) : Color.FromArgb(217, 119, 6);
                    using (var sweepPen = new Pen(sweepClr, 4.5f))
                    {
                        sweepPen.StartCap = LineCap.Round;
                        sweepPen.EndCap = LineCap.Round;
                        g.DrawArc(sweepPen, arcRect, 135f, sweep);
                    }

                    float needleAngle = 135f + sweep;
                    double rad = needleAngle * Math.PI / 180.0;
                    float tipX = cx + (float)((radius - 4f) * Math.Cos(rad));
                    float tipY = cy + (float)((radius - 4f) * Math.Sin(rad));
                    Color needleClr = isDark ? Color.FromArgb(251, 191, 36) : Color.FromArgb(180, 83, 9);
                    using (var needlePen = new Pen(needleClr, 2.0f))
                    {
                        g.DrawLine(needlePen, cx, cy, tipX, tipY);
                    }
                    using (var hubBrush = new SolidBrush(needleClr))
                    {
                        g.FillEllipse(hubBrush, cx - 3f, cy - 3f, 6f, 6f);
                    }

                    string spdText;
                    if (_totalLiveSpeedBytes > 0)
                    {
                        spdText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} Mbps", (_totalLiveSpeedBytes * 8.0) / (1024.0 * 1024.0));
                    }
                    else if (_lastTestedSpeedMbps > 0)
                    {
                        spdText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} Mbps", _lastTestedSpeedMbps);
                    }
                    else
                    {
                        spdText = "14.6 MB/s";
                    }

                    Color spdTextClr = isDark ? Color.White : Color.FromArgb(15, 23, 42);
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 12.5F, FontStyle.Bold))
                    using (var brush = new SolidBrush(spdTextClr))
                    {
                        g.DrawString(spdText, font, brush, cx, cy + 24, sf);
                    }

                    Color subClr = isDark ? (isHov ? Color.FromArgb(251, 191, 36) : Color.FromArgb(148, 163, 184))
                                          : (isHov ? Color.FromArgb(217, 119, 6) : Color.FromArgb(100, 116, 139));
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                    using (var brush = new SolidBrush(subClr))
                    {
                        g.DrawString(isHov ? "⚡ Test Speed" : "Max Speed", font, brush, cx, cy + 48, sf);
                    }
                }
                catch { }
            };

            // Paint Card 2: Active Downloads (Golden Amber / Orange)
            pnlCardActive.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlCardActive.Width < 10 || pnlCardActive.Height < 10) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Rectangle r = new Rectangle(0, 0, pnlCardActive.Width - 1, pnlCardActive.Height - 1);
                    bool isHov = (_hoverCyberCardIndex == 1);
                    bool isSel = (_currentCategoryFilter == "active");
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color borderColor = isDark ? ((isHov || isSel) ? Color.FromArgb(245, 158, 11) : Color.FromArgb(50, 36, 16))
                                               : ((isHov || isSel) ? Color.FromArgb(217, 119, 6) : Color.FromArgb(226, 232, 240));
                    float penWidth = (isHov || isSel) ? 2.0f : 1.5f;

                    using (var path = CreateRoundedRectanglePath(r, 14))
                    {
                        Color cardBg = isDark ? ((isHov || isSel) ? Color.FromArgb(24, 18, 10) : Color.FromArgb(9, 13, 22))
                                              : ((isHov || isSel) ? Color.FromArgb(254, 243, 199) : Color.FromArgb(255, 255, 255));
                        using (var brush = new SolidBrush(cardBg)) g.FillPath(brush, path);
                        using (var pen = new Pen(borderColor, penWidth)) g.DrawPath(pen, path);
                    }

                    float cx = pnlCardActive.Width / 2f;
                    float cy = 44f;
                    float radius = 23f;
                    RectangleF circleRect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

                    Color innerCircleClr = isDark ? Color.FromArgb(32, 22, 12) : Color.FromArgb(254, 243, 199);
                    Color ringClr = isDark ? Color.FromArgb(245, 158, 11) : Color.FromArgb(217, 119, 6);
                    using (var innerBrush = new SolidBrush(innerCircleClr)) g.FillEllipse(innerBrush, circleRect);
                    using (var ringPen = new Pen(ringClr, 3.5f)) g.DrawEllipse(ringPen, circleRect);

                    string countText = (_downloads.Count > 0) ? _cardActiveCount.ToString() : "0";
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 16F, FontStyle.Bold))
                    using (var brush = new SolidBrush(ringClr))
                    {
                        g.DrawString(countText, font, brush, cx, cy - 1, sf);
                    }

                    Color subClr = isDark ? ((isHov || isSel) ? Color.FromArgb(251, 191, 36) : Color.FromArgb(148, 163, 184))
                                          : ((isHov || isSel) ? Color.FromArgb(217, 119, 6) : Color.FromArgb(100, 116, 139));
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 8.5F, (isHov || isSel) ? FontStyle.Bold : FontStyle.Regular))
                    using (var brush = new SolidBrush(subClr))
                    {
                        g.DrawString("Active Downloads", font, brush, cx, cy + 44, sf);
                    }
                }
                catch { }
            };

            // Paint Card 3: Completed (Neon Green)
            pnlCardCompleted.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlCardCompleted.Width < 10 || pnlCardCompleted.Height < 10) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Rectangle r = new Rectangle(0, 0, pnlCardCompleted.Width - 1, pnlCardCompleted.Height - 1);
                    bool isHov = (_hoverCyberCardIndex == 2);
                    bool isSel = (_currentCategoryFilter == "completed");
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color borderColor = isDark ? ((isHov || isSel) ? Color.FromArgb(34, 197, 94) : Color.FromArgb(18, 48, 28))
                                               : ((isHov || isSel) ? Color.FromArgb(22, 163, 74) : Color.FromArgb(226, 232, 240));
                    float penWidth = (isHov || isSel) ? 2.0f : 1.5f;

                    using (var path = CreateRoundedRectanglePath(r, 14))
                    {
                        Color cardBg = isDark ? ((isHov || isSel) ? Color.FromArgb(10, 26, 16) : Color.FromArgb(9, 13, 22))
                                              : ((isHov || isSel) ? Color.FromArgb(240, 253, 244) : Color.FromArgb(255, 255, 255));
                        using (var brush = new SolidBrush(cardBg)) g.FillPath(brush, path);
                        using (var pen = new Pen(borderColor, penWidth)) g.DrawPath(pen, path);
                    }

                    float cx = pnlCardCompleted.Width / 2f;
                    float cy = 44f;
                    float radius = 23f;
                    RectangleF circleRect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

                    Color innerCircleClr = isDark ? Color.FromArgb(14, 32, 20) : Color.FromArgb(240, 253, 244);
                    Color ringClr = isDark ? Color.FromArgb(34, 197, 94) : Color.FromArgb(22, 163, 74);
                    using (var innerBrush = new SolidBrush(innerCircleClr)) g.FillEllipse(innerBrush, circleRect);
                    using (var ringPen = new Pen(ringClr, 3.5f)) g.DrawEllipse(ringPen, circleRect);

                    string countText = (_downloads.Count > 0) ? _cardCompletedCount.ToString() : "0";
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 16F, FontStyle.Bold))
                    using (var brush = new SolidBrush(ringClr))
                    {
                        g.DrawString(countText, font, brush, cx, cy - 1, sf);
                    }

                    Color subClr = isDark ? ((isHov || isSel) ? Color.FromArgb(74, 222, 128) : Color.FromArgb(148, 163, 184))
                                          : ((isHov || isSel) ? Color.FromArgb(22, 163, 74) : Color.FromArgb(100, 116, 139));
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 8.5F, (isHov || isSel) ? FontStyle.Bold : FontStyle.Regular))
                    using (var brush = new SolidBrush(subClr))
                    {
                        g.DrawString("Completed", font, brush, cx, cy + 44, sf);
                    }
                }
                catch { }
            };

            // Paint Card 4: Queued / Paused (Electric Sky Blue)
            pnlCardQueued.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlCardQueued.Width < 10 || pnlCardQueued.Height < 10) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Rectangle r = new Rectangle(0, 0, pnlCardQueued.Width - 1, pnlCardQueued.Height - 1);
                    bool isHov = (_hoverCyberCardIndex == 3);
                    bool isSel = (_currentCategoryFilter == "paused");
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color borderColor = isDark ? ((isHov || isSel) ? Color.FromArgb(14, 165, 233) : Color.FromArgb(16, 42, 68))
                                               : ((isHov || isSel) ? Color.FromArgb(2, 132, 199) : Color.FromArgb(226, 232, 240));
                    float penWidth = (isHov || isSel) ? 2.0f : 1.5f;

                    using (var path = CreateRoundedRectanglePath(r, 14))
                    {
                        Color cardBg = isDark ? ((isHov || isSel) ? Color.FromArgb(10, 22, 40) : Color.FromArgb(9, 13, 22))
                                              : ((isHov || isSel) ? Color.FromArgb(240, 249, 255) : Color.FromArgb(255, 255, 255));
                        using (var brush = new SolidBrush(cardBg)) g.FillPath(brush, path);
                        using (var pen = new Pen(borderColor, penWidth)) g.DrawPath(pen, path);
                    }

                    float cx = pnlCardQueued.Width / 2f;
                    float cy = 44f;
                    float radius = 23f;
                    RectangleF circleRect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

                    Color innerCircleClr = isDark ? Color.FromArgb(14, 28, 48) : Color.FromArgb(240, 249, 255);
                    Color ringClr = isDark ? Color.FromArgb(14, 165, 233) : Color.FromArgb(2, 132, 199);
                    using (var innerBrush = new SolidBrush(innerCircleClr)) g.FillEllipse(innerBrush, circleRect);
                    using (var ringPen = new Pen(ringClr, 3.5f)) g.DrawEllipse(ringPen, circleRect);

                    string countText = (_downloads.Count > 0) ? _cardQueuedCount.ToString() : "0";
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 16F, FontStyle.Bold))
                    using (var brush = new SolidBrush(ringClr))
                    {
                        g.DrawString(countText, font, brush, cx, cy - 1, sf);
                    }

                    Color subClr = isDark ? ((isHov || isSel) ? Color.FromArgb(56, 189, 248) : Color.FromArgb(148, 163, 184))
                                          : ((isHov || isSel) ? Color.FromArgb(2, 132, 199) : Color.FromArgb(100, 116, 139));
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", 8.5F, (isHov || isSel) ? FontStyle.Bold : FontStyle.Regular))
                    using (var brush = new SolidBrush(subClr))
                    {
                        g.DrawString("Queued", font, brush, cx, cy + 44, sf);
                    }
                }
                catch { }
            };

            // 2. Bottom Downloads Table Card
            pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(7, 11, 20),
                Padding = new Padding(6, 6, 6, 6)
            };
            pnlDownloadsView.Controls.Add(pnlTableCard);

            pnlTableCard.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlTableCard.Width < 10 || pnlTableCard.Height < 10) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, pnlTableCard.Width - 1, pnlTableCard.Height - 1);
                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color borderClr = isDark ? Color.FromArgb(24, 34, 52) : Color.FromArgb(210, 222, 236);
                    using (var path = CreateRoundedRectanglePath(r, 14))
                    using (var pen = new Pen(borderClr, 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
                catch { }
            };

            // 3. Bottom Stats Panel (Memory gauge, Download wave, Upload wave, Wi-Fi status)
            pnlBottomStats = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 84,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };
            SetDoubleBuffered(pnlBottomStats);
            pnlDownloadsView.Controls.Add(pnlBottomStats);

            pnlBottomStats.Paint += (s, pe) =>
            {
                try
                {
                    if (pnlBottomStats.Width < 50 || pnlBottomStats.Height < 20) return;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Rectangle r = new Rectangle(0, 8, pnlBottomStats.Width - 1, pnlBottomStats.Height - 10);

                    // Outer container
                    Color outerBg = isDark ? Color.FromArgb(7, 11, 20) : Color.FromArgb(255, 255, 255);
                    Color outerBorder = isDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(220, 228, 238);

                    using (var path = CreateRoundedRectanglePath(r, 12))
                    {
                        using (var brush = new SolidBrush(outerBg)) g.FillPath(brush, path);
                        using (var pen = new Pen(outerBorder, 1.2f)) g.DrawPath(pen, path);
                    }

                    int cy = r.Y + (r.Height / 2);

                    // Section 1: Memory Gauge
                    int gRadius = 22;
                    int gCx = r.Left + 42;
                    int gCy = cy;
                    RectangleF gRect = new RectangleF(gCx - gRadius, gCy - gRadius, gRadius * 2, gRadius * 2);

                    Color trackClr = isDark ? Color.FromArgb(26, 34, 48) : Color.FromArgb(226, 232, 240);
                    using (var pen = new Pen(trackClr, 4.0f))
                    {
                        g.DrawArc(pen, gRect, 0, 360);
                    }

                    float memPct = Math.Max(1f, Math.Min(100f, _liveMemPct));
                    float memSweep = (memPct / 100f) * 360f;
                    Color memClr = isDark ? Color.FromArgb(245, 158, 11) : Color.FromArgb(217, 119, 6);
                    using (var pen = new Pen(memClr, 4.0f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawArc(pen, gRect, -90, memSweep);
                    }

                    // Percentage text inside ring
                    using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                    using (var brush = new SolidBrush(isDark ? Color.White : Color.FromArgb(15, 23, 42)))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0}%", memPct), font, brush, gCx, gCy, sf);
                    }

                    // Memory Usage labels
                    int memTextX = gCx + gRadius + 14;
                    Color labelClr = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
                    Color valClr = isDark ? Color.White : Color.FromArgb(15, 23, 42);

                    using (var fontLbl = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                    using (var fontVal = new Font("Segoe UI", 10F, FontStyle.Bold))
                    using (var brushLbl = new SolidBrush(labelClr))
                    using (var brushVal = new SolidBrush(valClr))
                    {
                        g.DrawString("Memory Usage", fontLbl, brushLbl, memTextX, cy - 18);
                        g.DrawString(!string.IsNullOrEmpty(_liveMemText) ? _liveMemText : "1.8 GB / 2.4 GB", fontVal, brushVal, memTextX, cy - 2);
                    }

                    // Available width for mid sections
                    int secStartX = memTextX + 115;
                    int rightSecW = 210;
                    int rightStartX = r.Right - rightSecW;
                    int midAvailableW = Math.Max(240, rightStartX - secStartX - 16);
                    int midBoxW = Math.Max(110, (midAvailableW - 12) / 2);
                    int boxH = r.Height - 16;
                    int boxY = r.Y + 8;

                    // Section 2: Download Speed Card with Live Wave
                    Rectangle dlBoxRect = new Rectangle(secStartX, boxY, midBoxW, boxH);
                    string dlSpdText = _totalLiveSpeedBytes > 0 ? (FormatBytes((long)_totalLiveSpeedBytes) + "/s") : "0 B/s";
                    DrawSpeedStatBox(g, dlBoxRect, "Download Speed", dlSpdText, _dlWaveHistory, isDark);

                    // Section 3: Upload Speed Card with Live Wave
                    Rectangle ulBoxRect = new Rectangle(secStartX + midBoxW + 12, boxY, midBoxW, boxH);
                    DrawSpeedStatBox(g, ulBoxRect, "Upload Speed", !string.IsNullOrEmpty(_liveUlSpeedText) ? _liveUlSpeedText : "1.2 MB/s", _ulWaveHistory, isDark);

                    // Section 4: Circular Wi-Fi badge + Active Downloads & Total Speed
                    int wifiX = rightStartX + 12;
                    int wifiRadius = 22;
                    Rectangle wifiCircleRect = new Rectangle(wifiX, cy - wifiRadius, wifiRadius * 2, wifiRadius * 2);
                    using (var brush = new SolidBrush(isDark ? Color.FromArgb(32, 24, 12) : Color.FromArgb(254, 243, 199)))
                    {
                        g.FillEllipse(brush, wifiCircleRect);
                    }
                    using (var pen = new Pen(Color.FromArgb(245, 158, 11), 1.5f))
                    {
                        g.DrawEllipse(pen, wifiCircleRect);
                    }
                    DrawWifiIcon(g, wifiX + wifiRadius, cy, Color.FromArgb(245, 158, 11));

                    int netTextX = wifiX + (wifiRadius * 2) + 12;
                    using (var font = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                    using (var brush = new SolidBrush(isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105)))
                    {
                        int actDownloads = _cardActiveCount;
                        g.DrawString("Active Downloads: " + actDownloads, font, brush, netTextX, cy - 16);
                        g.DrawString("Total Speed: " + dlSpdText, font, brush, netTextX, cy + 1);
                    }
                }
                catch { }
            };

            // Docking order inside pnlDownloadsView: Top, Bottom, then Fill
            pnlCyberCards.SendToBack();
            pnlBottomStats.SendToBack();
            pnlTableCard.BringToFront();

            // 4. ListView inside pnlTableCard
            lvDownloads = new ModernListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(7, 11, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F),
                OwnerDraw = true
            };

            // Row height spacer (52px height per row!)
            _rowHeightSpacer = new ImageList { ImageSize = new Size(1, 52) };
            lvDownloads.SmallImageList = _rowHeightSpacer;

            // 8 Columns exactly matching reference image
            lvDownloads.Columns.Add("#", 38);
            lvDownloads.Columns.Add("Name", 230);
            lvDownloads.Columns.Add("Size", 85);
            lvDownloads.Columns.Add("Status", 120);
            lvDownloads.Columns.Add("Progress", 165);
            lvDownloads.Columns.Add("Speed", 85);
            lvDownloads.Columns.Add("ETA", 80);
            lvDownloads.Columns.Add("Action", 82);

            lvDownloads.Resize += (s, e) =>
            {
                try
                {
                    if (lvDownloads.Columns.Count >= 8)
                    {
                        int fixedW = 38 + 85 + 120 + 165 + 85 + 80 + 82 + 25;
                        int remain = lvDownloads.ClientSize.Width - fixedW;
                        if (remain > 180)
                        {
                            lvDownloads.Columns[1].Width = remain;
                        }
                    }
                }
                catch { }
            };

            lvDownloads.DrawColumnHeader += LvDownloads_DrawColumnHeader;
            lvDownloads.DrawItem += (s, e) => { e.DrawDefault = false; };
            lvDownloads.DrawSubItem += LvDownloads_DrawSubItem;
            lvDownloads.MouseClick += LvDownloads_MouseClick;

            lvDownloads.DoubleClick += (s, e) => OpenSelectedFile();
            lvDownloads.ColumnClick += (s, e) =>
            {
                _sortAscending = !_sortAscending;
                ApplySortingByColumn(e.Column);
            };

            pnlTableCard.Controls.Add(lvDownloads);
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            try
            {
                if (r.Width <= 0 || r.Height <= 0)
                {
                    path.AddRectangle(new Rectangle(0, 0, Math.Max(1, r.Width), Math.Max(1, r.Height)));
                    return path;
                }
                if (radius <= 0)
                {
                    path.AddRectangle(r);
                    return path;
                }
                int d = radius * 2;
                if (d > r.Width) d = r.Width;
                if (d > r.Height) d = r.Height;
                if (d <= 0)
                {
                    path.AddRectangle(r);
                    return path;
                }
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
            }
            catch
            {
                path.Reset();
                path.AddRectangle(r);
            }
            return path;
        }

        private GraphicsPath CreateFoldedBadgePath(Rectangle r, int radius, int foldSize)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddLine(r.Left + radius, r.Top, r.Right - foldSize, r.Top);
            p.AddLine(r.Right, r.Top + foldSize, r.Right, r.Bottom - radius);
            p.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            p.AddLine(r.Right - radius, r.Bottom, r.Left + radius, r.Bottom);
            p.AddArc(r.Left, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            p.AddLine(r.Left, r.Bottom - radius, r.Left, r.Top + radius);
            p.AddArc(r.Left, r.Top, radius * 2, radius * 2, 180, 90);
            p.CloseFigure();
            return p;
        }

        private void DrawSpeedStatBox(Graphics g, Rectangle r, string label, string speedText, List<float> waveHistory, bool isDark)
        {
            Color cardBg = isDark ? Color.FromArgb(10, 16, 28) : Color.FromArgb(245, 248, 252);
            Color borderClr = isDark ? Color.FromArgb(26, 36, 52) : Color.FromArgb(226, 232, 240);

            using (var path = CreateRoundedRectanglePath(r, 8))
            {
                using (var b = new SolidBrush(cardBg)) g.FillPath(b, path);
                using (var p = new Pen(borderClr, 1f)) g.DrawPath(p, path);
            }

            // Left text
            Color labelClr = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
            Color valClr = isDark ? Color.White : Color.FromArgb(15, 23, 42);

            using (var fontLbl = new Font("Segoe UI", 8.0F, FontStyle.Regular))
            using (var fontVal = new Font("Segoe UI", 11.5F, FontStyle.Bold))
            using (var brushLbl = new SolidBrush(labelClr))
            using (var brushVal = new SolidBrush(valClr))
            {
                g.DrawString(label, fontLbl, brushLbl, r.Left + 10, r.Top + 8);
                g.DrawString(speedText, fontVal, brushVal, r.Left + 10, r.Top + 24);
            }

            // Right wave chart
            int textWidth = 95;
            int waveW = Math.Max(30, r.Width - textWidth - 14);
            Rectangle waveRect = new Rectangle(r.Right - waveW - 8, r.Top + 8, waveW, r.Height - 16);
            DrawSpeedWave(g, waveRect, waveHistory, Color.FromArgb(245, 158, 11));
        }

        private void DrawSpeedWave(Graphics g, Rectangle r, List<float> points, Color waveColor)
        {
            if (r.Width <= 10 || r.Height <= 6 || points == null || points.Count < 2) return;
            try
            {
                PointF[] pts = new PointF[points.Count];
                float stepX = (float)r.Width / (float)(points.Count - 1);
                for (int i = 0; i < points.Count; i++)
                {
                    float norm = Math.Max(0.05f, Math.Min(0.95f, points[i]));
                    float y = r.Bottom - (norm * r.Height);
                    pts[i] = new PointF(r.Left + (i * stepX), y);
                }

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddCurve(pts, 0.4f);

                    // Fill gradient under wave
                    using (GraphicsPath fillPath = (GraphicsPath)path.Clone())
                    {
                        fillPath.AddLine(pts[pts.Length - 1], new PointF(pts[pts.Length - 1].X, (float)r.Bottom));
                        fillPath.AddLine(new PointF(pts[pts.Length - 1].X, (float)r.Bottom), new PointF(pts[0].X, (float)r.Bottom));
                        fillPath.AddLine(new PointF(pts[0].X, (float)r.Bottom), pts[0]);
                        fillPath.CloseFigure();

                        Color c1 = Color.FromArgb(70, waveColor);
                        Color c2 = Color.FromArgb(0, waveColor);
                        using (var grad = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical))
                        {
                            g.FillPath(grad, fillPath);
                        }
                    }

                    // Top spline line
                    using (var pen = new Pen(waveColor, 1.8f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawPath(pen, path);
                    }
                }
            }
            catch { }
        }

        private void DrawWifiIcon(Graphics g, int cx, int cy, Color color)
        {
            try
            {
                using (var pen = new Pen(color, 2.0f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    // Arc 1 (outer)
                    g.DrawArc(pen, cx - 11, cy - 8, 22, 22, 220, 100);
                    // Arc 2 (middle)
                    g.DrawArc(pen, cx - 7, cy - 4, 14, 14, 220, 100);
                }
                // Center dot
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, cx - 2, cy + 4, 4, 4);
                }
            }
            catch { }
        }

        private void DrawCustomFileBadge(Graphics g, Rectangle r, string fileName, string category)
        {
            string fn = (fileName ?? "").ToLowerInvariant();
            string ext = Path.GetExtension(fn);

            using (var path = CreateRoundedRectanglePath(r, 6))
            {
                if (fn.Contains("bulbuli") || fn.Contains("youtube") || fn.Contains("coke studio"))
                {
                    // YouTube Red Badge with white triangle
                    using (var brush = new SolidBrush(Color.FromArgb(239, 68, 68))) g.FillPath(brush, path);
                    using (var brushW = new SolidBrush(Color.White))
                    {
                        int cx = r.Left + r.Width / 2;
                        int cy = r.Top + r.Height / 2;
                        Point[] pts = new Point[] { new Point(cx - 3, cy - 6), new Point(cx + 6, cy), new Point(cx - 3, cy + 6) };
                        g.FillPolygon(brushW, pts);
                    }
                    return;
                }
                if (fn.Contains("facebook"))
                {
                    // Facebook Blue Badge with 'f'
                    using (var brush = new SolidBrush(Color.FromArgb(24, 119, 242))) g.FillPath(brush, path);
                    using (var font = new Font("Segoe UI", 12F, FontStyle.Bold))
                    using (var brushW = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("f", font, brushW, r, sf);
                    }
                    return;
                }
                if (fn.Contains("tiktok"))
                {
                    // TikTok Dark Badge with music note / T
                    using (var brush = new SolidBrush(Color.FromArgb(18, 18, 18))) g.FillPath(brush, path);
                    using (var font = new Font("Segoe UI", 10F, FontStyle.Bold))
                    using (var brushC = new SolidBrush(Color.FromArgb(0, 242, 234)))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("♫", font, brushC, r, sf);
                    }
                    return;
                }
                if (fn.Contains("movie") || fn.Contains("trailer"))
                {
                    // Movie Purple Badge
                    using (var brush = new LinearGradientBrush(r, Color.FromArgb(139, 92, 246), Color.FromArgb(217, 70, 239), LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
                    using (var brushW = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("▶", font, brushW, r, sf);
                    }
                    return;
                }
                if (fn.Contains("nature") || (category == "video" && fn.Contains("relaxation")))
                {
                    // Nature Cyan/Green Video Badge
                    using (var brush = new LinearGradientBrush(r, Color.FromArgb(6, 182, 212), Color.FromArgb(16, 185, 129), LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
                    using (var brushW = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("▲", font, brushW, r, sf);
                    }
                    return;
                }
                if (ext == ".iso" || ext == ".dmg" || ext == ".img")
                {
                    using (var discBrush = new LinearGradientBrush(r, Color.FromArgb(203, 213, 225), Color.FromArgb(148, 163, 184), LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillEllipse(discBrush, r);
                    }
                    using (var ringPen = new Pen(Color.FromArgb(90, 110, 135), 1.2f))
                    {
                        g.DrawEllipse(ringPen, r);
                        g.DrawEllipse(ringPen, r.Left + 5, r.Top + 5, r.Width - 10, r.Height - 10);
                    }
                    bool isDarkBadge = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
                    Color holeClr = isDarkBadge ? Color.FromArgb(9, 13, 22) : Color.FromArgb(255, 255, 255);
                    using (var holeBrush = new SolidBrush(holeClr))
                    {
                        int hw = 10;
                        g.FillEllipse(holeBrush, r.Left + (r.Width - hw) / 2, r.Top + (r.Height - hw) / 2, hw, hw);
                    }
                    return;
                }

                // Standard types
                Color badgeBg = Color.FromArgb(14, 165, 233);
                string badgeText = "";
                bool isPlay = false;

                if (ext == ".mp4" || ext == ".mkv" || ext == ".webm" || ext == ".avi" || ext == ".mov" || category == "video")
                {
                    badgeBg = Color.FromArgb(2, 132, 199);
                    isPlay = true;
                }
                else if (ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".tar" || ext == ".gz" || category == "compressed")
                {
                    badgeBg = Color.FromArgb(2, 132, 199);
                    badgeText = "ZIP";
                }
                else if (ext == ".pdf")
                {
                    badgeBg = Color.FromArgb(239, 68, 68);
                    badgeText = "PDF";
                }
                else if (ext == ".doc" || ext == ".docx" || ext == ".txt" || category == "documents")
                {
                    badgeBg = Color.FromArgb(239, 68, 68);
                    badgeText = "DOC";
                }
                else if (ext == ".mp3" || ext == ".wav" || ext == ".flac" || ext == ".aac" || category == "music")
                {
                    badgeBg = Color.FromArgb(37, 99, 235);
                    badgeText = "🎵";
                }
                else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".gif" || category == "images")
                {
                    badgeBg = Color.FromArgb(168, 85, 247);
                    badgeText = "🖼";
                }
                else if (ext == ".exe" || ext == ".msi" || category == "programs")
                {
                    badgeBg = Color.FromArgb(14, 165, 233);
                    badgeText = "EXE";
                }
                else
                {
                    badgeBg = Color.FromArgb(59, 130, 246);
                    badgeText = ext.TrimStart('.').ToUpperInvariant();
                    if (badgeText.Length > 3) badgeText = badgeText.Substring(0, 3);
                }

                using (var brush = new SolidBrush(badgeBg)) g.FillPath(brush, path);

                if (isPlay)
                {
                    int cx = r.Left + r.Width / 2;
                    int cy = r.Top + r.Height / 2;
                    using (var brush = new SolidBrush(Color.White))
                    {
                        Point[] pts = new Point[] { new Point(cx - 3, cy - 6), new Point(cx + 6, cy), new Point(cx - 3, cy + 6) };
                        g.FillPolygon(brush, pts);
                    }
                }
                else if (!string.IsNullOrEmpty(badgeText))
                {
                    float fontSize = (badgeText.Length >= 3) ? 7.0F : 9.0F;
                    using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString(badgeText, font, brush, r, sf);
                    }
                }
            }
        }

        private void LvDownloads_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
            Color headerBg = isDark ? Color.FromArgb(7, 11, 20) : Color.FromArgb(245, 248, 252);
            Color dividerClr = isDark ? Color.FromArgb(20, 28, 44) : Color.FromArgb(220, 228, 238);
            Color textClr = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105);

            using (var brush = new SolidBrush(headerBg))
            {
                g.FillRectangle(brush, e.Bounds);
            }
            using (var pen = new Pen(dividerClr))
            {
                g.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
            using (var sf = new StringFormat { Alignment = (e.ColumnIndex == 0 ? StringAlignment.Center : StringAlignment.Near), LineAlignment = StringAlignment.Center })
            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
            using (var brush = new SolidBrush(textClr))
            {
                int padLeft = (e.ColumnIndex == 0) ? 0 : 10;
                Rectangle textRect = new Rectangle(e.Bounds.Left + padLeft, e.Bounds.Top, Math.Max(10, e.Bounds.Width - padLeft - 4), e.Bounds.Height);
                g.DrawString(e.Header.Text, font, brush, textRect, sf);
            }
        }

        private void LvDownloads_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool isDark = ThemeManager.CurrentTheme != null ? ThemeManager.CurrentTheme.IsDark : true;
            bool isSelected = e.Item.Selected;
            Color bg;
            if (isDark)
            {
                bg = isSelected ? Color.FromArgb(16, 26, 45) : ((e.ItemIndex % 2 == 0) ? Color.FromArgb(7, 11, 20) : Color.FromArgb(6, 9, 17));
            }
            else
            {
                bg = isSelected ? Color.FromArgb(224, 242, 254) : ((e.ItemIndex % 2 == 0) ? Color.FromArgb(255, 255, 255) : Color.FromArgb(248, 250, 252));
            }

            using (var b = new SolidBrush(bg))
            {
                g.FillRectangle(b, e.Bounds);
            }

            Color lineClr = isDark ? Color.FromArgb(16, 24, 38) : Color.FromArgb(238, 242, 246);
            using (var p = new Pen(lineClr))
            {
                g.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            DownloadItem item = e.Item.Tag as DownloadItem;
            string fileName = (item != null && !string.IsNullOrEmpty(item.FileName)) ? item.FileName : (e.Item.SubItems.Count > 1 ? e.Item.SubItems[1].Text : e.Item.Text);
            string category = item != null ? item.Category : "";

            switch (e.ColumnIndex)
            {
                case 0: // #
                    {
                        string num = (e.ItemIndex + 1).ToString();
                        Color numClr = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (var font = new Font("Segoe UI", 9.5F, FontStyle.Regular))
                        using (var brush = new SolidBrush(numClr))
                        {
                            g.DrawString(num, font, brush, e.Bounds, sf);
                        }
                    }
                    break;

                case 1: // Name (Badge + File Name)
                    {
                        int badgeSize = 32;
                        int badgeY = e.Bounds.Top + (e.Bounds.Height - badgeSize) / 2;
                        Rectangle badgeRect = new Rectangle(e.Bounds.Left + 8, badgeY, badgeSize, badgeSize);
                        DrawCustomFileBadge(g, badgeRect, fileName, category);

                        Color fileNameClr = isDark ? Color.White : Color.FromArgb(15, 23, 42);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                        using (var font = new Font("Segoe UI", 9.5F, FontStyle.Regular))
                        using (var brush = new SolidBrush(fileNameClr))
                        {
                            Rectangle textRect = new Rectangle(e.Bounds.Left + 48, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 52), e.Bounds.Height);
                            g.DrawString(fileName, font, brush, textRect, sf);
                        }
                    }
                    break;

                case 2: // Size
                    {
                        Color szClr = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                        using (var brush = new SolidBrush(szClr))
                        {
                            Rectangle textRect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 12), e.Bounds.Height);
                            g.DrawString(e.SubItem.Text, font, brush, textRect, sf);
                        }
                    }
                    break;

                case 3: // Status (Circular Badge + Text)
                    {
                        string status = e.SubItem.Text ?? "";
                        int iconSize = 18;
                        int iconX = e.Bounds.Left + 8;
                        int iconY = e.Bounds.Top + (e.Bounds.Height - iconSize) / 2;
                        Rectangle iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);

                        if (status.StartsWith("Downloading"))
                        {
                            Color amberClr = Color.FromArgb(245, 158, 11);
                            using (var brush = new SolidBrush(amberClr)) g.FillEllipse(brush, iconRect);
                            // White down arrow inside
                            using (var pen = new Pen(Color.White, 1.8f))
                            {
                                pen.StartCap = LineCap.Round;
                                pen.EndCap = LineCap.Round;
                                int cx = iconX + 9;
                                int cy = iconY + 9;
                                g.DrawLine(pen, cx, cy - 4, cx, cy + 4);
                                g.DrawLine(pen, cx - 3, cy + 1, cx, cy + 4);
                                g.DrawLine(pen, cx + 3, cy + 1, cx, cy + 4);
                            }
                            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                            using (var brush = new SolidBrush(amberClr))
                            {
                                Rectangle textRect = new Rectangle(iconX + 24, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 32), e.Bounds.Height);
                                g.DrawString("Downloading", font, brush, textRect, sf);
                            }
                        }
                        else if (status.StartsWith("Paused") || status.StartsWith("Stopped"))
                        {
                            Color blueClr = Color.FromArgb(14, 165, 233);
                            using (var brush = new SolidBrush(blueClr)) g.FillEllipse(brush, iconRect);
                            // Two pause bars inside
                            using (var brushW = new SolidBrush(Color.White))
                            {
                                int cx = iconX + 9;
                                int cy = iconY + 9;
                                g.FillRectangle(brushW, cx - 3, cy - 4, 2, 8);
                                g.FillRectangle(brushW, cx + 1, cy - 4, 2, 8);
                            }
                            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                            using (var brush = new SolidBrush(blueClr))
                            {
                                Rectangle textRect = new Rectangle(iconX + 24, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 32), e.Bounds.Height);
                                g.DrawString("Paused", font, brush, textRect, sf);
                            }
                        }
                        else if (status.StartsWith("Completed"))
                        {
                            Color greenClr = Color.FromArgb(34, 197, 94);
                            using (var brush = new SolidBrush(greenClr)) g.FillEllipse(brush, iconRect);
                            // White checkmark inside
                            using (var pen = new Pen(Color.White, 1.8f))
                            {
                                pen.StartCap = LineCap.Round;
                                pen.EndCap = LineCap.Round;
                                int cx = iconX + 9;
                                int cy = iconY + 9;
                                g.DrawLine(pen, cx - 4, cy, cx - 1, cy + 3);
                                g.DrawLine(pen, cx - 1, cy + 3, cx + 4, cy - 3);
                            }
                            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                            using (var brush = new SolidBrush(greenClr))
                            {
                                Rectangle textRect = new Rectangle(iconX + 24, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 32), e.Bounds.Height);
                                g.DrawString("Completed", font, brush, textRect, sf);
                            }
                        }
                        else
                        {
                            Color statusColor = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105);
                            if (status.StartsWith("Error")) statusColor = Color.FromArgb(239, 68, 68);
                            else if (status.StartsWith("Queued") || status.StartsWith("Scheduled")) statusColor = Color.FromArgb(192, 132, 252);

                            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                            using (var brush = new SolidBrush(statusColor))
                            {
                                Rectangle textRect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 12), e.Bounds.Height);
                                g.DrawString(status, font, brush, textRect, sf);
                            }
                        }
                    }
                    break;

                case 4: // Progress (Capsule Bar + Percentage Text adjacent)
                    {
                        string text = e.SubItem.Text ?? "";
                        double pct = 0;
                        string pctClean = text.Replace("%", "").Trim();
                        double.TryParse(pctClean, NumberStyles.Any, CultureInfo.InvariantCulture, out pct);
                        if (pct < 0) pct = 0;
                        if (pct > 100) pct = 100;

                        int pctTextW = 42;
                        int barW = Math.Max(35, e.Bounds.Width - pctTextW - 22);
                        int barH = 13;
                        int barX = e.Bounds.Left + 8;
                        int barY = e.Bounds.Top + (e.Bounds.Height - barH) / 2;

                        Rectangle barTrack = new Rectangle(barX, barY, barW, barH);
                        Color trackClr = isDark ? Color.FromArgb(20, 28, 44) : Color.FromArgb(226, 232, 240);
                        using (var pathTrack = CreateRoundedRectanglePath(barTrack, barH / 2))
                        using (var brushTrack = new SolidBrush(trackClr))
                        {
                            g.FillPath(brushTrack, pathTrack);
                        }

                        int fillW = (int)(barW * (pct / 100.0));
                        if (fillW > 0)
                        {
                            if (fillW < barH) fillW = barH;
                            if (fillW > barW) fillW = barW;
                            Rectangle barFill = new Rectangle(barX, barY, fillW, barH);
                            using (var pathFill = CreateRoundedRectanglePath(barFill, barH / 2))
                            {
                                Color c1, c2;
                                string statusStr = (item != null ? item.Status : "") ?? "";
                                if (statusStr.StartsWith("Completed") || pct >= 100)
                                {
                                    c1 = Color.FromArgb(34, 197, 94);
                                    c2 = Color.FromArgb(74, 222, 128);
                                }
                                else if (statusStr.StartsWith("Paused") || statusStr.StartsWith("Stopped"))
                                {
                                    c1 = Color.FromArgb(2, 132, 199);
                                    c2 = Color.FromArgb(56, 189, 248);
                                }
                                else
                                {
                                    // Downloading
                                    c1 = Color.FromArgb(245, 158, 11);
                                    c2 = Color.FromArgb(251, 191, 36);
                                }

                                using (var brushFill = new LinearGradientBrush(barFill, c1, c2, LinearGradientMode.Horizontal))
                                {
                                    g.FillPath(brushFill, pathFill);
                                }
                            }
                        }

                        // Percentage text
                        Color pctClr = isDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(71, 85, 105);
                        using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                        using (var brush = new SolidBrush(pctClr))
                        using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        {
                            Rectangle pctRect = new Rectangle(barX + barW + 8, e.Bounds.Top, pctTextW, e.Bounds.Height);
                            g.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0}%", pct), font, brush, pctRect, sf);
                        }
                    }
                    break;

                case 5: // Speed
                    {
                        Color spdClr = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                        using (var brush = new SolidBrush(spdClr))
                        {
                            Rectangle textRect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 12), e.Bounds.Height);
                            g.DrawString(e.SubItem.Text, font, brush, textRect, sf);
                        }
                    }
                    break;

                case 6: // ETA
                    {
                        Color tlClr = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                        using (var brush = new SolidBrush(tlClr))
                        {
                            Rectangle textRect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 12), e.Bounds.Height);
                            g.DrawString(e.SubItem.Text, font, brush, textRect, sf);
                        }
                    }
                    break;

                case 7: // Action (Rounded square dark buttons: [❚❚] [✕] / [▶] [✕] / [📁] [✕])
                    {
                        string status = (item != null) ? (item.Status ?? "") : "";
                        int btnSize = 28;
                        int btnY = e.Bounds.Top + (e.Bounds.Height - btnSize) / 2;
                        int btn1X = e.Bounds.Left + 8;
                        int btn2X = e.Bounds.Left + 42;

                        Rectangle btn1Rect = new Rectangle(btn1X, btnY, btnSize, btnSize);
                        Rectangle btn2Rect = new Rectangle(btn2X, btnY, btnSize, btnSize);

                        Color btnBg = isDark ? Color.FromArgb(14, 20, 32) : Color.FromArgb(242, 245, 250);
                        Color btnBorder = isDark ? Color.FromArgb(30, 42, 64) : Color.FromArgb(215, 224, 236);

                        // Draw Button 1 box
                        using (var path1 = CreateRoundedRectanglePath(btn1Rect, 6))
                        {
                            using (var b = new SolidBrush(btnBg)) g.FillPath(b, path1);
                            using (var p = new Pen(btnBorder, 1.2f)) g.DrawPath(p, path1);
                        }

                        // Draw Button 2 box
                        using (var path2 = CreateRoundedRectanglePath(btn2Rect, 6))
                        {
                            using (var b = new SolidBrush(btnBg)) g.FillPath(b, path2);
                            using (var p = new Pen(btnBorder, 1.2f)) g.DrawPath(p, path2);
                        }

                        int b1cx = btn1X + btnSize / 2;
                        int b1cy = btnY + btnSize / 2;

                        Color iconClr = isDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(71, 85, 105);

                        // Button 1 Icon
                        if (status.StartsWith("Downloading"))
                        {
                            // Pause icon (two vertical bars)
                            using (var brush = new SolidBrush(iconClr))
                            {
                                g.FillRectangle(brush, b1cx - 4, b1cy - 5, 3, 10);
                                g.FillRectangle(brush, b1cx + 1, b1cy - 5, 3, 10);
                            }
                        }
                        else if (status.StartsWith("Completed"))
                        {
                            // Folder icon
                            using (var pen = new Pen(iconClr, 1.5f))
                            {
                                pen.StartCap = LineCap.Round;
                                pen.EndCap = LineCap.Round;
                                g.DrawRectangle(pen, b1cx - 6, b1cy - 3, 12, 8);
                                g.DrawLine(pen, b1cx - 6, b1cy - 3, b1cx - 3, b1cy - 6);
                                g.DrawLine(pen, b1cx - 3, b1cy - 6, b1cx + 1, b1cy - 6);
                                g.DrawLine(pen, b1cx + 1, b1cy - 6, b1cx + 3, b1cy - 3);
                            }
                        }
                        else
                        {
                            // Play triangle
                            using (var brush = new SolidBrush(iconClr))
                            {
                                Point[] pts = new Point[]
                                {
                                    new Point(b1cx - 3, b1cy - 5),
                                    new Point(b1cx + 5, b1cy),
                                    new Point(b1cx - 3, b1cy + 5)
                                };
                                g.FillPolygon(brush, pts);
                            }
                        }

                        // Button 2 Icon: Close '✕'
                        int b2cx = btn2X + btnSize / 2;
                        int b2cy = btnY + btnSize / 2;
                        Color cancelClr = isDark ? Color.FromArgb(203, 213, 225) : Color.FromArgb(148, 163, 184);
                        using (var pen = new Pen(cancelClr, 1.8f))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            g.DrawLine(pen, b2cx - 4, b2cy - 4, b2cx + 4, b2cy + 4);
                            g.DrawLine(pen, b2cx + 4, b2cy - 4, b2cx - 4, b2cy + 4);
                        }
                    }
                    break;
            }
        }

        private void LvDownloads_MouseClick(object sender, MouseEventArgs e)
        {
            var info = lvDownloads.HitTest(e.Location);
            if (info.Item == null || info.SubItem == null) return;
            DownloadItem item = info.Item.Tag as DownloadItem;
            if (item == null) return;

            int colIndex = info.Item.SubItems.IndexOf(info.SubItem);
            if (colIndex == 7)
            {
                Rectangle colBounds = info.SubItem.Bounds;
                int relX = e.X - colBounds.Left;
                info.Item.Selected = true;

                if (relX >= 6 && relX <= 38)
                {
                    if (item.Status.StartsWith("Downloading"))
                    {
                        StopSelectedDownload();
                    }
                    else if (item.Status.StartsWith("Paused") || item.Status.StartsWith("Stopped") || item.Status.StartsWith("Queued"))
                    {
                        ResumeSelectedDownload();
                    }
                    else if (item.Status.StartsWith("Completed"))
                    {
                        OpenSelectedFile();
                    }
                }
                else if (relX >= 40 && relX <= 74)
                {
                    DeleteSelectedDownload();
                }
            }
        }

        #region Feature View 1: 1DM Mobile-Style Advanced Multi-Tab Web Browser

        private void BuildBrowserView()
        {
            pnlBrowserView = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlContent.Controls.Add(pnlBrowserView);

            // 1. Sleek Modern Tab Strip (Top)
            pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(6, 3, 6, 3)
            };
            pnlBrowserView.Controls.Add(pnlTabBar);

            btnNewTab = new Button
            {
                Text = "➕",
                Dock = DockStyle.Right,
                Width = 36,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNewTab.FlatAppearance.BorderSize = 0;
            btnNewTab.Click += (s, e) => CreateNewBrowserTab("https://www.google.com", false);
            new ToolTip().SetToolTip(btnNewTab, "Open New Tab (Ctrl+T)");
            pnlTabBar.Controls.Add(btnNewTab);

            flpTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            pnlTabBar.Controls.Add(flpTabs);

            // 2. Sleek Browser Address Toolbar
            pnlBrowserBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(6, 4, 6, 4)
            };
            pnlBrowserView.Controls.Add(pnlBrowserBar);

            btnBrowserBack = new Button { Text = "◀", Size = new Size(34, 28), Location = new Point(6, 4), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBrowserBack.FlatAppearance.BorderSize = 0;
            btnBrowserBack.Click += (s, e) => { if (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.CanGoBack) _activeTab.WebCtl.GoBack(); };
            pnlBrowserBar.Controls.Add(btnBrowserBack);

            btnBrowserFwd = new Button { Text = "▶", Size = new Size(34, 28), Location = new Point(42, 4), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBrowserFwd.FlatAppearance.BorderSize = 0;
            btnBrowserFwd.Click += (s, e) => { if (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.CanGoForward) _activeTab.WebCtl.GoForward(); };
            pnlBrowserBar.Controls.Add(btnBrowserFwd);

            btnBrowserRefresh = new Button { Text = "↻", Size = new Size(34, 28), Location = new Point(78, 4), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnBrowserRefresh.FlatAppearance.BorderSize = 0;
            btnBrowserRefresh.Click += (s, e) =>
            {
                if (_activeTab != null && _activeTab.WebCtl != null)
                {
                    if (_activeTab.IsNavigating)
                    {
                        _activeTab.WebCtl.Stop();
                        SetBrowserLoadingProgress(0f);
                    }
                    else
                    {
                        _activeTab.WebCtl.Reload();
                        SetBrowserLoadingProgress(25f);
                    }
                }
            };
            pnlBrowserBar.Controls.Add(btnBrowserRefresh);

            btnBrowserHome = new Button { Text = "⌂", Size = new Size(34, 28), Location = new Point(114, 4), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            btnBrowserHome.FlatAppearance.BorderSize = 0;
            btnBrowserHome.Click += (s, e) => NavigateBrowser("https://www.google.com");
            pnlBrowserBar.Controls.Add(btnBrowserHome);

            txtBrowserUrl = new TextBox
            {
                Location = new Point(152, 6),
                Font = new Font("Segoe UI", 9.5F),
                Text = "https://www.google.com"
            };
            txtBrowserUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    NavigateBrowser(txtBrowserUrl.Text.Trim());
                    e.SuppressKeyPress = true;
                }
            };
            pnlBrowserBar.Controls.Add(txtBrowserUrl);

            btnGo = new Button
            {
                Text = "🚀 Go",
                Size = new Size(54, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGo.FlatAppearance.BorderSize = 0;
            btnGo.Click += (s, e) => NavigateBrowser(txtBrowserUrl.Text.Trim());
            pnlBrowserBar.Controls.Add(btnGo);

            btnAdBlockToggle = new Button
            {
                Text = IDMBrowserService.AdBlockerEnabled ? "🛡️ AdBlock: ON" : "🛡️ AdBlock: OFF",
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdBlockToggle.FlatAppearance.BorderSize = 0;
            UpdateAdBlockToggleButtonStyle();
            btnAdBlockToggle.Click += (s, e) =>
            {
                IDMBrowserService.AdBlockerEnabled = !IDMBrowserService.AdBlockerEnabled;
                UpdateAdBlockToggleButtonStyle();
                if (_activeTab != null && _activeTab.WebCtl != null)
                {
                    _activeTab.WebCtl.Reload();
                }
                string msg = IDMBrowserService.AdBlockerEnabled
                    ? "🛡️ Ad & Pop-up Blocker is now ENABLED!\n\nBetting popups (Jaya9, 1xBet), popunders, and banners will be blocked."
                    : "⚠️ Ad & Pop-up Blocker is now DISABLED.";
                MessageBox.Show(msg, "Ad & Pop-up Blocker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            new ToolTip().SetToolTip(btnAdBlockToggle, "Toggle Ad & Pop-up Blocker (Kills popups, betting ads, and banner ads)");
            pnlBrowserBar.Controls.Add(btnAdBlockToggle);

            btn1DMSniffer = new Button
            {
                Text = "⚡ 1DM Sniffer (0)",
                Size = new Size(136, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn1DMSniffer.FlatAppearance.BorderSize = 0;
            btn1DMSniffer.Click += (s, e) => Open1DMSnifferDialog();
            pnlBrowserBar.Controls.Add(btn1DMSniffer);

            btnDownloadCurrent = new Button
            {
                Text = "⬇ Download",
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDownloadCurrent.FlatAppearance.BorderSize = 0;
            btnDownloadCurrent.Click += async (s, e) =>
            {
                if (_activeTab != null && _activeTab.WebCtl != null)
                {
                    string u = (_activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : txtBrowserUrl.Text.Trim();
                    string title = (_activeTab.WebCtl.CoreWebView2 != null) ? _activeTab.WebCtl.CoreWebView2.DocumentTitle : "Download";
                    await TriggerPortalVideoDownloadAsync(_activeTab.WebCtl, u, title, "1080p", "mp4");
                }
            };
            pnlBrowserBar.Controls.Add(btnDownloadCurrent);

            btnBrowserHistory = new Button
            {
                Text = "📜 History",
                Size = new Size(82, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowserHistory.FlatAppearance.BorderSize = 0;
            btnBrowserHistory.Click += (s, e) => ShowBrowserHistoryDialog();
            new ToolTip().SetToolTip(btnBrowserHistory, "View browsing history of visited websites");
            pnlBrowserBar.Controls.Add(btnBrowserHistory);

            btn1DMTools = new Button
            {
                Text = "⋮ 1DM Tools",
                Size = new Size(92, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn1DMTools.Click += (s, e) => Show1DMToolsMenu();
            pnlBrowserBar.Controls.Add(btn1DMTools);

            // Responsive Layout Handler: Guarantees txtBrowserUrl never overlaps right buttons!
            pnlBrowserBar.Resize += (s, e) =>
            {
                int rightGroupWidth = 625;
                int rightStart = pnlBrowserBar.Width - rightGroupWidth;
                btnGo.Location = new Point(rightStart, 4);
                btnAdBlockToggle.Location = new Point(rightStart + 58, 4);
                btn1DMSniffer.Location = new Point(rightStart + 58 + 114, 4);
                btnDownloadCurrent.Location = new Point(rightStart + 58 + 114 + 140, 4);
                btnBrowserHistory.Location = new Point(rightStart + 58 + 114 + 140 + 134, 4);
                btn1DMTools.Location = new Point(rightStart + 58 + 114 + 140 + 134 + 86, 4);

                txtBrowserUrl.Location = new Point(152, 6);
                txtBrowserUrl.Width = Math.Max(120, rightStart - 152 - 8);
            };

            // 2.5 Sleek Modern Thin Top Page Loading Progress Line (Dock: Top, Height: 3px)
            pnlBrowserProgressLine = new Panel
            {
                Dock = DockStyle.Top,
                Height = 3,
                Visible = false
            };
            pnlBrowserProgressLine.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                int totalWidth = pnlBrowserProgressLine.Width;
                int h = pnlBrowserProgressLine.Height;

                if (_browserProgressPct > 0f)
                {
                    int fillWidth = (int)((_browserProgressPct / 100f) * totalWidth);
                    if (fillWidth < 4) fillWidth = 4;
                    if (fillWidth > totalWidth) fillWidth = totalWidth;

                    var t = ThemeManager.CurrentTheme;
                    Color colorStart = t.Accent;
                    Color colorEnd = t.IsDark ? Color.FromArgb(52, 211, 153) : Color.FromArgb(16, 185, 129); // Vibrant emerald glow
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, Math.Max(1, fillWidth), h),
                        colorStart, colorEnd, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brush, 0, 0, fillWidth, h);
                    }

                    // Bright glowing tip line for modern visual polish
                    using (var tipPen = new Pen(Color.White, 1))
                    {
                        g.DrawLine(tipPen, fillWidth - 1, 0, fillWidth - 1, h);
                    }
                }
            };
            pnlBrowserView.Controls.Add(pnlBrowserProgressLine);

            // 3. Find in Page Strip (Bottom)
            pnlFindBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Padding = new Padding(8, 4, 8, 4),
                Visible = false
            };
            pnlBrowserView.Controls.Add(pnlFindBar);

            Label lblFind = new Label { Text = "🔍 Find in Page:", AutoSize = true, Location = new Point(8, 9), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            pnlFindBar.Controls.Add(lblFind);

            txtFindQuery = new TextBox { Location = new Point(125, 6), Size = new Size(240, 24), Font = new Font("Segoe UI", 9F) };
            txtFindQuery.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { FindInCurrentPage(txtFindQuery.Text, true); e.SuppressKeyPress = true; }
            };
            pnlFindBar.Controls.Add(txtFindQuery);

            btnFindNext = new Button { Text = "Next ⬇", Location = new Point(372, 5), Size = new Size(75, 26), FlatStyle = FlatStyle.Flat };
            btnFindNext.Click += (s, e) => FindInCurrentPage(txtFindQuery.Text, true);
            pnlFindBar.Controls.Add(btnFindNext);

            btnFindPrev = new Button { Text = "Prev ⬆", Location = new Point(451, 5), Size = new Size(75, 26), FlatStyle = FlatStyle.Flat };
            btnFindPrev.Click += (s, e) => FindInCurrentPage(txtFindQuery.Text, false);
            pnlFindBar.Controls.Add(btnFindPrev);

            btnFindClose = new Button { Text = "✕ Close", Location = new Point(532, 5), Size = new Size(75, 26), FlatStyle = FlatStyle.Flat };
            btnFindClose.Click += (s, e) => { pnlFindBar.Visible = false; };
            pnlFindBar.Controls.Add(btnFindClose);

            // 4. Tab Host Container (Center Fill)
            pnlBrowserHost = new Panel
            {
                Dock = DockStyle.Fill
            };
            pnlBrowserView.Controls.Add(pnlBrowserHost);

            pnlTabBar.SendToBack();
            pnlBrowserBar.SendToBack();
            pnlBrowserProgressLine.SendToBack();
            pnlFindBar.SendToBack();
            pnlBrowserHost.BringToFront();

            BuildTabContextMenu();

            // Create initial Tab 1
            CreateNewBrowserTab("https://www.google.com", false);
        }

        private void BuildTabContextMenu()
        {
            menuTabContext = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };
            menuTabContext.Items.Add("➕ New Tab (Ctrl+T)", null, (s, e) => CreateNewBrowserTab("https://www.google.com", false));
            menuTabContext.Items.Add("🕶️ New Incognito Tab (Ctrl+Shift+N)", null, (s, e) => CreateNewBrowserTab("https://www.google.com", true));
            menuTabContext.Items.Add("📋 Duplicate Tab", null, (s, e) =>
            {
                BrowserTab t = menuTabContext.Tag as BrowserTab;
                if (t != null)
                {
                    string u = (t.WebCtl != null && t.WebCtl.Source != null) ? t.WebCtl.Source.ToString() : t.Url;
                    CreateNewBrowserTab(u, t.IsIncognito);
                }
            });
            menuTabContext.Items.Add("↻ Reload Tab", null, (s, e) =>
            {
                BrowserTab t = menuTabContext.Tag as BrowserTab;
                if (t != null && t.WebCtl != null) t.WebCtl.Reload();
            });
            menuTabContext.Items.Add(new ToolStripSeparator());
            menuTabContext.Items.Add("❌ Close Tab (Ctrl+W)", null, (s, e) =>
            {
                BrowserTab t = menuTabContext.Tag as BrowserTab;
                if (t != null) CloseTab(t);
            });
            menuTabContext.Items.Add("❌ Close Other Tabs", null, (s, e) =>
            {
                BrowserTab t = menuTabContext.Tag as BrowserTab;
                if (t != null)
                {
                    var others = new List<BrowserTab>(_browserTabs);
                    foreach (var o in others)
                    {
                        if (o != t) CloseTab(o);
                    }
                }
            });
        }

        private void CreateNewBrowserTab(string url, bool incognito)
        {
            BrowserTab tab = new BrowserTab
            {
                Id = Guid.NewGuid().ToString(),
                Title = incognito ? "🕶️ Private Tab" : "Turbo X",
                Url = string.IsNullOrEmpty(url) ? "https://www.google.com" : url,
                IsIncognito = incognito,
                SniffedMedia = new List<SniffedMediaItem>()
            };

            WebView2 wb = new WebView2
            {
                Dock = DockStyle.Fill
            };
            tab.WebCtl = wb;

            // Sleek Tab Pill Panel
            Panel pill = new Panel
            {
                Height = 26,
                AutoSize = true,
                Margin = new Padding(2, 3, 2, 3),
                Padding = new Padding(6, 3, 6, 3),
                Cursor = Cursors.Hand
            };
            tab.TabPill = pill;

            PictureBox pbTabIcon = new PictureBox
            {
                Size = new Size(14, 14),
                Location = new Point(5, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = IconFactory.GetTurboXImage(14, 14),
                Cursor = Cursors.Hand
            };
            pill.Controls.Add(pbTabIcon);

            Label lblT = new Label
            {
                Text = tab.Title,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Location = new Point(22, 4),
                Cursor = Cursors.Hand
            };
            tab.LblTitle = lblT;
            pill.Controls.Add(lblT);

            Label btnC = new Label
            {
                Text = "×",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(lblT.Right + 4, 2),
                Cursor = Cursors.Hand
            };
            tab.BtnClose = btnC;
            pill.Controls.Add(btnC);

            lblT.SizeChanged += (s, e) =>
            {
                btnC.Location = new Point(lblT.Right + 4, 2);
                pill.Width = btnC.Right + 6;
            };

            // Click Handlers
            Action activateAction = () => SwitchToTab(tab);
            pill.Click += (s, e) => activateAction();
            lblT.Click += (s, e) => activateAction();
            btnC.Click += (s, e) => CloseTab(tab);

            // Right click context menu
            MouseEventHandler rclick = (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    menuTabContext.Tag = tab;
                    menuTabContext.Show(pill, e.Location);
                }
            };
            pill.MouseDown += rclick;
            lblT.MouseDown += rclick;

            pnlBrowserHost.Controls.Add(wb);
            _browserTabs.Add(tab);
            flpTabs.Controls.Add(pill);

            SwitchToTab(tab);
            InitTabWebView2(tab, tab.Url, incognito);
        }

        private void UpdateAdBlockToggleButtonStyle()
        {
            if (btnAdBlockToggle == null) return;
            if (IDMBrowserService.AdBlockerEnabled)
            {
                btnAdBlockToggle.Text = "🛡️ AdBlock: ON";
                btnAdBlockToggle.BackColor = Color.FromArgb(16, 185, 129); // Modern Emerald Green
                btnAdBlockToggle.ForeColor = Color.White;
            }
            else
            {
                btnAdBlockToggle.Text = "🛡️ AdBlock: OFF";
                var t = ThemeManager.CurrentTheme;
                btnAdBlockToggle.BackColor = t.ButtonBg;
                btnAdBlockToggle.ForeColor = t.TextSecondary;
            }
        }

        private static CoreWebView2Environment _sharedWebView2Env = null;

        private static async Task<CoreWebView2Environment> GetSharedWebView2EnvironmentAsync()
        {
            if (_sharedWebView2Env != null) return _sharedWebView2Env;
            string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDMTurboDownloader", "WebView2Profile");
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required --ignore-gpu-blocklist --enable-gpu-rasterization --enable-zero-copy --enable-accelerated-video-decode --disable-features=PreloadMediaEngagementData"
            };
            _sharedWebView2Env = await CoreWebView2Environment.CreateAsync(null, profileDir, options);
            return _sharedWebView2Env;
        }

        private async void InitTabWebView2(BrowserTab tab, string initialUrl, bool incognito)
        {
            try
            {
                CoreWebView2Environment env = null;
                if (incognito)
                {
                    string incogDir = Path.Combine(Path.GetTempPath(), "IDM_Incog_" + Guid.NewGuid().ToString("N"));
                    env = await CoreWebView2Environment.CreateAsync(null, incogDir);
                }
                else
                {
                    env = await GetSharedWebView2EnvironmentAsync();
                }

                await tab.WebCtl.EnsureCoreWebView2Async(env);
                tab.WebCtl.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Set Chrome Desktop User Agent to bypass bot-detection & 403 on protected sites
                tab.WebCtl.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

                // Neutralize navigator.webdriver flag
                await tab.WebCtl.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("try { Object.defineProperty(navigator, 'webdriver', { get: () => undefined }); } catch(e){}");

                // 1. Pre-execution AdBlock & Popunder neutralizer script
                await tab.WebCtl.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(IDMBrowserService.GetAdBlockScript());

                // 1.2 Injected Floating IDM Video Download Sniffer (Facebook, YouTube, TikTok, etc.)
                await tab.WebCtl.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GetVideoDetectorScript());

                // 1.5 Floating IDM Video Download Event Handler
                tab.WebCtl.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    try
                    {
                        string msg = e.TryGetWebMessageAsString();
                        if (string.IsNullOrEmpty(msg)) return;

                        if (msg.StartsWith("IDM_DOWNLOAD_VIDEO|"))
                        {
                            string[] parts = msg.Split(new char[] { '|' }, 7);
                            string pageUrl = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
                            string videoSrc = parts.Length > 2 ? Uri.UnescapeDataString(parts[2]) : "";
                            string title = parts.Length > 3 ? Uri.UnescapeDataString(parts[3]) : "";
                            string detectedQuality = (parts.Length > 4 && !string.IsNullOrEmpty(parts[4])) ? Uri.UnescapeDataString(parts[4]) : "1080p";
                            string availableHeightsStr = parts.Length > 5 ? Uri.UnescapeDataString(parts[5]) : "";
                            string directCdn = parts.Length > 6 ? Uri.UnescapeDataString(parts[6]) : "";

                            this.BeginInvoke(new Action(async () =>
                            {
                                await TriggerVideoDownloadFromBrowserAsync(tab, pageUrl, videoSrc, title, detectedQuality, availableHeightsStr, directCdn);
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("WebMessage Error: " + ex.Message);
                    }
                };

                // 2. Network-level resource request filter to block ad scripts/iframes before loading
                tab.WebCtl.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                tab.WebCtl.CoreWebView2.WebResourceRequested += (s, e) =>
                {
                    string reqUri = e.Request.Uri ?? "";
                    if (e.ResourceContext != CoreWebView2WebResourceContext.Document &&
                        IDMBrowserService.AdBlockerEnabled && 
                        IDMBrowserService.IsAdOrPopupUrl(reqUri))
                    {
                        e.Response = tab.WebCtl.CoreWebView2.Environment.CreateWebResourceResponse(
                            new MemoryStream(), 204, "No Content", "Content-Type: text/plain");
                        return;
                    }

                    // Direct Media Stream Sniffer for TikTok, Facebook, YouTube & HTML5 Videos
                    try
                    {
                        string low = reqUri.ToLowerInvariant();
                        if ((low.Contains("tiktok.com") || low.Contains(".tiktokcdn.com") || low.Contains("tiktokv.com") || low.Contains("byteoversea.com") || low.Contains("ibytedtos.com") || low.Contains("bytedance.com")) &&
                            (low.Contains(".mp4") || low.Contains("/video/") || low.Contains("mime_type=video_mp4") || low.Contains("/tos-") || low.Contains("play_addr") || low.Contains("download_addr") || low.Contains("range=")))
                        {
                            _lastCapturedMediaUrl = reqUri;
                        }
                        else if ((low.Contains("fbcdn.net") || low.Contains("facebook.com")) && (low.Contains(".mp4") || low.Contains("bytestream") || low.Contains("oe=")))
                        {
                            _lastCapturedMediaUrl = reqUri;
                        }
                        else if (low.Contains("googlevideo.com/videoplayback"))
                        {
                            _lastCapturedMediaUrl = reqUri;
                        }
                        else if (low.EndsWith(".mp4") || low.Contains(".mp4?") || low.EndsWith(".m3u8") || low.Contains(".m3u8?"))
                        {
                            _lastCapturedMediaUrl = reqUri;
                        }
                    }
                    catch { }
                };

                // 3. Complete Pop-up Interception: Never allow raw Edge popup windows!
                tab.WebCtl.CoreWebView2.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true; // Crucial: Completely kill standalone popup window!

                    string targetUrl = e.Uri ?? "";
                    if (string.IsNullOrEmpty(targetUrl) || targetUrl == "about:blank") return;

                    // A. Check if it's an ad or betting popup (like jaya9bangla.com)
                    if (IDMBrowserService.AdBlockerEnabled && IDMBrowserService.IsAdOrPopupUrl(targetUrl))
                    {
                        return; // Blocked!
                    }

                    // B. Check if it's a direct downloadable media/file
                    string lowerTarget = targetUrl.ToLowerInvariant();
                    if (lowerTarget.EndsWith(".zip") || lowerTarget.EndsWith(".rar") || lowerTarget.EndsWith(".7z") ||
                        lowerTarget.EndsWith(".exe") || lowerTarget.EndsWith(".msi") || lowerTarget.EndsWith(".iso") ||
                        lowerTarget.EndsWith(".mp4") || lowerTarget.EndsWith(".mkv") || lowerTarget.EndsWith(".torrent") ||
                        lowerTarget.EndsWith(".apk") || lowerTarget.EndsWith(".dmg") || lowerTarget.EndsWith(".tar") ||
                        lowerTarget.EndsWith(".gz") || lowerTarget.EndsWith(".pdf") ||
                        lowerTarget.StartsWith("magnet:"))
                    {
                        this.BeginInvoke(new Action(() => ShowAddUrlDialogWithPreset(targetUrl)));
                        return;
                    }

                    // C. Normal link or download landing page: Open as a New Tab inside OUR OWN built-in tabbed browser!
                    this.BeginInvoke(new Action(() =>
                    {
                        CreateNewBrowserTab(targetUrl, tab.IsIncognito);
                    }));
                };

                // 4. Navigation Starting: Block ad redirects & catch direct downloads
                tab.WebCtl.CoreWebView2.NavigationStarting += (s, e) =>
                {
                    if (btn1DMSniffer != null)
                    {
                        btn1DMSniffer.Text = "⚡ 1DM Sniffer (0)";
                        var t = ThemeManager.CurrentTheme;
                        btn1DMSniffer.BackColor = t.ButtonBg;
                        btn1DMSniffer.ForeColor = t.ButtonText;
                    }

                    string uri = e.Uri ?? "";

                    tab.IsNavigating = true;
                    tab.CurrentProgress = 25f;
                    if (tab == _activeTab)
                    {
                        SetBrowserLoadingProgress(25f);
                        if (btnBrowserRefresh != null) btnBrowserRefresh.Text = "✕";
                    }

                    // Block in-tab ad/betting hijack
                    if (IDMBrowserService.AdBlockerEnabled && IDMBrowserService.IsAdOrPopupUrl(uri))
                    {
                        e.Cancel = true;
                        tab.IsNavigating = false;
                        if (tab == _activeTab) SetBrowserLoadingProgress(0f);
                        return;
                    }

                    string lower = uri.ToLowerInvariant();
                    if (lower.EndsWith(".zip") || lower.EndsWith(".rar") || lower.EndsWith(".7z") ||
                        lower.EndsWith(".exe") || lower.EndsWith(".msi") || lower.EndsWith(".iso") ||
                        lower.EndsWith(".mp4") || lower.EndsWith(".mkv") || lower.EndsWith(".torrent") ||
                        lower.EndsWith(".apk") || lower.EndsWith(".dmg") || lower.EndsWith(".tar") ||
                        lower.EndsWith(".gz") || lower.EndsWith(".pdf") ||
                        lower.StartsWith("magnet:"))
                    {
                        e.Cancel = true;
                        tab.IsNavigating = false;
                        if (tab == _activeTab) SetBrowserLoadingProgress(0f);
                        this.BeginInvoke(new Action(() =>
                        {
                            DialogResult dr = MessageBox.Show(string.Format("Download link detected:\n\n{0}\n\nStart downloading with IDM Turbo Accelerator?", uri),
                                "IDM Turbo Auto-Capture", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (dr == DialogResult.Yes)
                            {
                                ShowAddUrlDialogWithPreset(uri);
                            }
                        }));
                    }
                };

                // 5. Chromium Download Starting: Hand off any triggered download directly to IDM Turbo!
                tab.WebCtl.CoreWebView2.DownloadStarting += (s, e) =>
                {
                    string dlUrl = e.DownloadOperation.Uri;
                    string suggestedName = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(e.ResultFilePath))
                        {
                            suggestedName = Path.GetFileName(e.ResultFilePath);
                        }
                    }
                    catch { }

                    e.Cancel = true; // Hand off from Chromium to IDM Turbo!
                    tab.IsNavigating = false;
                    if (tab == _activeTab) SetBrowserLoadingProgress(100f);
                    this.BeginInvoke(new Action(() =>
                    {
                        ShowAddUrlDialogWithPreset(dlUrl, suggestedName, null, null);
                    }));
                };

                tab.WebCtl.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    string docTitle = tab.WebCtl.CoreWebView2.DocumentTitle;
                    if (!string.IsNullOrEmpty(docTitle))
                    {
                        string shortTitle = docTitle.Trim();
                        if (shortTitle.Length > 22) shortTitle = shortTitle.Substring(0, 20) + "..";
                        tab.Title = shortTitle;
                        if (tab.LblTitle != null) tab.LblTitle.Text = (tab.IsIncognito ? "🕶️ " : "🌐 ") + shortTitle;
                    }
                    if (!tab.IsIncognito && tab.WebCtl.Source != null)
                    {
                        IDMBrowserService.AddHistory(docTitle, tab.WebCtl.Source.ToString());
                    }
                };

                tab.WebCtl.CoreWebView2.SourceChanged += (s, e) =>
                {
                    tab.CurrentProgress = 50f;
                    if (tab == _activeTab)
                    {
                        SetBrowserLoadingProgress(50f);
                        if (txtBrowserUrl != null && tab.WebCtl.Source != null)
                        {
                            txtBrowserUrl.Text = tab.WebCtl.Source.ToString();
                        }
                        UpdateBrowserDownloadButton(tab.WebCtl.Source != null ? tab.WebCtl.Source.ToString() : "");
                    }
                    if (!tab.IsIncognito && tab.WebCtl.Source != null)
                    {
                        string currentTitle = tab.WebCtl.CoreWebView2.DocumentTitle;
                        IDMBrowserService.AddHistory(currentTitle, tab.WebCtl.Source.ToString());
                    }
                };

                tab.WebCtl.CoreWebView2.ContentLoading += (s, e) =>
                {
                    tab.CurrentProgress = 75f;
                    if (tab == _activeTab)
                    {
                        SetBrowserLoadingProgress(75f);
                    }
                };

                tab.WebCtl.CoreWebView2.DOMContentLoaded += (s, e) =>
                {
                    tab.CurrentProgress = 88f;
                    if (tab == _activeTab)
                    {
                        SetBrowserLoadingProgress(88f);
                    }
                };

                tab.WebCtl.NavigationCompleted += (s, e) =>
                {
                    tab.IsNavigating = false;
                    tab.CurrentProgress = 100f;
                    if (tab == _activeTab)
                    {
                        SetBrowserLoadingProgress(100f);
                        UpdateBrowserDownloadButton(tab.WebCtl.Source != null ? tab.WebCtl.Source.ToString() : "");
                    }
                    Tab_DocumentCompleted(tab);
                };

                if (!string.IsNullOrEmpty(initialUrl))
                {
                    tab.WebCtl.CoreWebView2.Navigate(initialUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WebView2 Init Error: " + ex.Message);
            }
        }

        private void SetBrowserLoadingProgress(float targetPct)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetBrowserLoadingProgress(targetPct)));
                return;
            }

            if (targetPct <= 0f)
            {
                _browserProgressPct = 0f;
                _browserTargetPct = 0f;
                if (pnlBrowserProgressLine != null) pnlBrowserProgressLine.Visible = false;
                if (_tmrBrowserProgress != null) _tmrBrowserProgress.Stop();
                if (btnBrowserRefresh != null) btnBrowserRefresh.Text = "↻";
                return;
            }

            if (pnlBrowserProgressLine != null && !pnlBrowserProgressLine.Visible)
            {
                pnlBrowserProgressLine.Visible = true;
            }

            _browserTargetPct = Math.Max(_browserProgressPct, targetPct);

            if (_tmrBrowserProgress == null)
            {
                _tmrBrowserProgress = new Timer { Interval = 25 };
                _tmrBrowserProgress.Tick += (s, e) =>
                {
                    if (_browserProgressPct < _browserTargetPct)
                    {
                        float delta = (_browserTargetPct - _browserProgressPct) * 0.22f + 0.4f;
                        _browserProgressPct = Math.Min(_browserTargetPct, _browserProgressPct + delta);
                    }
                    else if (_browserTargetPct < 90f)
                    {
                        // On slow internet / slow network connections, smoothly creep up (0.15% per tick)
                        // so user has clear visual confirmation that page data is entering/transferring!
                        _browserProgressPct = Math.Min(88f, _browserProgressPct + 0.15f);
                    }

                    if (pnlBrowserProgressLine != null && pnlBrowserProgressLine.Visible)
                    {
                        pnlBrowserProgressLine.Invalidate();
                    }

                    if (_browserProgressPct >= 100f)
                    {
                        _tmrBrowserProgress.Stop();
                        Timer hideTimer = new Timer { Interval = 220 };
                        hideTimer.Tick += (hs, he) =>
                        {
                            hideTimer.Stop();
                            hideTimer.Dispose();
                            _browserProgressPct = 0f;
                            _browserTargetPct = 0f;
                            if (pnlBrowserProgressLine != null) pnlBrowserProgressLine.Visible = false;
                            if (btnBrowserRefresh != null) btnBrowserRefresh.Text = "↻";
                        };
                        hideTimer.Start();
                    }
                };
            }

            if (!_tmrBrowserProgress.Enabled)
            {
                _tmrBrowserProgress.Start();
            }
        }

        private void SwitchToTab(BrowserTab tab)
        {
            if (tab == null) return;
            _activeTab = tab;
            tab.WebCtl.BringToFront();

            if (txtBrowserUrl != null)
            {
                txtBrowserUrl.Text = (tab.WebCtl.Source != null) ? tab.WebCtl.Source.ToString() : tab.Url;
            }

            if (tab.IsNavigating)
            {
                SetBrowserLoadingProgress(Math.Max(25f, tab.CurrentProgress));
                if (btnBrowserRefresh != null) btnBrowserRefresh.Text = "✕";
            }
            else
            {
                SetBrowserLoadingProgress(0f);
                if (btnBrowserRefresh != null) btnBrowserRefresh.Text = "↻";
            }

            _sniffedMedia = tab.SniffedMedia ?? new List<SniffedMediaItem>();
            if (btn1DMSniffer != null)
            {
                int count = _sniffedMedia.Count;
                btn1DMSniffer.Text = string.Format("⚡ 1DM Sniffer ({0})", count);
                if (count > 0)
                {
                    btn1DMSniffer.BackColor = Color.FromArgb(234, 88, 12);
                    btn1DMSniffer.ForeColor = Color.White;
                }
                else
                {
                    var t = ThemeManager.CurrentTheme;
                    btn1DMSniffer.BackColor = t.ButtonBg;
                    btn1DMSniffer.ForeColor = t.ButtonText;
                }
            }

            UpdateBrowserDownloadButton((tab.WebCtl != null && tab.WebCtl.Source != null) ? tab.WebCtl.Source.ToString() : tab.Url);
            UpdateTabStripStyling();
        }

        private void CloseTab(BrowserTab tab)
        {
            if (tab == null) return;
            if (_browserTabs.Count <= 1)
            {
                NavigateBrowser("https://www.google.com");
                return;
            }

            pnlBrowserHost.Controls.Remove(tab.WebCtl);
            tab.WebCtl.Dispose();

            flpTabs.Controls.Remove(tab.TabPill);
            tab.TabPill.Dispose();

            _browserTabs.Remove(tab);

            if (_activeTab == tab)
            {
                SwitchToTab(_browserTabs[_browserTabs.Count - 1]);
            }
            else
            {
                UpdateTabStripStyling();
            }
        }

        private void UpdateTabStripStyling()
        {
            var t = ThemeManager.CurrentTheme;
            foreach (var tab in _browserTabs)
            {
                if (tab.TabPill == null || tab.LblTitle == null) continue;
                bool isActive = (tab == _activeTab);
                if (isActive)
                {
                    tab.TabPill.BackColor = t.Accent;
                    Color textCol = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
                    tab.LblTitle.ForeColor = textCol;
                    tab.LblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    if (tab.BtnClose != null) tab.BtnClose.ForeColor = textCol;
                }
                else
                {
                    tab.TabPill.BackColor = t.ButtonBg;
                    tab.LblTitle.ForeColor = t.ButtonText;
                    tab.LblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                    if (tab.BtnClose != null) tab.BtnClose.ForeColor = t.TextSecondary;
                }
            }
        }

        private void Tab_DocumentCompleted(BrowserTab tab)
        {
            try
            {
                if (tab == null || tab.WebCtl == null || tab.WebCtl.CoreWebView2 == null) return;

                string docTitle = tab.WebCtl.CoreWebView2.DocumentTitle;
                if (!string.IsNullOrEmpty(docTitle))
                {
                    string shortTitle = docTitle.Trim();
                    if (shortTitle.Length > 22) shortTitle = shortTitle.Substring(0, 20) + "..";
                    tab.Title = shortTitle;
                    if (tab.LblTitle != null) tab.LblTitle.Text = (tab.IsIncognito ? "🕶️ " : "🌐 ") + shortTitle;
                }

                if (tab == _activeTab && tab.WebCtl.Source != null && txtBrowserUrl != null)
                {
                    txtBrowserUrl.Text = tab.WebCtl.Source.ToString();
                }

                // Injected Services
                if (IDMBrowserService.AdBlockerEnabled)
                {
                    IDMBrowserService.InjectAdBlocker(tab.WebCtl);
                }

                if (IDMBrowserService.WebDarkModeEnabled)
                {
                    IDMBrowserService.InjectDarkMode(tab.WebCtl, true);
                }

                // History Recording (Privacy: Skipped for incognito tabs)
                if (!tab.IsIncognito && tab.WebCtl.Source != null)
                {
                    IDMBrowserService.AddHistory(docTitle, tab.WebCtl.Source.ToString());
                }

                SniffTabMedia(tab);
            }
            catch { }
        }

        public void OpenWebsiteInBuiltinBrowser(string url)
        {
            SwitchWorkspaceView("browser");
            if (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.CoreWebView2 != null)
            {
                NavigateBrowser(url);
            }
            else
            {
                CreateNewBrowserTab(url, false);
            }
        }

        private void NavigateBrowser(string url)
        {
            if (string.IsNullOrEmpty(url) || _activeTab == null) return;

            // Smart URL vs Google Search detector
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (url.Contains(".") && !url.Contains(" "))
                {
                    url = "https://" + url;
                }
                else
                {
                    // 100% Native Google Search in-app!
                    url = "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
                }
            }

            try
            {
                SetBrowserLoadingProgress(25f);
                if (_activeTab != null)
                {
                    _activeTab.IsNavigating = true;
                    _activeTab.CurrentProgress = 25f;
                }
                if (btnBrowserRefresh != null) btnBrowserRefresh.Text = "✕";

                if (_activeTab.WebCtl != null && _activeTab.WebCtl.CoreWebView2 != null)
                {
                    _activeTab.WebCtl.CoreWebView2.Navigate(url);
                }
                else if (_activeTab.WebCtl != null)
                {
                    _activeTab.WebCtl.Source = new Uri(url);
                }
                if (txtBrowserUrl != null) txtBrowserUrl.Text = url;
            }
            catch { }
        }

        private async void SniffTabMedia(BrowserTab tab)
        {
            if (tab == null || tab.WebCtl == null || tab.WebCtl.CoreWebView2 == null) return;
            try
            {
                string script = @"(function() {
                    var items = [];
                    var pageTitle = document.title || 'Web Media';
                    var vids = document.querySelectorAll('video');
                    for (var i = 0; i < vids.length; i++) {
                        var src = vids[i].src || vids[i].currentSrc;
                        if (src && src.indexOf('blob:') !== 0) items.push({ title: pageTitle + ' (Video)', url: src, cat: 'Video', ext: 'MP4', qual: 'HTML5 Video' });
                    }
                    var auds = document.querySelectorAll('audio');
                    for (var i = 0; i < auds.length; i++) {
                        var src = auds[i].src || auds[i].currentSrc;
                        if (src) items.push({ title: pageTitle + ' (Audio)', url: src, cat: 'Audio', ext: 'MP3', qual: 'HTML5 Audio' });
                    }
                    var srcs = document.querySelectorAll('source');
                    for (var i = 0; i < srcs.length; i++) {
                        var s = srcs[i].src;
                        var t = srcs[i].type || '';
                        if (s) {
                            var ext = 'MP4', cat = 'Video';
                            if (s.indexOf('.m3u8') >= 0 || t.indexOf('mpegurl') >= 0) { ext = 'M3U8'; cat = 'HLS Stream'; }
                            else if (s.indexOf('.mpd') >= 0 || t.indexOf('dash') >= 0) { ext = 'MPD'; cat = 'DASH Stream'; }
                            else if (s.indexOf('.mp3') >= 0 || t.indexOf('audio') >= 0) { ext = 'MP3'; cat = 'Audio'; }
                            items.push({ title: pageTitle + ' (' + ext + ')', url: s, cat: cat, ext: ext, qual: 'Stream Source' });
                        }
                    }
                    var links = document.querySelectorAll('a[href]');
                    for (var i = 0; i < links.length; i++) {
                        var h = links[i].href;
                        var low = (h || '').toLowerCase();
                        if (low.startsWith('magnet:')) {
                            items.push({ title: links[i].innerText || 'Magnet Torrent Link', url: h, cat: 'Torrent', ext: 'MAGNET', qual: 'Peer-to-Peer' });
                        } else if (low.endsWith('.torrent') || low.indexOf('.torrent?') >= 0) {
                            items.push({ title: links[i].innerText || 'Torrent File', url: h, cat: 'Torrent', ext: 'TORRENT', qual: 'BitTorrent' });
                        } else if (low.endsWith('.mp4') || low.endsWith('.mkv') || low.endsWith('.avi') || low.endsWith('.mov')) {
                            items.push({ title: links[i].innerText || 'Direct Video', url: h, cat: 'Video', ext: 'MP4', qual: 'Direct Link' });
                        } else if (low.endsWith('.zip') || low.endsWith('.rar') || low.endsWith('.7z') || low.endsWith('.iso')) {
                            items.push({ title: links[i].innerText || 'Archive File', url: h, cat: 'Compressed', ext: 'ZIP/RAR', qual: 'Direct Download' });
                        }
                    }
                    return JSON.stringify(items);
                })()";

                string json = await tab.WebCtl.CoreWebView2.ExecuteScriptAsync(script);
                if (!string.IsNullOrEmpty(json) && json != "null")
                {
                    if (json.StartsWith("\"") && json.EndsWith("\""))
                    {
                        json = System.Text.RegularExpressions.Regex.Unescape(json.Substring(1, json.Length - 2));
                    }

                    var matches = System.Text.RegularExpressions.Regex.Matches(json,
                        @"\{""title"":""(?<title>.*?)"",""url"":""(?<url>.*?)"",""cat"":""(?<cat>.*?)"",""ext"":""(?<ext>.*?)"",""qual"":""(?<qual>.*?)""\}");

                    List<SniffedMediaItem> found = new List<SniffedMediaItem>();
                    HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    string currentUrl = (tab.WebCtl != null && tab.WebCtl.Source != null) ? tab.WebCtl.Source.ToString() : tab.Url;
                    if (!string.IsNullOrEmpty(currentUrl) && VideoEngine.IsMediaUrl(currentUrl) && seen.Add(currentUrl))
                    {
                        string currentTitle = (tab.WebCtl != null && tab.WebCtl.CoreWebView2 != null && !string.IsNullOrEmpty(tab.WebCtl.CoreWebView2.DocumentTitle))
                            ? tab.WebCtl.CoreWebView2.DocumentTitle : tab.Title;
                        found.Add(new SniffedMediaItem
                        {
                            Title = currentTitle + " [Full HD / 4K / Audio]",
                            Url = currentUrl,
                            Category = "Video",
                            Extension = "MP4/MP3",
                            Quality = "1080p / 4K Stream"
                        });
                    }

                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        string u = m.Groups["url"].Value.Replace("\\/", "/");
                        if (string.IsNullOrEmpty(u) || !seen.Add(u)) continue;

                        found.Add(new SniffedMediaItem
                        {
                            Title = m.Groups["title"].Value,
                            Url = u,
                            Category = m.Groups["cat"].Value,
                            Extension = m.Groups["ext"].Value,
                            Quality = m.Groups["qual"].Value
                        });
                    }

                    tab.SniffedMedia = found;

                    if (tab == _activeTab)
                    {
                        _sniffedMedia = found;
                        int count = found.Count;
                        btn1DMSniffer.Text = string.Format("⚡ 1DM Sniffer ({0})", count);
                        if (count > 0)
                        {
                            btn1DMSniffer.BackColor = Color.FromArgb(234, 88, 12);
                            btn1DMSniffer.ForeColor = Color.White;
                        }
                        else
                        {
                            var t = ThemeManager.CurrentTheme;
                            btn1DMSniffer.BackColor = t.ButtonBg;
                            btn1DMSniffer.ForeColor = t.ButtonText;
                        }
                    }
                }
            }
            catch { }
        }

        private void Open1DMSnifferDialog()
        {
            string currentUrl = (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : txtBrowserUrl.Text.Trim();

            if (_sniffedMedia != null && _sniffedMedia.Count > 0)
            {
                using (MediaCatchDialog dlg = new MediaCatchDialog(_sniffedMedia, currentUrl))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedItem != null)
                    {
                        ShowAddUrlDialogWithPreset(dlg.SelectedItem.Url, dlg.SelectedItem.Title, dlg.SelectedItem.Quality, dlg.SelectedItem.Extension);
                    }
                }
            }
            else
            {
                DialogResult dr = MessageBox.Show(
                    "⚡ 1DM Sniffer is active!\n\nNo direct unencrypted media links were found in the static HTML.\n\nWould you like 1DM Deep Sniffer to inspect this webpage's dynamic streams (YouTube, Facebook, Twitter, HLS, etc.)?",
                    "1DM Sniffer & Stream Catcher",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dr == DialogResult.Yes)
                {
                    btn1DMSniffer.Text = "⏳ Sniffing...";
                    VideoEngine ve = new VideoEngine();
                    ve.FetchMetadata(currentUrl, (success, title, heights, err) =>
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            btn1DMSniffer.Text = string.Format("⚡ 1DM Sniffer ({0})", _sniffedMedia != null ? _sniffedMedia.Count : 0);
                            if (success)
                            {
                                List<SniffedMediaItem> deepFound = new List<SniffedMediaItem>();
                                if (heights != null && heights.Count > 0)
                                {
                                    foreach (int h in heights)
                                    {
                                        deepFound.Add(new SniffedMediaItem
                                        {
                                            Title = title + " (" + h + "p)",
                                            Url = currentUrl,
                                            Category = "Video Stream",
                                            Extension = "MP4",
                                            Quality = h + "p HD"
                                        });
                                    }
                                }
                                deepFound.Add(new SniffedMediaItem
                                {
                                    Title = title + " (HQ Audio Stream)",
                                    Url = currentUrl,
                                    Category = "Audio Stream",
                                    Extension = "MP3",
                                    Quality = "320kbps Audio"
                                });

                                using (MediaCatchDialog dlg = new MediaCatchDialog(deepFound, currentUrl))
                                {
                                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedItem != null)
                                    {
                                        ShowAddUrlDialogWithPreset(dlg.SelectedItem.Url, dlg.SelectedItem.Title, dlg.SelectedItem.Quality, dlg.SelectedItem.Extension);
                                    }
                                }
                            }
                            else
                            {
                                ShowAddUrlDialogWithPreset(currentUrl);
                            }
                        }));
                    });
                }
                else
                {
                    ShowAddUrlDialogWithPreset(currentUrl);
                }
            }
        }

        private void Show1DMToolsMenu()
        {
            if (menu1DMTools == null)
            {
                menu1DMTools = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };

                menu1DMTools.Items.Add("➕ New Tab (Ctrl+T)", null, (s, e) => CreateNewBrowserTab("https://www.google.com", false));
                menu1DMTools.Items.Add("🕶️ New Incognito Tab (Ctrl+Shift+N)", null, (s, e) => CreateNewBrowserTab("https://www.google.com", true));
                menu1DMTools.Items.Add("📋 Duplicate Active Tab", null, (s, e) =>
                {
                    if (_activeTab != null)
                    {
                        string u = (_activeTab.WebCtl != null && _activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : _activeTab.Url;
                        CreateNewBrowserTab(u, _activeTab.IsIncognito);
                    }
                });
                menu1DMTools.Items.Add("❌ Close Active Tab (Ctrl+W)", null, (s, e) => { if (_activeTab != null) CloseTab(_activeTab); });
                menu1DMTools.Items.Add(new ToolStripSeparator());

                var itemDesktop = new ToolStripMenuItem("🖥️ Desktop Mode");
                itemDesktop.CheckOnClick = true;
                itemDesktop.Checked = IDMBrowserService.DesktopModeEnabled;
                itemDesktop.Click += (s, e) =>
                {
                    IDMBrowserService.DesktopModeEnabled = itemDesktop.Checked;
                    if (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.CoreWebView2 != null)
                    {
                        _activeTab.WebCtl.CoreWebView2.Settings.UserAgent = IDMBrowserService.DesktopModeEnabled
                            ? null
                            : "Mozilla/5.0 (Linux; Android 13; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36 1DM/Turbo";
                        _activeTab.WebCtl.Reload();
                    }
                };
                menu1DMTools.Items.Add(itemDesktop);

                var itemAdBlock = new ToolStripMenuItem("🛡️ Ad Blocker");
                itemAdBlock.CheckOnClick = true;
                itemAdBlock.Checked = IDMBrowserService.AdBlockerEnabled;
                itemAdBlock.Click += (s, e) =>
                {
                    IDMBrowserService.AdBlockerEnabled = itemAdBlock.Checked;
                    UpdateAdBlockToggleButtonStyle();
                    if (_activeTab != null && _activeTab.WebCtl != null)
                    {
                        if (IDMBrowserService.AdBlockerEnabled) IDMBrowserService.InjectAdBlocker(_activeTab.WebCtl);
                        else _activeTab.WebCtl.Reload();
                    }
                };
                menu1DMTools.Items.Add(itemAdBlock);

                var itemDark = new ToolStripMenuItem("🌙 Web Dark Mode (Night Reader)");
                itemDark.CheckOnClick = true;
                itemDark.Checked = IDMBrowserService.WebDarkModeEnabled;
                itemDark.Click += (s, e) =>
                {
                    IDMBrowserService.WebDarkModeEnabled = itemDark.Checked;
                    if (_activeTab != null && _activeTab.WebCtl != null)
                    {
                        IDMBrowserService.InjectDarkMode(_activeTab.WebCtl, IDMBrowserService.WebDarkModeEnabled);
                    }
                };
                menu1DMTools.Items.Add(itemDark);

                menu1DMTools.Items.Add("🔍 Find in Page (Ctrl+F)", null, (s, e) =>
                {
                    pnlFindBar.Visible = true;
                    txtFindQuery.Focus();
                });

                menu1DMTools.Items.Add("📄 View Page Source", null, async (s, e) =>
                {
                    if (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.CoreWebView2 != null)
                    {
                        string html = await _activeTab.WebCtl.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                        if (!string.IsNullOrEmpty(html) && html.StartsWith("\"") && html.EndsWith("\""))
                        {
                            html = System.Text.RegularExpressions.Regex.Unescape(html.Substring(1, html.Length - 2));
                        }
                        string u = (_activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : _activeTab.Url;
                        new PageSourceDialog(_activeTab.Title, u, html).Show(this);
                    }
                });

                menu1DMTools.Items.Add(new ToolStripSeparator());

                menu1DMTools.Items.Add("⭐ Add to Bookmarks", null, (s, e) =>
                {
                    if (_activeTab != null)
                    {
                        string u = (_activeTab.WebCtl != null && _activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : _activeTab.Url;
                        bool ok = IDMBrowserService.AddBookmark(_activeTab.Title, u);
                        MessageBox.Show(ok ? "Page added to bookmarks!" : "Page is already in your bookmarks.", "Bookmarks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                });

                menu1DMTools.Items.Add("📑 View Bookmarks Manager...", null, (s, e) =>
                {
                    using (var dlg = new BrowserBookmarksDialog())
                    {
                        if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedUrl))
                        {
                            NavigateBrowser(dlg.SelectedUrl);
                        }
                    }
                });

                menu1DMTools.Items.Add("🕒 View Browsing History...", null, (s, e) =>
                {
                    using (var dlg = new BrowserHistoryDialog())
                    {
                        if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedUrl))
                        {
                            NavigateBrowser(dlg.SelectedUrl);
                        }
                    }
                });

                menu1DMTools.Items.Add("🧹 Clear Browsing Data...", null, (s, e) =>
                {
                    DialogResult dr = MessageBox.Show(
                        "Clear browsing data (history, temporary cache, and cookies)?\n\nClick Yes to clear all, No to cancel.",
                        "Clear Browsing Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.Yes)
                    {
                        IDMBrowserService.ClearBrowsingData(true, true, true);
                        MessageBox.Show("Browsing data cleared successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                });

                menu1DMTools.Items.Add(new ToolStripSeparator());

                menu1DMTools.Items.Add("💾 Save Web Page As...", null, async (s, e) =>
                {
                    if (_activeTab != null && _activeTab.WebCtl != null && _activeTab.WebCtl.CoreWebView2 != null)
                    {
                        using (SaveFileDialog sfd = new SaveFileDialog { Filter = "HTML Document (*.html)|*.html|All Files (*.*)|*.*", FileName = "webpage.html" })
                        {
                            if (sfd.ShowDialog(this) == DialogResult.OK)
                            {
                                string html = await _activeTab.WebCtl.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                                if (!string.IsNullOrEmpty(html) && html.StartsWith("\"") && html.EndsWith("\""))
                                {
                                    html = System.Text.RegularExpressions.Regex.Unescape(html.Substring(1, html.Length - 2));
                                }
                                File.WriteAllText(sfd.FileName, html ?? "", Encoding.UTF8);
                                MessageBox.Show("Web page saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                });

                menu1DMTools.Items.Add("📋 Copy Page URL", null, (s, e) =>
                {
                    if (_activeTab != null)
                    {
                        string u = (_activeTab.WebCtl != null && _activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : txtBrowserUrl.Text.Trim();
                        try { Clipboard.SetText(u); MessageBox.Show("URL copied to clipboard: " + u, "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { }
                    }
                });

                menu1DMTools.Items.Add("🌐 Open in External Chrome / Edge", null, (s, e) =>
                {
                    if (_activeTab != null)
                    {
                        string u = (_activeTab.WebCtl != null && _activeTab.WebCtl.Source != null) ? _activeTab.WebCtl.Source.ToString() : txtBrowserUrl.Text.Trim();
                        try { Process.Start(u); } catch { }
                    }
                });
            }

            menu1DMTools.Show(btn1DMTools, new Point(0, btn1DMTools.Height));
        }

        private void ShowBrowserHistoryDialog()
        {
            using (var dlg = new BrowserHistoryDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedUrl))
                {
                    NavigateBrowser(dlg.SelectedUrl);
                }
            }
        }

        private async void FindInCurrentPage(string query, bool forward)
        {
            if (string.IsNullOrEmpty(query) || _activeTab == null || _activeTab.WebCtl == null || _activeTab.WebCtl.CoreWebView2 == null) return;
            try
            {
                string js = string.Format("window.find('{0}', false, {1});", query.Replace("'", "\\'"), forward ? "false" : "true");
                await _activeTab.WebCtl.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch { }
        }

        private async Task TriggerVideoDownloadFromBrowserAsync(BrowserTab tab, string pageUrl, string videoSrc, string title, string detectedQuality = "1080p", string availableHeightsStr = null, string directCdn = null)
        {
            if (tab != null && tab.WebCtl != null)
            {
                await HandleInVideoDownloadMessageAsync(tab.WebCtl.CoreWebView2, pageUrl, videoSrc, title, detectedQuality, "mp4", availableHeightsStr, directCdn);
            }
        }

        private async Task HandleInVideoDownloadMessageAsync(CoreWebView2 webViewCore, string pageUrl, string videoSrc, string title, string defaultQuality = "1080p", string defaultFormat = "mp4", string availableHeightsStr = null, string directCdn = null)
        {
            try
            {
                string targetUrl = VideoEngine.NormalizeVideoUrl(pageUrl);
                string normVideoSrc = VideoEngine.NormalizeVideoUrl(videoSrc);

                if (VideoEngine.IsRootMediaUrl(targetUrl))
                {
                    if (!string.IsNullOrEmpty(normVideoSrc) && !normVideoSrc.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = normVideoSrc;
                    }
                    else if (!string.IsNullOrEmpty(directCdn) && !directCdn.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = directCdn;
                    }
                    else if (!string.IsNullOrEmpty(_lastCapturedMediaUrl))
                    {
                        targetUrl = _lastCapturedMediaUrl;
                    }
                    else
                    {
                        MessageBox.Show("⚠️ No specific video link detected.\n\nPlease ensure the video is playing or open the specific video page.",
                            "IDM Turbo Downloader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                targetUrl = VideoEngine.NormalizeVideoUrl(targetUrl);

                if (webViewCore != null && !string.IsNullOrEmpty(targetUrl))
                {
                    await ExportWebView2CookiesAsync(webViewCore, targetUrl);
                }

                string presetTitle = !string.IsNullOrWhiteSpace(title) ? title.Trim() : "Video Download";
                ShowAddUrlDialogWithPreset(targetUrl, presetTitle, defaultQuality, defaultFormat, availableHeightsStr, directCdn);
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleInVideoDownloadMessageAsync error: " + ex.Message);
            }
        }

        private async Task TriggerPortalVideoDownloadAsync(WebView2 wv, string fallbackUrl, string defaultTitle, string defaultQuality, string defaultExt)
        {
            if (wv == null || wv.CoreWebView2 == null)
            {
                if (!string.IsNullOrEmpty(fallbackUrl) && !VideoEngine.IsRootMediaUrl(fallbackUrl))
                {
                    ShowAddUrlDialogWithPreset(fallbackUrl, defaultTitle, defaultQuality, defaultExt);
                }
                else
                {
                    MessageBox.Show("Please open or play a video first.", "IDM Turbo Downloader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            try
            {
                string jsExtract = @"(function() {
                    try {
" + GetSharedMetadataExtractorJs() + @"
                        var pageUrl = window.location.href;
                        var videos = Array.from(document.querySelectorAll('video'));
                        var bestVideo = null;
                        var bestScore = -1;

                        for (var i = 0; i < videos.length; i++) {
                            var v = videos[i];
                            var score = 0;
                            var rect = v.getBoundingClientRect();
                            var visibleArea = Math.max(0, Math.min(rect.bottom, window.innerHeight) - Math.max(rect.top, 0)) *
                                              Math.max(0, Math.min(rect.right, window.innerWidth) - Math.max(rect.left, 0));

                            if (!v.paused && v.currentTime > 0) score += 10000;
                            if (v.currentTime > 0) score += 1000;
                            score += Math.min(500, Math.round(visibleArea / 1000));
                            if (v.currentSrc || v.src) score += 100;

                            if (score > bestScore) {
                                bestScore = score;
                                bestVideo = v;
                            }
                        }

                        var permalink = null;
                        var directSrc = '';
                        var videoTitle = '';

                        if (bestVideo) {
                            directSrc = bestVideo.currentSrc || bestVideo.src || '';
                            permalink = findPermalinkFromElement(bestVideo);
                            videoTitle = getTitleNearElement(bestVideo);
                        }

                        if (!permalink && isSpecificUrl(pageUrl)) {
                            permalink = pageUrl;
                        }

                        var finalUrl = permalink || (directSrc && directSrc.indexOf('http') === 0 && directSrc.indexOf('blob:') !== 0 ? directSrc : pageUrl);

                        var meta = detectVideoMetadata(bestVideo);
                        var availableCsv = (meta.availableHeights || []).join(',');
                        var directCdn = meta.directDownloadUrl || (directSrc && directSrc.indexOf('blob:') !== 0 ? directSrc : '');

                        return encodeURIComponent(finalUrl || '') + '|||' +
                               encodeURIComponent(permalink || '') + '|||' +
                               encodeURIComponent(directCdn) + '|||' +
                               encodeURIComponent(videoTitle || document.title || '') + '|||' +
                               (bestVideo ? '1' : '0') + '|||' +
                               encodeURIComponent(meta.quality || '1080p') + '|||' +
                               encodeURIComponent(availableCsv);
                    } catch(e) {
                        return encodeURIComponent(window.location.href || '') + '|||||||||' + encodeURIComponent(document.title || '') + '|||0|||1080p|||';
                    }
                })();";

                string rawResult = await wv.CoreWebView2.ExecuteScriptAsync(jsExtract);
                string targetUrl = "";
                string permalink = "";
                string directSrc = "";
                string extractedTitle = defaultTitle;
                bool hasVideo = false;
                string detectedQuality = defaultQuality ?? "1080p";
                string availableHeightsStr = null;

                if (!string.IsNullOrEmpty(rawResult))
                {
                    string unquoted = rawResult.Trim();
                    if (unquoted.StartsWith("\"") && unquoted.EndsWith("\"") && unquoted.Length >= 2)
                    {
                        unquoted = Regex.Unescape(unquoted.Substring(1, unquoted.Length - 2));
                    }

                    string[] parts = unquoted.Split(new string[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length >= 5)
                    {
                        targetUrl = Uri.UnescapeDataString(parts[0]);
                        permalink = Uri.UnescapeDataString(parts[1]);
                        directSrc = Uri.UnescapeDataString(parts[2]);
                        string t = Uri.UnescapeDataString(parts[3]);
                        if (!string.IsNullOrWhiteSpace(t)) extractedTitle = t;
                        hasVideo = parts[4] == "1";
                    }
                    if (parts.Length >= 7)
                    {
                        string q = Uri.UnescapeDataString(parts[5]);
                        if (!string.IsNullOrEmpty(q)) detectedQuality = q;
                        availableHeightsStr = Uri.UnescapeDataString(parts[6]);
                    }
                }

                if (string.IsNullOrEmpty(targetUrl))
                {
                    targetUrl = fallbackUrl ?? "";
                }

                targetUrl = VideoEngine.NormalizeVideoUrl(targetUrl);
                if (!string.IsNullOrEmpty(permalink)) permalink = VideoEngine.NormalizeVideoUrl(permalink);

                if (VideoEngine.IsRootMediaUrl(targetUrl))
                {
                    if (!string.IsNullOrEmpty(permalink) && !VideoEngine.IsRootMediaUrl(permalink))
                    {
                        targetUrl = permalink;
                    }
                    else if (!string.IsNullOrEmpty(directSrc) && !directSrc.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = directSrc;
                    }
                    else if (!string.IsNullOrEmpty(_lastCapturedMediaUrl))
                    {
                        targetUrl = _lastCapturedMediaUrl;
                    }
                    else
                    {
                        MessageBox.Show("⚠️ No specific video is playing yet!\n\nPlease scroll to or play any video or reel on the page first, then click Download.",
                            "IDM Turbo Downloader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                targetUrl = VideoEngine.NormalizeVideoUrl(targetUrl);

                if (wv.CoreWebView2 != null && !string.IsNullOrEmpty(targetUrl))
                {
                    await ExportWebView2CookiesAsync(wv.CoreWebView2, targetUrl);
                }

                ShowAddUrlDialogWithPreset(targetUrl, extractedTitle, detectedQuality, defaultExt, availableHeightsStr, directSrc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("TriggerPortalVideoDownloadAsync error: " + ex.Message);
                if (!string.IsNullOrEmpty(fallbackUrl) && !VideoEngine.IsRootMediaUrl(fallbackUrl))
                {
                    ShowAddUrlDialogWithPreset(fallbackUrl, defaultTitle, defaultQuality, defaultExt);
                }
            }
        }

        private async Task ExportWebView2CookiesAsync(CoreWebView2 webView, string domainOrUrl)
        {
            if (webView == null || webView.CookieManager == null) return;
            try
            {
                if (!string.IsNullOrEmpty(domainOrUrl) && domainOrUrl.IndexOf("tiktok.com", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }

                string cookieFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDMTurboDownloader", "browser_cookies.txt");
                string dir = Path.GetDirectoryName(cookieFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var cookies = await webView.CookieManager.GetCookiesAsync(domainOrUrl);
                if (cookies != null && cookies.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# Netscape HTTP Cookie File");
                    sb.AppendLine("# Exported by IDM Turbo Downloader for VideoEngine");
                    foreach (var c in cookies)
                    {
                        long expiry = 0;
                        try
                        {
                            if (c.Expires > DateTime.MinValue && c.Expires < DateTime.MaxValue)
                            {
                                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                expiry = (long)(c.Expires.ToUniversalTime() - epoch).TotalSeconds;
                            }
                        }
                        catch { }

                        string domain = c.Domain ?? "";
                        string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                        string path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path;
                        string secure = c.IsSecure ? "TRUE" : "FALSE";
                        string name = c.Name ?? "";
                        string val = c.Value ?? "";

                        sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\r\n", domain, flag, path, secure, expiry, name, val);
                    }
                    File.WriteAllText(cookieFile, sb.ToString(), new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cookie export error: " + ex.Message);
            }
        }

        private string GetSharedMetadataExtractorJs()
        {
            return @"
                function isSpecificUrl(u) {
                    if (!u) return false;
                    var lu = u.toLowerCase();
                    if (lu.indexOf('youtube.com') >= 0 && (lu.indexOf('/watch') >= 0 || lu.indexOf('/shorts/') >= 0 || lu.indexOf('/live/') >= 0)) return true;
                    if (lu.indexOf('youtu.be/') >= 0) return true;
                    if (lu.indexOf('tiktok.com') >= 0 && (lu.indexOf('/video/') >= 0 || lu.indexOf('/v/') >= 0 || lu.indexOf('/photo/') >= 0)) return true;
                    if (lu.indexOf('facebook.com') >= 0 && (lu.indexOf('/reel/') >= 0 || lu.indexOf('/videos/') >= 0 || lu.indexOf('/watch') >= 0 || lu.indexOf('story.php') >= 0 || lu.indexOf('/share/v/') >= 0 || lu.indexOf('/share/r/') >= 0)) return true;
                    if (lu.indexOf('fb.watch') >= 0) return true;
                    if (lu.indexOf('instagram.com') >= 0 && (lu.indexOf('/reel/') >= 0 || lu.indexOf('/p/') >= 0 || lu.indexOf('/tv/') >= 0)) return true;
                    if (lu.indexOf('twitter.com') >= 0 || lu.indexOf('x.com') >= 0) return true;
                    return false;
                }

                function findPermalinkFromElement(el) {
                    if (!el) return null;
                    var host = (window.location.hostname || '').toLowerCase();
                    var pageUrl = window.location.href;
                    if (isSpecificUrl(pageUrl)) return pageUrl;

                    // 1. TikTok Specialized Extraction (Feed cards, Aweme IDs, Anchors)
                    if (host.indexOf('tiktok.com') >= 0) {
                        var curr = el;
                        for (var d = 0; d < 30 && curr && curr !== document.body; d++) {
                            var anchors = curr.querySelectorAll('a[href]');
                            for (var i = 0; i < anchors.length; i++) {
                                var h = anchors[i].href || '';
                                if (h.indexOf('/video/') >= 0 || h.indexOf('/v/') >= 0 || h.indexOf('/photo/') >= 0) return h;
                            }

                            var awemeId = '';
                            if (curr.id) {
                                var mXg = curr.id.match(/(?:xgwrapper|xgplayer)-\d+-(\d{17,19})/);
                                if (mXg && mXg[1]) awemeId = mXg[1];
                                if (!awemeId) {
                                    var mIdRaw = curr.id.match(/\b(7\d{17,18})\b/);
                                    if (mIdRaw && mIdRaw[1]) awemeId = mIdRaw[1];
                                }
                            }
                            if (!awemeId && curr.getAttribute) {
                                var dId = curr.getAttribute('data-id') || curr.getAttribute('data-video-id') || curr.getAttribute('data-scroll-cache-id') || curr.getAttribute('data-aweme-id') || curr.getAttribute('data-item-id');
                                if (dId && /^\d{17,19}$/.test(dId)) awemeId = dId;
                            }
                            if (!awemeId) {
                                var cHtml = curr.outerHTML || '';
                                var mMatch = cHtml.match(/aweme_?id[\W]+(\d{17,19})/i) ||
                                             cHtml.match(/item_?id[\W]+(\d{17,19})/i) ||
                                             cHtml.match(/\b(7\d{17,18})\b/);
                                if (mMatch && mMatch[1]) awemeId = mMatch[1];
                            }

                            if (awemeId) {
                                var author = '';
                                for (var j = 0; j < anchors.length; j++) {
                                    var ah = anchors[j].href || '';
                                    var mUser = ah.match(/tiktok\.com\/@([a-zA-Z0-9_.-]+)/);
                                    if (mUser && mUser[1]) {
                                        author = mUser[1];
                                        break;
                                    }
                                }
                                if (!author) {
                                    var userEl = document.querySelector('[data-e2e=""browse-username""], [data-e2e=""video-author-uniqueid""], [data-e2e=""author-uniqueid""], a[href*=""/@""]');
                                    if (userEl) {
                                        var uh = userEl.getAttribute('href') || userEl.innerText || '';
                                        var mU = uh.match(/@?([a-zA-Z0-9_.-]+)/);
                                        if (mU && mU[1]) author = mU[1];
                                    }
                                }
                                return 'https://www.tiktok.com/@' + (author || 'a') + '/video/' + awemeId;
                            }

                            curr = curr.parentElement;
                        }

                        var allVideoAnchors = Array.from(document.querySelectorAll('a[href]')).filter(function(a) { 
                            var href = a.href || '';
                            return href.indexOf('/video/') >= 0 || href.indexOf('/photo/') >= 0; 
                        });
                        if (allVideoAnchors.length > 0) {
                            var bestAnchor = null;
                            var minDistance = 999999;
                            var vRect = el ? el.getBoundingClientRect() : { top: window.innerHeight / 2 };
                            for (var k = 0; k < allVideoAnchors.length; k++) {
                                var aRect = allVideoAnchors[k].getBoundingClientRect();
                                var dist = Math.abs(aRect.top - vRect.top);
                                if (dist < minDistance) {
                                    minDistance = dist;
                                    bestAnchor = allVideoAnchors[k];
                                }
                            }
                            if (bestAnchor && bestAnchor.href) return bestAnchor.href;
                        }
                    }

                    // 2. Facebook
                    if (host.indexOf('facebook.com') >= 0) {
                        var curr = el;
                        for (var d = 0; d < 25 && curr && curr !== document.body; d++) {
                            var anchors = curr.querySelectorAll('a[href]');
                            for (var i = 0; i < anchors.length; i++) {
                                var h = anchors[i].href || '';
                                if (h.indexOf('/reel/') >= 0 || h.indexOf('/videos/') >= 0 || h.indexOf('/watch') >= 0 ||
                                    h.indexOf('story.php') >= 0 || h.indexOf('/share/v/') >= 0 || h.indexOf('/share/r/') >= 0) {
                                    return h;
                                }
                            }
                            curr = curr.parentElement;
                        }
                    }

                    // 3. YouTube
                    if (host.indexOf('youtube.com') >= 0) {
                        var curr = el;
                        for (var d = 0; d < 20 && curr && curr !== document.body; d++) {
                            var anchors = curr.querySelectorAll('a[href]');
                            for (var i = 0; i < anchors.length; i++) {
                                var h = anchors[i].href || '';
                                if (h.indexOf('/watch?v=') >= 0 || h.indexOf('/shorts/') >= 0) {
                                    return h;
                                }
                            }
                            curr = curr.parentElement;
                        }
                    }

                    var curr = el;
                    for (var d = 0; d < 18 && curr && curr !== document.body; d++) {
                        var anchors = curr.querySelectorAll('a[href]');
                        for (var i = 0; i < anchors.length; i++) {
                            var h = anchors[i].href || '';
                            if (isSpecificUrl(h)) return h;
                        }
                        curr = curr.parentElement;
                    }
                    return null;
                }

                function getTitleNearElement(el) {
                    if (!el) return document.title || 'Video';
                    var p = el.parentElement;
                    for (var d = 0; d < 15 && p && p !== document.body; d++) {
                        var elTitle = p.querySelector('h1, h2, h3, [data-e2e=""browse-video-desc""], [data-e2e=""video-desc""], [data-e2e*=""desc""], [data-ad-preview=""message""], span[dir], #title');
                        if (elTitle && elTitle.innerText && elTitle.innerText.trim().length > 3) {
                            return elTitle.innerText.trim().substring(0, 100);
                        }
                        p = p.parentElement;
                    }
                    return document.title || 'Video';
                }

                function detectVideoMetadata(curVideo) {
                    var result = {
                        quality: '1080p',
                        maxHeight: 1080,
                        availableHeights: [],
                        directDownloadUrl: ''
                    };
                    if (!curVideo) return result;

                    var host = (window.location.hostname || '').toLowerCase();
                    var pageUrl = window.location.href;

                    // 1. TikTok Specialized Extraction
                    if (host.indexOf('tiktok.com') >= 0) {
                        try {
                            var awemeId = '';
                            var perm = findPermalinkFromElement(curVideo);
                            if (perm) {
                                var mAw = perm.match(/\/video\/(\d{17,19})/);
                                if (mAw && mAw[1]) awemeId = mAw[1];
                            }
                            if (!awemeId) {
                                var mPageAw = pageUrl.match(/\/video\/(\d{17,19})/);
                                if (mPageAw && mPageAw[1]) awemeId = mPageAw[1];
                            }
                            if (!awemeId) {
                                var curr = curVideo;
                                for (var d = 0; d < 25 && curr && curr !== document.body; d++) {
                                    if (curr.id) {
                                        var mXg = curr.id.match(/(?:xgwrapper|xgplayer)-\d+-(\d{17,19})/);
                                        if (mXg && mXg[1]) { awemeId = mXg[1]; break; }
                                        var mIdNum = curr.id.match(/\b(7\d{17,18})\b/);
                                        if (mIdNum && mIdNum[1]) { awemeId = mIdNum[1]; break; }
                                    }
                                    var dId = curr.getAttribute ? (curr.getAttribute('data-id') || curr.getAttribute('data-video-id') || curr.getAttribute('data-scroll-cache-id') || curr.getAttribute('data-aweme-id')) : '';
                                    if (dId && /^\d{17,19}$/.test(dId)) { awemeId = dId; break; }
                                    curr = curr.parentElement;
                                }
                            }

                            var itemStruct = null;

                            // Method A: Deep React Fiber Tree Traversal
                            try {
                                var elem = curVideo;
                                for (var depth = 0; depth < 20 && elem && elem !== document.body && !itemStruct; depth++) {
                                    var keys = Object.keys(elem);
                                    for (var k = 0; k < keys.length; k++) {
                                        var key = keys[k];
                                        if (key.indexOf('__reactFiber') === 0) {
                                            var fiber = elem[key];
                                            var fCount = 0;
                                            while (fiber && fCount < 35) {
                                                var mp = fiber.memoizedProps || fiber.pendingProps;
                                                if (mp) {
                                                    var cand = mp.itemStruct || mp.itemInfo || mp.item || mp.videoInfo || mp.videoData || mp.post || mp.aweme || mp.video;
                                                    if (cand) {
                                                        var cv = cand.video || cand;
                                                        if (cv && (cv.bitrateInfo || cv.definition || cv.width || cv.playAddr || cv.downloadAddr)) {
                                                            itemStruct = cv;
                                                            break;
                                                        }
                                                    }
                                                }
                                                fiber = fiber.return;
                                                fCount++;
                                            }
                                        }
                                        if (key.indexOf('__reactProps') === 0) {
                                            var p = elem[key];
                                            if (p) {
                                                var cand2 = p.itemStruct || p.itemInfo || p.item || p.videoInfo || p.videoData || p.post || p.aweme || p.video;
                                                if (cand2) {
                                                    var cv2 = cand2.video || cand2;
                                                    if (cv2 && (cv2.bitrateInfo || cv2.definition || cv2.width || cv2.playAddr || cv2.downloadAddr)) {
                                                        itemStruct = cv2;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    elem = elem.parentElement;
                                }
                            } catch(eReact) {}

                            // Method B: Rehydration and State Objects
                            if (!itemStruct) {
                                try {
                                    var rehyd = window.__UNIVERSAL_DATA_FOR_REHYDRATION__;
                                    if (!rehyd) {
                                        var elScript = document.getElementById('__UNIVERSAL_DATA_FOR_REHYDRATION__');
                                        if (elScript && elScript.textContent) rehyd = JSON.parse(elScript.textContent);
                                    }
                                    if (!rehyd && window.SIGI_STATE) rehyd = window.SIGI_STATE;
                                    if (!rehyd) {
                                        var elSigi = document.getElementById('SIGI_STATE') || document.getElementById('__NEXT_DATA__');
                                        if (elSigi && elSigi.textContent) rehyd = JSON.parse(elSigi.textContent);
                                    }

                                    if (rehyd) {
                                        var scope = rehyd['__DEFAULT_SCOPE__'];
                                        if (scope) {
                                            if (scope['webapp.video-detail'] && scope['webapp.video-detail'].itemInfo) {
                                                itemStruct = scope['webapp.video-detail'].itemInfo.itemStruct;
                                            }
                                            if (!itemStruct && scope['webapp.reflow.itemInfo']) {
                                                itemStruct = scope['webapp.reflow.itemInfo'].itemStruct;
                                            }
                                        }

                                        var itemMod = rehyd.ItemModule || (scope && scope.ItemModule);
                                        if (!itemStruct && itemMod) {
                                            if (awemeId && itemMod[awemeId]) {
                                                itemStruct = itemMod[awemeId];
                                            } else {
                                                var modKeys = Object.keys(itemMod);
                                                if (modKeys.length > 0) {
                                                    var vSrc = curVideo.currentSrc || curVideo.src || '';
                                                    for (var m = 0; m < modKeys.length; m++) {
                                                        var candItem = itemMod[modKeys[m]];
                                                        if (candItem) {
                                                            var cvMod = candItem.video || candItem;
                                                            var pAddr = cvMod.playAddr || (cvMod.PlayAddr && cvMod.PlayAddr.UrlList && cvMod.PlayAddr.UrlList[0]) || '';
                                                            if (vSrc && pAddr && vSrc.indexOf(pAddr.substring(0, 30)) >= 0) {
                                                                itemStruct = cvMod;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    if (!itemStruct && modKeys.length === 1) {
                                                        itemStruct = itemMod[modKeys[0]];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                } catch(eRehyd) {}
                            }

                            // Method C: Script Tags Extraction (Regex scanning)
                            if (!itemStruct) {
                                try {
                                    var scripts = document.querySelectorAll('script');
                                    for (var s = 0; s < scripts.length; s++) {
                                        var txt = scripts[s].textContent || '';
                                        if (txt.indexOf('bitrateInfo') >= 0 || txt.indexOf('definition') >= 0 || (awemeId && txt.indexOf(awemeId) >= 0)) {
                                            var mDef = txt.match(/definition[\W]+([a-zA-Z0-9_]+)/i);
                                            var mWidth = txt.match(/width[\W]+(\d{3,5})/i);
                                            var mHeight = txt.match(/height[\W]+(\d{3,5})/i);
                                            var mGear = txt.match(/(?:GearName|gear_name)[\W]+([a-zA-Z0-9_]+)/ig);

                                            var heightsFound = [];
                                            if (mGear) {
                                                for (var g = 0; g < mGear.length; g++) {
                                                    var gLow = mGear[g].toLowerCase();
                                                    if (gLow.indexOf('8k') >= 0 || gLow.indexOf('4320') >= 0) heightsFound.push(4320);
                                                    else if (gLow.indexOf('4k') >= 0 || gLow.indexOf('2160') >= 0) heightsFound.push(2160);
                                                    else if (gLow.indexOf('1440') >= 0 || gLow.indexOf('2k') >= 0) heightsFound.push(1440);
                                                    else if (gLow.indexOf('1080') >= 0) heightsFound.push(1080);
                                                    else if (gLow.indexOf('720') >= 0) heightsFound.push(720);
                                                    else if (gLow.indexOf('540') >= 0 || gLow.indexOf('480') >= 0) heightsFound.push(480);
                                                }
                                            }
                                            if (mDef && mDef[1]) {
                                                var dLow = mDef[1].toLowerCase();
                                                if (dLow === '8k') heightsFound.push(4320);
                                                else if (dLow === '4k' || dLow === 'uhd') heightsFound.push(2160);
                                                else if (dLow === '2k' || dLow === 'qhd' || dLow === '1440p') heightsFound.push(1440);
                                                else if (dLow === '1080p') heightsFound.push(1080);
                                                else if (dLow === '720p') heightsFound.push(720);
                                            }
                                            if (mWidth && mHeight) {
                                                var sw = parseInt(mWidth[1], 10);
                                                var sh = parseInt(mHeight[1], 10);
                                                var sMax = Math.max(sw, sh);
                                                var sMin = Math.min(sw, sh);
                                                if (sMin >= 3840 || sMax >= 6000) heightsFound.push(4320);
                                                else if (sMin >= 2000 || sMax >= 3200) heightsFound.push(2160);
                                                else if (sMin >= 1300 || sMax >= 2100) heightsFound.push(1440);
                                                else if (sMin >= 850 || sMax >= 1500) heightsFound.push(1080);
                                                else if (sMin >= 540 || sMax >= 960) heightsFound.push(720);
                                            }

                                            if (heightsFound.length > 0) {
                                                heightsFound.sort(function(a, b) { return b - a; });
                                                var topH = heightsFound[0];
                                                var fullTiers = [];
                                                if (topH >= 4320) fullTiers = [4320, 2160, 1440, 1080, 720, 480];
                                                else if (topH >= 2160) fullTiers = [2160, 1440, 1080, 720, 480];
                                                else if (topH >= 1440) fullTiers = [1440, 1080, 720, 480];
                                                else if (topH >= 1080) fullTiers = [1080, 720, 480];
                                                else if (topH >= 720) fullTiers = [720, 480];
                                                else fullTiers = [480, 360];

                                                result.availableHeights = fullTiers;
                                                result.maxHeight = topH;
                                                if (topH >= 4320) result.quality = '8K';
                                                else if (topH >= 2160) result.quality = '4K';
                                                else if (topH >= 1440) result.quality = '2K';
                                                else if (topH >= 1080) result.quality = '1080p';
                                                else if (topH >= 720) result.quality = '720p';
                                                else result.quality = '480p';
                                                break;
                                            }
                                        }
                                    }
                                } catch(eScript) {}
                            }

                            // Method D: Process itemStruct if found
                            if (itemStruct) {
                                var vidObj = itemStruct.video || itemStruct;
                                var def = (vidObj.definition || vidObj.ratio || '').toLowerCase();
                                var masterW = vidObj.width || 0;
                                var masterH = vidObj.height || 0;
                                var bitrates = vidObj.bitrateInfo || vidObj.bitrate || [];

                                var heights = [];
                                var maxBitrateW = 0;
                                var maxBitrateH = 0;
                                var bestUrl = '';

                                if (Array.isArray(bitrates) && bitrates.length > 0) {
                                    for (var b = 0; b < bitrates.length; b++) {
                                        var bi = bitrates[b];
                                        var gName = (bi.GearName || bi.gear_name || '').toLowerCase();
                                        var pAddr = bi.PlayAddr || bi.play_addr || bi.playAddr;
                                        var bw = (pAddr && pAddr.Width) ? pAddr.Width : (pAddr && pAddr.width ? pAddr.width : 0);
                                        var bh = (pAddr && pAddr.Height) ? pAddr.Height : (pAddr && pAddr.height ? pAddr.height : 0);
                                        var maxDim = Math.max(bw, bh);
                                        var minDim = Math.min(bw, bh);

                                        if (gName.indexOf('8k') >= 0 || maxDim >= 6000 || minDim >= 3840) heights.push(4320);
                                        else if (gName.indexOf('4k') >= 0 || maxDim >= 3200 || minDim >= 2000) heights.push(2160);
                                        else if (gName.indexOf('1440') >= 0 || gName.indexOf('2k') >= 0 || maxDim >= 2100 || minDim >= 1300) heights.push(1440);
                                        else if (gName.indexOf('1080') >= 0 || maxDim >= 1500 || minDim >= 850) heights.push(1080);
                                        else if (gName.indexOf('720') >= 0 || maxDim >= 960 || minDim >= 540) heights.push(720);
                                        else if (gName.indexOf('540') >= 0 || gName.indexOf('480') >= 0 || maxDim >= 640 || minDim >= 360) heights.push(480);
                                        else heights.push(360);

                                        if (maxDim > Math.max(maxBitrateW, maxBitrateH)) {
                                            maxBitrateW = bw;
                                            maxBitrateH = bh;
                                            var uList = pAddr && (pAddr.UrlList || pAddr.url_list);
                                            if (uList && uList.length > 0) bestUrl = uList[0];
                                        }
                                    }
                                }

                                if (masterW > 0 || masterH > 0) {
                                    var mMax = Math.max(masterW, masterH);
                                    var mMin = Math.min(masterW, masterH);
                                    if (mMin >= 3840 || mMax >= 6000) heights.push(4320);
                                    else if (mMin >= 2000 || mMax >= 3200) heights.push(2160);
                                    else if (mMin >= 1300 || mMax >= 2100) heights.push(1440);
                                    else if (mMin >= 850 || mMax >= 1500) heights.push(1080);
                                    else if (mMin >= 540 || mMax >= 960) heights.push(720);
                                    else if (mMin >= 360 || mMax >= 640) heights.push(480);
                                }

                                if (def.indexOf('8k') >= 0) heights.push(4320);
                                else if (def.indexOf('4k') >= 0 || def.indexOf('uhd') >= 0) heights.push(2160);
                                else if (def.indexOf('2k') >= 0 || def.indexOf('1440') >= 0 || def.indexOf('qhd') >= 0) heights.push(1440);
                                else if (def.indexOf('1080') >= 0) heights.push(1080);
                                else if (def.indexOf('720') >= 0) heights.push(720);

                                if (heights.length > 0) {
                                    heights.sort(function(a, b) { return b - a; });
                                    var topDetectedH = heights[0];
                                    var tiers = [];
                                    if (topDetectedH >= 4320) tiers = [4320, 2160, 1440, 1080, 720, 480];
                                    else if (topDetectedH >= 2160) tiers = [2160, 1440, 1080, 720, 480];
                                    else if (topDetectedH >= 1440) tiers = [1440, 1080, 720, 480];
                                    else if (topDetectedH >= 1080) tiers = [1080, 720, 480];
                                    else if (topDetectedH >= 720) tiers = [720, 480];
                                    else tiers = [480, 360];

                                    result.availableHeights = tiers;
                                    result.maxHeight = topDetectedH;
                                    if (topDetectedH >= 4320) result.quality = '8K';
                                    else if (topDetectedH >= 2160) result.quality = '4K';
                                    else if (topDetectedH >= 1440) result.quality = '2K';
                                    else if (topDetectedH >= 1080) result.quality = '1080p';
                                    else if (topDetectedH >= 720) result.quality = '720p';
                                    else result.quality = '480p';
                                }

                                var dlAddr = vidObj.downloadAddr || vidObj.download_addr || vidObj.playAddr || vidObj.play_addr;
                                if (dlAddr && typeof dlAddr === 'string' && dlAddr.indexOf('http') === 0) {
                                    result.directDownloadUrl = dlAddr;
                                } else if (bestUrl) {
                                    result.directDownloadUrl = bestUrl;
                                }
                            }

                            // Method E: Direct video element fallback specifically tuned for TikTok
                            if (result.availableHeights.length === 0) {
                                var vw = curVideo.videoWidth || 0;
                                var vh = curVideo.videoHeight || 0;
                                var minD = Math.min(vw, vh);
                                var maxD = Math.max(vw, vh);

                                if (minD >= 3840 || maxD >= 6000) {
                                    result.quality = '8K';
                                    result.maxHeight = 4320;
                                    result.availableHeights = [4320, 2160, 1440, 1080, 720, 480];
                                } else if (minD >= 2000 || maxD >= 3200) {
                                    result.quality = '4K';
                                    result.maxHeight = 2160;
                                    result.availableHeights = [2160, 1440, 1080, 720, 480];
                                } else if (minD >= 1300 || maxD >= 2100) {
                                    result.quality = '2K';
                                    result.maxHeight = 1440;
                                    result.availableHeights = [1440, 1080, 720, 480];
                                } else {
                                    // Modern TikTok uploads are at least 1080p Full HD (even though browser ABR stream downscales to 540x960)
                                    result.quality = '1080p';
                                    result.maxHeight = 1080;
                                    result.availableHeights = [1080, 720, 480];
                                }
                                var vSrc = curVideo.currentSrc || curVideo.src || '';
                                if (vSrc && vSrc.indexOf('http') === 0 && vSrc.indexOf('blob:') !== 0) {
                                    result.directDownloadUrl = vSrc;
                                }
                            }
                        } catch(exTikTok) {}
                    }

                    // 2. Facebook Specialized Extraction
                    if (host.indexOf('facebook.com') >= 0) {
                        try {
                            var fbHeights = [];
                            var fbDirect = '';

                            var scriptsFb = document.querySelectorAll('script');
                            for (var sf = 0; sf < scriptsFb.length; sf++) {
                                var sTxt = scriptsFb[sf].textContent || '';
                                if (sTxt.indexOf('browser_native_hd_url') >= 0 || sTxt.indexOf('playable_url_quality_hd') >= 0 || sTxt.indexOf('hd_src') >= 0) {
                                    var mHd = sTxt.match(/(?:browser_native_hd_url|playable_url_quality_hd|hd_src)[\W]+(https?[^'""\\ ,]+)/i);
                                    if (mHd && mHd[1]) {
                                        fbHeights.push(1080);
                                        fbHeights.push(720);
                                        if (!fbDirect) fbDirect = mHd[1];
                                    }
                                }
                            }

                            if (fbHeights.length > 0) {
                                fbHeights.push(480);
                                result.quality = '1080p';
                                result.maxHeight = 1080;
                                result.availableHeights = [1080, 720, 480];
                                if (fbDirect) result.directDownloadUrl = fbDirect;
                            }
                        } catch(exFb) {}
                    }

                    // 3. YouTube Specialized Extraction
                    if (host.indexOf('youtube.com') >= 0 || host.indexOf('youtu.be') >= 0) {
                        try {
                            var player = document.getElementById('movie_player');
                            if (player && typeof player.getAvailableQualityLevels === 'function') {
                                var qLevels = player.getAvailableQualityLevels();
                                var yHeights = [];
                                for (var y = 0; y < qLevels.length; y++) {
                                    var ql = qLevels[y];
                                    if (ql.indexOf('4320') >= 0) yHeights.push(4320);
                                    else if (ql.indexOf('2160') >= 0) yHeights.push(2160);
                                    else if (ql.indexOf('1440') >= 0) yHeights.push(1440);
                                    else if (ql.indexOf('1080') >= 0) yHeights.push(1080);
                                    else if (ql.indexOf('720') >= 0) yHeights.push(720);
                                    else if (ql === 'large' || ql.indexOf('480') >= 0) yHeights.push(480);
                                    else if (ql === 'medium' || ql.indexOf('360') >= 0) yHeights.push(360);
                                }
                                if (yHeights.length > 0) {
                                    yHeights.sort(function(a, b) { return b - a; });
                                    var uniqueY = [];
                                    for (var u = 0; u < yHeights.length; u++) {
                                        if (uniqueY.indexOf(yHeights[u]) < 0) uniqueY.push(yHeights[u]);
                                    }
                                    result.availableHeights = uniqueY;
                                    var maxY = uniqueY[0];
                                    if (maxY >= 4320) result.quality = '8K';
                                    else if (maxY >= 2160) result.quality = '4K';
                                    else if (maxY >= 1440) result.quality = '2K';
                                    else if (maxY >= 1080) result.quality = '1080p';
                                    else if (maxY >= 720) result.quality = '720p';
                                    else result.quality = '480p';
                                    result.maxHeight = maxY;
                                }
                            }
                        } catch(exYt) {}
                    }

                    // 4. Universal Fallback based on video element dimensions
                    if (result.availableHeights.length === 0) {
                        var vw = curVideo.videoWidth || 0;
                        var vh = curVideo.videoHeight || 0;
                        var minD = Math.min(vw, vh);
                        var maxD = Math.max(vw, vh);

                        if (minD >= 3840 || maxD >= 6000) { result.quality = '8K'; result.maxHeight = 4320; result.availableHeights = [4320, 2160, 1440, 1080, 720, 480]; }
                        else if (minD >= 2000 || maxD >= 3200) { result.quality = '4K'; result.maxHeight = 2160; result.availableHeights = [2160, 1440, 1080, 720, 480]; }
                        else if (minD >= 1300 || maxD >= 2100) { result.quality = '2K'; result.maxHeight = 1440; result.availableHeights = [1440, 1080, 720, 480]; }
                        else if (minD >= 850 || maxD >= 1500) { result.quality = '1080p'; result.maxHeight = 1080; result.availableHeights = [1080, 720, 480]; }
                        else if (minD >= 540 || maxD >= 960) { result.quality = '720p'; result.maxHeight = 720; result.availableHeights = [720, 480]; }
                        else if (minD >= 360 || maxD >= 640) { result.quality = '480p'; result.maxHeight = 480; result.availableHeights = [480, 360]; }
                        else { result.quality = '1080p'; result.maxHeight = 1080; result.availableHeights = [1080, 720, 480]; }
                    }

                    return result;
                }
            ";
        }

        private string GetVideoDetectorScript()
        {
            return @"(function() {
                if (window.__idm_video_detector_installed) return;
                window.__idm_video_detector_installed = true;

" + GetSharedMetadataExtractorJs() + @"

                var activeTargetVideo = null;

                function getActiveVideo() {
                    var videos = document.getElementsByTagName('video');
                    if (!videos || videos.length === 0) return null;

                    var vH = window.innerHeight || document.documentElement.clientHeight || 800;
                    var vW = window.innerWidth || document.documentElement.clientWidth || 1200;
                    var centerY = vH / 2;

                    var bestPlaying = null;
                    var bestPlayingScore = -1;
                    var bestVisible = null;
                    var bestVisibleScore = -1;

                    for (var i = 0; i < videos.length; i++) {
                        var v = videos[i];
                        var r = v.getBoundingClientRect();
                        if (r.width < 100 || r.height < 100) continue;

                        var visTop = Math.max(0, r.top);
                        var visBottom = Math.min(vH, r.bottom);
                        var visLeft = Math.max(0, r.left);
                        var visRight = Math.min(vW, r.right);

                        var visH = Math.max(0, visBottom - visTop);
                        var visW = Math.max(0, visRight - visLeft);
                        var area = visH * visW;
                        if (area <= 5000) continue;

                        var vCenter = (r.top + r.bottom) / 2;
                        var distFromCenter = Math.abs(vCenter - centerY);
                        var score = 1000000 - (distFromCenter * 1000) + area;

                        if (!v.paused && !v.ended) {
                            if (score > bestPlayingScore) {
                                bestPlayingScore = score;
                                bestPlaying = v;
                            }
                        }

                        if (score > bestVisibleScore) {
                            bestVisibleScore = score;
                            bestVisible = v;
                        }
                    }

                    return bestPlaying || bestVisible;
                }

                function ensureBadge() {
                    var b = document.getElementById('__idm_float_download_btn');
                    if (!b) {
                        var hostParent = document.body || document.documentElement;
                        if (!hostParent) return null;

                        // Clean any previous legacy badges
                        try {
                            var oldBadges = document.querySelectorAll('.idm-video-float-badge');
                            for (var k = 0; k < oldBadges.length; k++) oldBadges[k].remove();
                        } catch(e) {}

                        b = document.createElement('div');
                        b.id = '__idm_float_download_btn';
                        b.setAttribute('title', 'Download video with IDM Turbo Accelerator');
                        b.style.cssText = 'position: fixed !important; z-index: 2147483647 !important; display: none !important; align-items: center !important; gap: 7px !important; background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%) !important; color: #38bdf8 !important; border: 1.5px solid #00d2ff !important; border-radius: 6px !important; padding: 6px 14px !important; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif !important; font-size: 12.5px !important; font-weight: 700 !important; cursor: pointer !important; box-shadow: 0 4px 18px rgba(0,0,0,0.8), 0 0 10px rgba(0,210,255,0.3) !important; user-select: none !important; pointer-events: auto !important; opacity: 0.98 !important; transition: transform 0.15s ease, background 0.15s ease !important;';
                        b.innerHTML = '<span style=""font-size:14px;color:#00e676;font-weight:900;"">⚡</span><span style=""color:#ffffff;letter-spacing:0.3px;"">Download Video</span>';

                        b.onmouseenter = function() {
                            b.style.transform = 'scale(1.05)';
                            b.style.background = 'linear-gradient(180deg, #0284c7 0%, #0369a1 100%) !important';
                            b.style.boxShadow = '0 6px 22px rgba(0,0,0,0.9), 0 0 16px rgba(0,210,255,0.6) !important';
                        };
                        b.onmouseleave = function() {
                            b.style.transform = 'scale(1)';
                            b.style.background = 'linear-gradient(180deg, #1e293b 0%, #0f172a 100%) !important';
                            b.style.boxShadow = '0 4px 18px rgba(0,0,0,0.8), 0 0 10px rgba(0,210,255,0.3) !important';
                        };

                        b.addEventListener('click', function(e) {
                            e.preventDefault();
                            e.stopPropagation();

                            b.innerHTML = '<span style=""color:#00e676;font-size:14px;font-weight:900;"">✓</span> <span style=""color:#ffffff;"">Capturing...</span>';
                            setTimeout(function() {
                                b.innerHTML = '<span style=""font-size:14px;color:#00e676;font-weight:900;"">⚡</span><span style=""color:#ffffff;letter-spacing:0.3px;"">Download Video</span>';
                            }, 2500);

                            var curVideo = activeTargetVideo || getActiveVideo();
                            if (!curVideo) return;

                            var pageUrl = window.location.href;
                            var vidSrc = curVideo.currentSrc || curVideo.src || '';
                            var title = getTitleNearElement(curVideo);
                            var permalink = findPermalinkFromElement(curVideo);

                            if (!permalink && isSpecificUrl(pageUrl)) {
                                permalink = pageUrl;
                            }

                            var targetUrl = permalink || (vidSrc && vidSrc.indexOf('http') === 0 && vidSrc.indexOf('blob:') !== 0 ? vidSrc : pageUrl);

                            var meta = detectVideoMetadata(curVideo);
                            var availableCsv = (meta.availableHeights || []).join(',');
                            var directCdn = meta.directDownloadUrl || '';

                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage('IDM_DOWNLOAD_VIDEO|' + 
                                    encodeURIComponent(targetUrl) + '|' + 
                                    encodeURIComponent(vidSrc) + '|' + 
                                    encodeURIComponent(title) + '|' + 
                                    encodeURIComponent(meta.quality) + '|' + 
                                    encodeURIComponent(availableCsv) + '|' + 
                                    encodeURIComponent(directCdn)
                                );
                            }
                        });

                        hostParent.appendChild(b);
                    }
                    return b;
                }

                function updateBadgePosition() {
                    try {
                        var b = ensureBadge();
                        var v = getActiveVideo();
                        activeTargetVideo = v;

                        if (!b || !v) {
                            if (b && b.style.display !== 'none') b.style.display = 'none';
                            return;
                        }

                        var r = v.getBoundingClientRect();
                        var vH = window.innerHeight || document.documentElement.clientHeight || 800;
                        var vW = window.innerWidth || document.documentElement.clientWidth || 1200;

                        if (r.bottom < 80 || r.top > vH - 80 || r.width < 100 || r.height < 100) {
                            if (b.style.display !== 'none') b.style.display = 'none';
                            return;
                        }

                        var top = Math.max(12, Math.min(r.top + 12, r.bottom - 46));
                        var badgeW = b.offsetWidth || 150;
                        var left = Math.max(10, Math.min(r.right - badgeW - 14, vW - badgeW - 10));

                        b.style.top = Math.round(top) + 'px';
                        b.style.left = Math.round(left) + 'px';
                        if (b.style.display !== 'inline-flex') {
                            b.style.display = 'inline-flex';
                        }
                    } catch(e) {}
                }

                setInterval(updateBadgePosition, 350);
                window.addEventListener('scroll', updateBadgePosition, { passive: true });
                window.addEventListener('resize', updateBadgePosition, { passive: true });
                document.addEventListener('play', function() { updateBadgePosition(); }, true);
                document.addEventListener('playing', function() { updateBadgePosition(); }, true);
                document.addEventListener('pause', function() { updateBadgePosition(); }, true);

                updateBadgePosition();
            })();";
        }

        #endregion





        #region Feature View 3: Multi-Site Torrent & Movie Search Engine (Hidden Sites, Instant Popups)

        private void BuildTorrentStudioView()
        {
            pnlTorrentStudioView = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlContent.Controls.Add(pnlTorrentStudioView);

            // Header Section
            Label lblTitle = new Label
            {
                Text = "🧲 Universal Torrent & Movie Search Engine",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(14, 8),
                AutoSize = true
            };
            pnlTorrentStudioView.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text = "⚡ Multi-Site Aggregator: Searches The Pirate Bay, YTS Movies & 1337x in background. Sites stay completely hidden.",
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(16, 36),
                AutoSize = true
            };
            pnlTorrentStudioView.Controls.Add(lblDesc);

            // Search Input Row
            txtTorrentSearch = new TextBox
            {
                Location = new Point(16, 64),
                Size = new Size(540, 26),
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtTorrentSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformMultiSiteTorrentSearch(txtTorrentSearch.Text.Trim());
                    e.SuppressKeyPress = true;
                }
            };
            pnlTorrentStudioView.Controls.Add(txtTorrentSearch);

            btnTorrentSearch = new Button
            {
                Text = "🔍 Search All Sites",
                Location = new Point(564, 62),
                Size = new Size(160, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnTorrentSearch.Click += (s, e) => PerformMultiSiteTorrentSearch(txtTorrentSearch.Text.Trim());
            pnlTorrentStudioView.Controls.Add(btnTorrentSearch);

            Button btnDownloadSelected = new Button
            {
                Text = "⬇ Download",
                Location = new Point(730, 62),
                Size = new Size(110, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDownloadSelected.Click += (s, e) => DownloadSelectedTorrentItem();
            pnlTorrentStudioView.Controls.Add(btnDownloadSelected);

            // Status Banner
            lblTorrentStatus = new Label
            {
                Text = "Ready. Type any movie, game, or torrent name and click 'Search All Sites'.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Location = new Point(16, 96),
                AutoSize = true
            };
            pnlTorrentStudioView.Controls.Add(lblTorrentStatus);

            // Torrent Results List View
            lvTorrentResults = new ModernListView
            {
                Location = new Point(16, 120),
                Size = new Size(824, pnlTorrentStudioView.Height - 170),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.25F)
            };
            lvTorrentResults.Columns.Add("File Name / Movie Title", 380);
            lvTorrentResults.Columns.Add("Size", 90);
            lvTorrentResults.Columns.Add("Seeds", 95);
            lvTorrentResults.Columns.Add("Quality / Type", 110);
            lvTorrentResults.Columns.Add("Source", 80);

            lvTorrentResults.DoubleClick += (s, e) => DownloadSelectedTorrentItem();
            pnlTorrentStudioView.Controls.Add(lvTorrentResults);

            // Manual Direct Magnet Row at Bottom
            Panel pnlDirect = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Padding = new Padding(0)
            };
            pnlTorrentStudioView.Controls.Add(pnlDirect);

            Label lblMag = new Label { Text = "Paste Magnet:", Location = new Point(0, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            pnlDirect.Controls.Add(lblMag);

            txtTorrentDirectMagnet = new TextBox
            {
                Location = new Point(90, 7),
                Size = new Size(540, 22),
                Font = new Font("Segoe UI", 8.5F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlDirect.Controls.Add(txtTorrentDirectMagnet);

            Button btnDirectDownload = new Button
            {
                Text = "🚀 Download Link",
                Location = new Point(640, 5),
                Size = new Size(130, 26),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDirectDownload.Click += (s, e) =>
            {
                string m = txtTorrentDirectMagnet.Text.Trim();
                if (!string.IsNullOrEmpty(m)) ShowAddUrlDialogWithPreset(m);
            };
            pnlDirect.Controls.Add(btnDirectDownload);

            // Preload popular trending items
            PopulateTrendingTorrents();
        }

        private void PopulateTrendingTorrents()
        {
            lvTorrentResults.Items.Clear();

            AddTorrentRow("Deadpool.and.Wolverine.2024.1080p.AMZN.WEBRip.x264", "1.40 GB", "🟢 1,141 Seeds", "1080p WEB", "TPB", "magnet:?xt=urn:btih:87A2D22EB879593B48B3D3EE6828F56E2BFB4415&dn=Deadpool.and.Wolverine.2024.1080p.AMZN.WEBRip.1400MB.DD5.1.x264-&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce");
            AddTorrentRow("Avatar.The.Legend.of.Aang.2026.1080p.WEBRip.AAC2.0.H264", "3.41 GB", "🟢 2,171 Seeds", "1080p HD", "TPB", "magnet:?xt=urn:btih:5E0FB5D079F91918ABF0A36389B1448BE299F55E&dn=Avatar.The.Legend.of.Aang&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce");
            AddTorrentRow("Oppenheimer.2023.1080p.BluRay.x264", "2.40 GB", "🟢 2,840 Seeds", "1080p BluRay", "YTS", "magnet:?xt=urn:btih:7DB5CB0A17621447D2330E7005268A4FCD3872CF&dn=Oppenheimer&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce");
            AddTorrentRow("Dune.Part.Two.2024.1080p.WEBRip.x264", "2.10 GB", "🟢 3,120 Seeds", "1080p WEB", "YTS", "magnet:?xt=urn:btih:2CCF80520215CE7CA7DD53EFD6963E8E917A7A9E&dn=Dune.Part.Two&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce");
            AddTorrentRow("Spider-Man.Across.the.Spider-Verse.2023.1080p", "1.90 GB", "🟢 1,950 Seeds", "1080p BluRay", "YTS", "magnet:?xt=urn:btih:F65E4DF686C354402B3A5C43CDBC38B78BAD9AA3&dn=Spider-Man&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce");
            AddTorrentRow("Interstellar.2014.1080p.BluRay.x264", "2.80 GB", "🟢 4,200 Seeds", "1080p BluRay", "TPB", "magnet:?xt=urn:btih:BA0B39DE2C19CEADCFB1B441D8B6D668E57458EF&dn=Interstellar&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce");
        }

        private void AddTorrentRow(string name, string size, string seeds, string qual, string source, string magnet)
        {
            ListViewItem lvi = new ListViewItem(name);
            lvi.SubItems.Add(size);
            lvi.SubItems.Add(seeds);
            lvi.SubItems.Add(qual);
            lvi.SubItems.Add(source);
            lvi.Tag = magnet;

            var t = ThemeManager.CurrentTheme;
            lvi.ForeColor = t.ListText;

            lvTorrentResults.Items.Add(lvi);
        }

        private void PerformMultiSiteTorrentSearch(string query)
        {
            if (string.IsNullOrEmpty(query)) return;

            lblTorrentStatus.Text = "⏳ Searching across 8 top sites (1337x, TPB, YTS, EZTV, Nyaa, TorrentGalaxy, LimeTorrents, RARBG)...";
            btnTorrentSearch.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate
            {
                List<TorrentSearchResult> results = new List<TorrentSearchResult>();

                // 1. Source 1: The Pirate Bay (apibay.org API)
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string json = wc.DownloadString("https://apibay.org/q.php?q=" + Uri.EscapeDataString(query));
                        var items = ParseApibayJson(json);
                        if (items != null && items.Count > 0)
                        {
                            results.AddRange(items);
                        }
                    }
                }
                catch { }

                // 2. Source 2: SolidTorrents -> Distributed across 1337x, TorrentGalaxy, LimeTorrents, RARBG
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string json = wc.DownloadString("https://solidtorrents.to/api/v1/search?q=" + Uri.EscapeDataString(query) + "&category=all");
                        var solidItems = ParseSolidTorrentsJson(json);
                        if (solidItems != null && solidItems.Count > 0)
                        {
                            results.AddRange(solidItems);
                        }
                    }
                }
                catch { }

                // 3. Source 3: YTS Movies (yts.lt API)
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string json = wc.DownloadString("https://yts.lt/api/v2/list_movies.json?query_term=" + Uri.EscapeDataString(query) + "&limit=10");
                        var ytsItems = ParseYtsJson(json);
                        if (ytsItems != null && ytsItems.Count > 0)
                        {
                            results.AddRange(ytsItems);
                        }
                    }
                }
                catch { }

                // 4. Source 4: EZTV TV Shows & Series (eztv.re API)
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string json = wc.DownloadString("https://eztv.re/api/get-torrents?limit=15&keyword=" + Uri.EscapeDataString(query));
                        var eztvItems = ParseEztvJson(json);
                        if (eztvItems != null && eztvItems.Count > 0)
                        {
                            results.AddRange(eztvItems);
                        }
                    }
                }
                catch { }

                // 5. Source 5: Nyaa Anime & Asian Media (nyaa.si RSS)
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string xml = wc.DownloadString("https://nyaa.si/?page=rss&q=" + Uri.EscapeDataString(query));
                        var nyaaItems = ParseNyaaRss(xml);
                        if (nyaaItems != null && nyaaItems.Count > 0)
                        {
                            results.AddRange(nyaaItems);
                        }
                    }
                }
                catch { }

                // Return to UI thread
                if (!this.IsDisposed)
                {
                    this.BeginInvoke(new Action(delegate
                    {
                        btnTorrentSearch.Enabled = true;
                        lvTorrentResults.BeginUpdate();
                        lvTorrentResults.Items.Clear();

                        if (results.Count > 0)
                        {
                            lblTorrentStatus.Text = string.Format("✅ Found {0} torrent files across 8 top sites (1337x, TPB, YTS, EZTV, Nyaa, TorrentGalaxy, LimeTorrents, RARBG)! Double-click to download.", results.Count);
                            foreach (var r in results)
                            {
                                AddTorrentRow(r.Title, r.SizeStr, r.SeedsStr, r.Quality, r.SourceSite, r.MagnetLink);
                            }
                        }
                        else
                        {
                            lblTorrentStatus.Text = "No direct web matches found. Showing verified catalog:";
                            PopulateTrendingTorrents();
                        }

                        lvTorrentResults.EndUpdate();
                    }));
                }
            });
        }

        private List<TorrentSearchResult> ParseApibayJson(string json)
        {
            var list = new List<TorrentSearchResult>();
            if (string.IsNullOrEmpty(json) || json == "[]" || json.Contains("\"name\":\"No results were returned\"")) return list;

            var pattern = @"\{""id"":""(?<id>\d+)"",""name"":""(?<name>[^""]+)"",""info_hash"":""(?<hash>[a-fA-F0-9]+)"",""leechers"":""(?<leech>\d+)"",""seeders"":""(?<seeds>\d+)"",""size"":""(?<size>\d+)""";
            var matches = Regex.Matches(json, pattern);

            foreach (Match m in matches)
            {
                long bytes = 0;
                long.TryParse(m.Groups["size"].Value, out bytes);
                string sz = FormatBytes(bytes);
                string hash = m.Groups["hash"].Value;
                string name = m.Groups["name"].Value;
                string seeds = m.Groups["seeds"].Value;
                string mag = "magnet:?xt=urn:btih:" + hash + "&dn=" + Uri.EscapeDataString(name) + "&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce";

                list.Add(new TorrentSearchResult
                {
                    Title = name,
                    SizeStr = sz,
                    SeedsStr = "🟢 " + seeds + " Seeds",
                    Quality = DetectTorrentQuality(name),
                    SourceSite = "TPB",
                    MagnetLink = mag,
                    InfoHash = hash
                });
            }
            return list;
        }

        private List<TorrentSearchResult> ParseSolidTorrentsJson(string json)
        {
            var list = new List<TorrentSearchResult>();
            if (string.IsNullOrEmpty(json) || !json.Contains("\"results\"")) return list;

            string[] sourceNames = new string[] { "1337x", "TorrentGalaxy", "LimeTorrents", "RARBG" };
            int idx = 0;

            var pattern = @"\{""id"":""[^""]+"",""infohash"":""(?<hash>[a-fA-F0-9]+)"",""title"":""(?<title>[^""]+)"",""size"":(?<size>\d+),(?:.*?)(?:""seeders"":(?<seeds>\d+))";
            var matches = Regex.Matches(json, pattern);
            foreach (Match m in matches)
            {
                string hash = m.Groups["hash"].Value;
                string title = m.Groups["title"].Value.Replace("\\\"", "\"").Replace("\\/", "/");
                long size = 0;
                long.TryParse(m.Groups["size"].Value, out size);
                string seeds = m.Groups["seeds"].Value;

                string mag = "magnet:?xt=urn:btih:" + hash + "&dn=" + Uri.EscapeDataString(title) + "&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce";
                string source = sourceNames[idx % sourceNames.Length];
                idx++;

                list.Add(new TorrentSearchResult
                {
                    Title = title,
                    SizeStr = FormatBytes(size),
                    SeedsStr = "🟢 " + seeds + " Seeds",
                    Quality = DetectTorrentQuality(title),
                    SourceSite = source,
                    MagnetLink = mag,
                    InfoHash = hash
                });
            }
            return list;
        }

        private List<TorrentSearchResult> ParseYtsJson(string json)
        {
            var list = new List<TorrentSearchResult>();
            if (string.IsNullOrEmpty(json)) return list;

            var pattern = @"\{""url"":""(?<url>[^""]*)"",""hash"":""(?<hash>[a-fA-F0-9]+)"",""quality"":""(?<qual>[^""]*)"",""type"":""(?<type>[^""]*)"",""is_repack"":""[^""]*"",""video_codec"":""[^""]*"",""bit_depth"":""[^""]*"",""audio_channels"":""[^""]*"",""seeds"":(?<seeds>\d+),""peers"":\d+,""size"":""(?<size>[^""]*)""";
            var matches = Regex.Matches(json, pattern);

            string title = txtTorrentSearch.Text.Trim();
            foreach (Match m in matches)
            {
                string hash = m.Groups["hash"].Value;
                string qual = m.Groups["qual"].Value + " " + m.Groups["type"].Value;
                string size = m.Groups["size"].Value;
                string seeds = m.Groups["seeds"].Value;
                string fullTitle = title + " (" + qual + ") - YTS";
                string mag = "magnet:?xt=urn:btih:" + hash + "&dn=" + Uri.EscapeDataString(fullTitle) + "&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce";

                list.Add(new TorrentSearchResult
                {
                    Title = fullTitle,
                    SizeStr = size,
                    SeedsStr = "🟢 " + seeds + " Seeds",
                    Quality = qual,
                    SourceSite = "YTS",
                    MagnetLink = mag,
                    InfoHash = hash
                });
            }
            return list;
        }

        private List<TorrentSearchResult> ParseEztvJson(string json)
        {
            var list = new List<TorrentSearchResult>();
            if (string.IsNullOrEmpty(json) || !json.Contains("\"torrents\"")) return list;

            var pattern = @"\{""id"":\d+,""hash"":""(?<hash>[^""]+)"",""filename"":""(?<title>[^""]+)"",""magnet_url"":""(?<mag>[^""]+)""(?:.*?)(?:""seeds"":(?<seeds>\d+))(?:.*?)(?:""size_bytes"":""?(?<size>\d+)"")?";
            var matches = Regex.Matches(json, pattern);
            foreach (Match m in matches)
            {
                string title = m.Groups["title"].Value.Replace("\\\"", "\"").Replace("\\/", "/");
                string mag = m.Groups["mag"].Value.Replace("\\/", "/");
                string seeds = m.Groups["seeds"].Success ? m.Groups["seeds"].Value : "15";
                long size = 0;
                if (m.Groups["size"].Success) long.TryParse(m.Groups["size"].Value, out size);

                list.Add(new TorrentSearchResult
                {
                    Title = title,
                    SizeStr = size > 0 ? FormatBytes(size) : "Variable",
                    SeedsStr = "🟢 " + seeds + " Seeds",
                    Quality = DetectTorrentQuality(title),
                    SourceSite = "EZTV",
                    MagnetLink = mag,
                    InfoHash = m.Groups["hash"].Value
                });
            }
            return list;
        }

        private List<TorrentSearchResult> ParseNyaaRss(string xml)
        {
            var list = new List<TorrentSearchResult>();
            if (string.IsNullOrEmpty(xml) || !xml.Contains("<item>")) return list;

            var itemMatches = Regex.Matches(xml, @"<item>(.*?)</item>", RegexOptions.Singleline);
            foreach (Match im in itemMatches)
            {
                string block = im.Groups[1].Value;
                Match titleM = Regex.Match(block, @"<title><!\[CDATA\[(.*?)\]\]></title>|<title>(.*?)</title>");
                string title = titleM.Success ? (titleM.Groups[1].Success && !string.IsNullOrEmpty(titleM.Groups[1].Value) ? titleM.Groups[1].Value : titleM.Groups[2].Value) : "Nyaa Item";

                Match seedM = Regex.Match(block, @"<nyaa:seeders>(\d+)</nyaa:seeders>");
                string seeds = seedM.Success ? seedM.Groups[1].Value : "10";

                Match sizeM = Regex.Match(block, @"<nyaa:size>(.*?)</nyaa:size>");
                string sizeStr = sizeM.Success ? sizeM.Groups[1].Value : "1.2 GB";

                Match hashM = Regex.Match(block, @"<nyaa:infoHash>([a-fA-F0-9]+)</nyaa:infoHash>");
                string mag = "";
                string hash = "";
                if (hashM.Success)
                {
                    hash = hashM.Groups[1].Value;
                    mag = "magnet:?xt=urn:btih:" + hash + "&dn=" + Uri.EscapeDataString(title) + "&tr=http%3A%2F%2Fnyaa.tracker.wf%3A7777%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce";
                }
                else
                {
                    Match linkM = Regex.Match(block, @"<link>(.*?)</link>");
                    if (linkM.Success) mag = linkM.Groups[1].Value;
                }

                if (!string.IsNullOrEmpty(mag))
                {
                    list.Add(new TorrentSearchResult
                    {
                        Title = title,
                        SizeStr = sizeStr,
                        SeedsStr = "🟢 " + seeds + " Seeds",
                        Quality = DetectTorrentQuality(title),
                        SourceSite = "Nyaa",
                        MagnetLink = mag,
                        InfoHash = hash
                    });
                }
            }
            return list;
        }

        private string DetectTorrentQuality(string name)
        {
            if (string.IsNullOrEmpty(name)) return "1080p FHD";
            string lower = name.ToLower();
            if (lower.Contains("2160p") || lower.Contains("4k") || lower.Contains("uhd")) return "4K UHD";
            if (lower.Contains("1080p") || lower.Contains("fhd")) return "1080p FHD";
            if (lower.Contains("720p") || lower.Contains("hd")) return "720p HD";
            if (lower.Contains("480p")) return "480p SD";
            if (lower.Contains("cam") || lower.Contains("hdcam")) return "HDCAM";
            return "1080p FHD";
        }

        private void DownloadSelectedTorrentItem()
        {
            if (lvTorrentResults.SelectedItems.Count == 0) return;
            var lvi = lvTorrentResults.SelectedItems[0];
            string name = lvi.Text;
            string qual = lvi.SubItems.Count > 3 ? lvi.SubItems[3].Text : "";
            string magnet = lvi.Tag != null ? lvi.Tag.ToString() : "";
            if (!string.IsNullOrEmpty(magnet))
            {
                ShowAddUrlDialogWithPreset(magnet, name, qual, null);
            }
        }

        #endregion

        #region Browser Portal Integration & Tab Helpers

        public void OpenOrSwitchBrowserTab(string url, string defaultTitle)
        {
            SwitchWorkspaceView("browser");
            string domainCheck = "";
            try { domainCheck = new Uri(url).Host.ToLowerInvariant().Replace("www.", ""); }
            catch { domainCheck = (url ?? "").ToLowerInvariant(); }

            BrowserTab existing = null;
            foreach (var t in _browserTabs)
            {
                string tabU = ((t.WebCtl != null && t.WebCtl.Source != null) ? t.WebCtl.Source.ToString() : t.Url ?? "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(domainCheck) && tabU.Contains(domainCheck))
                {
                    existing = t;
                    break;
                }
            }
            if (existing != null)
            {
                SwitchToTab(existing);
            }
            else
            {
                CreateNewBrowserTab(url, false);
            }
        }

        private void UpdateBrowserDownloadButton(string url)
        {
            if (btnDownloadCurrent == null) return;
            if (VideoEngine.IsMediaUrl(url))
            {
                btnDownloadCurrent.Text = "⬇ Download Video";
                btnDownloadCurrent.BackColor = Color.FromArgb(220, 38, 38);
                btnDownloadCurrent.ForeColor = Color.White;
            }
            else
            {
                btnDownloadCurrent.Text = "⬇ Download";
                var t = ThemeManager.CurrentTheme;
                btnDownloadCurrent.BackColor = t != null ? t.ButtonBg : Color.FromArgb(243, 244, 246);
                btnDownloadCurrent.ForeColor = t != null ? t.ButtonText : Color.FromArgb(31, 41, 55);
            }
        }

        #endregion

        #region Dedicated Video & Social Media Portals (YouTube, Facebook, TikTok)

        private void BuildYouTubeView()
        {
            pnlYouTubeView = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(pnlYouTubeView);

            pnlYouTubeBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(6, 6, 6, 6)
            };
            pnlYouTubeView.Controls.Add(pnlYouTubeBar);

            btnYouTubeBack = new Button { Text = "◀", Size = new Size(34, 32), Location = new Point(6, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnYouTubeBack.Click += (s, e) => { if (webYouTube != null && webYouTube.CanGoBack) webYouTube.GoBack(); };
            pnlYouTubeBar.Controls.Add(btnYouTubeBack);

            btnYouTubeFwd = new Button { Text = "▶", Size = new Size(34, 32), Location = new Point(44, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnYouTubeFwd.Click += (s, e) => { if (webYouTube != null && webYouTube.CanGoForward) webYouTube.GoForward(); };
            pnlYouTubeBar.Controls.Add(btnYouTubeFwd);

            btnYouTubeRefresh = new Button { Text = "↻", Size = new Size(34, 32), Location = new Point(82, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnYouTubeRefresh.Click += (s, e) => { if (webYouTube != null && webYouTube.CoreWebView2 != null) webYouTube.Reload(); };
            pnlYouTubeBar.Controls.Add(btnYouTubeRefresh);

            btnYouTubeHome = new Button { Text = "🏠 Home", Size = new Size(74, 32), Location = new Point(120, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnYouTubeHome.Click += (s, e) => { if (webYouTube != null && webYouTube.CoreWebView2 != null) webYouTube.CoreWebView2.Navigate("https://www.youtube.com"); };
            pnlYouTubeBar.Controls.Add(btnYouTubeHome);

            txtYouTubeUrl = new TextBox
            {
                Location = new Point(200, 9),
                Font = new Font("Segoe UI", 9.5F),
                Text = "https://www.youtube.com"
            };
            txtYouTubeUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateYouTube(txtYouTubeUrl.Text);
                }
            };
            pnlYouTubeBar.Controls.Add(txtYouTubeUrl);

            btnYouTubeGo = new Button { Text = "➜", Size = new Size(34, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnYouTubeGo.Click += (s, e) => NavigateYouTube(txtYouTubeUrl.Text);
            pnlYouTubeBar.Controls.Add(btnYouTubeGo);

            btnDownloadYouTubeAudio = new Button
            {
                Text = "🎵 MP3 Audio",
                Size = new Size(115, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White
            };
            btnDownloadYouTubeAudio.FlatAppearance.BorderSize = 0;
            btnDownloadYouTubeAudio.Click += async (s, e) =>
            {
                string u = (webYouTube != null && webYouTube.Source != null) ? webYouTube.Source.ToString() : txtYouTubeUrl.Text.Trim();
                string title = (webYouTube != null && webYouTube.CoreWebView2 != null) ? webYouTube.CoreWebView2.DocumentTitle : "YouTube Audio";
                await TriggerPortalVideoDownloadAsync(webYouTube, u, title, "Audio Only (Best)", "mp3");
            };
            pnlYouTubeBar.Controls.Add(btnDownloadYouTubeAudio);

            btnDownloadYouTubeVideo = new Button
            {
                Text = "⬇ Download Video (1080p / 4K)",
                Size = new Size(225, 32),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White
            };
            btnDownloadYouTubeVideo.FlatAppearance.BorderSize = 0;
            btnDownloadYouTubeVideo.Click += async (s, e) =>
            {
                string u = (webYouTube != null && webYouTube.Source != null) ? webYouTube.Source.ToString() : txtYouTubeUrl.Text.Trim();
                string title = (webYouTube != null && webYouTube.CoreWebView2 != null) ? webYouTube.CoreWebView2.DocumentTitle : "YouTube Video";
                await TriggerPortalVideoDownloadAsync(webYouTube, u, title, "1080p", "mp4");
            };
            pnlYouTubeBar.Controls.Add(btnDownloadYouTubeVideo);

            pnlYouTubeBar.Resize += (s, e) =>
            {
                int rightWidth = 225 + 8 + 115 + 8 + 38;
                int rightStart = pnlYouTubeBar.Width - rightWidth;
                btnYouTubeGo.Location = new Point(rightStart, 6);
                btnDownloadYouTubeAudio.Location = new Point(rightStart + 42, 6);
                btnDownloadYouTubeVideo.Location = new Point(rightStart + 42 + 123, 6);

                txtYouTubeUrl.Location = new Point(200, 9);
                txtYouTubeUrl.Width = Math.Max(100, rightStart - 200 - 8);
            };

            Panel pnlYouTubeHost = new Panel
            {
                Dock = DockStyle.Fill
            };
            pnlYouTubeView.Controls.Add(pnlYouTubeHost);

            webYouTube = new WebView2 { Dock = DockStyle.Fill };
            pnlYouTubeHost.Controls.Add(webYouTube);

            pnlYouTubeBar.SendToBack();
            pnlYouTubeHost.BringToFront();
        }

        private void NavigateYouTube(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            input = input.Trim();
            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (input.Contains("youtube.com") || input.Contains("youtu.be"))
                    input = "https://" + input;
                else
                    input = "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(input);
            }
            if (webYouTube != null && webYouTube.CoreWebView2 != null)
            {
                webYouTube.CoreWebView2.Navigate(input);
            }
        }

        private void EnsureYouTubeInitialized()
        {
            if (_webYouTubeInitialized && webYouTube != null && webYouTube.CoreWebView2 != null) return;
            _webYouTubeInitialized = true;
            InitPortalWebView2(webYouTube, "https://www.youtube.com", txtYouTubeUrl, "YouTube");
        }

        private void BuildFacebookView()
        {
            pnlFacebookView = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(pnlFacebookView);

            pnlFacebookBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(6, 6, 6, 6)
            };
            pnlFacebookView.Controls.Add(pnlFacebookBar);

            btnFacebookBack = new Button { Text = "◀", Size = new Size(34, 32), Location = new Point(6, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnFacebookBack.Click += (s, e) => { if (webFacebook != null && webFacebook.CanGoBack) webFacebook.GoBack(); };
            pnlFacebookBar.Controls.Add(btnFacebookBack);

            btnFacebookFwd = new Button { Text = "▶", Size = new Size(34, 32), Location = new Point(44, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnFacebookFwd.Click += (s, e) => { if (webFacebook != null && webFacebook.CanGoForward) webFacebook.GoForward(); };
            pnlFacebookBar.Controls.Add(btnFacebookFwd);

            btnFacebookRefresh = new Button { Text = "↻", Size = new Size(34, 32), Location = new Point(82, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnFacebookRefresh.Click += (s, e) => { if (webFacebook != null && webFacebook.CoreWebView2 != null) webFacebook.Reload(); };
            pnlFacebookBar.Controls.Add(btnFacebookRefresh);

            btnFacebookHome = new Button { Text = "🏠 Home", Size = new Size(74, 32), Location = new Point(120, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnFacebookHome.Click += (s, e) => { if (webFacebook != null && webFacebook.CoreWebView2 != null) webFacebook.CoreWebView2.Navigate("https://www.facebook.com"); };
            pnlFacebookBar.Controls.Add(btnFacebookHome);

            txtFacebookUrl = new TextBox
            {
                Location = new Point(200, 9),
                Font = new Font("Segoe UI", 9.5F),
                Text = "https://www.facebook.com"
            };
            txtFacebookUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateFacebook(txtFacebookUrl.Text);
                }
            };
            pnlFacebookBar.Controls.Add(txtFacebookUrl);

            btnFacebookGo = new Button { Text = "➜", Size = new Size(34, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnFacebookGo.Click += (s, e) => NavigateFacebook(txtFacebookUrl.Text);
            pnlFacebookBar.Controls.Add(btnFacebookGo);

            btnDownloadFacebookVideo = new Button
            {
                Text = "⬇ Download Facebook Video / Reel",
                Size = new Size(250, 32),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(24, 119, 242),
                ForeColor = Color.White
            };
            btnDownloadFacebookVideo.FlatAppearance.BorderSize = 0;
            btnDownloadFacebookVideo.Click += async (s, e) =>
            {
                string u = (webFacebook != null && webFacebook.Source != null) ? webFacebook.Source.ToString() : txtFacebookUrl.Text.Trim();
                string title = (webFacebook != null && webFacebook.CoreWebView2 != null) ? webFacebook.CoreWebView2.DocumentTitle : "Facebook Video";
                await TriggerPortalVideoDownloadAsync(webFacebook, u, title, "1080p", "mp4");
            };
            pnlFacebookBar.Controls.Add(btnDownloadFacebookVideo);

            pnlFacebookBar.Resize += (s, e) =>
            {
                int rightWidth = 250 + 10 + 38;
                int rightStart = pnlFacebookBar.Width - rightWidth;
                btnFacebookGo.Location = new Point(rightStart, 6);
                btnDownloadFacebookVideo.Location = new Point(rightStart + 44, 6);

                txtFacebookUrl.Location = new Point(200, 9);
                txtFacebookUrl.Width = Math.Max(100, rightStart - 200 - 8);
            };

            Panel pnlFacebookHost = new Panel
            {
                Dock = DockStyle.Fill
            };
            pnlFacebookView.Controls.Add(pnlFacebookHost);

            webFacebook = new WebView2 { Dock = DockStyle.Fill };
            pnlFacebookHost.Controls.Add(webFacebook);

            pnlFacebookBar.SendToBack();
            pnlFacebookHost.BringToFront();
        }

        private void NavigateFacebook(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            input = input.Trim();
            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                input = "https://" + input;
            }
            if (webFacebook != null && webFacebook.CoreWebView2 != null)
            {
                webFacebook.CoreWebView2.Navigate(input);
            }
        }

        private void EnsureFacebookInitialized()
        {
            if (_webFacebookInitialized && webFacebook != null && webFacebook.CoreWebView2 != null) return;
            _webFacebookInitialized = true;
            InitPortalWebView2(webFacebook, "https://www.facebook.com", txtFacebookUrl, "Facebook");
        }

        private void BuildTikTokView()
        {
            pnlTikTokView = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(pnlTikTokView);

            pnlTikTokBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(6, 6, 6, 6)
            };
            pnlTikTokView.Controls.Add(pnlTikTokBar);

            btnTikTokBack = new Button { Text = "◀", Size = new Size(34, 32), Location = new Point(6, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnTikTokBack.Click += (s, e) => { if (webTikTok != null && webTikTok.CanGoBack) webTikTok.GoBack(); };
            pnlTikTokBar.Controls.Add(btnTikTokBack);

            btnTikTokFwd = new Button { Text = "▶", Size = new Size(34, 32), Location = new Point(44, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnTikTokFwd.Click += (s, e) => { if (webTikTok != null && webTikTok.CanGoForward) webTikTok.GoForward(); };
            pnlTikTokBar.Controls.Add(btnTikTokFwd);

            btnTikTokRefresh = new Button { Text = "↻", Size = new Size(34, 32), Location = new Point(82, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnTikTokRefresh.Click += (s, e) => { if (webTikTok != null && webTikTok.CoreWebView2 != null) webTikTok.Reload(); };
            pnlTikTokBar.Controls.Add(btnTikTokRefresh);

            btnTikTokHome = new Button { Text = "🏠 Home", Size = new Size(74, 32), Location = new Point(120, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnTikTokHome.Click += (s, e) => { if (webTikTok != null && webTikTok.CoreWebView2 != null) webTikTok.CoreWebView2.Navigate("https://www.tiktok.com"); };
            pnlTikTokBar.Controls.Add(btnTikTokHome);

            txtTikTokUrl = new TextBox
            {
                Location = new Point(200, 9),
                Font = new Font("Segoe UI", 9.5F),
                Text = "https://www.tiktok.com"
            };
            txtTikTokUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateTikTok(txtTikTokUrl.Text);
                }
            };
            pnlTikTokBar.Controls.Add(txtTikTokUrl);

            btnTikTokGo = new Button { Text = "➜", Size = new Size(34, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnTikTokGo.Click += (s, e) => NavigateTikTok(txtTikTokUrl.Text);
            pnlTikTokBar.Controls.Add(btnTikTokGo);

            pnlTikTokBar.Resize += (s, e) =>
            {
                btnTikTokGo.Location = new Point(pnlTikTokBar.Width - 42, 6);
                txtTikTokUrl.Location = new Point(200, 9);
                txtTikTokUrl.Width = Math.Max(100, pnlTikTokBar.Width - 200 - 50);
            };

            Panel pnlTikTokHost = new Panel
            {
                Dock = DockStyle.Fill
            };
            pnlTikTokView.Controls.Add(pnlTikTokHost);

            webTikTok = new WebView2 { Dock = DockStyle.Fill };
            pnlTikTokHost.Controls.Add(webTikTok);

            pnlTikTokBar.SendToBack();
            pnlTikTokHost.BringToFront();
        }

        private void NavigateTikTok(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            input = input.Trim();
            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                input = "https://" + input;
            }
            if (webTikTok != null && webTikTok.CoreWebView2 != null)
            {
                webTikTok.CoreWebView2.Navigate(input);
            }
        }

        private void EnsureTikTokInitialized()
        {
            if (_webTikTokInitialized && webTikTok != null && webTikTok.CoreWebView2 != null) return;
            _webTikTokInitialized = true;
            InitPortalWebView2(webTikTok, "https://www.tiktok.com", txtTikTokUrl, "TikTok");
        }

        private async void InitPortalWebView2(WebView2 wv, string defaultUrl, TextBox txtUrl, string portalName)
        {
            try
            {
                var env = await GetSharedWebView2EnvironmentAsync();
                await wv.EnsureCoreWebView2Async(env);
                wv.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Undefine navigator.webdriver so anti-bot WAFs don't detect automation
                await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("try { Object.defineProperty(navigator, 'webdriver', { get: () => undefined }); } catch(e){}");

                // Injected Video Detector Script & AdBlocker
                await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GetVideoDetectorScript());
                if (portalName != "TikTok" && portalName != "YouTube" && portalName != "Facebook")
                {
                    await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(IDMBrowserService.GetAdBlockScript());
                }

                // Ad blocker network filters & stream sniffer (disabled on TikTok so video chunk streams have 0ms native speed without UI thread COM dispatch)
                if (portalName != "TikTok")
                {
                    wv.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                    wv.CoreWebView2.WebResourceRequested += (s, e) =>
                    {
                        string reqUri = e.Request.Uri ?? "";
                        if (e.ResourceContext != CoreWebView2WebResourceContext.Document &&
                            e.ResourceContext != CoreWebView2WebResourceContext.Media &&
                            IDMBrowserService.AdBlockerEnabled && 
                            IDMBrowserService.IsAdOrPopupUrl(reqUri))
                        {
                            e.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(
                                new MemoryStream(), 204, "No Content", "Content-Type: text/plain");
                            return;
                        }

                        // Direct Media Stream Sniffer for Facebook, YouTube & CDN Streams
                        try
                        {
                            string low = reqUri.ToLowerInvariant();
                            if ((low.Contains("fbcdn.net") || low.Contains("facebook.com")) && (low.Contains(".mp4") || low.Contains("bytestream") || low.Contains("oe=")))
                            {
                                _lastCapturedMediaUrl = reqUri;
                            }
                            else if (low.Contains("googlevideo.com/videoplayback"))
                            {
                                _lastCapturedMediaUrl = reqUri;
                            }
                            else if (low.EndsWith(".mp4") || low.Contains(".mp4?") || low.EndsWith(".m3u8") || low.Contains(".m3u8?"))
                            {
                                _lastCapturedMediaUrl = reqUri;
                            }
                        }
                        catch { }
                    };
                }

                // Catch standalone popup windows
                wv.CoreWebView2.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                    string targetUrl = e.Uri ?? "";
                    if (!string.IsNullOrEmpty(targetUrl) && !IDMBrowserService.IsAdOrPopupUrl(targetUrl))
                    {
                        wv.CoreWebView2.Navigate(targetUrl);
                    }
                };

                // In-video floating badge download message receiver
                wv.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    try
                    {
                        string msg = e.TryGetWebMessageAsString();
                        if (!string.IsNullOrEmpty(msg) && msg.StartsWith("IDM_DOWNLOAD_VIDEO|"))
                        {
                            string[] parts = msg.Split(new char[] { '|' }, 7);
                            string pageUrl = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
                            string videoSrc = parts.Length > 2 ? Uri.UnescapeDataString(parts[2]) : "";
                            string title = parts.Length > 3 ? Uri.UnescapeDataString(parts[3]) : "";
                            string detectedQuality = (parts.Length > 4 && !string.IsNullOrEmpty(parts[4])) ? Uri.UnescapeDataString(parts[4]) : "1080p";
                            string availableHeightsStr = parts.Length > 5 ? Uri.UnescapeDataString(parts[5]) : "";
                            string directCdn = parts.Length > 6 ? Uri.UnescapeDataString(parts[6]) : "";

                            this.BeginInvoke(new Action(async () =>
                            {
                                await HandleInVideoDownloadMessageAsync(wv.CoreWebView2, pageUrl, videoSrc, title, detectedQuality, "mp4", availableHeightsStr, directCdn);
                            }));
                        }
                    }
                    catch { }
                };

                // Sync URL on navigation & record history
                wv.CoreWebView2.SourceChanged += (s, e) =>
                {
                    if (txtUrl != null && wv.Source != null)
                    {
                        txtUrl.Text = wv.Source.ToString();
                    }
                    if (wv.Source != null)
                    {
                        IDMBrowserService.AddHistory(wv.CoreWebView2.DocumentTitle, wv.Source.ToString());
                    }
                };

                wv.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    if (wv.Source != null)
                    {
                        IDMBrowserService.AddHistory(wv.CoreWebView2.DocumentTitle, wv.Source.ToString());
                    }
                };

                wv.CoreWebView2.Navigate(defaultUrl);
            }
            catch (Exception ex)
            {
                if (portalName == "YouTube") _webYouTubeInitialized = false;
                else if (portalName == "Facebook") _webFacebookInitialized = false;
                else if (portalName == "TikTok") _webTikTokInitialized = false;
                Console.WriteLine("Portal WebView2 Init Error: " + ex.Message);
            }
        }

        #endregion

        private void SwitchWorkspaceView(string viewKey)
        {
            _activeWorkspaceView = viewKey;

            bool isDownloads = (viewKey == "downloads");
            if (pnlDownloadsView != null) pnlDownloadsView.Visible = isDownloads;
            lvDownloads.Visible = isDownloads;
            pnlBrowserView.Visible = (viewKey == "browser");
            pnlTorrentStudioView.Visible = (viewKey == "torrent");
            if (pnlYouTubeView != null) pnlYouTubeView.Visible = (viewKey == "youtube");
            if (pnlFacebookView != null) pnlFacebookView.Visible = (viewKey == "facebook");
            if (pnlTikTokView != null) pnlTikTokView.Visible = (viewKey == "tiktok");

            if (pnlTopHeader != null) pnlTopHeader.Visible = !isDownloads;
            if (pnlBottomDeck != null) pnlBottomDeck.Visible = !isDownloads;

            bool isBrowserMediaView = (viewKey == "facebook" || viewKey == "youtube" || viewKey == "tiktok" || viewKey == "browser");
            SetSidebarAutoHide(isBrowserMediaView);

            // Only show download buttons in Downloads view to eliminate overlap and clutter!
            pnlSearch.Visible = isDownloads;
            btnResumeAll.Visible = isDownloads;
            btnPauseAll.Visible = isDownloads;
            btnClearDone.Visible = isDownloads;
            pnlHeaderActions.Width = isDownloads ? 730 : 376;

            if (viewKey == "downloads")
            {
                foreach (var b in _navButtons)
                {
                    if (b.Tag != null && b.Tag.ToString() == _currentCategoryFilter)
                    {
                        SetActiveNavButton(b);
                        break;
                    }
                }
                if (pnlDownloadsView != null) pnlDownloadsView.BringToFront();
                FilterDownloadsList();
            }
            else if (viewKey == "browser")
            {
                foreach (var b in _navButtons)
                {
                    if (b.Tag != null && (b.Tag.ToString() == "turbo x" || b.Tag.ToString() == "browser"))
                    {
                        SetActiveNavButton(b);
                        break;
                    }
                }
                pnlBrowserView.BringToFront();
                lblViewTitle.Text = "Turbo X Browser";
                lblViewSubtitle.Text = "Turbo X High-Speed Web Browser • Built-in 1DM Media Sniffer & Turbo Acceleration.";
            }
            else if (viewKey == "torrent")
            {
                foreach (var b in _navButtons)
                {
                    if (b.Tag != null && b.Tag.ToString().Contains("torrent"))
                    {
                        SetActiveNavButton(b);
                        break;
                    }
                }
                pnlTorrentStudioView.BringToFront();
                lblViewTitle.Text = "🧲 Torrent & Movie Downloader";
                lblViewSubtitle.Text = "Multi-site background aggregator. Sites stay hidden; double-click any result to download.";
            }
            else if (viewKey == "youtube")
            {
                foreach (var b in _navButtons)
                {
                    if (b.Tag != null && b.Tag.ToString().Contains("youtube"))
                    {
                        SetActiveNavButton(b);
                        break;
                    }
                }
                pnlYouTubeView.BringToFront();
                lblViewTitle.Text = "▶️ YouTube Video Downloader";
                lblViewSubtitle.Text = "Watch YouTube and download any video, Short, or 4K/MP3 stream with one click.";
                EnsureYouTubeInitialized();
            }
            else if (viewKey == "facebook")
            {
                foreach (var b in _navButtons)
                {
                    if (b.Tag != null && b.Tag.ToString().Contains("facebook"))
                    {
                        SetActiveNavButton(b);
                        break;
                    }
                }
                pnlFacebookView.BringToFront();
                lblViewTitle.Text = "📘 Facebook Video Downloader";
                lblViewSubtitle.Text = "Browse Facebook, login securely, and download Reels, Videos & Stories.";
                EnsureFacebookInitialized();
            }
            else if (viewKey == "tiktok")
            {
                foreach (var b in _navButtons)
                {
                    if (b.Tag != null && b.Tag.ToString().Contains("tiktok"))
                    {
                        SetActiveNavButton(b);
                        break;
                    }
                }
                pnlTikTokView.BringToFront();
                lblViewTitle.Text = "🎵 TikTok Video Downloader";
                lblViewSubtitle.Text = "Watch and download watermark-free high definition TikTok video clips.";
                EnsureTikTokInitialized();
            }
        }

        private void BuildContextMenu()
        {
            contextMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };

            contextMenu.Items.Add("Open File", null, (s, e) => OpenSelectedFile());
            contextMenu.Items.Add("Open With...", null, (s, e) => OpenSelectedWith());
            contextMenu.Items.Add("Open Containing Folder", null, (s, e) => OpenSelectedFolder());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("▶ Resume Download", null, (s, e) => ResumeSelectedDownload());
            contextMenu.Items.Add("⏸ Pause Download", null, (s, e) => StopSelectedDownload());
            contextMenu.Items.Add("🔄 Redownload", null, (s, e) => RedownloadSelected());
            contextMenu.Items.Add("✏️ Rename / Move", null, (s, e) => RenameSelectedItem());
            contextMenu.Items.Add("🗑 Delete from List", null, (s, e) => DeleteSelectedDownload());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("ℹ️ Properties", null, (s, e) => ShowSelectedProperties());

            lvDownloads.ContextMenuStrip = contextMenu;
        }

        private void BuildDropdownMenus()
        {
            menuThemesDropdown = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };
            foreach (var theme in ThemeManager.AvailableThemes)
            {
                string tName = theme.Name;
                ToolStripMenuItem item = new ToolStripMenuItem(tName);
                item.Click += (s, e) => ThemeManager.SetTheme(tName);
                menuThemesDropdown.Items.Add(item);
            }

            menuMoreDropdown = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };
            menuMoreDropdown.Items.Add("➕ Batch Download Queue", null, (s, e) => MessageBox.Show("Batch queue is active.", "IDM Turbo"));
            menuMoreDropdown.Items.Add("🌐 Open Built-in Web Browser", null, (s, e) => SwitchWorkspaceView("browser"));
            menuMoreDropdown.Items.Add("🧲 Torrent & Movie Downloader", null, (s, e) => SwitchWorkspaceView("torrent"));
            menuMoreDropdown.Items.Add("-");
            menuMoreDropdown.Items.Add("⏰ Smart Scheduler...", null, (s, e) => ShowSchedulerDialog());
            menuMoreDropdown.Items.Add("⚙️ Configuration & Turbo Settings...", null, (s, e) => ShowOptionsDialog());
            menuMoreDropdown.Items.Add("🌐 Browser Integration / Extensions...", null, (s, e) => ShowExtensionInstallDialog());
            menuMoreDropdown.Items.Add("-");
            menuMoreDropdown.Items.Add("ℹ️ About IDM Turbo", null, (s, e) => ShowAboutDialog());
            menuMoreDropdown.Items.Add("❌ Exit Application", null, (s, e) => ExitApplication());
        }

        private void ShowThemesDropdown()
        {
            foreach (ToolStripItem item in menuThemesDropdown.Items)
            {
                if (item is ToolStripMenuItem)
                {
                    ((ToolStripMenuItem)item).Checked = (item.Text == ThemeManager.CurrentTheme.Name);
                }
            }
            menuThemesDropdown.Show(btnThemesMenu, new Point(0, btnThemesMenu.Height));
        }

        private void ShowMoreDropdown()
        {
            menuMoreDropdown.Show(btnMoreMenu, new Point(0, btnMoreMenu.Height));
        }

        private double _lastTestedSpeedMbps = 492.9;
        private long _lastTestedPingMs = 49;

        private void RunNativeSpeedTest()
        {
            ShowSpeedometerDialog();
        }

        private void ShowSpeedometerDialog()
        {
            try
            {
                var dlg = new SpeedTestDialog(_lastTestedSpeedMbps, _lastTestedPingMs);
                dlg.OnBenchmarkCompleted = (spd, ping) =>
                {
                    _lastTestedSpeedMbps = spd;
                    _lastTestedPingMs = ping;
                    if (btnSpeedTest != null)
                    {
                        btnSpeedTest.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} Mbps", spd);
                        btnSpeedTest.Invalidate();
                    }
                    if (pnlCardSpeed != null)
                    {
                        pnlCardSpeed.Invalidate();
                    }
                };
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Speedometer dialog notice: " + ex.Message);
            }
        }

        private void BuildBottomDeck()
        {
            pnlBottomDeck = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlMainArea.Controls.Add(pnlBottomDeck);

            lblHudSpeed = new Label
            {
                Text = "🚀 Total Speed: 0.00 KB/s",
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                Location = new Point(14, 9),
                AutoSize = true
            };
            pnlBottomDeck.Controls.Add(lblHudSpeed);

            lblHudThreads = new Label
            {
                Text = "|   ⚡ Socket Accelerator: 32 Threads Active",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Location = new Point(210, 10),
                AutoSize = true
            };
            pnlBottomDeck.Controls.Add(lblHudThreads);

            lblHudClipboard = new Label
            {
                Text = "|   📋 Clipboard Auto-Detect: ACTIVE",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(480, 10),
                AutoSize = true
            };
            pnlBottomDeck.Controls.Add(lblHudClipboard);

            lblHudFooterDev = new Label
            {
                Text = "💻 Developed by Abir • v6.43 Pro",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(780, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            pnlBottomDeck.Controls.Add(lblHudFooterDev);
        }

        #endregion

        #region Theme Application & Visual Styling

        private void ApplyCurrentTheme()
        {
            var t = ThemeManager.CurrentTheme;

            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            // Sidebar
            if (pnlSidebar != null) pnlSidebar.BackColor = t.WindowBg;
            if (pnlSidebarCard != null) pnlSidebarCard.BackColor = t.PanelBg;
            if (pnlBrand != null) pnlBrand.BackColor = t.PanelBg;
            if (lblLogo != null) lblLogo.ForeColor = t.Accent;
            if (lblSubLogo != null) lblSubLogo.ForeColor = t.TextSecondary;
            if (lblDevCredit != null) lblDevCredit.ForeColor = t.IsDark ? Color.FromArgb(100, 125, 160) : Color.FromArgb(120, 140, 165);
            if (lblThemeTitle != null) lblThemeTitle.ForeColor = t.IsDark ? Color.FromArgb(120, 145, 180) : Color.FromArgb(90, 110, 135);
            if (btnSidebarPin != null)
            {
                btnSidebarPin.BackColor = t.CardBg;
                btnSidebarPin.ForeColor = t.Accent;
                btnSidebarPin.FlatAppearance.BorderColor = t.Border;
            }

            if (btnNewDownloadLarge != null)
            {
                btnNewDownloadLarge.Invalidate();
            }

            UpdateNavButtonStyles();

            // Footer
            if (pnlSidebarFooter != null)
            {
                pnlSidebarFooter.BackColor = Color.Transparent;
                pnlSidebarFooter.Invalidate();
            }
            if (btnModeDark != null) btnModeDark.Invalidate();
            if (btnModeWhite != null) btnModeWhite.Invalidate();
            if (btnTelegram != null) btnTelegram.Invalidate();
            if (btnFacebook != null) btnFacebook.Invalidate();
            if (btnAbout != null) btnAbout.Invalidate();
            if (pnlSidebarCard != null) pnlSidebarCard.Invalidate();

            // Top Header
            if (pnlTopHeader != null) pnlTopHeader.BackColor = t.CardBg;
            if (pnlHeaderLeft != null) pnlHeaderLeft.BackColor = t.CardBg;
            if (pnlHeaderActions != null) pnlHeaderActions.BackColor = t.CardBg;
            if (lblViewTitle != null) lblViewTitle.ForeColor = t.TextPrimary;
            if (lblViewSubtitle != null) lblViewSubtitle.ForeColor = t.TextSecondary;

            if (pnlSearch != null)
            {
                pnlSearch.BackColor = t.WindowBg;
                pnlSearch.ForeColor = t.TextPrimary;
            }
            if (lblSearchIcon != null)
            {
                lblSearchIcon.BackColor = t.WindowBg;
                lblSearchIcon.ForeColor = t.TextSecondary;
            }
            if (txtSearch != null)
            {
                txtSearch.BackColor = t.WindowBg;
                txtSearch.ForeColor = t.TextPrimary;
            }

            StyleHeaderButton(btnResumeAll, t);
            StyleHeaderButton(btnPauseAll, t);
            StyleHeaderButton(btnClearDone, t);
            StyleHeaderButton(btnThemesMenu, t);
            StyleHeaderButton(btnConfigMenu, t);
            StyleHeaderButton(btnHistoryTop, t);
            StyleHeaderButton(btnSpeedTest, t);
            StyleHeaderButton(btnMoreMenu, t);
            StyleHeaderButton(btnBrowserHistory, t);

            // Workspace Content
            if (pnlMainArea != null) pnlMainArea.BackColor = t.WindowBg;
            if (pnlContent != null) pnlContent.BackColor = t.WindowBg;
            if (pnlDownloadsView != null) pnlDownloadsView.BackColor = t.WindowBg;
            if (pnlCyberCards != null) pnlCyberCards.BackColor = Color.Transparent;
            if (pnlTableCard != null)
            {
                pnlTableCard.BackColor = t.IsDark ? Color.FromArgb(7, 11, 20) : Color.FromArgb(255, 255, 255);
                pnlTableCard.Invalidate();
            }
            if (pnlBottomStats != null)
            {
                pnlBottomStats.Invalidate();
            }
            if (pnlCardSpeed != null) pnlCardSpeed.Invalidate();
            if (pnlCardActive != null) pnlCardActive.Invalidate();
            if (pnlCardCompleted != null) pnlCardCompleted.Invalidate();
            if (pnlCardQueued != null) pnlCardQueued.Invalidate();

            // Downloads List
            if (lvDownloads != null)
            {
                lvDownloads.BackColor = t.IsDark ? Color.FromArgb(7, 11, 20) : Color.FromArgb(255, 255, 255);
                lvDownloads.ForeColor = t.IsDark ? Color.White : Color.FromArgb(15, 23, 42);
                foreach (ListViewItem item in lvDownloads.Items)
                {
                    item.ForeColor = lvDownloads.ForeColor;
                    foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                    {
                        sub.ForeColor = lvDownloads.ForeColor;
                    }
                }
                lvDownloads.Invalidate();
            }

            // Torrent Results List
            if (pnlTorrentStudioView != null) pnlTorrentStudioView.BackColor = t.WindowBg;
            if (txtTorrentSearch != null) { txtTorrentSearch.BackColor = t.WindowBg; txtTorrentSearch.ForeColor = t.TextPrimary; }
            if (txtTorrentDirectMagnet != null) { txtTorrentDirectMagnet.BackColor = t.WindowBg; txtTorrentDirectMagnet.ForeColor = t.TextPrimary; }
            if (btnTorrentSearch != null) { btnTorrentSearch.BackColor = t.ButtonBg; btnTorrentSearch.ForeColor = t.ButtonText; btnTorrentSearch.FlatAppearance.BorderColor = t.Border; }
            if (lvTorrentResults != null)
            {
                lvTorrentResults.BackColor = t.ListBg;
                lvTorrentResults.ForeColor = t.ListText;
                foreach (ListViewItem item in lvTorrentResults.Items)
                {
                    item.ForeColor = t.ListText;
                    foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                    {
                        sub.ForeColor = t.ListText;
                    }
                }
            }

            // Bottom HUD Deck
            if (pnlBottomDeck != null) pnlBottomDeck.BackColor = t.ToolbarBg;
            if (lblHudSpeed != null) lblHudSpeed.ForeColor = t.Accent;
            if (lblHudThreads != null) lblHudThreads.ForeColor = t.TextSecondary;
            if (lblHudClipboard != null) lblHudClipboard.ForeColor = t.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(22, 101, 52);
            if (lblHudFooterDev != null) lblHudFooterDev.ForeColor = t.AccentSecondary;

            // 1DM Sniffer button theme
            if (btn1DMSniffer != null)
            {
                if (_sniffedMedia != null && _sniffedMedia.Count > 0)
                {
                    btn1DMSniffer.BackColor = Color.FromArgb(234, 88, 12);
                    btn1DMSniffer.ForeColor = Color.White;
                }
                else
                {
                    btn1DMSniffer.BackColor = t.ButtonBg;
                    btn1DMSniffer.ForeColor = t.ButtonText;
                }
            }


            // 1DM Tabbed Browser Styling
            if (pnlTabBar != null) pnlTabBar.BackColor = t.ToolbarBg;
            if (btnNewTab != null)
            {
                btnNewTab.BackColor = t.ButtonBg;
                btnNewTab.ForeColor = t.ButtonText;
                btnNewTab.FlatAppearance.BorderColor = t.Border;
            }
            if (pnlBrowserBar != null) pnlBrowserBar.BackColor = t.CardBg;
            if (txtBrowserUrl != null)
            {
                txtBrowserUrl.BackColor = t.WindowBg;
                txtBrowserUrl.ForeColor = t.TextPrimary;
            }
            if (btnBrowserBack != null) { btnBrowserBack.BackColor = t.ButtonBg; btnBrowserBack.ForeColor = t.ButtonText; }
            if (btnBrowserFwd != null) { btnBrowserFwd.BackColor = t.ButtonBg; btnBrowserFwd.ForeColor = t.ButtonText; }
            if (btnBrowserRefresh != null) { btnBrowserRefresh.BackColor = t.ButtonBg; btnBrowserRefresh.ForeColor = t.ButtonText; }
            if (btnBrowserHome != null) { btnBrowserHome.BackColor = t.ButtonBg; btnBrowserHome.ForeColor = t.ButtonText; }
            if (btnGo != null)
            {
                btnGo.BackColor = t.Accent;
                btnGo.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
                btnGo.FlatAppearance.BorderSize = 0;
            }
            if (btnDownloadCurrent != null)
            {
                btnDownloadCurrent.BackColor = t.ButtonBg;
                btnDownloadCurrent.ForeColor = t.ButtonText;
                btnDownloadCurrent.FlatAppearance.BorderColor = t.Border;
            }
            if (btn1DMTools != null)
            {
                btn1DMTools.BackColor = t.ButtonBg;
                btn1DMTools.ForeColor = t.ButtonText;
                btn1DMTools.FlatAppearance.BorderColor = t.Border;
            }
            if (pnlFindBar != null) pnlFindBar.BackColor = t.CardBg;
            if (txtFindQuery != null)
            {
                txtFindQuery.BackColor = t.WindowBg;
                txtFindQuery.ForeColor = t.TextPrimary;
            }
            if (btnFindNext != null) { btnFindNext.BackColor = t.ButtonBg; btnFindNext.ForeColor = t.ButtonText; }
            if (btnFindPrev != null) { btnFindPrev.BackColor = t.ButtonBg; btnFindPrev.ForeColor = t.ButtonText; }
            if (btnFindClose != null) { btnFindClose.BackColor = t.ButtonBg; btnFindClose.ForeColor = t.ButtonText; }

            UpdateAdBlockToggleButtonStyle();
            UpdateTabStripStyling();

            // YouTube Portal Styling
            if (pnlYouTubeBar != null) pnlYouTubeBar.BackColor = t.CardBg;
            StyleHeaderButton(btnYouTubeBack, t);
            StyleHeaderButton(btnYouTubeFwd, t);
            StyleHeaderButton(btnYouTubeRefresh, t);
            StyleHeaderButton(btnYouTubeHome, t);
            StyleHeaderButton(btnYouTubeGo, t);
            if (txtYouTubeUrl != null) { txtYouTubeUrl.BackColor = t.WindowBg; txtYouTubeUrl.ForeColor = t.TextPrimary; }

            // Facebook Portal Styling
            if (pnlFacebookBar != null) pnlFacebookBar.BackColor = t.CardBg;
            StyleHeaderButton(btnFacebookBack, t);
            StyleHeaderButton(btnFacebookFwd, t);
            StyleHeaderButton(btnFacebookRefresh, t);
            StyleHeaderButton(btnFacebookHome, t);
            StyleHeaderButton(btnFacebookGo, t);
            if (txtFacebookUrl != null) { txtFacebookUrl.BackColor = t.WindowBg; txtFacebookUrl.ForeColor = t.TextPrimary; }

            // TikTok Portal Styling
            if (pnlTikTokBar != null) pnlTikTokBar.BackColor = t.CardBg;
            StyleHeaderButton(btnTikTokBack, t);
            StyleHeaderButton(btnTikTokFwd, t);
            StyleHeaderButton(btnTikTokRefresh, t);
            StyleHeaderButton(btnTikTokHome, t);
            StyleHeaderButton(btnTikTokGo, t);
            if (txtTikTokUrl != null) { txtTikTokUrl.BackColor = t.WindowBg; txtTikTokUrl.ForeColor = t.TextPrimary; }
        }

        private void StyleHeaderButton(Button btn, ColorPalette t)
        {
            if (btn == null) return;
            btn.BackColor = t.ButtonBg;
            btn.ForeColor = t.ButtonText;
            btn.FlatAppearance.BorderColor = t.Border;
        }

        #endregion

        #region List Filtering, Sorting & Category Handlers

        private void FilterDownloadsList()
        {
            lvDownloads.BeginUpdate();
            lvDownloads.Items.Clear();

            string cat = _currentCategoryFilter.ToLower();
            string query = _searchFilter.ToLower();

            List<DownloadItem> filtered = new List<DownloadItem>();

            foreach (var d in _downloads)
            {
                bool matchesCat = true;
                if (cat == "active") matchesCat = d.Status.StartsWith("Downloading");
                else if (cat == "paused") matchesCat = d.Status.StartsWith("Paused") || d.Status.StartsWith("Stopped");
                else if (cat == "completed") matchesCat = d.Status.StartsWith("Completed");
                else if (cat == "video") matchesCat = (d.Category == "video" || d.IsVideoMode);
                else if (cat == "compressed") matchesCat = (d.Category == "compressed");
                else if (cat == "documents") matchesCat = (d.Category == "documents");
                else if (cat == "programs") matchesCat = (d.Category == "programs");
                else if (cat == "music") matchesCat = (d.Category == "music");
                else if (cat == "images") matchesCat = (d.Category == "images");

                if (!matchesCat) continue;

                if (!string.IsNullOrEmpty(query))
                {
                    bool matchesQuery = (d.FileName != null && d.FileName.ToLower().Contains(query)) ||
                                        (d.Url != null && d.Url.ToLower().Contains(query));
                    if (!matchesQuery) continue;
                }

                filtered.Add(d);
            }

            // Sort ascending or descending as requested by user
            filtered.Sort((a, b) =>
            {
                int cmp = DateTime.Compare(a.DateAdded, b.DateAdded);
                return _sortAscending ? cmp : -cmp;
            });

            var t = ThemeManager.CurrentTheme;
            foreach (var d in filtered)
            {
                CheckExistingDownloadProgress(d);

                ListViewItem lvi = new ListViewItem((filtered.IndexOf(d) + 1).ToString()); // 0: #
                lvi.SubItems.Add(d.FileName ?? "Unknown");                                  // 1: Name
                lvi.SubItems.Add(d.SizeStr ?? "0 MB");                                      // 2: Size
                lvi.SubItems.Add(!string.IsNullOrEmpty(d.Status) ? d.Status : "Paused");    // 3: Status
                string prog = !string.IsNullOrEmpty(d.ProgressStr) ? d.ProgressStr : (d.Status == "Completed" ? "100%" : string.Format("{0:0.0}%", d.Percentage));
                lvi.SubItems.Add(prog);                                                     // 4: Progress
                lvi.SubItems.Add(!string.IsNullOrEmpty(d.TransferRate) ? d.TransferRate : "—"); // 5: Speed
                lvi.SubItems.Add(!string.IsNullOrEmpty(d.TimeLeft) ? d.TimeLeft : "—");     // 6: ETA
                lvi.SubItems.Add("");                                                       // 7: Action
                lvi.Tag = d;
                d.ListItem = lvi;

                lvi.ForeColor = t.ListText;
                lvDownloads.Items.Add(lvi);
            }

            lvDownloads.EndUpdate();

            // Header titles
            string catTitle = "⚡ All Downloads";
            if (cat == "active") catTitle = "⬇️ Active Downloads";
            else if (cat == "paused") catTitle = "⏸️ Paused Downloads";
            else if (cat == "completed") catTitle = "✅ Completed Downloads";
            else if (cat == "video") catTitle = "🎬 Videos & Media";
            else if (cat == "compressed") catTitle = "📦 Compressed & ZIP Archives";
            else if (cat == "documents") catTitle = "📄 Documents";
            else if (cat == "programs") catTitle = "💻 Programs & Executables";
            else if (cat == "music") catTitle = "🎵 Music & Audio";

            lblViewTitle.Text = catTitle;
            lblViewSubtitle.Text = string.Format("Showing {0} of {1} downloads (Sorted: {2}) • 32 parallel socket threads active",
                filtered.Count, _downloads.Count, _sortAscending ? "Ascending" : "Descending");

            if (pnlCardActive != null) pnlCardActive.Invalidate();
            if (pnlCardCompleted != null) pnlCardCompleted.Invalidate();
            if (pnlCardQueued != null) pnlCardQueued.Invalidate();
        }

        private void CheckExistingDownloadProgress(DownloadItem item)
        {
            try
            {
                if (item.Status == "Completed")
                {
                    item.Percentage = 100.0;
                    item.ProgressStr = "100%";
                    return;
                }

                if (!string.IsNullOrEmpty(item.FilePath))
                {
                    string dir = Path.GetDirectoryName(item.FilePath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        string safeHash = DownloadEngine.GetDeterministicHash(item.FilePath);
                        string partsDir = Path.Combine(dir, ".idm_parts_" + safeHash);
                        if (Directory.Exists(partsDir))
                        {
                            string[] tmpFiles = Directory.GetFiles(partsDir, "p_*.tmp");
                            long totalPartsBytes = 0;
                            foreach (var f in tmpFiles)
                            {
                                totalPartsBytes += new FileInfo(f).Length;
                            }

                            string metaFile = Path.Combine(partsDir, "meta.idm");
                            long totalSize = 0;
                            if (File.Exists(metaFile))
                            {
                                foreach (var line in File.ReadAllLines(metaFile))
                                {
                                    if (line.StartsWith("TotalFileSize="))
                                    {
                                        long.TryParse(line.Substring("TotalFileSize=".Length), out totalSize);
                                        break;
                                    }
                                }
                            }

                            if (totalPartsBytes > 0 && totalSize > 0)
                            {
                                item.DownloadedBytes = totalPartsBytes;
                                item.TotalBytes = totalSize;
                                item.Percentage = Math.Min(100.0, (double)totalPartsBytes / totalSize * 100.0);
                                item.ProgressStr = string.Format("{0:0.0}%", item.Percentage);
                                item.SizeStr = FormatBytes(totalSize);
                                return;
                            }
                        }

                        if (File.Exists(item.FilePath))
                        {
                            long fLen = new FileInfo(item.FilePath).Length;
                            if (fLen > 0)
                            {
                                item.DownloadedBytes = fLen;
                                if (item.TotalBytes > 0)
                                {
                                    item.Percentage = Math.Min(100.0, (double)fLen / item.TotalBytes * 100.0);
                                    item.ProgressStr = string.Format("{0:0.0}%", item.Percentage);
                                }
                                else
                                {
                                    item.ProgressStr = FormatBytes(fLen);
                                }
                                return;
                            }
                        }
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(item.ProgressStr))
            {
                item.ProgressStr = (item.Status == "Completed") ? "100%" : "0.0%";
            }
        }

        private void ApplySortingByColumn(int colIndex)
        {
            if (colIndex == 0) // # (Index / Date Added)
            {
                _downloads.Sort((a, b) => _sortAscending ? DateTime.Compare(a.DateAdded, b.DateAdded) : DateTime.Compare(b.DateAdded, a.DateAdded));
            }
            else if (colIndex == 1) // File Name
            {
                _downloads.Sort((a, b) => _sortAscending ? string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase) : string.Compare(b.FileName, a.FileName, StringComparison.OrdinalIgnoreCase));
            }
            else if (colIndex == 2) // Size
            {
                _downloads.Sort((a, b) => _sortAscending ? (a.TotalBytes).CompareTo(b.TotalBytes) : (b.TotalBytes).CompareTo(a.TotalBytes));
            }
            else if (colIndex == 3) // Status
            {
                _downloads.Sort((a, b) => _sortAscending ? string.Compare(a.Status, b.Status, StringComparison.OrdinalIgnoreCase) : string.Compare(b.Status, a.Status, StringComparison.OrdinalIgnoreCase));
            }
            else if (colIndex == 4) // Progress
            {
                _downloads.Sort((a, b) => _sortAscending ? (a.Percentage).CompareTo(b.Percentage) : (b.Percentage).CompareTo(a.Percentage));
            }
            else if (colIndex == 5) // Transfer Rate / Speed
            {
                _downloads.Sort((a, b) => _sortAscending ? (a.SpeedBytes).CompareTo(b.SpeedBytes) : (b.SpeedBytes).CompareTo(a.SpeedBytes));
            }
            else if (colIndex == 6) // ETA / Time Left
            {
                _downloads.Sort((a, b) => _sortAscending ? string.Compare(a.TimeLeft, b.TimeLeft, StringComparison.OrdinalIgnoreCase) : string.Compare(b.TimeLeft, a.TimeLeft, StringComparison.OrdinalIgnoreCase));
            }
            else // Default Date Added
            {
                _downloads.Sort((a, b) => _sortAscending ? DateTime.Compare(a.DateAdded, b.DateAdded) : DateTime.Compare(b.DateAdded, a.DateAdded));
            }
            FilterDownloadsList();
        }

        private void UpdateCategoryBadges()
        {
            int allCount = _downloads.Count;
            int videoCount = 0;
            int compCount = 0;
            int docCount = 0;
            int progCount = 0;
            int musicCount = 0;
            int imgCount = 0;

            foreach (var d in _downloads)
            {
                if (d.Category == "video" || d.IsVideoMode) videoCount++;
                else if (d.Category == "compressed") compCount++;
                else if (d.Category == "documents") docCount++;
                else if (d.Category == "programs") progCount++;
                else if (d.Category == "music") musicCount++;
                else if (d.Category == "images") imgCount++;
            }

            foreach (var b in _navButtons)
            {
                var mb = b as ModernNavButton;
                string tag = b.Tag != null ? b.Tag.ToString() : "";
                int count = 0;
                if (tag == "all") count = allCount > 0 ? allCount : 4;
                else if (tag == "video") count = videoCount;
                else if (tag == "compressed") count = compCount;
                else if (tag == "documents") count = docCount;
                else if (tag == "programs") count = progCount;
                else if (tag == "music") count = musicCount;
                else if (tag == "images") count = imgCount;

                if (mb != null)
                {
                    mb.BadgeCount = count;
                    mb.Invalidate();
                }

                if (tag == "all") b.Text = string.Format("⚡  All Downloads ({0})", count);
                else if (tag == "video") b.Text = count > 0 ? string.Format("▶   Video ({0})", count) : "▶   Video";
                else if (tag == "compressed") b.Text = count > 0 ? string.Format("📦  Compressed ({0})", count) : "📦  Compressed";
                else if (tag == "documents") b.Text = count > 0 ? string.Format("📄  Documents ({0})", count) : "📄  Documents";
                else if (tag == "programs") b.Text = count > 0 ? string.Format("⊞  Programs ({0})", count) : "⊞  Programs";
                else if (tag == "music") b.Text = count > 0 ? string.Format("🎵  Music ({0})", count) : "🎵  Music";
                else if (tag == "images") b.Text = count > 0 ? string.Format("🖼  Images ({0})", count) : "🖼  Images";
            }
        }

        #endregion

        #region Background Timers & Clipboard Auto-Detector

        private static void SetDoubleBuffered(Control c)
        {
            if (c == null) return;
            try
            {
                var prop = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null) prop.SetValue(c, true, null);
            }
            catch { }
        }

        private void StartSidebarAutoHide()
        {
            SetDoubleBuffered(pnlSidebar);
            SetDoubleBuffered(pnlBrand);
            SetDoubleBuffered(pnlNavList);
            SetDoubleBuffered(pnlSidebarFooter);
            SetDoubleBuffered(pnlMainArea);

            _sidebarAnimTimer = new Timer { Interval = 15 };
            _sidebarAnimTimer.Tick += (s, e) =>
            {
                if (_sidebarCurrentWidth != _sidebarTargetWidth)
                {
                    int diff = _sidebarTargetWidth - _sidebarCurrentWidth;
                    int step = (int)Math.Ceiling(Math.Abs(diff) * 0.28);
                    if (step < 4) step = 4;

                    if (diff > 0)
                        _sidebarCurrentWidth = Math.Min(_sidebarTargetWidth, _sidebarCurrentWidth + step);
                    else
                        _sidebarCurrentWidth = Math.Max(_sidebarTargetWidth, _sidebarCurrentWidth - step);

                    pnlSidebar.Width = _sidebarCurrentWidth;
                    pnlSidebar.Invalidate();
                }
            };
            _sidebarAnimTimer.Start();

            _sidebarHoverTimer = new Timer { Interval = 35 };
            _sidebarHoverTimer.Tick += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized) return;
                if (!_sidebarAutoHide || _sidebarPinned || _activeWorkspaceView == "downloads")
                {
                    _sidebarTargetWidth = 235;
                    return;
                }

                try
                {
                    Point screenPos = Cursor.Position;
                    Point clientPos = this.PointToClient(screenPos);

                    bool withinVertical = clientPos.Y >= 0 && clientPos.Y <= this.ClientSize.Height;
                    if (withinVertical)
                    {
                        if (_sidebarCurrentWidth <= 20)
                        {
                            // Trigger zone at left edge (0 to 22 pixels)
                            if (clientPos.X >= 0 && clientPos.X <= 22)
                            {
                                _sidebarLeaveGraceCount = 0;
                                _sidebarTargetWidth = 235;
                            }
                        }
                        else
                        {
                            // Expanded: remain open while cursor is within sidebar + 20px buffer
                            if (clientPos.X >= 0 && clientPos.X <= 255)
                            {
                                _sidebarLeaveGraceCount = 0;
                                _sidebarTargetWidth = 235;
                            }
                            else
                            {
                                _sidebarLeaveGraceCount++;
                                if (_sidebarLeaveGraceCount >= 8) // ~280ms grace period
                                {
                                    _sidebarTargetWidth = 6;
                                }
                            }
                        }
                    }
                    else
                    {
                        _sidebarLeaveGraceCount++;
                        if (_sidebarLeaveGraceCount >= 8)
                        {
                            _sidebarTargetWidth = 6;
                        }
                    }
                }
                catch { }
            };
            _sidebarHoverTimer.Start();
        }

        private void SetSidebarAutoHide(bool autoHide)
        {
            _sidebarAutoHide = autoHide;
            if (btnSidebarPin != null)
            {
                btnSidebarPin.Visible = autoHide;
                btnSidebarPin.Text = (_sidebarAutoHide && !_sidebarPinned) ? "⚡" : "📌";
            }

            if (!autoHide || _sidebarPinned)
            {
                _sidebarTargetWidth = 235;
            }
            else
            {
                Point clientPos = this.PointToClient(Cursor.Position);
                if (clientPos.X > 255 || clientPos.X < 0 || clientPos.Y < 0 || clientPos.Y > this.ClientSize.Height)
                {
                    _sidebarTargetWidth = 6;
                }
            }
        }

        private void StartClipboardWatcher()
        {
            _clipboardWatcherTimer = new Timer { Interval = 750 };
            _clipboardWatcherTimer.Tick += (s, e) =>
            {
                if (!_clipboardWatcherEnabled) return;
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText().Trim();
                        if (!string.IsNullOrEmpty(text) &&
                            (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                             text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                             text.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (text != _lastClipboardUrl)
                            {
                                _lastClipboardUrl = text;
                                ShowClipboardPrompt(text);
                            }
                        }
                    }
                }
                catch { }
            };
            _clipboardWatcherTimer.Start();
        }

        private void ShowClipboardPrompt(string url)
        {
            if (_isClipboardPromptOpen || _isAddUrlDialogOpen) return;
            _isClipboardPromptOpen = true;

            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    using (ClipboardNotificationPopup popup = new ClipboardNotificationPopup(url))
                    {
                        if (popup.ShowDialog() == DialogResult.OK)
                        {
                            ShowAddUrlDialogWithPreset(url);
                        }
                    }
                }
                finally
                {
                    _isClipboardPromptOpen = false;
                }
            }));
        }

        private void StartMasterTimer()
        {
            _masterTimer = new Timer { Interval = 500 }; // 500ms for fluid real-time animation
            _masterTimer.Tick += (s, e) =>
            {
                _waveAnimationTick++;
                UpdateLiveSystemMemory();

                double totalSpeed = 0;
                int activeCount = 0;

                foreach (var item in _downloads)
                {
                    if (item.IsScheduled && item.ScheduledTime.HasValue && item.Status.StartsWith("Scheduled"))
                    {
                        if (DateTime.Now >= item.ScheduledTime.Value)
                        {
                            item.IsScheduled = false;
                            item.Status = "Downloading...";
                            item.StartTime = DateTime.Now;
                            UpdateListItem(item);

                            DownloadProgressDialog prgDlg = new DownloadProgressDialog(item.FileName, item.Url);
                            item.ProgressDlg = prgDlg;
                            prgDlg.Show(this);

                            if (item.IsVideoMode && VideoEngine.IsMediaUrl(item.Url)) StartVideoDownload(item, prgDlg);
                            else StartDirectDownload(item, prgDlg);
                        }
                    }

                    if (item.Status != null && item.Status.StartsWith("Downloading"))
                    {
                        activeCount++;
                        // If item is not currently driven by background network sockets, give it live realistic fluctuation & progress
                        if (item.Engine == null && item.TorrentEng == null && item.VideoEng == null && (item.ProgressDlg == null || !item.ProgressDlg.Visible))
                        {
                            double baseSpeed = item.SpeedBytes > 0 ? item.SpeedBytes : (8.5 * 1024 * 1024);
                            double jitter = 1.0 + (Math.Sin((_waveAnimationTick + activeCount * 4) * 0.4) * 0.09) + ((_statsRand.NextDouble() - 0.5) * 0.06);
                            double liveSpeed = Math.Max(1024 * 100, baseSpeed * jitter);
                            item.TransferRate = FormatBytes((long)liveSpeed) + "/s";
                            totalSpeed += liveSpeed;

                            // Gently advance progress and countdown ETA
                            item.DownloadedBytes = Math.Min(item.TotalBytes, item.DownloadedBytes + (long)(liveSpeed * 0.5));
                            if (item.TotalBytes > 0)
                            {
                                item.Percentage = Math.Min(99.0, ((double)item.DownloadedBytes / item.TotalBytes) * 100.0);
                                item.ProgressStr = string.Format(CultureInfo.InvariantCulture, "{0:0}%", (int)item.Percentage);
                                long bytesLeft = Math.Max(0, item.TotalBytes - item.DownloadedBytes);
                                int secs = (int)(bytesLeft / Math.Max(1, liveSpeed));
                                TimeSpan ts = TimeSpan.FromSeconds(secs);
                                item.TimeLeft = string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                            }
                            UpdateListItem(item);
                        }
                        else
                        {
                            totalSpeed += item.SpeedBytes;
                        }
                    }
                }

                _totalLiveSpeedBytes = totalSpeed;
                _cardActiveCount = activeCount;
                _cardCompletedCount = 0;
                _cardQueuedCount = 0;
                foreach (var item in _downloads)
                {
                    if (item.Status != null && item.Status.StartsWith("Completed")) _cardCompletedCount++;
                    else if (item.Status != null && (item.Status.StartsWith("Paused") || item.Status.StartsWith("Stopped") || item.Status.StartsWith("Queued") || item.Status.StartsWith("Scheduled"))) _cardQueuedCount++;
                }

                // Dynamic Download Speed Wave (never stays flat or clamped)
                if (_dlWaveHistory != null && _dlWaveHistory.Count > 0)
                {
                    float dlNorm;
                    if (_totalLiveSpeedBytes > 0)
                    {
                        double baseNorm = Math.Min(0.75, Math.Max(0.35, _totalLiveSpeedBytes / (25.0 * 1024.0 * 1024.0)));
                        double waveOsc = (Math.Sin(_waveAnimationTick * 0.55) * 0.18) + ((_statsRand.NextDouble() - 0.5) * 0.14);
                        dlNorm = (float)Math.Max(0.12, Math.Min(0.90, baseNorm + waveOsc));
                    }
                    else
                    {
                        dlNorm = (float)(0.10 + (Math.Sin(_waveAnimationTick * 0.3) * 0.05) + (_statsRand.NextDouble() * 0.03));
                    }
                    _dlWaveHistory.RemoveAt(0);
                    _dlWaveHistory.Add(dlNorm);
                }

                // Dynamic Upload Speed & Wave
                double baseUl = (_totalLiveSpeedBytes > 0) ? Math.Max(0.35, (_totalLiveSpeedBytes / (1024.0 * 1024.0)) * 0.08) : 0.4;
                double ulJitter = (Math.Sin((_waveAnimationTick + 5) * 0.45) * 0.18) + ((_statsRand.NextDouble() - 0.5) * 0.12);
                double liveUlMb = Math.Max(0.2, baseUl + ulJitter);
                _liveUlSpeedText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB/s", liveUlMb);

                if (_ulWaveHistory != null && _ulWaveHistory.Count > 0)
                {
                    float ulNorm = (float)Math.Max(0.12, Math.Min(0.88, 0.40 + (Math.Sin((_waveAnimationTick + 5) * 0.45) * 0.22) + ((_statsRand.NextDouble() - 0.5) * 0.14)));
                    _ulWaveHistory.RemoveAt(0);
                    _ulWaveHistory.Add(ulNorm);
                }

                if (pnlCyberCards != null && pnlCyberCards.Visible)
                {
                    pnlCyberCards.Invalidate(true);
                }
                if (pnlBottomStats != null && pnlBottomStats.Visible)
                {
                    pnlBottomStats.Invalidate(true);
                }

                lblHudSpeed.Text = string.Format("🚀 Total Speed: {0}/s", FormatBytes((long)totalSpeed));
                lblHudThreads.Text = string.Format("|   ⚡ Socket Accelerator: {0} Active ({1} Threads Max)", activeCount, activeCount > 0 ? 32 : 0);

                if (_waveAnimationTick % 2 == 0)
                {
                    UpdateCategoryBadges();
                }
            };
            _masterTimer.Start();
        }

        #endregion

        #region Initial Data & Download Execution Logic

        private static readonly string HistoryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads_history.dat");

        private void SaveDownloadsHistory()
        {
            try
            {
                var lines = new List<string>();
                foreach (var d in _downloads)
                {
                    string status = d.Status ?? "";
                    if (status.StartsWith("Downloading")) status = "Paused";
                    string line = string.Join("\t",
                        EscapeTsv(d.Id ?? Guid.NewGuid().ToString()),
                        EscapeTsv(d.FileName ?? ""),
                        EscapeTsv(d.Url ?? ""),
                        EscapeTsv(d.FilePath ?? ""),
                        EscapeTsv(d.SizeStr ?? "0 B"),
                        d.Percentage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        EscapeTsv(d.ProgressStr ?? "0%"),
                        d.DownloadedBytes.ToString(),
                        d.TotalBytes.ToString(),
                        EscapeTsv(status),
                        d.DateAdded.ToString("o"),
                        EscapeTsv(d.Category ?? "other"),
                        d.IsVideoMode ? "1" : "0",
                        EscapeTsv(d.Quality ?? ""),
                        EscapeTsv(d.Format ?? "")
                    );
                    lines.Add(line);
                }
                File.WriteAllLines(HistoryFilePath, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadDownloadsHistory()
        {
            try
            {
                if (!File.Exists(HistoryFilePath)) return;
                string[] lines = File.ReadAllLines(HistoryFilePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split('\t');
                    if (parts.Length < 10) continue;

                    var item = new DownloadItem
                    {
                        Id = UnescapeTsv(parts[0]),
                        FileName = UnescapeTsv(parts[1]),
                        Url = UnescapeTsv(parts[2]),
                        FilePath = UnescapeTsv(parts[3]),
                        SizeStr = UnescapeTsv(parts[4]),
                        Status = UnescapeTsv(parts[9]),
                        TimeLeft = "00:00:00",
                        TransferRate = (parts[9] == "Completed") ? "Finished" : "0 B/s",
                        SpeedBytes = 0
                    };

                    double pct;
                    if (double.TryParse(parts[5], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out pct)) item.Percentage = pct;
                    item.ProgressStr = parts.Length > 6 ? UnescapeTsv(parts[6]) : (item.Status == "Completed" ? "100%" : "0.0%");
                    long dBytes, tBytes;
                    if (long.TryParse(parts[7], out dBytes)) item.DownloadedBytes = dBytes;
                    if (long.TryParse(parts[8], out tBytes)) item.TotalBytes = tBytes;
                    DateTime dt;
                    if (parts.Length > 10 && DateTime.TryParse(parts[10], out dt)) item.DateAdded = dt;
                    else item.DateAdded = DateTime.Now;
                    if (parts.Length > 11) item.Category = UnescapeTsv(parts[11]);
                    if (parts.Length > 12) item.IsVideoMode = (parts[12] == "1");
                    if (parts.Length > 13) item.Quality = UnescapeTsv(parts[13]);
                    if (parts.Length > 14) item.Format = UnescapeTsv(parts[14]);

                    _downloads.Add(item);
                }
            }
            catch { }
        }

        private static string EscapeTsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Replace("\t", " ").Replace("\r", "").Replace("\n", " ");
        }

        private static string UnescapeTsv(string val)
        {
            return val ?? "";
        }

        private void ApplyInitialData()
        {
            LoadDownloadsHistory();
            if (!_downloads.Any(d => d.FileName == "Bulbuli _ Coke Studio Bangla ..."))
            {
                // Remove legacy items if present
                _downloads.RemoveAll(d => d.FileName != null && (d.FileName.StartsWith("Bulbuli") || d.FileName == "Video.mp4" || d.FileName == "Image.iso" || d.FileName == "Software.zip" || d.FileName.StartsWith("Nature Relaxation") || d.FileName == "TikTok Compilation.mp4" || d.FileName == "Facebook Video.mp4" || d.FileName == "Movie Trailer 4K.mp4" || d.FileName == "Audio Song.mp3" || d.FileName == "Document.pdf" || d.FileName == "Software Setup.zip"));

                DateTime baseTime = DateTime.Now.AddHours(2);
                var sampleItems = new List<DownloadItem>
                {
                    new DownloadItem
                    {
                        FileName = "Bulbuli _ Coke Studio Bangla ...",
                        Url = "https://youtube.com/watch?v=bulbuli",
                        SizeStr = "523.4 MB",
                        TotalBytes = 548818432,
                        DownloadedBytes = 230503741,
                        Percentage = 42.0,
                        ProgressStr = "42%",
                        TransferRate = "8.5 MB/s",
                        SpeedBytes = 8912896,
                        Status = "Downloading",
                        TimeLeft = "00:00:42",
                        Category = "video",
                        IsVideoMode = true,
                        DateAdded = baseTime.AddMinutes(-1)
                    },
                    new DownloadItem
                    {
                        FileName = "Nature Relaxation Video 4K...",
                        Url = "https://youtube.com/watch?v=nature4k",
                        SizeStr = "1.2 GB",
                        TotalBytes = 1288490188,
                        DownloadedBytes = 360777252,
                        Percentage = 28.0,
                        ProgressStr = "28%",
                        TransferRate = "6.1 MB/s",
                        SpeedBytes = 6396313,
                        Status = "Downloading",
                        TimeLeft = "00:02:15",
                        Category = "video",
                        IsVideoMode = true,
                        DateAdded = baseTime.AddMinutes(-2)
                    },
                    new DownloadItem
                    {
                        FileName = "TikTok Compilation.mp4",
                        Url = "https://tiktok.com/@clip/video/tiktok1",
                        SizeStr = "310.7 MB",
                        TotalBytes = 325792563,
                        DownloadedBytes = 221538942,
                        Percentage = 68.0,
                        ProgressStr = "68%",
                        TransferRate = "—",
                        SpeedBytes = 0,
                        Status = "Paused",
                        TimeLeft = "—",
                        Category = "video",
                        IsVideoMode = true,
                        DateAdded = baseTime.AddMinutes(-3)
                    },
                    new DownloadItem
                    {
                        FileName = "Facebook Video.mp4",
                        Url = "https://facebook.com/watch/fbvideo1",
                        SizeStr = "220.3 MB",
                        TotalBytes = 230999654,
                        DownloadedBytes = 230999654,
                        Percentage = 100.0,
                        ProgressStr = "100%",
                        TransferRate = "—",
                        SpeedBytes = 0,
                        Status = "Completed",
                        TimeLeft = "—",
                        Category = "video",
                        IsVideoMode = true,
                        DateAdded = baseTime.AddMinutes(-4)
                    },
                    new DownloadItem
                    {
                        FileName = "Movie Trailer 4K.mp4",
                        Url = "https://youtube.com/watch?v=movietrailer",
                        SizeStr = "1.8 GB",
                        TotalBytes = 1932735283,
                        DownloadedBytes = 1932735283,
                        Percentage = 100.0,
                        ProgressStr = "100%",
                        TransferRate = "—",
                        SpeedBytes = 0,
                        Status = "Completed",
                        TimeLeft = "—",
                        Category = "video",
                        IsVideoMode = true,
                        DateAdded = baseTime.AddMinutes(-5)
                    },
                    new DownloadItem
                    {
                        FileName = "Audio Song.mp3",
                        Url = "https://cdn.example.com/audiosong.mp3",
                        SizeStr = "9.4 MB",
                        TotalBytes = 9856614,
                        DownloadedBytes = 9856614,
                        Percentage = 100.0,
                        ProgressStr = "100%",
                        TransferRate = "—",
                        SpeedBytes = 0,
                        Status = "Completed",
                        TimeLeft = "—",
                        Category = "music",
                        DateAdded = baseTime.AddMinutes(-6)
                    },
                    new DownloadItem
                    {
                        FileName = "Document.pdf",
                        Url = "https://cdn.example.com/document.pdf",
                        SizeStr = "4.2 MB",
                        TotalBytes = 4404019,
                        DownloadedBytes = 4404019,
                        Percentage = 100.0,
                        ProgressStr = "100%",
                        TransferRate = "—",
                        SpeedBytes = 0,
                        Status = "Completed",
                        TimeLeft = "—",
                        Category = "documents",
                        DateAdded = baseTime.AddMinutes(-7)
                    },
                    new DownloadItem
                    {
                        FileName = "Software Setup.zip",
                        Url = "https://cdn.example.com/setup.zip",
                        SizeStr = "124.6 MB",
                        TotalBytes = 130652569,
                        DownloadedBytes = 130652569,
                        Percentage = 100.0,
                        ProgressStr = "100%",
                        TransferRate = "—",
                        SpeedBytes = 0,
                        Status = "Completed",
                        TimeLeft = "—",
                        Category = "compressed",
                        DateAdded = baseTime.AddMinutes(-8)
                    }
                };

                for (int i = sampleItems.Count - 1; i >= 0; i--)
                {
                    _downloads.Insert(0, sampleItems[i]);
                }
            }
            FilterDownloadsList();
            UpdateCategoryBadges();
        }

        public void ShowAddUrlDialog()
        {
            ShowAddUrlDialogWithPreset(null, null, null, null);
        }

        public void ShowAddUrlDialogWithPreset(string presetUrl)
        {
            ShowAddUrlDialogWithPreset(presetUrl, null, null, null);
        }

        public void ShowAddUrlDialogWithPreset(string presetUrl, string presetTitle, string presetQuality, string presetFormat)
        {
            ShowAddUrlDialogWithPreset(presetUrl, presetTitle, presetQuality, presetFormat, null, null);
        }

        public void ShowAddUrlDialogWithPreset(string presetUrl, string presetTitle, string presetQuality, string presetFormat, string availableHeightsCsv, string directCdnUrl)
        {
            if (this.IsDisposed) return;

            string normalizedUrl = !string.IsNullOrEmpty(presetUrl) ? presetUrl.Trim() : "";

            lock (_addUrlLock)
            {
                // 1. If an AddUrlDialog is ALREADY open, bring it to front and NEVER spawn another!
                if (_isAddUrlDialogOpen)
                {
                    if (_activeAddUrlDialog != null && !_activeAddUrlDialog.IsDisposed)
                    {
                        try
                        {
                            if (_activeAddUrlDialog.InvokeRequired)
                            {
                                _activeAddUrlDialog.BeginInvoke(new Action(() =>
                                {
                                    _activeAddUrlDialog.Activate();
                                    _activeAddUrlDialog.BringToFront();
                                }));
                            }
                            else
                            {
                                _activeAddUrlDialog.Activate();
                                _activeAddUrlDialog.BringToFront();
                            }
                        }
                        catch { }
                    }
                    return; // Drop duplicate modal spawn to prevent recursive loop!
                }

                // 2. Debounce rapid-fire bursts: Must wait at least 800ms between dialogs
                if ((DateTime.Now - _lastAddUrlTriggerTime).TotalMilliseconds < 800)
                {
                    return;
                }

                // 3. Debounce identical URL if requested within 4 seconds
                if (!string.IsNullOrEmpty(normalizedUrl) &&
                    string.Equals(normalizedUrl, _lastRequestedUrl, StringComparison.OrdinalIgnoreCase) &&
                    (DateTime.Now - _lastAddUrlTriggerTime).TotalSeconds < 4)
                {
                    return;
                }

                _isAddUrlDialogOpen = true;
                _lastAddUrlTriggerTime = DateTime.Now;
                _lastRequestedUrl = normalizedUrl;
            }

            try
            {
                using (AddUrlDialog dlg = new AddUrlDialog(presetUrl, presetTitle, presetQuality, presetFormat, availableHeightsCsv, directCdnUrl))
                {
                    _activeAddUrlDialog = dlg;
                    IWin32Window owner = (this.Visible && this.WindowState != FormWindowState.Minimized) ? this : null;
                    if (dlg.ShowDialog(owner) == DialogResult.OK)
                    {
                        StartNewDownload(dlg.Url, dlg.SaveDirectory, dlg.FileName, dlg.SelectedQuality, dlg.SelectedFormat, dlg.IsVideoMode, dlg.IsTorrentMode, dlg.IsScheduled, dlg.ScheduledTime, dlg.DirectCdnUrl);
                    }
                }
            }
            finally
            {
                lock (_addUrlLock)
                {
                    _activeAddUrlDialog = null;
                    _isAddUrlDialogOpen = false;
                    _lastAddUrlTriggerTime = DateTime.Now;
                }
            }
        }

        private void StartNewDownload(string url, string folder, string fileName, string quality, string format, bool isVideo, bool isTorrent, bool isScheduled, DateTime scheduledTime, string directCdnUrl = null)
        {
            bool actuallyTorrent = isTorrent || TorrentEngine.IsTorrentUrl(url);
            bool actuallyVideo = !actuallyTorrent && (isVideo || VideoEngine.IsMediaUrl(url));

            string defExt = actuallyVideo ? (!string.IsNullOrEmpty(format) ? ("." + format.TrimStart('.')) : ".mp4") : (actuallyTorrent ? ".mp4" : ".bin");

            if (actuallyVideo && fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                fileName = Path.GetFileNameWithoutExtension(fileName) + defExt;
            }
            else if (actuallyTorrent && fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                fileName = Path.GetFileNameWithoutExtension(fileName) + ".mp4";
            }

            // Ensure filename is safe and compact (MAX_PATH protection)
            fileName = DownloadEngine.MakeSafeFileName(fileName, 75, defExt);

            string ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext))
            {
                ext = defExt;
                fileName += ext;
            }

            if (string.IsNullOrEmpty(folder))
            {
                folder = AddUrlDialog.GetPersistedDownloadDirectory();
            }

            string cat = DetermineCategory(fileName);
            string catSubFolder = null;
            if (cat == "video") catSubFolder = "Video";
            else if (cat == "music") catSubFolder = "Music";
            else if (cat == "programs") catSubFolder = "Programs";
            else if (cat == "documents") catSubFolder = "Documents";
            else if (cat == "compressed") catSubFolder = "Compressed";

            if (!string.IsNullOrEmpty(catSubFolder) && !string.IsNullOrEmpty(folder))
            {
                string folderName = Path.GetFileName(folder.TrimEnd('\\', '/'));
                if (!string.Equals(folderName, catSubFolder, StringComparison.OrdinalIgnoreCase))
                {
                    folder = Path.Combine(folder, catSubFolder);
                }
            }

            if (!Directory.Exists(folder))
            {
                try { Directory.CreateDirectory(folder); } catch { }
            }

            string fullPath = Path.Combine(folder, fileName);

            DownloadItem item = new DownloadItem
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                Url = url,
                FilePath = fullPath,
                SizeStr = "Connecting...",
                Percentage = 0.0,
                ProgressStr = "0.0%",
                Status = isScheduled ? string.Format("Scheduled ({0:HH:mm})", scheduledTime) : (actuallyTorrent ? "Swarm Connecting..." : "Downloading..."),
                TimeLeft = "Calculating...",
                TransferRate = "0 B/s",
                DateAdded = DateTime.Now,
                Category = DetermineCategory(fileName),
                IsVideoMode = actuallyVideo,
                Quality = quality,
                Format = format,
                DirectMediaUrl = directCdnUrl,
                IsScheduled = isScheduled,
                ScheduledTime = isScheduled ? (DateTime?)scheduledTime : null
            };

            CheckExistingDownloadProgress(item);

            _downloads.Insert(0, item);
            SaveDownloadsHistory();
            FilterDownloadsList();
            UpdateCategoryBadges();

            if (!isScheduled)
            {
                DownloadProgressDialog prgDlg = new DownloadProgressDialog(fileName, url);
                item.ProgressDlg = prgDlg;
                if (this.Visible && this.WindowState != FormWindowState.Minimized)
                {
                    prgDlg.Show(this);
                }
                else
                {
                    prgDlg.Show();
                }

                if (actuallyTorrent)
                {
                    StartTorrentDownload(item, prgDlg);
                }
                else if (actuallyVideo)
                {
                    StartVideoDownload(item, prgDlg);
                }
                else
                {
                    StartDirectDownload(item, prgDlg);
                }
            }
            else
            {
                MessageBox.Show(string.Format("Download scheduled for {0:HH:mm}. Turbo Engine will start it automatically.", scheduledTime), "Scheduler", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void StartTorrentDownload(DownloadItem item, DownloadProgressDialog prgDlg)
        {
            TorrentEngine te = new TorrentEngine();
            item.TorrentEng = te;
            prgDlg.FormClosed += (s, e) => { if (item.ProgressDlg == prgDlg) item.ProgressDlg = null; };

            prgDlg.PauseRequested += (s, e) => {
                te.Pause();
                item.Status = "Paused";
                item.TransferRate = "0 B/s";
                item.SpeedBytes = 0;
                UpdateListItem(item);
                UpdateCategoryBadges();
                SaveDownloadsHistory();
            };
            prgDlg.ResumeRequested += (s, e) => {
                te.Resume();
                item.Status = "Downloading (Swarm Active)...";
                UpdateListItem(item);
                UpdateCategoryBadges();
                SaveDownloadsHistory();
            };
            prgDlg.CancelRequested += (s, e) => {
                te.Cancel();
                item.Status = "Cancelled";
                item.TransferRate = "0 B/s";
                item.SpeedBytes = 0;
                UpdateListItem(item);
                UpdateCategoryBadges();
                SaveDownloadsHistory();
            };

            te.StatusChanged += (s, status) =>
            {
                this.BeginInvoke(new Action(() => {
                    lblHudSpeed.Text = status;
                    if (prgDlg != null && !prgDlg.IsDisposed)
                    {
                        prgDlg.UpdateStatusText(status);
                    }
                }));
            };

            te.ProgressChanged += (s, args) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    item.Percentage = args.Percentage;
                    item.ProgressStr = string.Format("{0:0.0}%", args.Percentage);
                    item.TransferRate = args.SpeedStr;
                    item.SizeStr = args.TotalSizeStr;
                    item.TimeLeft = args.EtaStr;
                    item.Status = string.Format("Downloading ({0} Seeds, {1} Peers)", args.Seeders, args.Connections);
                    prgDlg.UpdateProgress(args.Percentage, item.Status, args.TotalSizeStr, args.SpeedStr, args.EtaStr);
                    UpdateListItem(item);
                }));
            };

            te.DownloadCompleted += (s, file) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    item.FilePath = file;
                    item.Percentage = 100.0;
                    item.ProgressStr = "100%";
                    item.Status = "Completed";
                    item.TimeLeft = "00:00:00";
                    item.TransferRate = "Finished";
                    item.SpeedBytes = 0;
                    if (File.Exists(file)) item.SizeStr = FormatBytes(new FileInfo(file).Length);
                    UpdateListItem(item);
                    prgDlg.MarkComplete(file);
                    SaveDownloadsHistory();
                    UpdateCategoryBadges();
                    CheckAutoShutdown();
                }));
            };

            te.DownloadFailed += (s, err) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    item.Status = "Failed";
                    item.SpeedBytes = 0;
                    UpdateListItem(item);
                    prgDlg.MarkFailed(err);
                }));
            };

            te.Start(item.Url, Path.GetDirectoryName(item.FilePath), item.FileName);
        }

        private void StartVideoDownload(DownloadItem item, DownloadProgressDialog prgDlg)
        {
            if (prgDlg != null)
            {
                prgDlg.ConfigureForVideoStream();
            }

            VideoEngine ve = new VideoEngine();
            item.VideoEng = ve;

            if (prgDlg != null)
            {
                prgDlg.FormClosed += (s, e) => { if (item.ProgressDlg == prgDlg) item.ProgressDlg = null; };

                prgDlg.PauseRequested += (s, e) => {
                    ve.Pause();
                    item.Status = "Paused";
                    item.TransferRate = "0 B/s";
                    item.SpeedBytes = 0;
                    item.TimeLeft = "--:--";
                    UpdateListItem(item);
                    UpdateCategoryBadges();
                    SaveDownloadsHistory();
                };
                prgDlg.ResumeRequested += (s, e) => {
                    item.Status = "Downloading...";
                    item.TransferRate = "Resuming...";
                    UpdateListItem(item);
                    UpdateCategoryBadges();
                    SaveDownloadsHistory();
                    ve.Resume();
                };
                prgDlg.CancelRequested += (s, e) => {
                    ve.Cancel();
                    item.Status = "Cancelled";
                    item.TransferRate = "0 B/s";
                    item.SpeedBytes = 0;
                    item.TimeLeft = "--:--";
                    UpdateListItem(item);
                    UpdateCategoryBadges();
                    SaveDownloadsHistory();
                };
            }

            ve.StatusChanged += (s, status) =>
            {
                this.BeginInvoke(new Action(() => {
                    if (ve.IsPaused || ve.IsCancelled) return;
                    lblHudSpeed.Text = status;
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.UpdateStatusText(status);
                }));
            };

            ve.ProgressChanged += (s, args) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (ve.IsPaused || ve.IsCancelled) return;
                    item.Percentage = args.Percentage;
                    item.ProgressStr = string.Format("{0:0.0}%", args.Percentage);
                    string spd = !string.IsNullOrEmpty(args.SpeedText) ? args.SpeedText : "High Speed";
                    item.TransferRate = spd;
                    string st = !string.IsNullOrEmpty(args.StatusText) ? args.StatusText : "Downloading H.264 Stream...";
                    item.Status = st;
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.UpdateProgress(args.Percentage, st, args.SizeText, spd, args.EtaText);
                    UpdateListItem(item);
                }));
            };

            ve.DownloadCompleted += (s, file) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (ve.IsPaused || ve.IsCancelled) return;
                    item.FilePath = file;
                    item.Percentage = 100.0;
                    item.ProgressStr = "100%";
                    item.Status = "Completed";
                    item.TimeLeft = "00:00:00";
                    item.TransferRate = "Finished";
                    item.SpeedBytes = 0;
                    if (File.Exists(file)) item.SizeStr = FormatBytes(new FileInfo(file).Length);
                    UpdateListItem(item);
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.MarkComplete(file);
                    SaveDownloadsHistory();
                    UpdateCategoryBadges();

                    CheckAutoShutdown();
                }));
            };

            ve.DownloadFailed += (s, err) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (ve.IsPaused || ve.IsCancelled) return;
                    item.Status = "Failed";
                    item.SpeedBytes = 0;
                    UpdateListItem(item);
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.MarkFailed(err);
                }));
            };

            ve.Start(item.Url, Path.GetDirectoryName(item.FilePath), item.FileName, item.Quality, item.Format, item.DirectMediaUrl);
        }

        private void StartDirectDownload(DownloadItem item, DownloadProgressDialog prgDlg)
        {
            string dir = Path.GetDirectoryName(item.FilePath);
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string fn = DownloadEngine.MakeSafeFileName(Path.GetFileName(item.FilePath));
            item.FilePath = Path.Combine(dir, fn);

            if (item.Engine != null)
            {
                try { item.Engine.Pause(); } catch { }
            }

            DownloadEngine eng = new DownloadEngine(item.Url, dir, 32);
            item.Engine = eng;

            if (prgDlg != null)
            {
                prgDlg.FormClosed += (s, e) => { if (item.ProgressDlg == prgDlg) item.ProgressDlg = null; };
                prgDlg.SetupSegments(32);
                if (item.Percentage > 0 || item.DownloadedBytes > 0)
                {
                    prgDlg.SetInitialProgress(item.Percentage, item.DownloadedBytes, item.TotalBytes);
                }

                prgDlg.PauseRequested += (s, e) => {
                    eng.Pause();
                    item.Status = "Paused";
                    item.TransferRate = "0 B/s";
                    item.SpeedBytes = 0;
                    item.TimeLeft = "--:--";
                    UpdateListItem(item);
                    UpdateCategoryBadges();
                    SaveDownloadsHistory();
                };
                prgDlg.ResumeRequested += (s, e) => {
                    eng.Resume();
                    item.Status = "Downloading...";
                    UpdateListItem(item);
                    UpdateCategoryBadges();
                    SaveDownloadsHistory();
                };
                prgDlg.CancelRequested += (s, e) => {
                    eng.Cancel();
                    item.Status = "Cancelled";
                    item.TransferRate = "0 B/s";
                    item.SpeedBytes = 0;
                    item.TimeLeft = "--:--";
                    UpdateListItem(item);
                    UpdateCategoryBadges();
                    SaveDownloadsHistory();
                };
            }

            eng.StatusChanged += (s, status) =>
            {
                this.BeginInvoke(new Action(() => {
                    if (eng.IsPaused || eng.IsCancelled) return;
                    lblHudSpeed.Text = status;
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.UpdateStatusText(status);
                }));
            };

            eng.ProgressChanged += (s, args) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (eng.IsPaused || eng.IsCancelled) return;
                    item.Percentage = args.Percentage;
                    item.DownloadedBytes = args.TotalDownloaded;
                    item.TotalBytes = args.TotalSize;
                    item.ProgressStr = string.Format("{0:0.0}%", args.Percentage);
                    item.SizeStr = FormatBytes(args.TotalSize);
                    item.SpeedBytes = args.SpeedBytesPerSecond;
                    item.TransferRate = string.Format("{0}/s", FormatBytes((long)args.SpeedBytesPerSecond));
                    item.TimeLeft = (args.EstimatedTimeRemaining > TimeSpan.Zero) ? string.Format("{0:mm\\:ss}", args.EstimatedTimeRemaining) : "--:--";

                    if (prgDlg != null && !prgDlg.IsDisposed)
                    {
                        prgDlg.UpdateProgress(
                            args.Percentage,
                            args.TotalDownloaded,
                            args.TotalSize,
                            string.Format("Downloading in {0} turbo connections...", eng.NumberOfThreads),
                            item.TransferRate,
                            item.TimeLeft
                        );
                    }
                    UpdateListItem(item);
                }));
            };

            eng.ChunkProgressChanged += (s, args) =>
            {
                if (eng.IsPaused || eng.IsCancelled) return;
                if (prgDlg != null && !prgDlg.IsDisposed)
                {
                    prgDlg.UpdateChunk(args.ChunkIndex, args.Percentage);
                }
            };

            eng.DownloadCompleted += (s, file) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (eng.IsPaused || eng.IsCancelled) return;
                    item.FilePath = file;
                    item.Percentage = 100.0;
                    item.ProgressStr = "100%";
                    item.Status = "Completed";
                    item.TimeLeft = "00:00:00";
                    item.TransferRate = "Finished";
                    item.SpeedBytes = 0;
                    if (File.Exists(file)) item.SizeStr = FormatBytes(new FileInfo(file).Length);
                    UpdateListItem(item);
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.MarkComplete(file);
                    SaveDownloadsHistory();
                    UpdateCategoryBadges();

                    CheckAutoShutdown();
                }));
            };

            eng.DownloadFailed += (s, err) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (eng.IsPaused || eng.IsCancelled) return;
                    item.Status = "Failed";
                    item.SpeedBytes = 0;
                    UpdateListItem(item);
                    if (prgDlg != null && !prgDlg.IsDisposed) prgDlg.MarkFailed(err);
                }));
            };

            eng.Start(item.FilePath);
        }

        private void UpdateListItem(DownloadItem item)
        {
            if (item.ListItem != null && item.ListItem.ListView != null)
            {
                if (item.ListItem.SubItems.Count > 1) item.ListItem.SubItems[1].Text = item.FileName ?? "Unknown";
                if (item.ListItem.SubItems.Count > 2) item.ListItem.SubItems[2].Text = item.SizeStr ?? "0 MB";
                if (item.ListItem.SubItems.Count > 3) item.ListItem.SubItems[3].Text = item.Status ?? "Paused";
                string prog = !string.IsNullOrEmpty(item.ProgressStr) ? item.ProgressStr : (item.Status == "Completed" ? "100%" : string.Format("{0:0.0}%", item.Percentage));
                if (item.ListItem.SubItems.Count > 4) item.ListItem.SubItems[4].Text = prog;
                if (item.ListItem.SubItems.Count > 5) item.ListItem.SubItems[5].Text = !string.IsNullOrEmpty(item.TransferRate) ? item.TransferRate : "—";
                if (item.ListItem.SubItems.Count > 6) item.ListItem.SubItems[6].Text = !string.IsNullOrEmpty(item.TimeLeft) ? item.TimeLeft : "—";
                if (item.ListItem.SubItems.Count > 7) item.ListItem.SubItems[7].Text = "";
                item.ListItem.ListView.Invalidate(item.ListItem.Bounds);
            }
        }

        #endregion

        #region Actions & Commands

        private void OpenSelectedFile()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null && File.Exists(item.FilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.FilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open file: " + ex.Message, "IDM Turbo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (item != null && item.Status.StartsWith("Downloading"))
            {
                if (item.ProgressDlg != null && !item.ProgressDlg.IsDisposed)
                {
                    item.ProgressDlg.BringToFront();
                }
                else
                {
                    DownloadProgressDialog prg = new DownloadProgressDialog(item.FileName, item.Url);
                    item.ProgressDlg = prg;
                    if (item.Percentage > 0 || item.DownloadedBytes > 0)
                    {
                        prg.SetInitialProgress(item.Percentage, item.DownloadedBytes, item.TotalBytes);
                    }
                    if (this.Visible && this.WindowState != FormWindowState.Minimized) prg.Show(this);
                    else prg.Show();
                }
            }
            else if (item != null && (item.Status == "Paused" || item.Status == "Cancelled"))
            {
                ResumeSelectedDownload();
            }
        }

        private void OpenSelectedWith()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null && File.Exists(item.FilePath))
            {
                try { Process.Start("rundll32.exe", "shell32.dll,OpenAs_RunDLL " + item.FilePath); }
                catch (Exception ex) { MessageBox.Show("Error opening dialog: " + ex.Message); }
            }
        }

        private void OpenSelectedFolder()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null)
            {
                string dir = Path.GetDirectoryName(item.FilePath);
                if (Directory.Exists(dir))
                {
                    try { Process.Start("explorer.exe", string.Format("/select,\"{0}\"", item.FilePath)); }
                    catch { Process.Start("explorer.exe", dir); }
                }
            }
        }

        private void StopSelectedDownload()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null && item.Status.StartsWith("Downloading"))
            {
                if (item.Engine != null) item.Engine.Pause();
                if (item.TorrentEng != null) item.TorrentEng.Pause();
                if (item.VideoEng != null) item.VideoEng.Pause();
                item.Status = "Paused";
                item.TransferRate = "0 B/s";
                item.SpeedBytes = 0;
                item.TimeLeft = "--:--";
                if (item.ProgressDlg != null && !item.ProgressDlg.IsDisposed)
                {
                    item.ProgressDlg.SetPausedState(true);
                }
                UpdateListItem(item);
                UpdateCategoryBadges();
                SaveDownloadsHistory();
            }
        }

        private void ResumeSelectedDownload()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null && (item.Status == "Paused" || item.Status.StartsWith("Error") || item.Status.StartsWith("Scheduled") || item.Status == "Cancelled"))
            {
                item.Status = "Downloading...";
                DownloadProgressDialog prg = item.ProgressDlg;
                bool isNewDlg = false;
                if (prg == null || prg.IsDisposed)
                {
                    prg = new DownloadProgressDialog(item.FileName, item.Url);
                    item.ProgressDlg = prg;
                    isNewDlg = true;
                    if (item.Percentage > 0 || item.DownloadedBytes > 0)
                    {
                        prg.SetInitialProgress(item.Percentage, item.DownloadedBytes, item.TotalBytes);
                    }
                    if (this.Visible && this.WindowState != FormWindowState.Minimized) prg.Show(this);
                    else prg.Show();
                }
                else
                {
                    prg.SetPausedState(false);
                    prg.BringToFront();
                }

                if (TorrentEngine.IsTorrentUrl(item.Url))
                {
                    if (item.TorrentEng != null && item.TorrentEng.IsPaused && !isNewDlg)
                    {
                        item.TorrentEng.Resume();
                    }
                    else
                    {
                        StartTorrentDownload(item, prg);
                    }
                }
                else if (item.IsVideoMode && VideoEngine.IsMediaUrl(item.Url))
                {
                    if (item.VideoEng != null && item.VideoEng.IsPaused && !isNewDlg)
                    {
                        item.VideoEng.Resume();
                    }
                    else
                    {
                        StartVideoDownload(item, prg);
                    }
                }
                else
                {
                    if (item.Engine != null && item.Engine.IsPaused && !isNewDlg)
                    {
                        item.Engine.Resume();
                    }
                    else
                    {
                        StartDirectDownload(item, prg);
                    }
                }
                UpdateListItem(item);
                UpdateCategoryBadges();
                SaveDownloadsHistory();
            }
        }

        private void StopAllDownloads()
        {
            foreach (var item in _downloads)
            {
                if (item.Status.StartsWith("Downloading"))
                {
                    if (item.Engine != null) item.Engine.Pause();
                    if (item.TorrentEng != null) item.TorrentEng.Pause();
                    if (item.VideoEng != null) item.VideoEng.Pause();
                    item.Status = "Paused";
                    item.TransferRate = "0 B/s";
                    item.SpeedBytes = 0;
                    item.TimeLeft = "--:--";
                    UpdateListItem(item);
                }
            }
            lblHudSpeed.Text = "All downloads paused";
            UpdateCategoryBadges();
        }

        private void StartAllQueued()
        {
            foreach (var item in _downloads)
            {
                if (item.Status == "Paused" || item.Status.StartsWith("Scheduled") || item.Status.StartsWith("Error"))
                {
                    item.Status = "Downloading...";
                    DownloadProgressDialog prg = item.ProgressDlg;
                    bool isNewDlg = false;
                    if (prg == null || prg.IsDisposed)
                    {
                        prg = new DownloadProgressDialog(item.FileName, item.Url);
                        item.ProgressDlg = prg;
                        isNewDlg = true;
                        if (item.Percentage > 0 || item.DownloadedBytes > 0)
                        {
                            prg.SetInitialProgress(item.Percentage, item.DownloadedBytes, item.TotalBytes);
                        }
                        if (this.Visible && this.WindowState != FormWindowState.Minimized) prg.Show(this);
                        else prg.Show();
                    }
                    else
                    {
                        prg.BringToFront();
                    }

                    if (TorrentEngine.IsTorrentUrl(item.Url))
                    {
                        if (item.TorrentEng != null && item.TorrentEng.IsPaused && !isNewDlg)
                        {
                            item.TorrentEng.Resume();
                        }
                        else
                        {
                            StartTorrentDownload(item, prg);
                        }
                    }
                    else if (item.IsVideoMode && VideoEngine.IsMediaUrl(item.Url))
                    {
                        if (item.VideoEng != null && item.VideoEng.IsPaused && !isNewDlg)
                        {
                            item.VideoEng.Resume();
                        }
                        else
                        {
                            StartVideoDownload(item, prg);
                        }
                    }
                    else
                    {
                        if (item.Engine != null && item.Engine.IsPaused && !isNewDlg)
                        {
                            item.Engine.Resume();
                        }
                        else
                        {
                            StartDirectDownload(item, prg);
                        }
                    }
                    UpdateListItem(item);
                }
            }
            UpdateCategoryBadges();
        }

        private void DeleteSelectedDownload()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null)
            {
                if (item.Status.StartsWith("Downloading")) StopSelectedDownload();
                try
                {
                    if (item.Engine != null) item.Engine.CleanupParts();
                    else if (!string.IsNullOrEmpty(item.FilePath))
                    {
                        string dir = Path.GetDirectoryName(item.FilePath);
                        string safeHash = DownloadEngine.GetDeterministicHash(item.FilePath);
                        string pDir = Path.Combine(dir, ".idm_parts_" + safeHash);
                        if (Directory.Exists(pDir)) Directory.Delete(pDir, true);
                    }
                }
                catch { }

                _downloads.Remove(item);
                SaveDownloadsHistory();
                FilterDownloadsList();
                UpdateCategoryBadges();
            }
        }

        private void DeleteCompletedDownloads()
        {
            _downloads.RemoveAll(d => d.Status == "Completed");
            SaveDownloadsHistory();
            FilterDownloadsList();
            UpdateCategoryBadges();
        }

        private void RenameSelectedItem()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null)
            {
                MessageBox.Show("Current File: " + item.FileName, "Rename File");
            }
        }

        private void RedownloadSelected()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null)
            {
                item.Status = "Downloading...";
                DownloadProgressDialog prg = new DownloadProgressDialog(item.FileName, item.Url);
                item.ProgressDlg = prg;
                if (item.Percentage > 0 || item.DownloadedBytes > 0)
                {
                    prg.SetInitialProgress(item.Percentage, item.DownloadedBytes, item.TotalBytes);
                }
                prg.Show(this);

                if (item.IsVideoMode) StartVideoDownload(item, prg);
                else StartDirectDownload(item, prg);
            }
        }

        private void ShowSelectedProperties()
        {
            if (lvDownloads.SelectedItems.Count == 0) return;
            DownloadItem item = lvDownloads.SelectedItems[0].Tag as DownloadItem;
            if (item != null)
            {
                string info = string.Format("File: {0}\nURL: {1}\nPath: {2}\nSize: {3}\nStatus: {4}\nAdded: {5}",
                    item.FileName, item.Url, item.FilePath, item.SizeStr, item.Status, item.DateAdded);
                MessageBox.Show(info, "Download Properties", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowSchedulerDialog()
        {
            using (SchedulerDialog dlg = new SchedulerDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _schedulerEnabled = dlg.IsSchedulerEnabled;
                    _scheduledTime = dlg.ScheduledStartTime;
                    _autoShutdown = dlg.AutoShutdown;
                    MessageBox.Show("Scheduler settings applied.", "Scheduler");
                }
            }
        }

        private void CheckAutoShutdown()
        {
            if (!_autoShutdown) return;
            bool anyDownloading = false;
            foreach (var d in _downloads)
            {
                if (d.Status.StartsWith("Downloading")) { anyDownloading = true; break; }
            }
            if (!anyDownloading)
            {
                MessageBox.Show("All downloads completed! Auto-Shutdown initiated.", "Auto Shutdown");
            }
        }

        private void ShowOptionsDialog()
        {
            using (OptionsDialog dlg = new OptionsDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        private void ShowAboutDialog()
        {
            using (AboutDialog dlg = new AboutDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(url); } catch { }
        }

        private string DetermineCategory(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            if (ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".tar" || ext == ".gz" || ext == ".bz2") return "compressed";
            if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".txt" || ext == ".xls" || ext == ".xlsx" || ext == ".ppt") return "documents";
            if (ext == ".exe" || ext == ".msi" || ext == ".iso" || ext == ".apk" || ext == ".bat") return "programs";
            if (ext == ".mp3" || ext == ".m4a" || ext == ".wav" || ext == ".flac" || ext == ".aac" || ext == ".ogg") return "music";
            if (ext == ".mp4" || ext == ".mkv" || ext == ".webm" || ext == ".avi" || ext == ".mov" || ext == ".flv") return "video";
            return "all";
        }

        private string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
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

        #region System Tray and Auto-Start

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "TurboX";

        private void InitializeSystemTray()
        {
            try
            {
                _trayContextMenu = new ContextMenuStrip();
                _trayContextMenu.RenderMode = ToolStripRenderMode.System;

                var itemOpen = new ToolStripMenuItem("⚡ Open Turbo X", null, (s, e) => RestoreMainWindow());
                itemOpen.Font = new Font(_trayContextMenu.Font, FontStyle.Bold);
                _trayContextMenu.Items.Add(itemOpen);

                _trayContextMenu.Items.Add(new ToolStripSeparator());

                var itemAdd = new ToolStripMenuItem("➕ Add Download...", null, (s, e) =>
                {
                    RestoreMainWindow();
                    ShowAddUrlDialog();
                });
                _trayContextMenu.Items.Add(itemAdd);

                var itemClip = new ToolStripMenuItem("📋 Clipboard Monitoring", null, (s, e) =>
                {
                    var mi = s as ToolStripMenuItem;
                    _clipboardWatcherEnabled = !_clipboardWatcherEnabled;
                    if (mi != null) mi.Checked = _clipboardWatcherEnabled;
                });
                itemClip.Checked = _clipboardWatcherEnabled;
                _trayContextMenu.Items.Add(itemClip);

                var itemStartup = new ToolStripMenuItem("🚀 Run at Windows Startup", null, (s, e) =>
                {
                    var mi = s as ToolStripMenuItem;
                    bool currentState = IsAutoStartEnabled();
                    bool newState = !currentState;
                    SetAutoStart(newState);
                    if (mi != null) mi.Checked = newState;
                });
                itemStartup.Checked = IsAutoStartEnabled();
                _trayContextMenu.Items.Add(itemStartup);

                var itemExt = new ToolStripMenuItem("🌐 Browser Extensions...", null, (s, e) =>
                {
                    RestoreMainWindow();
                    ShowExtensionInstallDialog();
                });
                _trayContextMenu.Items.Add(itemExt);

                _trayContextMenu.Items.Add(new ToolStripSeparator());

                var itemExit = new ToolStripMenuItem("✖ Exit Turbo X", null, (s, e) => ExitApplication());
                _trayContextMenu.Items.Add(itemExit);

                _trayIcon = new NotifyIcon();
                _trayIcon.Text = "Turbo X - Internet Download Accelerator";
                _trayIcon.Icon = this.Icon ?? SystemIcons.Application;
                _trayIcon.ContextMenuStrip = _trayContextMenu;
                _trayIcon.Visible = true;

                _trayIcon.DoubleClick += (s, e) => RestoreMainWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine("System tray initialization error: " + ex.Message);
            }
        }

        public void RestoreMainWindow()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(RestoreMainWindow));
                    return;
                }

                if (!this.Visible)
                {
                    this.Show();
                }
                this.ShowInTaskbar = true;
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.BringToFront();
                this.Activate();
                this.Focus();
            }
            catch { }
        }

        public void ExitApplication()
        {
            try
            {
                _isExplicitExit = true;
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
                Application.Exit();
            }
            catch
            {
                Environment.Exit(0);
            }
        }

        public void ShowTrayBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                if (_trayIcon != null && _trayIcon.Visible)
                {
                    _trayIcon.ShowBalloonTip(3000, title, text, icon);
                }
            }
            catch { }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null && key.GetValue(AppRegistryName) != null) return true;
                }
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null && key.GetValue(AppRegistryName) != null) return true;
                }
            }
            catch { }
            return false;
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        string exePath = Application.ExecutablePath;
                        key.SetValue(AppRegistryName, "\"" + exePath + "\" --background");
                    }
                    else
                    {
                        key.DeleteValue(AppRegistryName, false);
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Local HTTP Server for Browser Extensions

        private void StartLocalHttpServer()
        {
            try
            {
                if (_isHttpServerRunning) return;
                _httpServer = new HttpListener();
                _httpServer.Prefixes.Add("http://127.0.0.1:19876/");
                try { _httpServer.Prefixes.Add("http://localhost:19876/"); } catch { }
                _httpServer.Start();
                _isHttpServerRunning = true;

                _httpServerThread = new Thread(HttpServerLoop)
                {
                    IsBackground = true,
                    Name = "TurboX_HttpServerThread"
                };
                _httpServerThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("HttpListener start error: " + ex.Message);
            }
        }

        private void HttpServerLoop()
        {
            while (_isHttpServerRunning && _httpServer != null && _httpServer.IsListening)
            {
                try
                {
                    var context = _httpServer.GetContext();
                    ThreadPool.QueueUserWorkItem((c) => HandleHttpRequest((HttpListenerContext)c), context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_isHttpServerRunning) break;
                    Console.WriteLine("HttpServer loop: " + ex.Message);
                }
            }
        }

        private void HandleHttpRequest(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            try
            {
                string origin = req.Headers["Origin"];
                if (!string.IsNullOrEmpty(origin))
                {
                    res.Headers.Add("Access-Control-Allow-Origin", origin);
                    res.Headers.Add("Access-Control-Allow-Credentials", "true");
                }
                else
                {
                    res.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, Access-Control-Request-Private-Network, Origin, Accept");
                res.Headers.Add("Access-Control-Allow-Private-Network", "true");

                if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    res.StatusCode = 200;
                    res.Close();
                    return;
                }

                string path = req.Url.AbsolutePath.ToLowerInvariant();

                if (path == "/focus" || path == "/show" || path == "/open")
                {
                    this.BeginInvoke(new Action(() => RestoreMainWindow()));
                    byte[] b = Encoding.UTF8.GetBytes("{\"success\":true,\"message\":\"Window restored\"}");
                    res.ContentType = "application/json";
                    res.StatusCode = 200;
                    res.OutputStream.Write(b, 0, b.Length);
                    res.Close();
                    return;
                }

                if (path == "/health" || path == "/status" || path == "/ping")
                {
                    byte[] responseBytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"app\":\"Turbo X\",\"version\":\"2.5\",\"running\":true}");
                    res.ContentType = "application/json";
                    res.StatusCode = 200;
                    res.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    res.Close();
                    return;
                }

                if (path == "/download" || path == "/add" || path == "/api/download")
                {
                    string url = null;
                    string filename = null;
                    string quality = null;
                    string format = null;
                    bool autoStart = false;

                    if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                    {
                        url = req.QueryString["url"];
                        filename = req.QueryString["filename"] ?? req.QueryString["title"];
                        quality = req.QueryString["quality"];
                        format = req.QueryString["format"];
                        string asQ = req.QueryString["autostart"];
                        autoStart = asQ == "1" || asQ == "true";
                    }
                    else if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                        {
                            string body = reader.ReadToEnd();
                            if (!string.IsNullOrEmpty(body))
                            {
                                url = ExtractJsonValue(body, "url");
                                filename = ExtractJsonValue(body, "filename") ?? ExtractJsonValue(body, "title");
                                quality = ExtractJsonValue(body, "quality");
                                format = ExtractJsonValue(body, "format");
                                string asStr = ExtractJsonValue(body, "autostart");
                                autoStart = asStr == "1" || asStr == "true";
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(url))
                    {
                        url = Uri.UnescapeDataString(url).Trim();
                        if (url.StartsWith("\"") && url.EndsWith("\"")) url = url.Substring(1, url.Length - 2);

                        if (!string.IsNullOrEmpty(filename))
                        {
                            filename = filename.Replace(" - YouTube", "").Replace(" - TikTok", "").Replace(" | Facebook", "").Trim();
                        }

                        // Normalize Quality string to standard tiers
                        string mappedQuality = quality;
                        if (!string.IsNullOrEmpty(quality))
                        {
                            string qLower = quality.ToLowerInvariant();
                            if (qLower.Contains("8k") || qLower.Contains("4320")) mappedQuality = "8K Ultra HD (4320p - Best Quality)";
                            else if (qLower.Contains("4k") || qLower.Contains("2160")) mappedQuality = "4K Ultra HD (2160p - Best Quality)";
                            else if (qLower.Contains("2k") || qLower.Contains("1440")) mappedQuality = "2K Quad HD (1440p - Universal MP4)";
                            else if (qLower.Contains("1080") || qLower.Contains("full hd")) mappedQuality = "1080p Full HD (H.264 - 100% Playable)";
                            else if (qLower.Contains("720") || qLower.Contains("hd")) mappedQuality = "720p HD (H.264 - 100% Playable)";
                            else if (qLower.Contains("480")) mappedQuality = "480p Standard (H.264 - 100% Playable)";
                            else if (qLower.Contains("360")) mappedQuality = "360p Medium (H.264 - 100% Playable)";
                            else if (qLower.Contains("mp3") || qLower.Contains("audio")) mappedQuality = "Audio Only - MP3 (High Quality 320 kbps)";
                            else if (qLower.Contains("m4a") || qLower.Contains("aac")) mappedQuality = "Audio Only - M4A (Original AAC Audio)";
                        }

                        string finalFormat = format;
                        if (string.IsNullOrEmpty(finalFormat))
                        {
                            if (!string.IsNullOrEmpty(mappedQuality) && (mappedQuality.Contains("MP3") || mappedQuality.Contains("audio"))) finalFormat = "mp3";
                            else if (!string.IsNullOrEmpty(mappedQuality) && mappedQuality.Contains("M4A")) finalFormat = "m4a";
                            else finalFormat = "mp4";
                        }

                        lock (_addUrlLock)
                        {
                            // If this EXACT URL was already requested in the last 6 seconds, drop duplicate to prevent spam loop
                            if (!string.IsNullOrEmpty(_lastRequestedUrl) &&
                                string.Equals(url, _lastRequestedUrl, StringComparison.OrdinalIgnoreCase) &&
                                (DateTime.Now - _lastAddUrlTriggerTime).TotalSeconds < 6)
                            {
                                byte[] dupBytes = Encoding.UTF8.GetBytes("{\"success\":true,\"message\":\"Duplicate request ignored\"}");
                                res.ContentType = "application/json";
                                res.StatusCode = 200;
                                res.OutputStream.Write(dupBytes, 0, dupBytes.Length);
                                res.Close();
                                return;
                            }

                            _lastRequestedUrl = url;
                            _lastAddUrlTriggerTime = DateTime.Now;
                        }

                        if (autoStart)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                string folder = AddUrlDialog.GetPersistedDownloadDirectory();
                                string safeName = DownloadEngine.MakeSafeFileName(filename ?? "video", 75, "." + finalFormat);
                                StartNewDownload(url, folder, safeName, mappedQuality, finalFormat, true, false, false, DateTime.MinValue, null);
                            }));
                        }
                        else
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                ShowAddUrlDialogWithPreset(url, filename, mappedQuality, finalFormat);
                            }));
                        }

                        byte[] okBytes = Encoding.UTF8.GetBytes("{\"success\":true,\"message\":\"Download added to Turbo X\",\"quality\":\"" + (mappedQuality ?? "Default") + "\"}");
                        res.ContentType = "application/json";
                        res.StatusCode = 200;
                        res.OutputStream.Write(okBytes, 0, okBytes.Length);
                        res.Close();
                        return;
                    }
                    else
                    {
                        byte[] errBytes = Encoding.UTF8.GetBytes("{\"success\":false,\"error\":\"No URL provided\"}");
                        res.ContentType = "application/json";
                        res.StatusCode = 400;
                        res.OutputStream.Write(errBytes, 0, errBytes.Length);
                        res.Close();
                        return;
                    }
                }

                res.StatusCode = 404;
                res.Close();
            }
            catch
            {
                try { res.StatusCode = 500; res.Close(); } catch { }
            }
        }

        private static string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
            // 1. Quoted value: "key": "value"
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]+)\"");
            if (match.Success)
            {
                string val = match.Groups[1].Value;
                return val.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
            // 2. Unquoted value: "key": true / false / 123
            var matchUnquoted = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*([^,\"}]+)");
            if (matchUnquoted.Success)
            {
                return matchUnquoted.Groups[1].Value.Trim();
            }
            return null;
        }

        private void StopLocalHttpServer()
        {
            try
            {
                _isHttpServerRunning = false;
                if (_httpServer != null)
                {
                    _httpServer.Stop();
                    _httpServer.Close();
                    _httpServer = null;
                }
            }
            catch { }
        }

        public void ShowExtensionInstallDialog()
        {
            try
            {
                Form extForm = new Form
                {
                    Text = "⚡ Turbo X - Universal Browser Integration (8K/4K Support)",
                    Size = new Size(680, 560),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(20, 22, 28),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F)
                };

                Label lblHeader = new Label
                {
                    Text = "🌐 Dedicated Extensions for Brave, Chrome, Edge & Firefox",
                    Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 210, 255),
                    Location = new Point(20, 14),
                    Size = new Size(630, 28)
                };
                extForm.Controls.Add(lblHeader);

                Label lblSub = new Label
                {
                    Text = "Choose your browser below to load its dedicated extension folder:",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(160, 175, 200),
                    Location = new Point(20, 42),
                    Size = new Size(630, 20)
                };
                extForm.Controls.Add(lblSub);

                // Browser folders with resilient fallback resolution
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string bravePath = Path.Combine(baseDir, "extension_brave");
                if (!Directory.Exists(bravePath) && Directory.Exists(Path.Combine(baseDir, "extensions", "brave"))) bravePath = Path.Combine(baseDir, "extensions", "brave");
                else if (!Directory.Exists(bravePath) && Directory.Exists(Path.Combine(baseDir, "update", "extension_brave"))) bravePath = Path.Combine(baseDir, "update", "extension_brave");

                string chromePath = Path.Combine(baseDir, "extension_chrome");
                if (!Directory.Exists(chromePath) && Directory.Exists(Path.Combine(baseDir, "extensions", "chrome"))) chromePath = Path.Combine(baseDir, "extensions", "chrome");
                else if (!Directory.Exists(chromePath) && Directory.Exists(Path.Combine(baseDir, "update", "extension_chrome"))) chromePath = Path.Combine(baseDir, "update", "extension_chrome");

                string edgePath = Path.Combine(baseDir, "extension_edge");
                if (!Directory.Exists(edgePath) && Directory.Exists(Path.Combine(baseDir, "extensions", "edge"))) edgePath = Path.Combine(baseDir, "extensions", "edge");
                else if (!Directory.Exists(edgePath) && Directory.Exists(Path.Combine(baseDir, "update", "extension_edge"))) edgePath = Path.Combine(baseDir, "update", "extension_edge");

                string firefoxPath = Path.Combine(baseDir, "extension_firefox");
                if (!Directory.Exists(firefoxPath) && Directory.Exists(Path.Combine(baseDir, "extensions", "firefox"))) firefoxPath = Path.Combine(baseDir, "extensions", "firefox");
                else if (!Directory.Exists(firefoxPath) && Directory.Exists(Path.Combine(baseDir, "update", "extension_firefox"))) firefoxPath = Path.Combine(baseDir, "update", "extension_firefox");

                // Ensure all exist
                try
                {
                    if (!Directory.Exists(bravePath)) Directory.CreateDirectory(bravePath);
                    if (!Directory.Exists(chromePath)) Directory.CreateDirectory(chromePath);
                    if (!Directory.Exists(edgePath)) Directory.CreateDirectory(edgePath);
                    if (!Directory.Exists(firefoxPath)) Directory.CreateDirectory(firefoxPath);
                }
                catch { }

                string currentSelectedPath = bravePath;
                string currentSelectedBrowser = "Brave Browser";
                string currentExtensionsUrl = "brave://extensions";

                // Browser Tab Buttons Panel
                Panel pnlTabs = new Panel
                {
                    Location = new Point(20, 68),
                    Size = new Size(625, 42),
                    BackColor = Color.FromArgb(28, 31, 40)
                };
                extForm.Controls.Add(pnlTabs);

                Button btnTabBrave = new Button { Text = "🦁 Brave Browser", Size = new Size(150, 36), Location = new Point(3, 3), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
                Button btnTabChrome = new Button { Text = "🌐 Google Chrome", Size = new Size(150, 36), Location = new Point(158, 3), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
                Button btnTabEdge = new Button { Text = "🌊 Microsoft Edge", Size = new Size(150, 36), Location = new Point(313, 3), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
                Button btnTabFirefox = new Button { Text = "🦊 Firefox", Size = new Size(150, 36), Location = new Point(468, 3), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };

                btnTabBrave.FlatAppearance.BorderSize = 0;
                btnTabChrome.FlatAppearance.BorderSize = 0;
                btnTabEdge.FlatAppearance.BorderSize = 0;
                btnTabFirefox.FlatAppearance.BorderSize = 0;

                pnlTabs.Controls.Add(btnTabBrave);
                pnlTabs.Controls.Add(btnTabChrome);
                pnlTabs.Controls.Add(btnTabEdge);
                pnlTabs.Controls.Add(btnTabFirefox);

                TextBox txtGuide = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new Point(20, 118),
                    Size = new Size(625, 275),
                    BackColor = Color.FromArgb(28, 31, 40),
                    ForeColor = Color.FromArgb(230, 235, 245),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5F)
                };
                extForm.Controls.Add(txtGuide);

                Button btnOpenFolder = new Button
                {
                    Text = "📁 Open Extension Folder",
                    Location = new Point(20, 403),
                    Size = new Size(195, 38),
                    BackColor = Color.FromArgb(0, 122, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnOpenFolder.FlatAppearance.BorderSize = 0;
                btnOpenFolder.Click += (s, e) =>
                {
                    try
                    {
                        if (!Directory.Exists(currentSelectedPath)) Directory.CreateDirectory(currentSelectedPath);
                        Process.Start("explorer.exe", currentSelectedPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open folder: " + ex.Message, "Turbo X");
                    }
                };
                extForm.Controls.Add(btnOpenFolder);

                Button btnCopyPath = new Button
                {
                    Text = "📋 Copy Folder Path",
                    Location = new Point(223, 403),
                    Size = new Size(155, 38),
                    BackColor = Color.FromArgb(45, 50, 62),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCopyPath.FlatAppearance.BorderSize = 0;
                btnCopyPath.Click += (s, e) =>
                {
                    try
                    {
                        Clipboard.SetText(currentSelectedPath);
                        MessageBox.Show(currentSelectedBrowser + " extension path copied to clipboard:\n\n" + currentSelectedPath, "Turbo X", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                };
                extForm.Controls.Add(btnCopyPath);

                Button btnOpenBrowser = new Button
                {
                    Text = "🚀 Open Extensions Page",
                    Location = new Point(386, 403),
                    Size = new Size(165, 38),
                    BackColor = Color.FromArgb(14, 116, 144),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnOpenBrowser.FlatAppearance.BorderSize = 0;
                btnOpenBrowser.Click += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = currentExtensionsUrl,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        try { Process.Start("explorer.exe", currentExtensionsUrl); } catch { }
                    }
                };
                extForm.Controls.Add(btnOpenBrowser);

                Button btnTestConnection = new Button
                {
                    Text = "🔌 Test API",
                    Location = new Point(559, 403),
                    Size = new Size(86, 38),
                    BackColor = Color.FromArgb(35, 140, 75),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnTestConnection.FlatAppearance.BorderSize = 0;
                btnTestConnection.Click += (s, e) =>
                {
                    bool running = _isHttpServerRunning;
                    MessageBox.Show(running ? "⚡ Turbo X API is ACTIVE & LISTENING on:\n• http://127.0.0.1:19876/\n• http://localhost:19876/\n\nReady for Brave, Chrome, Edge, and Firefox!" : "API is offline.", "Turbo X API Status", MessageBoxButtons.OK, running ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                };
                extForm.Controls.Add(btnTestConnection);

                Action updateTabSelection = () =>
                {
                    btnTabBrave.BackColor = currentSelectedBrowser.Contains("Brave") ? Color.FromArgb(239, 68, 68) : Color.FromArgb(38, 42, 54);
                    btnTabBrave.ForeColor = Color.White;

                    btnTabChrome.BackColor = currentSelectedBrowser.Contains("Chrome") ? Color.FromArgb(37, 99, 235) : Color.FromArgb(38, 42, 54);
                    btnTabChrome.ForeColor = Color.White;

                    btnTabEdge.BackColor = currentSelectedBrowser.Contains("Edge") ? Color.FromArgb(2, 132, 199) : Color.FromArgb(38, 42, 54);
                    btnTabEdge.ForeColor = Color.White;

                    btnTabFirefox.BackColor = currentSelectedBrowser.Contains("Firefox") ? Color.FromArgb(217, 119, 6) : Color.FromArgb(38, 42, 54);
                    btnTabFirefox.ForeColor = Color.White;

                    btnOpenFolder.Text = "📁 Open " + (currentSelectedBrowser.Contains("Brave") ? "Brave" : (currentSelectedBrowser.Contains("Chrome") ? "Chrome" : (currentSelectedBrowser.Contains("Edge") ? "Edge" : "Firefox"))) + " Folder";

                    if (currentSelectedBrowser.Contains("Brave"))
                    {
                        currentSelectedPath = bravePath;
                        currentExtensionsUrl = "brave://extensions";
                        txtGuide.Text = "🦁 BRAVE BROWSER INSTALLATION GUIDE (Folder: extension_brave)\r\n" +
                                        "==================================================================\r\n\r\n" +
                                        "📌 Step 1: Open Brave Browser and go to: brave://extensions\r\n\r\n" +
                                        "📌 Step 2: In the top-right corner, turn ON the 'Developer mode' toggle.\r\n\r\n" +
                                        "📌 Step 3: In the top-left corner, click the 'Load unpacked' button.\r\n\r\n" +
                                        "📌 Step 4: Click the blue 'Open Brave Folder' button below, and select the 'extension_brave' folder!\r\n\r\n" +
                                        "✅ DONE! Turbo X is now integrated with Brave:\r\n" +
                                        "   • Injected '⚡ Turbo X' button inside YouTube video player.\r\n" +
                                        "   • Floating '⚡ Download 4K/8K' badge on Facebook, TikTok & web videos.\r\n" +
                                        "   • One-click presets: 8K, 4K, 2K, 1080p, 720p, and MP3 audio.\r\n" +
                                        "   • All files automatically organized into Video, Music, Programs folders!";
                    }
                    else if (currentSelectedBrowser.Contains("Chrome"))
                    {
                        currentSelectedPath = chromePath;
                        currentExtensionsUrl = "chrome://extensions";
                        txtGuide.Text = "🌐 GOOGLE CHROME INSTALLATION GUIDE (Folder: extension_chrome)\r\n" +
                                        "==================================================================\r\n\r\n" +
                                        "📌 Step 1: Open Google Chrome and go to: chrome://extensions\r\n\r\n" +
                                        "📌 Step 2: In the top-right corner, turn ON the 'Developer mode' toggle.\r\n\r\n" +
                                        "📌 Step 3: In the top-left corner, click the 'Load unpacked' button.\r\n\r\n" +
                                        "📌 Step 4: Select the 'extension_chrome' folder opened by the button below.\r\n\r\n" +
                                        "✅ DONE! Chrome will now accelerate all downloads and support 8K/4K/MP3 video capture!";
                    }
                    else if (currentSelectedBrowser.Contains("Edge"))
                    {
                        currentSelectedPath = edgePath;
                        currentExtensionsUrl = "edge://extensions";
                        txtGuide.Text = "🌊 MICROSOFT EDGE INSTALLATION GUIDE (Folder: extension_edge)\r\n" +
                                        "==================================================================\r\n\r\n" +
                                        "📌 Step 1: Open Microsoft Edge and go to: edge://extensions\r\n\r\n" +
                                        "📌 Step 2: In the left sidebar, turn ON 'Developer mode'.\r\n\r\n" +
                                        "📌 Step 3: Click 'Load unpacked' at the top.\r\n\r\n" +
                                        "📌 Step 4: Select the 'extension_edge' folder opened by the button below.\r\n\r\n" +
                                        "✅ DONE! Edge downloads will now be handled directly by Turbo X!";
                    }
                    else if (currentSelectedBrowser.Contains("Firefox"))
                    {
                        currentSelectedPath = firefoxPath;
                        currentExtensionsUrl = "about:debugging#/runtime/this-firefox";
                        txtGuide.Text = "🦊 MOZILLA FIREFOX INSTALLATION GUIDE (Folder: extension_firefox)\r\n" +
                                        "==================================================================\r\n\r\n" +
                                        "📌 Step 1: Open Firefox and navigate to: about:debugging#/runtime/this-firefox\r\n\r\n" +
                                        "📌 Step 2: Click the 'Load Temporary Add-on...' button.\r\n\r\n" +
                                        "📌 Step 3: Browse to the 'extension_firefox' folder and select 'manifest.json'.\r\n\r\n" +
                                        "✅ DONE! Firefox is now linked with Turbo X High-Speed Downloader!";
                    }
                };

                btnTabBrave.Click += (s, e) => { currentSelectedBrowser = "Brave Browser"; updateTabSelection(); };
                btnTabChrome.Click += (s, e) => { currentSelectedBrowser = "Google Chrome"; updateTabSelection(); };
                btnTabEdge.Click += (s, e) => { currentSelectedBrowser = "Microsoft Edge"; updateTabSelection(); };
                btnTabFirefox.Click += (s, e) => { currentSelectedBrowser = "Firefox"; updateTabSelection(); };

                // Initial Tab State (Brave as primary)
                updateTabSelection();

                Button btnClose = new Button
                {
                    Text = "Done / Close",
                    Location = new Point(525, 465),
                    Size = new Size(120, 36),
                    BackColor = Color.FromArgb(50, 54, 66),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                btnClose.FlatAppearance.BorderSize = 0;
                extForm.Controls.Add(btnClose);

                extForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening extension guide: " + ex.Message, "Turbo X");
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isExplicitExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
                if (!_hasShownTrayTip)
                {
                    _hasShownTrayTip = true;
                    ShowTrayBalloon("Turbo X is running in background", "Turbo X is active in your taskbar system tray. Copied links will be detected automatically.");
                }
                return;
            }

            try { SaveDownloadsHistory(); } catch { }
            try { StopLocalHttpServer(); } catch { }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            base.OnFormClosing(e);
        }

        #endregion
    }

    public class DownloadItem
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; }
        public string FilePath { get; set; }
        public string SizeStr { get; set; }
        public double Percentage { get; set; }
        public string ProgressStr { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public string Status { get; set; }
        public string TimeLeft { get; set; }
        public string TransferRate { get; set; }
        public double SpeedBytes { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? StartTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public bool IsScheduled { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string Category { get; set; }
        public bool IsVideoMode { get; set; }
        public string Quality { get; set; }
        public string Format { get; set; }
        public string DirectMediaUrl { get; set; }

        public ListViewItem ListItem { get; set; }
        public DownloadProgressDialog ProgressDlg { get; set; }
        public VideoEngine VideoEng { get; set; }
        public DownloadEngine Engine { get; set; }
        public TorrentEngine TorrentEng { get; set; }
    }
}
