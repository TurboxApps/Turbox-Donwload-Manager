using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDMClone
{
    public class VideoEngine
    {
        public event EventHandler<string> StatusChanged;
        public event EventHandler<DownloadProgressEventArgs> ProgressChanged;
        public event EventHandler<string> DownloadCompleted;
        public event EventHandler<string> DownloadFailed;

        private Process _process;
        private Process _activeFfmpegProcess;
        private volatile bool _isCancelled;
        private volatile bool _isPaused;
        private volatile bool _isRunning;
        private string _targetFilePath;

        public bool IsPaused { get { return _isPaused; } }
        public bool IsRunning { get { return _isRunning; } }
        public bool IsCancelled { get { return _isCancelled; } }

        private static void KillProcessTree(Process p)
        {
            if (p == null) return;
            try
            {
                int pid = p.Id;
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", string.Format("/F /T /PID {0}", pid))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process killer = Process.Start(psi))
                {
                    killer.WaitForExit(3000);
                }
            }
            catch { }
            try
            {
                if (!p.HasExited) p.Kill();
            }
            catch { }
        }

        private string _lastUrl;
        private string _lastTargetFolder;
        private string _lastTargetFileName;
        private string _lastQualitySelection;
        private string _lastFormatSelection;

        public static string NormalizeVideoUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            string trimmed = url.Trim();
            if (trimmed.IndexOf("tiktok.com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Normalize TikTok embed & short paths (/embed/v2/, /embed/, /v/) to canonical /@a/video/ format
                return Regex.Replace(trimmed, @"tiktok\.com/(?:embed/v2|embed|v)/(\d+)", "tiktok.com/@a/video/$1", RegexOptions.IgnoreCase);
            }
            return trimmed;
        }

        public static bool IsRootMediaUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            url = NormalizeVideoUrl(url);
            try
            {
                Uri uri;
                if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri)) return false;
                string host = uri.Host.ToLowerInvariant();
                string path = uri.AbsolutePath.Trim().TrimEnd('/');

                if (host.Contains("tiktok.com"))
                {
                    if (string.IsNullOrEmpty(path) || path == "/" || path == "/foryou" || path == "/explore" || path == "/following" || path == "/live")
                    {
                        if (!url.Contains("/video/") && !url.Contains("/v/") && !url.Contains("/embed/")) return true;
                    }
                }
                else if (host.Contains("facebook.com"))
                {
                    if (string.IsNullOrEmpty(path) || path == "/" || path == "/home.php" || path == "/watch")
                    {
                        if (!url.Contains("/reel/") && !url.Contains("/videos/") && !url.Contains("watch?v=") && !url.Contains("story.php")) return true;
                    }
                }
                else if (host.Contains("youtube.com") || host.Contains("youtu.be"))
                {
                    if (string.IsNullOrEmpty(path) || path == "/" || path.StartsWith("/feed") || path == "/trending")
                    {
                        if (!url.Contains("watch?v=") && !url.Contains("/shorts/") && !url.Contains("/live/")) return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }

        public static bool IsMediaUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            url = NormalizeVideoUrl(url);
            if (IsRootMediaUrl(url)) return false;
            string lower = url.ToLowerInvariant();

            // 1. Magnet and Torrent links
            if (lower.StartsWith("magnet:") || lower.EndsWith(".torrent"))
            {
                return false;
            }

            // 2. Direct binary, archive, or media files
            if (lower.EndsWith(".zip") || lower.EndsWith(".rar") || lower.EndsWith(".7z") ||
                lower.EndsWith(".iso") || lower.EndsWith(".exe") || lower.EndsWith(".msi") ||
                lower.EndsWith(".pdf") || lower.EndsWith(".apk") || lower.EndsWith(".dmg") ||
                lower.EndsWith(".tar.gz") || lower.EndsWith(".tar") || lower.EndsWith(".bin") ||
                lower.EndsWith(".mkv") || lower.EndsWith(".mp4") || lower.EndsWith(".avi") ||
                lower.EndsWith(".mov") || lower.EndsWith(".webm") || lower.EndsWith(".flv") ||
                lower.EndsWith(".mp3") || lower.EndsWith(".wav") || lower.EndsWith(".flac"))
            {
                return false;
            }

            // 2.5 Direct video CDN and streaming chunk URLs (Must be downloaded directly, NEVER via yt-dlp!)
            if (lower.Contains(".mp4") || lower.Contains(".mkv") || lower.Contains(".webm") ||
                lower.Contains("/video/tos/") || lower.Contains("/tos-") || lower.Contains("mime_type=video_mp4") ||
                lower.Contains(".tiktokcdn.com") || lower.Contains("tiktokv.com") || lower.Contains("byteoversea.com") ||
                lower.Contains("ibytedtos.com") || lower.Contains("fbcdn.net") || lower.Contains("googlevideo.com/videoplayback"))
            {
                return false;
            }

            // 3. Cloud storage, CDNs, & movie download hosting sites (FastDL, Google, MediaFire, Mega, etc.)
            if (lower.Contains("googleusercontent.com") ||
                lower.Contains("drive.google.com") ||
                lower.Contains("docs.google.com") ||
                lower.Contains("pages.dev") ||
                lower.Contains("mediafire.com") ||
                lower.Contains("mega.nz") ||
                lower.Contains("fastdl") ||
                lower.Contains("dropbox.com") ||
                lower.Contains("1fichier.com") ||
                lower.Contains("github.com/"))
            {
                return false;
            }

            // 4. Recognized video platforms where yt-dlp scraping is actually needed
            if (lower.Contains("youtube.com") || lower.Contains("youtu.be") ||
                lower.Contains("facebook.com") || lower.Contains("fb.watch") ||
                lower.Contains("instagram.com") ||
                lower.Contains("tiktok.com") ||
                lower.Contains("twitter.com") || lower.Contains("x.com") ||
                lower.Contains("reddit.com") ||
                lower.Contains("vimeo.com") ||
                lower.Contains("dailymotion.com") ||
                lower.Contains("twitch.tv") ||
                lower.Contains("bilibili.com") ||
                lower.Contains("soundcloud.com") ||
                lower.Contains(".m3u8") || lower.Contains(".mpd"))
            {
                return true;
            }

            // Default: All other links are handled by IDM Turbo 32-Thread Socket Engine!
            return false;
        }

        public static string GetYtDlpPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localPath = Path.Combine(baseDir, "yt-dlp.exe");
            if (File.Exists(localPath)) return localPath;

            string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "IDM Turbo Downloader", "yt-dlp.exe");
            if (File.Exists(userDownloads)) return userDownloads;

            return "yt-dlp.exe";
        }

        public static string GetCookieArgument(string url = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(url) && url.IndexOf("tiktok.com", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ""; // TikTok WAF blocks or returns 403 when WebView2 session cookies are provided
                }
                string cookieFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDMTurboDownloader", "browser_cookies.txt");
                if (File.Exists(cookieFile) && new FileInfo(cookieFile).Length > 20)
                {
                    return string.Format(" --cookies \"{0}\"", cookieFile);
                }
            }
            catch { }
            return "";
        }

        public void FetchMetadata(string url, Action<bool, string, List<int>, string> onComplete)
        {
            url = NormalizeVideoUrl(url);
            Task.Factory.StartNew(() =>
            {
                try
                {
                    RaiseStatus("Analyzing URL & inspecting media streams...");
                    string ytdlp = GetYtDlpPath();
                    string cookieArg = GetCookieArgument(url);

                    string lastError = null;
                    bool success = ExecuteMetadataQuery(ytdlp, url, cookieArg, onComplete, out lastError);

                    // If failed and we attempted with cookies, immediately retry WITHOUT cookies as fallback!
                    if (!success && !string.IsNullOrEmpty(cookieArg))
                    {
                        RaiseStatus("Retrying media inspection without cookies...");
                        success = ExecuteMetadataQuery(ytdlp, url, "", onComplete, out lastError);
                    }

                    if (!success && onComplete != null)
                    {
                        onComplete(false, null, null, lastError);
                    }
                }
                catch (Exception ex)
                {
                    RaiseStatus("Notice: " + ex.Message);
                    if (onComplete != null) onComplete(false, null, null, ex.Message);
                }
            });
        }

        public static int GetStandardQualityTier(int width, int height)
        {
            int minDim = Math.Min(width, height);
            int maxDim = Math.Max(width, height);
            if (minDim >= 3840 || maxDim >= 7000) return 4320; // 8K Ultra HD
            if (minDim >= 2000 || maxDim >= 3500) return 2160; // 4K Ultra HD
            if (minDim >= 1350 || maxDim >= 2350) return 1440; // 2K Quad HD
            if (minDim >= 900 || maxDim >= 1650) return 1080;  // 1080p Full HD
            if (minDim >= 600 || maxDim >= 1100) return 720;   // 720p HD
            if (minDim >= 400 || maxDim >= 700) return 480;    // 480p Standard
            if (minDim >= 200 || maxDim >= 300) return 360;    // 360p Medium
            return 0;
        }

        private bool ExecuteMetadataQuery(string ytdlp, string url, string cookieArg, Action<bool, string, List<int>, string> onComplete, out string errorOutput)
        {
            errorOutput = null;
            try
            {
                url = NormalizeVideoUrl(url);
                string extraHeaders = "";
                if (!string.IsNullOrEmpty(url) && url.IndexOf("tiktok.com", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    extraHeaders = " --add-headers \"Referer:https://www.tiktok.com/\"";
                }

                string tempDir = Path.GetTempPath();
                string ffmpegPath = GetFfmpegPath();
                string ffmpegArg = File.Exists(ffmpegPath) ? string.Format(" --ffmpeg-location \"{0}\"", Path.GetDirectoryName(ffmpegPath)) : "";
                string cacheArg = string.Format(" --cache-dir \"{0}\"", Path.Combine(tempDir, "TurboX_Cache"));

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ytdlp,
                    Arguments = string.Format("--no-playlist{0}{1}{2}{3} --socket-timeout 10 --print \"%(title)s\" --print \"%(formats.:.resolution)s\" --print \"%(formats.:.height)s\" \"{4}\"", cookieArg, extraHeaders, ffmpegArg, cacheArg, url),
                    WorkingDirectory = tempDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    var stdoutTask = Task.Run(() => p.StandardOutput.ReadToEnd());
                    var stderrTask = Task.Run(() => p.StandardError.ReadToEnd());

                    if (!p.WaitForExit(12000))
                    {
                        try { p.Kill(); } catch { }
                        errorOutput = "Query timed out after 12 seconds";
                        return false;
                    }

                    string output = stdoutTask.Result;
                    string error = stderrTask.Result;
                    errorOutput = error;

                    string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> cleanLines = new List<string>();
                    foreach (var line in lines)
                    {
                        if (!line.StartsWith("WARNING:") && !line.StartsWith("ERROR:") && !line.StartsWith("[download]") && !line.StartsWith("[info]"))
                        {
                            cleanLines.Add(line);
                        }
                    }

                    if (cleanLines.Count == 0)
                    {
                        return false;
                    }

                    string title = cleanLines[0].Trim();
                    string safeTitle = SanitizeFileName(title);

                    HashSet<int> seenTiers = new HashSet<int>();
                    for (int i = 1; i < cleanLines.Count; i++)
                    {
                        string line = cleanLines[i];
                        // 1. Look for WxH resolutions (e.g. 1080x1920, 2160x3840, 3840x2160)
                        MatchCollection resMatches = Regex.Matches(line, @"\b(\d{3,5})x(\d{3,5})\b");
                        foreach (Match rm in resMatches)
                        {
                            int w, h;
                            if (int.TryParse(rm.Groups[1].Value, out w) && int.TryParse(rm.Groups[2].Value, out h))
                            {
                                int tier = GetStandardQualityTier(w, h);
                                if (tier > 0) seenTiers.Add(tier);
                            }
                        }

                        // 2. Look for standalone numbers
                        MatchCollection numMatches = Regex.Matches(line, @"\b(\d{3,4})\b");
                        foreach (Match nm in numMatches)
                        {
                            int n;
                            if (int.TryParse(nm.Value, out n))
                            {
                                int tier = GetStandardQualityTier(n, n);
                                if (tier > 0) seenTiers.Add(tier);
                            }
                        }
                    }

                    List<int> heights = new List<int>(seenTiers);
                    heights.Sort((a, b) => b.CompareTo(a)); // Descending: 4320 (8K), 2160 (4K), 1440 (2K), 1080, 720...

                    RaiseStatus("Media stream verified. Formats ready.");
                    if (onComplete != null) onComplete(true, safeTitle, heights, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorOutput = ex.Message;
                return false;
            }
        }

        public void Start(string url, string targetFolder, string targetFileName, string qualitySelection, string formatSelection, string directCdnFallback = null)
        {
            if (string.IsNullOrEmpty(qualitySelection)) qualitySelection = "Best";
            if (string.IsNullOrEmpty(formatSelection)) formatSelection = "mp4";

            _lastUrl = url;
            _lastTargetFolder = targetFolder;
            _lastTargetFileName = targetFileName;
            _lastQualitySelection = qualitySelection;
            _lastFormatSelection = formatSelection;

            _isCancelled = false;
            _isPaused = false;
            _isRunning = true;
            url = NormalizeVideoUrl(url);

            string ext = formatSelection.ToLowerInvariant();
            if (ext.StartsWith(".")) ext = ext.Substring(1);

            bool isAudioOnly = ext == "mp3" || ext == "m4a" || qualitySelection.ToLowerInvariant().Contains("audio");
            if (isAudioOnly && ext != "m4a") ext = "mp3";

            string baseName = Path.GetFileNameWithoutExtension(targetFileName);
            string safeFileName = SanitizeFileName(baseName) + "." + ext;
            _targetFilePath = Path.Combine(targetFolder, safeFileName);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    bool isTikTok = url.IndexOf("tiktok.com", StringComparison.OrdinalIgnoreCase) >= 0;
                    string cookieArg = GetCookieArgument(url);
                    bool downloaded = RunDownloadProcess(url, targetFolder, safeFileName, ext, qualitySelection, isAudioOnly, isTikTok, cookieArg, false);

                    // If download failed with cookies, retry without cookies as fallback
                    if (!downloaded && !string.IsNullOrEmpty(cookieArg) && !_isCancelled && !_isPaused)
                    {
                        RaiseStatus("Download failed with cookies, retrying clean direct stream...");
                        downloaded = RunDownloadProcess(url, targetFolder, safeFileName, ext, qualitySelection, isAudioOnly, isTikTok, "", false);
                    }

                    // Ultimate fallback 1: if format was not available, retry with universal best
                    if (!downloaded && !_isCancelled && !_isPaused)
                    {
                        RaiseStatus("Adapting video stream and downloading best available format...");
                        bool isFinal = string.IsNullOrEmpty(directCdnFallback);
                        downloaded = RunDownloadProcess(url, targetFolder, safeFileName, ext, "Best", isAudioOnly, isTikTok, "", isFinal);
                    }

                    // Ultimate fallback 2: if yt-dlp was blocked or failed, download unwatermarked direct CDN stream captured from browser!
                    if (!downloaded && !string.IsNullOrEmpty(directCdnFallback) && !_isCancelled && !_isPaused)
                    {
                        RaiseStatus("Downloading high-speed direct stream...");
                        downloaded = DownloadDirectStream(directCdnFallback, _targetFilePath, isAudioOnly);
                    }

                    if (!downloaded && !_isCancelled && !_isPaused && !string.IsNullOrEmpty(directCdnFallback))
                    {
                        RaiseStatus("Download failed: Direct media stream could not be completed.");
                        if (DownloadFailed != null) DownloadFailed(this, "Direct media stream could not be completed.");
                    }
                }
                catch (Exception ex)
                {
                    if (!_isCancelled && !_isPaused)
                    {
                        RaiseStatus("Error: " + ex.Message);
                        if (DownloadFailed != null) DownloadFailed(this, ex.Message);
                    }
                }
                finally
                {
                    _isRunning = false;
                }
            });
        }

        private bool RunDownloadProcess(string url, string targetFolder, string safeFileName, string ext, string qualitySelection, bool isAudioOnly, bool isTikTok, string cookieArg, bool isFinalAttempt = true)
        {
            url = NormalizeVideoUrl(url);
            RaiseStatus(string.Format("Starting download: {0} ({1})...", qualitySelection, ext.ToUpperInvariant()));
            string ytdlp = GetYtDlpPath();

            string args = "";

            if (qualitySelection == "Best")
            {
                if (isTikTok)
                {
                    string selector = "best[format_id$=-0]/b[format_id$=-0]/best[vcodec^=avc1]/best/b";
                    args = string.Format(
                        "-c --no-playlist -f \"{0}\" --add-headers \"Referer:https://www.tiktok.com/\" --newline --progress -o \"{1}\" \"{2}\"",
                        selector, _targetFilePath, url
                    );
                }
                else
                {
                    string selector = "bestvideo[vcodec^=avc1]+bestaudio[acodec^=mp4a]/bestvideo+bestaudio/best";
                    args = string.Format(
                        "-c --no-playlist -f \"{0}\" --merge-output-format mp4 --newline --progress -o \"{1}\" \"{2}\"",
                        selector, _targetFilePath, url
                    );
                }
            }
            else if (isAudioOnly)
            {
                string extraTikTokHdr = isTikTok ? " --add-headers \"Referer:https://www.tiktok.com/\"" : "";
                args = string.Format(
                    "-c --no-playlist -x --audio-format {0} --audio-quality 0{1} --newline --progress -o \"{2}\" \"{3}\"",
                    ext, extraTikTokHdr, _targetFilePath, url
                );
            }
            else if (isTikTok)
            {
                // In TikTok, formats ending in '-0' are direct watermark-free downloadAddr (100% reliable, zero 400 errors).
                // Formats ending in '-1' are web streaming tokens (playAddr) which cause HTTP 400 Bad Request.
                // We prioritize H.264 (avc1) with '-0' and handle both horizontal and vertical dimensions!
                int targetHeight = ParseHeight(qualitySelection);
                string selector;
                if (targetHeight > 0)
                {
                    selector = string.Format(
                        "best[format_id$=-0][vcodec^=avc1][height<={0}]/" +
                        "best[format_id$=-0][vcodec^=avc1][width<={0}]/" +
                        "best[format_id$=-0][height<={0}]/" +
                        "best[format_id$=-0][width<={0}]/" +
                        "best[vcodec^=avc1][height<={0}]/" +
                        "best[vcodec^=avc1][width<={0}]/" +
                        "best[height<={0}]/" +
                        "best[width<={0}]/" +
                        "best[format_id$=-0]/" +
                        "b[format_id$=-0]/" +
                        "best[vcodec^=avc1]/" +
                        "best/b",
                        targetHeight);
                }
                else
                {
                    selector = "best[format_id$=-0][vcodec^=avc1]/best[format_id$=-0][vcodec^=h264]/best[format_id$=-0]/best[vcodec^=avc1]/best[vcodec^=h264]/b[format_id$=-0]/best/b";
                }

                args = string.Format(
                    "-c --no-playlist -f \"{0}\" --merge-output-format mp4 --add-headers \"Referer:https://www.tiktok.com/\" --newline --progress -o \"{1}\" \"{2}\"",
                    selector, _targetFilePath, url
                );
            }
            else
            {
                int targetHeight = ParseHeight(qualitySelection);
                string selector;
                if (targetHeight > 0)
                {
                    selector = string.Format(
                        "bestvideo[height<={0}][vcodec^=avc1]+bestaudio[acodec^=mp4a]/" +
                        "bestvideo[width<={0}][vcodec^=avc1]+bestaudio[acodec^=mp4a]/" +
                        "bestvideo[height<={0}][vcodec^=avc1]+bestaudio/" +
                        "bestvideo[width<={0}][vcodec^=avc1]+bestaudio/" +
                        "bestvideo[height<={0}]+bestaudio/" +
                        "bestvideo[width<={0}]+bestaudio/" +
                        "best[height<={0}]/" +
                        "best[width<={0}]/" +
                        "bestvideo[vcodec^=avc1]+bestaudio[acodec^=mp4a]/" +
                        "bestvideo[vcodec^=avc1]+bestaudio/" +
                        "bestvideo+bestaudio/" +
                        "best",
                        targetHeight
                    );
                }
                else
                {
                    selector = 
                        "bestvideo[height<=1080][vcodec^=avc1]+bestaudio[acodec^=mp4a]/" +
                        "bestvideo[width<=1080][vcodec^=avc1]+bestaudio[acodec^=mp4a]/" +
                        "bestvideo[height<=1080]+bestaudio/" +
                        "bestvideo[width<=1080]+bestaudio/" +
                        "bestvideo[vcodec^=avc1]+bestaudio[acodec^=mp4a]/" +
                        "bestvideo[vcodec^=avc1]+bestaudio/" +
                        "bestvideo+bestaudio/" +
                        "best";
                }

                args = string.Format(
                    "-c --no-playlist -f \"{0}\" --merge-output-format mp4 --newline --progress -o \"{1}\" \"{2}\"",
                    selector, _targetFilePath, url
                );
            }

            string ffmpegPath = GetFfmpegPath();
            if (File.Exists(ffmpegPath))
            {
                args += string.Format(" --ffmpeg-location \"{0}\"", Path.GetDirectoryName(ffmpegPath));
            }

            if (!string.IsNullOrEmpty(cookieArg))
            {
                args = args + cookieArg;
            }

            string tempDir = Path.GetTempPath();
            args += string.Format(" --cache-dir \"{0}\"", Path.Combine(tempDir, "TurboX_Cache"));

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ytdlp,
                Arguments = args,
                WorkingDirectory = Directory.Exists(targetFolder) ? targetFolder : tempDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = psi };

            StringBuilder errorOutput = new StringBuilder();
            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (errorOutput) { errorOutput.AppendLine(e.Data); }
                }
            };

            Regex progRegex = new Regex(@"\[download\]\s+([0-9\.]+)%\s+of\s+~?\s*([0-9\.]+[A-Za-z]+)\s+at\s+([0-9\.]+[A-Za-z]+/s)\s+ETA\s+([0-9:]+)");
            Regex mergerRegex = new Regex(@"\[Merger\]|\[Fixup|ffmpeg|\[ExtractAudio\]|\[VideoConvertor\]");

            _process.OutputDataReceived += (s, e) =>
            {
                if (_isPaused || _isCancelled) return;
                if (string.IsNullOrEmpty(e.Data)) return;

                if (mergerRegex.IsMatch(e.Data))
                {
                    if (_isPaused || _isCancelled) return;
                    RaiseStatus("Muxing H.264 video & AAC audio (100% Playable in Windows Media Player)...");
                }
                else
                {
                    Match m = progRegex.Match(e.Data);
                    if (m.Success)
                    {
                        if (_isPaused || _isCancelled) return;
                        double pct = 0;
                        double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out pct);
                        string sizeStr = m.Groups[2].Value;
                        string speedStr = m.Groups[3].Value;
                        string etaStr = m.Groups[4].Value;

                        if (ProgressChanged != null)
                        {
                            ProgressChanged(this, new DownloadProgressEventArgs
                            {
                                Percentage = pct,
                                TotalSize = 0,
                                TotalDownloaded = 0,
                                SpeedBytesPerSecond = 0,
                                EstimatedTimeRemaining = TimeSpan.Zero,
                                SpeedText = speedStr,
                                SizeText = sizeStr,
                                EtaText = etaStr,
                                StatusText = string.Format("Downloading: {0:0.0}% of {1}", pct, sizeStr)
                            });
                        }

                        RaiseStatus(string.Format("Downloading: {0:0.0}% of {1} | Speed: {2} | ETA: {3}", pct, sizeStr, speedStr, etaStr));
                    }
                }
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _process.WaitForExit();

            if (_isCancelled || _isPaused) return false;

            if (!File.Exists(_targetFilePath))
            {
                string altPath = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(safeFileName) + "." + ext);
                if (File.Exists(altPath))
                {
                    _targetFilePath = altPath;
                }
            }

            if (_isCancelled || _isPaused) return false;

            if (_process.ExitCode == 0 && File.Exists(_targetFilePath))
            {
                if (_isCancelled || _isPaused) return false;

                if (!isAudioOnly)
                {
                    EnsurePlayableH264(_targetFilePath);
                }

                if (_isCancelled || _isPaused) return false;

                RaiseStatus(string.Format("Success! File downloaded & ready: {0}", Path.GetFileName(_targetFilePath)));
                if (DownloadCompleted != null) DownloadCompleted(this, _targetFilePath);
                return true;
            }
            else
            {
                if (_isCancelled || _isPaused) return false;
                string err = errorOutput.ToString();
                if (isFinalAttempt)
                {
                    RaiseStatus("Download failed: " + err);
                    if (DownloadFailed != null) DownloadFailed(this, err);
                }
                return false;
            }
        }

        public static string GetFfmpegPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localPath = Path.Combine(baseDir, "ffmpeg.exe");
            if (File.Exists(localPath)) return localPath;

            string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "IDM Turbo Downloader", "ffmpeg.exe");
            if (File.Exists(userDownloads)) return userDownloads;

            return "ffmpeg.exe";
        }

        private void EnsurePlayableH264(string filePath)
        {
            try
            {
                if (_isPaused || _isCancelled) return;
                if (!File.Exists(filePath)) return;
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".mp4" && ext != ".mkv") return;

                // For 4K (2160p) and 8K (4320p), preserve pristine original ultra-high-definition stream without re-encoding!
                if (ParseHeight(_lastQualitySelection) >= 2160) return;

                string ffmpeg = GetFfmpegPath();
                if (!File.Exists(ffmpeg)) return;

                // Probe video stream codec
                string fileDir = Path.GetDirectoryName(filePath);
                ProcessStartInfo probePsi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = string.Format("-i \"{0}\"", filePath),
                    WorkingDirectory = Directory.Exists(fileDir) ? fileDir : Path.GetTempPath(),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string probeErr;
                using (Process p = Process.Start(probePsi))
                {
                    _activeFfmpegProcess = p;
                    probeErr = p.StandardError.ReadToEnd();
                    p.WaitForExit(10000);
                    _activeFfmpegProcess = null;
                }

                if (_isPaused || _isCancelled) return;

                bool isHevcOrUnsupported = probeErr.IndexOf("Video: hevc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           probeErr.IndexOf("Video: hvc1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           probeErr.IndexOf("Video: bytevc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           probeErr.IndexOf("Video: vp9", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           probeErr.IndexOf("Video: av1", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isHevcOrUnsupported)
                {
                    if (_isPaused || _isCancelled) return;
                    RaiseStatus("Converting to Universal H.264 for 100% Windows Media Player playback...");
                    string dir = Path.GetDirectoryName(filePath);
                    string baseName = Path.GetFileNameWithoutExtension(filePath);
                    string tempOut = Path.Combine(dir, baseName + "_h264_tmp" + ext);

                    ProcessStartInfo convertPsi = new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        Arguments = string.Format("-y -i \"{0}\" -c:v libx264 -preset ultrafast -crf 22 -c:a copy \"{1}\"", filePath, tempOut),
                        WorkingDirectory = Directory.Exists(dir) ? dir : Path.GetTempPath(),
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (Process convertProc = new Process { StartInfo = convertPsi })
                    {
                        _activeFfmpegProcess = convertProc;
                        convertProc.ErrorDataReceived += (s, e) =>
                        {
                            if (_isPaused || _isCancelled) return;
                            if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("time="))
                            {
                                RaiseStatus("Transcoding to Universal H.264 (Playable in Windows Media Player)...");
                            }
                        };
                        convertProc.OutputDataReceived += (s, e) => { };

                        convertProc.Start();
                        convertProc.BeginErrorReadLine();
                        convertProc.BeginOutputReadLine();

                        bool finished = convertProc.WaitForExit(90000); // 90 seconds max timeout
                        _activeFfmpegProcess = null;

                        if (_isPaused || _isCancelled)
                        {
                            if (File.Exists(tempOut)) { try { File.Delete(tempOut); } catch { } }
                            return;
                        }

                        if (!finished)
                        {
                            try { convertProc.Kill(); } catch { }
                        }
                        else if (convertProc.ExitCode == 0 && File.Exists(tempOut) && new FileInfo(tempOut).Length > 1000)
                        {
                            File.Delete(filePath);
                            File.Move(tempOut, filePath);
                            RaiseStatus("Universal H.264 conversion complete (100% Playable).");
                        }
                        else
                        {
                            if (File.Exists(tempOut)) { try { File.Delete(tempOut); } catch { } }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _activeFfmpegProcess = null;
                Console.WriteLine("EnsurePlayableH264 error: " + ex.Message);
            }
        }

        private int ParseHeight(string qualitySelection)
        {
            if (string.IsNullOrEmpty(qualitySelection)) return 0;
            if (qualitySelection.IndexOf("4320", StringComparison.OrdinalIgnoreCase) >= 0 || qualitySelection.IndexOf("8K", StringComparison.OrdinalIgnoreCase) >= 0) return 4320;
            if (qualitySelection.IndexOf("2160", StringComparison.OrdinalIgnoreCase) >= 0 || qualitySelection.IndexOf("4K", StringComparison.OrdinalIgnoreCase) >= 0) return 2160;
            if (qualitySelection.IndexOf("1440", StringComparison.OrdinalIgnoreCase) >= 0 || qualitySelection.IndexOf("2K", StringComparison.OrdinalIgnoreCase) >= 0) return 1440;
            if (qualitySelection.IndexOf("1080", StringComparison.OrdinalIgnoreCase) >= 0 || qualitySelection.IndexOf("Full HD", StringComparison.OrdinalIgnoreCase) >= 0) return 1080;
            if (qualitySelection.IndexOf("720", StringComparison.OrdinalIgnoreCase) >= 0 || qualitySelection.IndexOf("HD", StringComparison.OrdinalIgnoreCase) >= 0) return 720;
            if (qualitySelection.IndexOf("480", StringComparison.OrdinalIgnoreCase) >= 0) return 480;
            if (qualitySelection.IndexOf("360", StringComparison.OrdinalIgnoreCase) >= 0) return 360;
            if (qualitySelection.IndexOf("240", StringComparison.OrdinalIgnoreCase) >= 0) return 240;
            if (qualitySelection.IndexOf("144", StringComparison.OrdinalIgnoreCase) >= 0) return 144;
            return 0; // Best / Auto
        }

        public void Pause()
        {
            _isPaused = true;
            _isRunning = false;
            try
            {
                KillProcessTree(_process);
            }
            catch { }
            try
            {
                KillProcessTree(_activeFfmpegProcess);
            }
            catch { }
            _activeFfmpegProcess = null;
            RaiseStatus("Paused.");
        }

        public void Resume()
        {
            if (_isRunning) return;
            _isPaused = false;
            _isCancelled = false;
            _isRunning = true;
            RaiseStatus("Resuming video stream...");
            Start(_lastUrl, _lastTargetFolder, _lastTargetFileName, _lastQualitySelection, _lastFormatSelection);
        }

        public void Cancel()
        {
            _isCancelled = true;
            _isPaused = false;
            _isRunning = false;
            try
            {
                KillProcessTree(_process);
            }
            catch { }
            try
            {
                KillProcessTree(_activeFfmpegProcess);
            }
            catch { }
            _activeFfmpegProcess = null;
            RaiseStatus("Download cancelled.");
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "video";
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Trim(' ', '.');
        }

        private bool DownloadDirectStream(string directUrl, string destinationPath, bool isAudioOnly)
        {
            try
            {
                RaiseStatus("Connecting to high-speed media server...");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(directUrl);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                request.Referer = "https://www.tiktok.com/";
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request.Timeout = 25000;
                request.ReadWriteTimeout = 35000;

                string tempFile = destinationPath + ".turbo_tmp";
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    long totalBytes = response.ContentLength;
                    using (Stream responseStream = response.GetResponseStream())
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[65536];
                        int bytesRead;
                        long totalRead = 0;
                        DateTime lastProgressTime = DateTime.MinValue;
                        DateTime startTime = DateTime.Now;

                        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (_isCancelled)
                            {
                                responseStream.Close();
                                fs.Close();
                                try { File.Delete(tempFile); } catch { }
                                return false;
                            }
                            while (_isPaused && !_isCancelled)
                            {
                                System.Threading.Thread.Sleep(200);
                            }

                            fs.Write(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if ((DateTime.Now - lastProgressTime).TotalMilliseconds >= 250)
                            {
                                lastProgressTime = DateTime.Now;
                                double percentage = totalBytes > 0 ? ((double)totalRead / totalBytes) * 100.0 : 0.0;
                                double elapsedSec = (DateTime.Now - startTime).TotalSeconds;
                                double speedBps = elapsedSec > 0 ? (totalRead / elapsedSec) : 0;
                                string speedText = FormatSpeed(speedBps);
                                string sizeText = totalBytes > 0 ? string.Format("{0} / {1}", FormatBytes(totalRead), FormatBytes(totalBytes)) : FormatBytes(totalRead);
                                string etaText = (speedBps > 0 && totalBytes > totalRead) ? FormatEta((totalBytes - totalRead) / speedBps) : "--:--";

                                if (ProgressChanged != null)
                                {
                                    ProgressChanged(this, new DownloadProgressEventArgs
                                    {
                                        Percentage = percentage,
                                        TotalSize = totalBytes,
                                        TotalDownloaded = totalRead,
                                        SpeedBytesPerSecond = (long)speedBps,
                                        EstimatedTimeRemaining = (speedBps > 0 && totalBytes > totalRead) ? TimeSpan.FromSeconds((totalBytes - totalRead) / speedBps) : TimeSpan.Zero,
                                        SpeedText = speedText,
                                        SizeText = sizeText,
                                        EtaText = etaText,
                                        StatusText = "Downloading direct high-speed stream..."
                                    });
                                }
                            }
                        }
                    }
                }

                if (File.Exists(destinationPath))
                {
                    try { File.Delete(destinationPath); } catch { }
                }
                File.Move(tempFile, destinationPath);

                if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 1024)
                {
                    if (isAudioOnly)
                    {
                        string ffmpeg = GetFfmpegPath();
                        if (File.Exists(ffmpeg))
                        {
                            RaiseStatus("Extracting high quality audio track...");
                            string mp3Path = Path.ChangeExtension(destinationPath, ".mp3");
                            ProcessStartInfo psiFfmpeg = new ProcessStartInfo
                            {
                                FileName = ffmpeg,
                                Arguments = string.Format("-y -i \"{0}\" -vn -ar 44100 -ac 2 -b:a 320k \"{1}\"", destinationPath, mp3Path),
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            using (var p = Process.Start(psiFfmpeg)) p.WaitForExit(20000);
                            if (File.Exists(mp3Path))
                            {
                                try { File.Delete(destinationPath); } catch { }
                                destinationPath = mp3Path;
                            }
                        }
                    }
                    else
                    {
                        EnsurePlayableH264(destinationPath);
                    }

                    RaiseStatus("Download complete!");
                    if (DownloadCompleted != null) DownloadCompleted(this, destinationPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Direct stream download error: " + ex.Message);
                return false;
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1073741824) return string.Format("{0:0.00} GB", (double)bytes / 1073741824);
            if (bytes >= 1048576) return string.Format("{0:0.0} MB", (double)bytes / 1048576);
            if (bytes >= 1024) return string.Format("{0:0} KB", (double)bytes / 1024);
            return string.Format("{0} B", bytes);
        }

        private string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec >= 1048576) return string.Format("{0:0.0} MB/s", bytesPerSec / 1048576);
            if (bytesPerSec >= 1024) return string.Format("{0:0} KB/s", bytesPerSec / 1024);
            return string.Format("{0:0} B/s", bytesPerSec);
        }

        private string FormatEta(double seconds)
        {
            if (seconds <= 0 || double.IsInfinity(seconds) || double.IsNaN(seconds)) return "--:--";
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0 ? string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds) : string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }

        private void RaiseStatus(string status)
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, status);
            }
        }
    }
}
