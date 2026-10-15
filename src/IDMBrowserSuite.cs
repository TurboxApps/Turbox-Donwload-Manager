using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace IDMClone
{
    public class BrowserHistoryEntry
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime VisitedAt { get; set; }
    }

    public class BrowserBookmarkEntry
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public static class IDMBrowserService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDMTurboDownloader");
        private static readonly string HistoryFile = Path.Combine(AppDataFolder, "browser_history.txt");
        private static readonly string BookmarksFile = Path.Combine(AppDataFolder, "browser_bookmarks.txt");

        private static List<BrowserHistoryEntry> _history = new List<BrowserHistoryEntry>();
        private static List<BrowserBookmarkEntry> _bookmarks = new List<BrowserBookmarkEntry>();
        private static bool _loaded = false;

        public static bool AdBlockerEnabled { get; set; }
        public static bool WebDarkModeEnabled { get; set; }
        public static bool DesktopModeEnabled { get; set; }

        static IDMBrowserService()
        {
            AdBlockerEnabled = true;
            WebDarkModeEnabled = false;
            DesktopModeEnabled = true;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);

                // Load History
                if (File.Exists(HistoryFile))
                {
                    string[] lines = File.ReadAllLines(HistoryFile, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split(new char[] { '|' }, 3);
                        if (parts.Length >= 2)
                        {
                            DateTime dt = DateTime.Now;
                            if (parts.Length >= 3) DateTime.TryParse(parts[2], out dt);
                            _history.Add(new BrowserHistoryEntry
                            {
                                Url = parts[0],
                                Title = parts[1],
                                VisitedAt = dt
                            });
                        }
                    }
                }

                // Load Bookmarks
                if (File.Exists(BookmarksFile))
                {
                    string[] lines = File.ReadAllLines(BookmarksFile, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split(new char[] { '|' }, 3);
                        if (parts.Length >= 2)
                        {
                            DateTime dt = DateTime.Now;
                            if (parts.Length >= 3) DateTime.TryParse(parts[2], out dt);
                            _bookmarks.Add(new BrowserBookmarkEntry
                            {
                                Url = parts[0],
                                Title = parts[1],
                                AddedAt = dt
                            });
                        }
                    }
                }

                _loaded = true;
            }
            catch { }
        }

        public static void AddHistory(string title, string url)
        {
            if (string.IsNullOrEmpty(url) || url.StartsWith("about:") || url.StartsWith("javascript:")) return;
            EnsureLoaded();

            if (string.IsNullOrEmpty(title)) title = url;
            try
            {
                if (_history.Count > 0 && string.Equals(_history[0].Url, url, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(title) && title != url)
                    {
                        _history[0].Title = title;
                        SaveHistory();
                    }
                    return;
                }

                _history.Insert(0, new BrowserHistoryEntry
                {
                    Title = title,
                    Url = url,
                    VisitedAt = DateTime.Now
                });

                if (_history.Count > 1000) _history.RemoveRange(1000, _history.Count - 1000);

                SaveHistory();
            }
            catch { }
        }

        public static List<BrowserHistoryEntry> GetHistory()
        {
            EnsureLoaded();
            return new List<BrowserHistoryEntry>(_history);
        }

        public static void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        private static void SaveHistory()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
                List<string> lines = new List<string>();
                foreach (var h in _history)
                {
                    lines.Add(string.Format("{0}|{1}|{2}", h.Url, h.Title.Replace("|", " "), h.VisitedAt.ToString("o")));
                }
                File.WriteAllLines(HistoryFile, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        public static bool AddBookmark(string title, string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            EnsureLoaded();

            if (string.IsNullOrEmpty(title)) title = url;

            foreach (var b in _bookmarks)
            {
                if (string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase)) return false;
            }

            _bookmarks.Insert(0, new BrowserBookmarkEntry
            {
                Title = title,
                Url = url,
                AddedAt = DateTime.Now
            });

            SaveBookmarks();
            return true;
        }

        public static List<BrowserBookmarkEntry> GetBookmarks()
        {
            EnsureLoaded();
            return new List<BrowserBookmarkEntry>(_bookmarks);
        }

        public static void RemoveBookmark(string url)
        {
            EnsureLoaded();
            _bookmarks.RemoveAll(b => string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
            SaveBookmarks();
        }

        private static void SaveBookmarks()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
                List<string> lines = new List<string>();
                foreach (var b in _bookmarks)
                {
                    lines.Add(string.Format("{0}|{1}|{2}", b.Url, b.Title.Replace("|", " "), b.AddedAt.ToString("o")));
                }
                File.WriteAllLines(BookmarksFile, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        public static void ClearBrowsingData(bool clearHistory, bool clearCookies, bool clearCache)
        {
            if (clearHistory) ClearHistory();

            if (clearCache || clearCookies)
            {
                try
                {
                    // Windows Native Inetcpl track clearing: 8 = Temp Cache, 2 = Cookies, 255 = All
                    int flags = 0;
                    if (clearCache) flags |= 8;
                    if (clearCookies) flags |= 2;
                    Process.Start("RunDll32.exe", "InetCpl.cpl,ClearMyTracksByProcess " + flags);
                }
                catch { }
            }
        }

        public static void InjectAdBlocker(WebBrowser wb)
        {
            if (!AdBlockerEnabled || wb == null || wb.Document == null) return;
            try
            {
                var doc = wb.Document;
                if (doc.Body == null) return;

                var headList = doc.GetElementsByTagName("head");
                HtmlElement head = (headList != null && headList.Count > 0) ? headList[0] : null;
                if (head != null)
                {
                    HtmlElement styleEl = doc.CreateElement("style");
                    styleEl.SetAttribute("type", "text/css");
                    styleEl.InnerText = "iframe[src*='ad'], iframe[src*='doubleclick'], iframe[src*='ads'], " +
                                        ".adsbygoogle, .ad-banner, .advertisement, [id*='google_ads'], " +
                                        "[class*='ad-box'], .sponsored-post, [id*='banner-ad'] " +
                                        "{ display: none !important; visibility: hidden !important; height: 0 !important; }";
                    head.AppendChild(styleEl);
                }
            }
            catch { }
        }

        public static void InjectDarkMode(WebBrowser wb, bool enable)
        {
            if (wb == null || wb.Document == null) return;
            try
            {
                var doc = wb.Document;
                if (doc.Body == null) return;

                var headList = doc.GetElementsByTagName("head");
                HtmlElement head = (headList != null && headList.Count > 0) ? headList[0] : null;
                if (head != null)
                {
                    if (enable)
                    {
                        HtmlElement styleEl = doc.CreateElement("style");
                        styleEl.SetAttribute("id", "idm-dark-mode-style");
                        styleEl.SetAttribute("type", "text/css");
                        styleEl.InnerText = "html, body { background-color: #121316 !important; color: #e2e8f0 !important; } " +
                                            "div, p, span, li, td, th, h1, h2, h3, h4, h5, h6 { color: #e2e8f0 !important; } " +
                                            "a { color: #38bdf8 !important; } " +
                                            "input, textarea, select { background-color: #1e293b !important; color: #f8fafc !important; }";
                        head.AppendChild(styleEl);
                    }
                    else
                    {
                        // Remove injected dark mode
                        var styles = head.GetElementsByTagName("style");
                        if (styles != null)
                        {
                            foreach (HtmlElement s in styles)
                            {
                                if (s.GetAttribute("id") == "idm-dark-mode-style")
                                {
                                    s.OuterHtml = "";
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static readonly string[] AdAndBettingPatterns = new string[]
        {
            // Bangladeshi & Asian Gambling/Betting Platforms (like jaya9, 1xbet, etc.)
            "jaya9", "joya9", "1xbet", "melbet", "babu88", "nagad88", "bajilive", "mcw",
            "betbuzz", "krikya", "jeetwin", "parimatch", "betway", "bet365", "casinobd",
            "linebet", "mostbet", "mega-casino", "baji999", "cricketbet", "lotus365",
            "fairplay", "dafabet", "betiton", "spinbounty", "betvisa", "jeetbuzz", "mega-pari",
            "glory-casino", "six6s", "velki", "nagadbet", "baji-live", "1win",

            // Pop-up / Pop-under Networks & Ad Redirectors
            "adsterra", "popads", "propellerads", "exoclick", "monetag", "hilltopads",
            "onclick", "adcash", "trafficjunky", "adxxx", "popunder", "adsupply",
            "clicksfly", "adfly", "ouo.io", "shrinkearn", "exe.io", "gplinks", "droplink",
            "adtrue", "zedo", "doubleclick", "googlesyndication", "googleadservices",
            "adservice.google", "moatads", "taboola", "outbrain", "mgid", "revcontent",
            "pubmatic", "criteo", "rubiconproject", "bidswitch", "openx", "appnexus",
            "adnxs", "adkeeper", "adkernel", "adreactor", "adflex", "adtarget",
            "richaudience", "smartadserver", "yieldmo", "inmobi", "vungle", "ironsource",
            "unity3d.com/ads", "adcolony", "applovin", "fyber", "chartboost", "media.net",
            "zeroredirect", "popcash", "admaven", "trafficstars", "ero-advertising",
            "juicyads", "plugrush", "wigetmedia", "trafficfactory", "exads", "ad-maven",
            "syndication.exdynsrv", "a.bestcontentfood", "trk.clks", "creative.revcontent",

            // Suspicious tracking & ad redirect parameters
            "/af=ag", "affiliate=", "ref_id=", "clickid=", "ad_id=", "/click.php",
            "/pop.php", "/redirect.php?ad=", "rotator.php", "track.php?id="
        };

        public static bool IsAdOrPopupUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            string low = url.ToLowerInvariant();

            // Do not block legitimate video portals, CDNs, or primary search/content services
            if (low.Contains("tiktok.com") || low.Contains("tiktokcdn.com") || low.Contains("byteoversea.com") ||
                low.Contains("ibytedtos.com") || low.Contains("tiktokv.com") || low.Contains("bytedance.com")) return false;
            if (low.Contains("facebook.com") || low.Contains("fbcdn.net") || low.Contains("instagram.com") || low.Contains("cdninstagram.com")) return false;
            if (low.Contains("youtube.com") || low.Contains("googlevideo.com") || low.Contains("youtu.be")) return false;
            if (low.Contains("google.com") && !low.Contains("googleadservices") && !low.Contains("googlesyndication")) return false;
            if (low.Contains("fastdl") || low.Contains("pages.dev") || low.Contains("googleusercontent.com")) return false;
            if (low.Contains("drive.google.com") || low.Contains("mediafire.com") || low.Contains("mega.nz")) return false;
            if (low.Contains("github.com") || low.Contains("bing.com")) return false;
            if (low.Contains("wikipedia.org") || low.Contains("duckduckgo.com")) return false;

            for (int i = 0; i < AdAndBettingPatterns.Length; i++)
            {
                if (low.Contains(AdAndBettingPatterns[i]))
                    return true;
            }
            return false;
        }

        public static string GetAdBlockScript()
        {
            return @"(function() {
                // 1. Defuse click-hijackers & popunders on window.open
                var originalOpen = window.open;
                window.open = function(url, target, features) {
                    if (!url || url === 'about:blank') {
                        return null;
                    }
                    var u = url.toLowerCase();
                    var adWords = ['jaya9','joya9','1xbet','melbet','babu88','nagad88','bajilive','mcw','krikya','jeetwin','parimatch','bet365','betway','mostbet','linebet','adsterra','popads','propellerads','exoclick','monetag','hilltopads','onclick','adcash','doubleclick','googlesyndication','popcash','admaven','affiliate','af=ag'];
                    for (var i = 0; i < adWords.length; i++) {
                        if (u.indexOf(adWords[i]) !== -1) {
                            console.log('[IDM Turbo AdBlock] Prevented popunder window.open: ' + url);
                            return null;
                        }
                    }
                    return originalOpen.apply(window, arguments);
                };

                // 2. Kill transparent full-screen overlay click-hijackers
                function killClickOverlays() {
                    try {
                        var h = (window.location.hostname || '').toLowerCase();
                        if (h.indexOf('tiktok.com') !== -1 || h.indexOf('youtube.com') !== -1 || h.indexOf('facebook.com') !== -1 || h.indexOf('instagram.com') !== -1) return;
                        var elements = document.querySelectorAll('div, a, span');
                        for (var i = 0; i < elements.length; i++) {
                            var el = elements[i];
                            var z = parseInt(window.getComputedStyle(el).zIndex) || 0;
                            if (z > 9999) {
                                var pos = window.getComputedStyle(el).position;
                                if (pos === 'fixed' || pos === 'absolute') {
                                    var w = el.offsetWidth;
                                    var h = el.offsetHeight;
                                    if (w > window.innerWidth * 0.7 && h > window.innerHeight * 0.7) {
                                        if (el.querySelectorAll('video, audio, input, form').length === 0) {
                                            el.remove();
                                        }
                                    }
                                }
                            }
                        }
                    } catch(e) {}
                }
                document.addEventListener('DOMContentLoaded', killClickOverlays);
                setTimeout(killClickOverlays, 800);
                setTimeout(killClickOverlays, 2000);

                // 3. Inject CSS ad remover
                var css = 'iframe[src*=\'ad\'], iframe[src*=\'doubleclick\'], iframe[src*=\'ads\'], ' +
                          '.adsbygoogle, .ad-banner, .advertisement, [id*=\'google_ads\'], ' +
                          '[class*=\'ad-box\'], .sponsored-post, [id*=\'banner-ad\'], ' +
                          '[class*=\'popunder\'], [id*=\'popunder\'], [class*=\'betting\'], ' +
                          '[class*=\'gambling\'], div[id*=\'ad_\'], div[class*=\'ad-container\'], ' +
                          'a[href*=\'jaya9\'], a[href*=\'1xbet\'], a[href*=\'babu88\'] ' +
                          '{ display: none !important; visibility: hidden !important; height: 0 !important; width: 0 !important; pointer-events: none !important; }';
                var head = document.head || document.documentElement;
                if (head) {
                    var style = document.createElement('style');
                    style.type = 'text/css';
                    style.appendChild(document.createTextNode(css));
                    head.appendChild(style);
                }
            })();";
        }

        public static async void InjectAdBlocker(WebView2 wv)
        {
            if (!AdBlockerEnabled || wv == null || wv.CoreWebView2 == null) return;
            try
            {
                await wv.CoreWebView2.ExecuteScriptAsync(GetAdBlockScript());
            }
            catch { }
        }

        public static async void InjectDarkMode(WebView2 wv, bool enable)
        {
            if (wv == null || wv.CoreWebView2 == null) return;
            try
            {
                if (enable)
                {
                    string js = @"(function() {
                        var el = document.getElementById('idm-dark-mode-style');
                        if (!el) {
                            var css = 'html, body { background-color: #121316 !important; color: #e2e8f0 !important; } div, p, span, li, td, th, h1, h2, h3, h4, h5, h6 { color: #e2e8f0 !important; } a { color: #38bdf8 !important; } input, textarea, select { background-color: #1e293b !important; color: #f8fafc !important; }';
                            var head = document.head || document.getElementsByTagName('head')[0];
                            if (head) {
                                var style = document.createElement('style');
                                style.id = 'idm-dark-mode-style';
                                style.type = 'text/css';
                                style.appendChild(document.createTextNode(css));
                                head.appendChild(style);
                            }
                        }
                    })();";
                    await wv.CoreWebView2.ExecuteScriptAsync(js);
                }
                else
                {
                    string js = @"(function() {
                        var el = document.getElementById('idm-dark-mode-style');
                        if (el) el.remove();
                    })();";
                    await wv.CoreWebView2.ExecuteScriptAsync(js);
                }
            }
            catch { }
        }
    }

    public class BrowserHistoryDialog : Form
    {
        private ListView lvHistory;
        private TextBox txtSearch;
        private Button btnOpen;
        private Button btnDelete;
        private Button btnClearAll;
        private Button btnClose;
        public string SelectedUrl { get; private set; }

        public BrowserHistoryDialog()
        {
            InitializeComponent();
            LoadHistory();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "🕒 1DM Browsing History";
            this.Size = new Size(820, 520);
            this.MinimumSize = new Size(650, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);
            this.DoubleBuffered = true;

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(14, 12, 14, 8) };
            this.Controls.Add(pnlTop);

            Label lblSearch = new Label { Text = "🔍 Filter History:", AutoSize = true, Location = new Point(14, 18), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            pnlTop.Controls.Add(lblSearch);

            txtSearch = new TextBox { Location = new Point(130, 15), Size = new Size(400, 26), Font = new Font("Segoe UI", 9.5F) };
            txtSearch.TextChanged += (s, e) => FilterHistory(txtSearch.Text.Trim());
            pnlTop.Controls.Add(txtSearch);

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(14, 10, 14, 10) };
            this.Controls.Add(pnlBottom);

            btnOpen = new Button { Text = "🌐 Open URL", Size = new Size(110, 32), Location = new Point(14, 10), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnOpen.Click += (s, e) => OpenSelected();
            pnlBottom.Controls.Add(btnOpen);

            btnDelete = new Button { Text = "🗑️ Delete", Size = new Size(95, 32), Location = new Point(132, 10), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F), Cursor = Cursors.Hand };
            btnDelete.Click += (s, e) => DeleteSelected();
            pnlBottom.Controls.Add(btnDelete);

            btnClearAll = new Button { Text = "🧹 Clear All History", Size = new Size(140, 32), Location = new Point(235, 10), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F), Cursor = Cursors.Hand };
            btnClearAll.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to clear your entire 1DM browsing history?", "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    IDMBrowserService.ClearHistory();
                    LoadHistory();
                }
            };
            pnlBottom.Controls.Add(btnClearAll);

            btnClose = new Button { Text = "Close", Size = new Size(90, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlBottom.Width - 104, 10), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);
            pnlBottom.Resize += (s, e) => { btnClose.Location = new Point(pnlBottom.Width - 104, 10); };

            lvHistory = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9F)
            };
            lvHistory.Columns.Add("Title", 280);
            lvHistory.Columns.Add("Web Address (URL)", 380);
            lvHistory.Columns.Add("Date & Time", 130);
            lvHistory.DoubleClick += (s, e) => OpenSelected();
            this.Controls.Add(lvHistory);
            lvHistory.BringToFront();
        }

        private void LoadHistory()
        {
            lvHistory.Items.Clear();
            var list = IDMBrowserService.GetHistory();
            foreach (var h in list)
            {
                ListViewItem item = new ListViewItem(h.Title);
                item.SubItems.Add(h.Url);
                item.SubItems.Add(h.VisitedAt.ToString("g"));
                item.Tag = h;
                lvHistory.Items.Add(item);
            }
        }

        private void FilterHistory(string q)
        {
            lvHistory.Items.Clear();
            var list = IDMBrowserService.GetHistory();
            foreach (var h in list)
            {
                if (string.IsNullOrEmpty(q) || h.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || h.Url.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ListViewItem item = new ListViewItem(h.Title);
                    item.SubItems.Add(h.Url);
                    item.SubItems.Add(h.VisitedAt.ToString("g"));
                    item.Tag = h;
                    lvHistory.Items.Add(item);
                }
            }
        }

        private void OpenSelected()
        {
            if (lvHistory.SelectedItems.Count > 0)
            {
                var h = lvHistory.SelectedItems[0].Tag as BrowserHistoryEntry;
                if (h != null)
                {
                    SelectedUrl = h.Url;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void DeleteSelected()
        {
            if (lvHistory.SelectedItems.Count > 0)
            {
                var h = lvHistory.SelectedItems[0].Tag as BrowserHistoryEntry;
                if (h != null)
                {
                    lvHistory.Items.Remove(lvHistory.SelectedItems[0]);
                }
            }
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            lvHistory.BackColor = t.ListBg;
            lvHistory.ForeColor = t.ListText;

            txtSearch.BackColor = t.WindowBg;
            txtSearch.ForeColor = t.TextPrimary;

            btnOpen.BackColor = t.Accent;
            btnOpen.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
            btnOpen.FlatAppearance.BorderSize = 0;

            btnDelete.BackColor = t.ButtonBg;
            btnDelete.ForeColor = t.ButtonText;
            btnDelete.FlatAppearance.BorderColor = t.Border;

            btnClearAll.BackColor = t.ButtonBg;
            btnClearAll.ForeColor = t.ButtonText;
            btnClearAll.FlatAppearance.BorderColor = t.Border;

            btnClose.BackColor = t.ButtonBg;
            btnClose.ForeColor = t.ButtonText;
            btnClose.FlatAppearance.BorderColor = t.Border;
        }
    }

    public class BrowserBookmarksDialog : Form
    {
        private ListView lvBookmarks;
        private TextBox txtSearch;
        private Button btnOpen;
        private Button btnDelete;
        private Button btnClose;
        public string SelectedUrl { get; private set; }

        public BrowserBookmarksDialog()
        {
            InitializeComponent();
            LoadBookmarks();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "⭐ 1DM Bookmarks Manager";
            this.Size = new Size(820, 500);
            this.MinimumSize = new Size(650, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);
            this.DoubleBuffered = true;

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(14, 12, 14, 8) };
            this.Controls.Add(pnlTop);

            Label lblSearch = new Label { Text = "⭐ Filter Bookmarks:", AutoSize = true, Location = new Point(14, 18), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            pnlTop.Controls.Add(lblSearch);

            txtSearch = new TextBox { Location = new Point(155, 15), Size = new Size(400, 26), Font = new Font("Segoe UI", 9.5F) };
            txtSearch.TextChanged += (s, e) => FilterBookmarks(txtSearch.Text.Trim());
            pnlTop.Controls.Add(txtSearch);

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(14, 10, 14, 10) };
            this.Controls.Add(pnlBottom);

            btnOpen = new Button { Text = "🌐 Open Bookmark", Size = new Size(130, 32), Location = new Point(14, 10), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnOpen.Click += (s, e) => OpenSelected();
            pnlBottom.Controls.Add(btnOpen);

            btnDelete = new Button { Text = "🗑️ Delete", Size = new Size(95, 32), Location = new Point(152, 10), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F), Cursor = Cursors.Hand };
            btnDelete.Click += (s, e) => DeleteSelected();
            pnlBottom.Controls.Add(btnDelete);

            btnClose = new Button { Text = "Close", Size = new Size(90, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlBottom.Width - 104, 10), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);
            pnlBottom.Resize += (s, e) => { btnClose.Location = new Point(pnlBottom.Width - 104, 10); };

            lvBookmarks = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9F)
            };
            lvBookmarks.Columns.Add("Title", 280);
            lvBookmarks.Columns.Add("Web Address (URL)", 380);
            lvBookmarks.Columns.Add("Added Date", 130);
            lvBookmarks.DoubleClick += (s, e) => OpenSelected();
            this.Controls.Add(lvBookmarks);
            lvBookmarks.BringToFront();
        }

        private void LoadBookmarks()
        {
            lvBookmarks.Items.Clear();
            var list = IDMBrowserService.GetBookmarks();
            foreach (var b in list)
            {
                ListViewItem item = new ListViewItem(b.Title);
                item.SubItems.Add(b.Url);
                item.SubItems.Add(b.AddedAt.ToString("g"));
                item.Tag = b;
                lvBookmarks.Items.Add(item);
            }
        }

        private void FilterBookmarks(string q)
        {
            lvBookmarks.Items.Clear();
            var list = IDMBrowserService.GetBookmarks();
            foreach (var b in list)
            {
                if (string.IsNullOrEmpty(q) || b.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || b.Url.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ListViewItem item = new ListViewItem(b.Title);
                    item.SubItems.Add(b.Url);
                    item.SubItems.Add(b.AddedAt.ToString("g"));
                    item.Tag = b;
                    lvBookmarks.Items.Add(item);
                }
            }
        }

        private void OpenSelected()
        {
            if (lvBookmarks.SelectedItems.Count > 0)
            {
                var b = lvBookmarks.SelectedItems[0].Tag as BrowserBookmarkEntry;
                if (b != null)
                {
                    SelectedUrl = b.Url;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void DeleteSelected()
        {
            if (lvBookmarks.SelectedItems.Count > 0)
            {
                var b = lvBookmarks.SelectedItems[0].Tag as BrowserBookmarkEntry;
                if (b != null)
                {
                    IDMBrowserService.RemoveBookmark(b.Url);
                    lvBookmarks.Items.Remove(lvBookmarks.SelectedItems[0]);
                }
            }
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            lvBookmarks.BackColor = t.ListBg;
            lvBookmarks.ForeColor = t.ListText;

            txtSearch.BackColor = t.WindowBg;
            txtSearch.ForeColor = t.TextPrimary;

            btnOpen.BackColor = t.Accent;
            btnOpen.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
            btnOpen.FlatAppearance.BorderSize = 0;

            btnDelete.BackColor = t.ButtonBg;
            btnDelete.ForeColor = t.ButtonText;
            btnDelete.FlatAppearance.BorderColor = t.Border;

            btnClose.BackColor = t.ButtonBg;
            btnClose.ForeColor = t.ButtonText;
            btnClose.FlatAppearance.BorderColor = t.Border;
        }
    }

    public class PageSourceDialog : Form
    {
        private TextBox txtSource;
        private TextBox txtFind;
        private Button btnCopy;
        private Button btnSave;
        private Button btnClose;
        private string _html;

        public PageSourceDialog(string title, string url, string html)
        {
            _html = html ?? "";
            InitializeComponent(title, url);
            ApplyTheme();
        }

        private void InitializeComponent(string title, string url)
        {
            this.Text = "📄 Page Source - " + (title ?? url ?? "HTML Document");
            this.Size = new Size(880, 580);
            this.MinimumSize = new Size(680, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);
            this.DoubleBuffered = true;

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 8) };
            this.Controls.Add(pnlTop);

            Label lblFind = new Label { Text = "Find:", AutoSize = true, Location = new Point(12, 14), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            pnlTop.Controls.Add(lblFind);

            txtFind = new TextBox { Location = new Point(54, 11), Size = new Size(220, 24), Font = new Font("Segoe UI", 9F) };
            txtFind.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { FindInSource(txtFind.Text); e.SuppressKeyPress = true; }
            };
            pnlTop.Controls.Add(txtFind);

            Button btnFindNext = new Button { Text = "Search ⬇", Location = new Point(280, 9), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat };
            btnFindNext.Click += (s, e) => FindInSource(txtFind.Text);
            pnlTop.Controls.Add(btnFindNext);

            btnCopy = new Button { Text = "📋 Copy All", Location = new Point(370, 9), Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCopy.Click += (s, e) =>
            {
                try { Clipboard.SetText(_html); MessageBox.Show("Page source HTML copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { }
            };
            pnlTop.Controls.Add(btnCopy);

            btnSave = new Button { Text = "💾 Save to File", Location = new Point(472, 9), Size = new Size(105, 28), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.Click += (s, e) =>
            {
                using (SaveFileDialog sfd = new SaveFileDialog { Filter = "HTML Files (*.html)|*.html|All Files (*.*)|*.*", FileName = "source.html" })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, _html, Encoding.UTF8);
                        MessageBox.Show("Source code saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
            pnlTop.Controls.Add(btnSave);

            btnClose = new Button { Text = "Close", Location = new Point(pnlTop.Width - 92, 9), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => this.Close();
            pnlTop.Controls.Add(btnClose);
            pnlTop.Resize += (s, e) => { btnClose.Location = new Point(pnlTop.Width - 92, 9); };

            txtSource = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5F),
                Text = _html
            };
            this.Controls.Add(txtSource);
            txtSource.BringToFront();
        }

        private int _lastSearchIdx = 0;
        private void FindInSource(string query)
        {
            if (string.IsNullOrEmpty(query)) return;
            int idx = _html.IndexOf(query, _lastSearchIdx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0 && _lastSearchIdx > 0)
            {
                idx = _html.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);
            }

            if (idx >= 0)
            {
                txtSource.Select(idx, query.Length);
                txtSource.ScrollToCaret();
                _lastSearchIdx = idx + query.Length;
            }
            else
            {
                MessageBox.Show("Text not found in page source.", "Find in Source", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _lastSearchIdx = 0;
            }
        }

        private void ApplyTheme()
        {
            var t = ThemeManager.CurrentTheme;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;

            txtSource.BackColor = t.IsDark ? Color.FromArgb(17, 24, 39) : Color.FromArgb(248, 250, 252);
            txtSource.ForeColor = t.IsDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(15, 23, 42);

            txtFind.BackColor = t.WindowBg;
            txtFind.ForeColor = t.TextPrimary;

            btnCopy.BackColor = t.ButtonBg;
            btnCopy.ForeColor = t.ButtonText;
            btnCopy.FlatAppearance.BorderColor = t.Border;

            btnSave.BackColor = t.Accent;
            btnSave.ForeColor = ((t.Accent.R * 0.299 + t.Accent.G * 0.587 + t.Accent.B * 0.114) > 150) ? Color.FromArgb(15, 23, 42) : Color.White;
            btnSave.FlatAppearance.BorderSize = 0;

            btnClose.BackColor = t.ButtonBg;
            btnClose.ForeColor = t.ButtonText;
            btnClose.FlatAppearance.BorderColor = t.Border;
        }
    }
}
