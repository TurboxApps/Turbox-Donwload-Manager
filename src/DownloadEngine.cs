using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IDMClone
{
    public class ChunkInfo
    {
        public int Index { get; set; }
        public long StartByte { get; set; }
        public long EndByte { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get { return EndByte - StartByte + 1; } }
        public string TempFilePath { get; set; }
        public bool IsFinished { get; set; }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public long TotalDownloaded { get; set; }
        public long TotalSize { get; set; }
        public double SpeedBytesPerSecond { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public double Percentage { get; set; }
        public string SpeedText { get; set; }
        public string SizeText { get; set; }
        public string EtaText { get; set; }
        public string StatusText { get; set; }
    }

    public class ChunkProgressEventArgs : EventArgs
    {
        public int ChunkIndex { get; set; }
        public long Downloaded { get; set; }
        public long Total { get; set; }
        public double Percentage { get; set; }
    }

    public class DownloadEngine
    {
        public event EventHandler<string> StatusChanged;
        public event EventHandler<DownloadProgressEventArgs> ProgressChanged;
        public event EventHandler<ChunkProgressEventArgs> ChunkProgressChanged;
        public event EventHandler<string> DownloadCompleted;
        public event EventHandler<string> DownloadFailed;

        public string Url { get; private set; }
        public string DestinationPath { get; private set; }
        public int NumberOfThreads { get; set; }
        public long TotalFileSize { get; private set; }
        public bool SupportsRangeRequests { get; private set; }
        public string SuggestedFileName { get; private set; }

        private volatile bool _isPaused;
        private volatile bool _isCancelled;
        private volatile bool _isRunning;
        private List<ChunkInfo> _chunks;
        private long _lastDownloadedForSpeed;
        private DateTime _lastSpeedCheckTime;
        private double _currentSpeed = 0;

        public bool IsRunning { get { return _isRunning; } }
        public bool IsPaused { get { return _isPaused; } }
        public bool IsCancelled { get { return _isCancelled; } }

        public DownloadEngine(string url, string destinationFolder, int threads = 16)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            ServicePointManager.DefaultConnectionLimit = 256;
            ThreadPool.SetMinThreads(64, 64);

            Url = url;
            NumberOfThreads = Math.Max(1, Math.Min(32, threads));
            DestinationPath = destinationFolder;
            _chunks = new List<ChunkInfo>();
        }

        private static CookieContainer _globalCookieJar = new CookieContainer();
        private readonly object _requestLock = new object();
        private readonly List<HttpWebRequest> _activeRequests = new List<HttpWebRequest>();

        private void RegisterActiveRequest(HttpWebRequest req)
        {
            if (req == null) return;
            lock (_requestLock)
            {
                _activeRequests.Add(req);
            }
        }

        private void UnregisterActiveRequest(HttpWebRequest req)
        {
            if (req == null) return;
            lock (_requestLock)
            {
                _activeRequests.Remove(req);
            }
        }

        private void AbortAllActiveRequests()
        {
            lock (_requestLock)
            {
                foreach (var req in _activeRequests)
                {
                    try { req.Abort(); } catch { }
                }
                _activeRequests.Clear();
            }
        }

        private HttpWebRequest CreateTurboRequest(string requestUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            request.Accept = "*/*";
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            if (requestUrl.IndexOf("tiktok", StringComparison.OrdinalIgnoreCase) >= 0 ||
                requestUrl.IndexOf("byteoversea", StringComparison.OrdinalIgnoreCase) >= 0 ||
                requestUrl.IndexOf("ibytedtos", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                request.Referer = "https://www.tiktok.com/";
            }
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 10;
            request.CookieContainer = _globalCookieJar;
            // Never use GZip/Deflate on byte-range downloads: it throttles media servers and breaks RFC 7233 range offsets!
            request.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
            request.Proxy = null; // Bypasses slow Windows WPAD proxy auto-detection
            request.KeepAlive = true;
            request.Pipelined = true;
            request.ServicePoint.UseNagleAlgorithm = false; // Zero-latency packet dispatch
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.ConnectionLimit = 256;
            request.ServicePoint.ReceiveBufferSize = 256 * 1024; // 256KB TCP receive buffer
            request.Timeout = 25000;
            request.ReadWriteTimeout = 25000;
            return request;
        }

        public static string GetDeterministicHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "00000000";
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 4; i++) // 8 hex characters
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public string GetPartsDirectory()
        {
            string dir = Path.GetDirectoryName(DestinationPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
            }
            string safeHash = GetDeterministicHash(DestinationPath);
            return Path.Combine(dir, ".idm_parts_" + safeHash);
        }

        private void SaveChunksMetadata(string tempDir)
        {
            try
            {
                string metaPath = Path.Combine(tempDir, "meta.idm");
                List<string> lines = new List<string>();
                lines.Add("Url=" + Url);
                lines.Add("TotalFileSize=" + TotalFileSize);
                lines.Add("NumberOfThreads=" + NumberOfThreads);
                lines.Add("ChunksCount=" + _chunks.Count);
                foreach (var c in _chunks)
                {
                    lines.Add(string.Format("{0}|{1}|{2}", c.Index, c.StartByte, c.EndByte));
                }
                File.WriteAllLines(metaPath, lines.ToArray());
            }
            catch { }
        }

        private bool LoadChunksMetadata(string tempDir)
        {
            try
            {
                string metaPath = Path.Combine(tempDir, "meta.idm");
                if (!File.Exists(metaPath)) return false;

                string[] lines = File.ReadAllLines(metaPath);
                long totalSize = 0;
                int threads = 0;
                List<ChunkInfo> loadedChunks = new List<ChunkInfo>();

                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("TotalFileSize="))
                    {
                        long.TryParse(line.Substring("TotalFileSize=".Length), out totalSize);
                    }
                    else if (line.StartsWith("NumberOfThreads="))
                    {
                        int.TryParse(line.Substring("NumberOfThreads=".Length), out threads);
                    }
                    else if (line.Contains("|"))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 3)
                        {
                            int idx;
                            long start, end;
                            if (int.TryParse(parts[0], out idx) && long.TryParse(parts[1], out start) && long.TryParse(parts[2], out end))
                            {
                                string pPath = Path.Combine(tempDir, string.Format("p_{0}.tmp", idx));
                                long downloaded = 0;
                                if (File.Exists(pPath))
                                {
                                    downloaded = new FileInfo(pPath).Length;
                                }
                                loadedChunks.Add(new ChunkInfo
                                {
                                    Index = idx,
                                    StartByte = start,
                                    EndByte = end,
                                    DownloadedBytes = downloaded,
                                    TempFilePath = pPath,
                                    IsFinished = (downloaded >= (end - start + 1))
                                });
                            }
                        }
                    }
                }

                if (totalSize > 0 && loadedChunks.Count > 0)
                {
                    TotalFileSize = totalSize;
                    SupportsRangeRequests = true;
                    if (threads > 0) NumberOfThreads = threads;
                    _chunks = loadedChunks;
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool PerformFetchMetadataSync(out string error)
        {
            error = null;
            try
            {
                // Check if existing chunk metadata exists first
                string tempDir = GetPartsDirectory();
                if (File.Exists(Path.Combine(tempDir, "meta.idm")))
                {
                    if (LoadChunksMetadata(tempDir))
                    {
                        RaiseStatus("Found existing download session. Resume ready.");
                        return true;
                    }
                }

                RaiseStatus("Connecting to server (Turbo Handshake)...");
                string dispositionFileName = null;
                long contentLength = -1;
                bool acceptsRanges = false;

                // Step 1: Range Probe (bytes 0-1) - The golden standard for range verification
                try
                {
                    HttpWebRequest rangeReq = CreateTurboRequest(Url);
                    rangeReq.Method = "GET";
                    rangeReq.AddRange(0, 1);

                    using (HttpWebResponse rangeResp = (HttpWebResponse)rangeReq.GetResponse())
                    {
                        string cd = rangeResp.Headers["Content-Disposition"];
                        dispositionFileName = ExtractFileName(cd);

                        if (rangeResp.StatusCode == HttpStatusCode.PartialContent)
                        {
                            acceptsRanges = true;
                            string cr = rangeResp.Headers["Content-Range"];
                            if (!string.IsNullOrEmpty(cr) && cr.Contains("/"))
                            {
                                string totalStr = cr.Substring(cr.LastIndexOf('/') + 1).Trim();
                                long parsedLen;
                                if (long.TryParse(totalStr, out parsedLen) && parsedLen > 0)
                                {
                                    contentLength = parsedLen;
                                }
                            }
                        }
                        else if (rangeResp.StatusCode == HttpStatusCode.OK)
                        {
                            // Server returned 200 OK directly, range header was ignored
                            contentLength = rangeResp.ContentLength;
                            string ar = rangeResp.Headers["Accept-Ranges"];
                            if (!string.IsNullOrEmpty(ar) && ar.ToLower().Contains("bytes"))
                            {
                                acceptsRanges = true;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to HEAD request if Range GET failed (e.g. server blocks 0-1 probe)
                    try
                    {
                        HttpWebRequest headReq = CreateTurboRequest(Url);
                        headReq.Method = "HEAD";
                        using (HttpWebResponse headResp = (HttpWebResponse)headReq.GetResponse())
                        {
                            contentLength = headResp.ContentLength;
                            string ar = headResp.Headers["Accept-Ranges"];
                            acceptsRanges = !string.IsNullOrEmpty(ar) && ar.ToLower().Contains("bytes");
                            string cd = headResp.Headers["Content-Disposition"];
                            if (string.IsNullOrEmpty(dispositionFileName)) dispositionFileName = ExtractFileName(cd);
                        }
                    }
                    catch
                    {
                        // Fallback to pure GET request without reading full stream
                        try
                        {
                            HttpWebRequest pureReq = CreateTurboRequest(Url);
                            pureReq.Method = "GET";
                            using (HttpWebResponse pureResp = (HttpWebResponse)pureReq.GetResponse())
                            {
                                contentLength = pureResp.ContentLength;
                                string ar = pureResp.Headers["Accept-Ranges"];
                                if (!string.IsNullOrEmpty(ar) && ar.ToLower().Contains("bytes")) acceptsRanges = true;
                                string cd = pureResp.Headers["Content-Disposition"];
                                if (string.IsNullOrEmpty(dispositionFileName)) dispositionFileName = ExtractFileName(cd);
                            }
                        }
                        catch { }
                    }
                }

                TotalFileSize = contentLength;
                SupportsRangeRequests = acceptsRanges && contentLength > 0;

                // 1. Check query parameter filename if disposition is not present or token-like
                if (string.IsNullOrEmpty(dispositionFileName) || dispositionFileName.Length > 50)
                {
                    try
                    {
                        Match mFn = Regex.Match(Url, @"[?&](?:filename|file|title|name|dn)=([^&]+)", RegexOptions.IgnoreCase);
                        if (mFn.Success)
                        {
                            string candidate = Uri.UnescapeDataString(mFn.Groups[1].Value.Replace('+', ' '));
                            if (!string.IsNullOrEmpty(candidate)) dispositionFileName = candidate;
                        }
                    }
                    catch { }
                }

                // 2. Check path segment
                if (string.IsNullOrEmpty(dispositionFileName))
                {
                    try
                    {
                        Uri uri = new Uri(Url);
                        string path = uri.AbsolutePath.Trim('/');
                        if (!string.IsNullOrEmpty(path))
                        {
                            dispositionFileName = Path.GetFileName(path);
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(dispositionFileName))
                {
                    dispositionFileName = "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bin";
                }

                SuggestedFileName = MakeSafeFileName(dispositionFileName);
                RaiseStatus("Connected. Ready for high-speed download.");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                RaiseStatus("Connection notice: " + ex.Message);
                return false;
            }
        }

        public void FetchMetadata(Action<bool, string> onComplete)
        {
            Task.Factory.StartNew(() =>
            {
                string err;
                bool ok = PerformFetchMetadataSync(out err);
                if (onComplete != null) onComplete(ok, err);
            });
        }

        public static string MakeSafeFileName(string name, int maxLen = 75, string defaultExt = ".bin")
        {
            if (string.IsNullOrEmpty(name)) return "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + defaultExt;

            try { name = Uri.UnescapeDataString(name); } catch { }

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid) name = name.Replace(c, '_');

            name = name.Trim().TrimEnd('.');
            if (string.IsNullOrEmpty(name)) return "download" + defaultExt;

            string ext = "";
            try { ext = Path.GetExtension(name); } catch { }
            string baseName = name;
            if (!string.IsNullOrEmpty(ext))
            {
                try { baseName = Path.GetFileNameWithoutExtension(name); } catch { }
            }

            if (baseName.Length > maxLen)
            {
                baseName = baseName.Substring(0, maxLen).TrimEnd('_', '-', ' ');
            }

            if (string.IsNullOrEmpty(ext))
            {
                ext = defaultExt;
            }

            return baseName + ext;
        }

        private string ExtractFileName(string contentDisposition)
        {
            if (string.IsNullOrEmpty(contentDisposition)) return null;

            // 1. Check filename*=UTF-8''... (RFC 5987 / 6266)
            string keyUtf = "filename*=";
            int idxUtf = contentDisposition.IndexOf(keyUtf, StringComparison.OrdinalIgnoreCase);
            if (idxUtf >= 0)
            {
                string fn = contentDisposition.Substring(idxUtf + keyUtf.Length).Trim(' ', '"', ';');
                int nextSemi = fn.IndexOf(';');
                if (nextSemi >= 0) fn = fn.Substring(0, nextSemi).Trim(' ', '"');
                if (fn.Contains("''")) fn = fn.Substring(fn.IndexOf("''") + 2);
                try { fn = Uri.UnescapeDataString(fn); } catch { }
                return Path.GetFileName(fn);
            }

            // 2. Check standard filename="..."
            string key = "filename=";
            int idx = contentDisposition.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                string fn = contentDisposition.Substring(idx + key.Length).Trim(' ', '"', ';');
                int nextSemi = fn.IndexOf(';');
                if (nextSemi >= 0) fn = fn.Substring(0, nextSemi).Trim(' ', '"');
                return Path.GetFileName(fn);
            }
            return null;
        }

        public void Start(string targetFilePath)
        {
            if (_isRunning) return;

            _isPaused = false;
            _isCancelled = false;
            _isRunning = true;

            try
            {
                string dir = Path.GetDirectoryName(targetFilePath);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                }
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string fn = Path.GetFileName(targetFilePath);
                fn = MakeSafeFileName(fn);
                DestinationPath = Path.Combine(dir, fn);
            }
            catch
            {
                DestinationPath = targetFilePath;
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // First check if parts already exist on disk
                    string pDir = GetPartsDirectory();
                    if (_chunks.Count == 0 && File.Exists(Path.Combine(pDir, "meta.idm")))
                    {
                        LoadChunksMetadata(pDir);
                    }

                    if (TotalFileSize <= 0 || !SupportsRangeRequests)
                    {
                        string metaErr;
                        PerformFetchMetadataSync(out metaErr);
                    }

                    // Check if DestinationPath has existing partial file and pDir has no parts
                    long destFileSize = 0;
                    if (File.Exists(DestinationPath))
                    {
                        try { destFileSize = new FileInfo(DestinationPath).Length; } catch { }
                    }

                    if (destFileSize > 0 && TotalFileSize > 0 && ProgressChanged != null)
                    {
                        double p = Math.Min(100.0, ((double)destFileSize / TotalFileSize) * 100.0);
                        ProgressChanged(this, new DownloadProgressEventArgs
                        {
                            TotalDownloaded = destFileSize,
                            TotalSize = TotalFileSize,
                            Percentage = p,
                            SpeedBytesPerSecond = 0,
                            EstimatedTimeRemaining = TimeSpan.Zero
                        });
                    }

                    bool hasExistingParts = Directory.Exists(pDir) && Directory.GetFiles(pDir, "p_*.tmp").Length > 0;

                    if (destFileSize > 0 && !hasExistingParts && (TotalFileSize <= 0 || destFileSize < TotalFileSize))
                    {
                        // File was partially downloaded in single-threaded mode; continue directly with single-threaded mode to preserve existing data!
                        DownloadSingleThreaded();
                    }
                    else if (SupportsRangeRequests && NumberOfThreads > 1 && TotalFileSize > (1024 * 100))
                    {
                        DownloadMultiThreaded();
                    }
                    else
                    {
                        DownloadSingleThreaded();
                    }
                }
                catch (Exception ex)
                {
                    _isRunning = false;
                    if (!_isCancelled && !_isPaused)
                    {
                        RaiseStatus("Error: " + ex.Message);
                        if (DownloadFailed != null) DownloadFailed(this, ex.Message);
                    }
                }
            });
        }

        public void Pause()
        {
            _isPaused = true;
            _isRunning = false;
            AbortAllActiveRequests();
            RaiseStatus("Paused.");
        }

        public void Resume()
        {
            if (_isRunning) return;
            Start(DestinationPath);
        }

        public void Cancel()
        {
            _isCancelled = true;
            _isPaused = false;
            _isRunning = false;
            AbortAllActiveRequests();
            RaiseStatus("Cancelled.");
        }

        private void DownloadMultiThreaded()
        {
            RaiseStatus(string.Format("Downloading in {0} parallel turbo connections...", NumberOfThreads));

            string tempDir = GetPartsDirectory();
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            if (_chunks.Count == 0)
            {
                if (!LoadChunksMetadata(tempDir))
                {
                    long chunkSize = TotalFileSize / NumberOfThreads;
                    for (int i = 0; i < NumberOfThreads; i++)
                    {
                        long start = i * chunkSize;
                        long end = (i == NumberOfThreads - 1) ? (TotalFileSize - 1) : ((i + 1) * chunkSize - 1);
                        ChunkInfo chunk = new ChunkInfo
                        {
                            Index = i,
                            StartByte = start,
                            EndByte = end,
                            DownloadedBytes = 0,
                            TempFilePath = Path.Combine(tempDir, string.Format("p_{0}.tmp", i)),
                            IsFinished = false
                        };
                        _chunks.Add(chunk);
                    }
                    SaveChunksMetadata(tempDir);
                }
            }

            foreach (var chunk in _chunks)
            {
                if (File.Exists(chunk.TempFilePath))
                {
                    FileInfo fi = new FileInfo(chunk.TempFilePath);
                    chunk.DownloadedBytes = fi.Length;
                    if (chunk.DownloadedBytes >= chunk.TotalBytes)
                    {
                        chunk.IsFinished = true;
                    }
                }
            }

            // Immediately report chunk statuses to UI so resume progress is shown instantly
            foreach (var c in _chunks)
            {
                if (ChunkProgressChanged != null)
                {
                    double cpct = c.TotalBytes > 0 ? ((double)c.DownloadedBytes / c.TotalBytes * 100.0) : 0;
                    ChunkProgressChanged(this, new ChunkProgressEventArgs
                    {
                        ChunkIndex = c.Index,
                        Downloaded = c.DownloadedBytes,
                        Total = c.TotalBytes,
                        Percentage = Math.Min(100.0, cpct)
                    });
                }
            }

            _lastSpeedCheckTime = DateTime.UtcNow;
            _lastDownloadedForSpeed = GetTotalDownloadedBytes();
            ReportOverallProgress();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < _chunks.Count; i++)
            {
                int chunkIndex = i;
                if (!_chunks[chunkIndex].IsFinished)
                {
                    tasks.Add(Task.Factory.StartNew(() => DownloadChunk(chunkIndex), TaskCreationOptions.LongRunning));
                }
            }

            while (_isRunning && !_isPaused && !_isCancelled)
            {
                bool allDone = true;
                foreach (var c in _chunks)
                {
                    if (!c.IsFinished) { allDone = false; break; }
                }

                ReportOverallProgress();
                if (allDone) break;
                Thread.Sleep(200);
            }

            if (_isPaused || _isCancelled) return;

            try { Task.WaitAll(tasks.ToArray()); } catch { }

            // Verify all chunks are 100% completed before merging
            bool allComplete = true;
            foreach (var c in _chunks)
            {
                if (!c.IsFinished || c.DownloadedBytes < c.TotalBytes)
                {
                    allComplete = false;
                    break;
                }
            }

            if (!allComplete)
            {
                RaiseStatus("Some chunks incomplete, will continue on next resume.");
                return;
            }

            RaiseStatus("Merging 32 turbo parts into destination file...");
            MergeChunks(tempDir);

            _isRunning = false;
            RaiseStatus("Download complete!");
            if (DownloadCompleted != null) DownloadCompleted(this, DestinationPath);
        }

        private void DownloadChunk(int chunkIndex)
        {
            ChunkInfo chunk = _chunks[chunkIndex];

            int retries = 0;
            while (retries < 5 && !chunk.IsFinished && !_isPaused && !_isCancelled)
            {
                // Always sync with actual file length on disk before resuming
                if (File.Exists(chunk.TempFilePath))
                {
                    chunk.DownloadedBytes = new FileInfo(chunk.TempFilePath).Length;
                }

                long currentStart = chunk.StartByte + chunk.DownloadedBytes;
                if (currentStart > chunk.EndByte || chunk.DownloadedBytes >= chunk.TotalBytes)
                {
                    chunk.IsFinished = true;
                    return;
                }

                HttpWebRequest request = null;
                try
                {
                    request = CreateTurboRequest(Url);
                    request.Method = "GET";
                    request.AddRange(currentStart, chunk.EndByte);
                    RegisterActiveRequest(request);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode != HttpStatusCode.PartialContent && response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception("Unexpected HTTP status: " + response.StatusCode);
                        }

                        // If server returns 200 OK when range was requested, server does not support ranges
                        if (response.StatusCode == HttpStatusCode.OK && currentStart > chunk.StartByte)
                        {
                            throw new Exception("Server ignored range header and returned 200 OK.");
                        }

                        using (Stream stream = response.GetResponseStream())
                        using (FileStream fs = new FileStream(chunk.TempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            byte[] buffer = new byte[256 * 1024]; // 256KB High-Performance Turbo Buffer
                            int bytesRead;

                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (_isPaused || _isCancelled) return;

                                fs.Write(buffer, 0, bytesRead);
                                chunk.DownloadedBytes += bytesRead;

                                double chunkPct = (double)chunk.DownloadedBytes / chunk.TotalBytes * 100.0;
                                if (ChunkProgressChanged != null)
                                {
                                    ChunkProgressChanged(this, new ChunkProgressEventArgs
                                    {
                                        ChunkIndex = chunkIndex,
                                        Downloaded = chunk.DownloadedBytes,
                                        Total = chunk.TotalBytes,
                                        Percentage = Math.Min(100.0, chunkPct)
                                    });
                                }
                            }
                        }
                    }

                    if (chunk.DownloadedBytes >= chunk.TotalBytes)
                    {
                        chunk.IsFinished = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (_isPaused || _isCancelled) return;
                    retries++;
                    RaiseStatus(string.Format("Part {0} retry {1}/5: {2}", chunkIndex + 1, retries, ex.Message));
                    Thread.Sleep(1000);
                }
                finally
                {
                    if (request != null) UnregisterActiveRequest(request);
                }
            }
        }

        private void DownloadSingleThreaded()
        {
            RaiseStatus("Downloading with turbo stream...");

            long existingBytes = 0;
            if (File.Exists(DestinationPath))
            {
                try { existingBytes = new FileInfo(DestinationPath).Length; } catch { }
            }

            if (TotalFileSize > 0 && existingBytes >= TotalFileSize)
            {
                _isRunning = false;
                RaiseStatus("Download complete!");
                if (DownloadCompleted != null) DownloadCompleted(this, DestinationPath);
                return;
            }

            HttpWebRequest request = null;
            try
            {
                request = CreateTurboRequest(Url);
                request.Method = "GET";

                bool requestedResume = false;
                if (existingBytes > 0 && (TotalFileSize <= 0 || existingBytes < TotalFileSize))
                {
                    request.AddRange(existingBytes);
                    requestedResume = true;
                }

                RegisterActiveRequest(request);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    FileMode mode = FileMode.Create;
                    long currentDownloaded = 0;
                    bool needsStreamFastForward = false;

                    if (requestedResume && response.StatusCode == HttpStatusCode.PartialContent)
                    {
                        mode = FileMode.Append;
                        currentDownloaded = existingBytes;
                        RaiseStatus(string.Format("Resuming stream from {0:F1} MB...", existingBytes / 1048576.0));
                    }
                    else if (requestedResume && response.StatusCode == HttpStatusCode.OK && existingBytes > 0)
                    {
                        // Server ignored Range header and sends 200 OK from byte 0.
                        // NEVER TRUNCATE! Preserve existing bytes on disk and fast-forward stream skip.
                        mode = FileMode.Append;
                        currentDownloaded = existingBytes;
                        needsStreamFastForward = true;
                        RaiseStatus(string.Format("Syncing stream with existing {0:F1} MB...", existingBytes / 1048576.0));
                    }
                    else
                    {
                        mode = FileMode.Create;
                        currentDownloaded = 0;
                    }

                    long totalBytesToRead = TotalFileSize;
                    if (totalBytesToRead <= 0)
                    {
                        string cr = response.Headers["Content-Range"];
                        if (!string.IsNullOrEmpty(cr) && cr.Contains("/"))
                        {
                            string totalStr = cr.Substring(cr.LastIndexOf('/') + 1).Trim();
                            long parsedLen;
                            if (long.TryParse(totalStr, out parsedLen) && parsedLen > 0)
                            {
                                totalBytesToRead = parsedLen;
                                TotalFileSize = parsedLen;
                            }
                        }

                        if (totalBytesToRead <= 0 && response.ContentLength > 0)
                        {
                            if (response.StatusCode == HttpStatusCode.PartialContent)
                            {
                                totalBytesToRead = currentDownloaded + response.ContentLength;
                            }
                            else
                            {
                                totalBytesToRead = response.ContentLength;
                            }
                            TotalFileSize = totalBytesToRead;
                        }
                    }

                    _lastSpeedCheckTime = DateTime.UtcNow;
                    _lastDownloadedForSpeed = currentDownloaded;

                    if (currentDownloaded > 0 && ProgressChanged != null)
                    {
                        double initPct = totalBytesToRead > 0 ? ((double)currentDownloaded / totalBytesToRead * 100.0) : 0;
                        ProgressChanged(this, new DownloadProgressEventArgs
                        {
                            TotalDownloaded = currentDownloaded,
                            TotalSize = totalBytesToRead,
                            SpeedBytesPerSecond = 0,
                            EstimatedTimeRemaining = TimeSpan.Zero,
                            Percentage = Math.Min(100.0, Math.Max(0.0, initPct))
                        });
                    }

                    using (Stream stream = response.GetResponseStream())
                    {
                        if (needsStreamFastForward)
                        {
                            long skippedBytes = 0;
                            byte[] skipBuffer = new byte[512 * 1024]; // 512KB Turbo skip buffer
                            DateTime lastSkipReport = DateTime.UtcNow;
                            DateTime skipStartTime = DateTime.UtcNow;

                            while (skippedBytes < existingBytes)
                            {
                                if (_isPaused || _isCancelled) return;
                                long toRead = Math.Min(skipBuffer.Length, existingBytes - skippedBytes);
                                int r = stream.Read(skipBuffer, 0, (int)toRead);
                                if (r <= 0) break;
                                skippedBytes += r;

                                DateTime now = DateTime.UtcNow;
                                if ((now - lastSkipReport).TotalMilliseconds >= 200)
                                {
                                    lastSkipReport = now;
                                    double elapsedSec = (now - skipStartTime).TotalSeconds;
                                    double skipSpeed = elapsedSec > 0 ? (skippedBytes / elapsedSec) : 0;
                                    _currentSpeed = skipSpeed;

                                    double pct = totalBytesToRead > 0 ? ((double)skippedBytes / totalBytesToRead * 100.0) : 0;
                                    RaiseStatus(string.Format("Fast-syncing stream: {0:F1} MB / {1:F1} MB ({2:0.0}%)...",
                                        skippedBytes / 1048576.0, existingBytes / 1048576.0, (double)skippedBytes / existingBytes * 100.0));

                                    if (ProgressChanged != null)
                                    {
                                        ProgressChanged(this, new DownloadProgressEventArgs
                                        {
                                            TotalDownloaded = skippedBytes,
                                            TotalSize = totalBytesToRead,
                                            SpeedBytesPerSecond = skipSpeed,
                                            EstimatedTimeRemaining = TimeSpan.Zero,
                                            Percentage = Math.Min(100.0, Math.Max(0.0, pct))
                                        });
                                    }
                                }
                            }

                            if (_isPaused || _isCancelled) return;
                            currentDownloaded = existingBytes;
                            RaiseStatus(string.Format("Synced at {0:F1} MB! Continuing download...", existingBytes / 1048576.0));
                        }

                        using (FileStream fs = new FileStream(DestinationPath, mode, FileAccess.Write, FileShare.ReadWrite))
                        {
                            byte[] buffer = new byte[256 * 1024]; // 256KB Turbo buffer
                            int bytesRead;
                            DateTime lastReportTime = DateTime.UtcNow;

                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (_isPaused || _isCancelled) return;

                                fs.Write(buffer, 0, bytesRead);
                                currentDownloaded += bytesRead;

                                DateTime now = DateTime.UtcNow;
                                if ((now - lastReportTime).TotalMilliseconds >= 250)
                                {
                                    double elapsedSeconds = (now - _lastSpeedCheckTime).TotalSeconds;
                                    if (elapsedSeconds >= 0.2)
                                    {
                                        double bytesDelta = currentDownloaded - _lastDownloadedForSpeed;
                                        if (bytesDelta >= 0)
                                        {
                                            double instantSpeed = bytesDelta / elapsedSeconds;
                                            if (_currentSpeed <= 0) _currentSpeed = instantSpeed;
                                            else _currentSpeed = (_currentSpeed * 0.65) + (instantSpeed * 0.35);
                                        }
                                        _lastDownloadedForSpeed = currentDownloaded;
                                        _lastSpeedCheckTime = now;
                                    }

                                    double pct = totalBytesToRead > 0 ? ((double)currentDownloaded / totalBytesToRead * 100.0) : 0;
                                    TimeSpan eta = (_currentSpeed > 512 && totalBytesToRead > currentDownloaded)
                                        ? TimeSpan.FromSeconds((totalBytesToRead - currentDownloaded) / _currentSpeed)
                                        : TimeSpan.Zero;

                                    lastReportTime = now;

                                    if (ProgressChanged != null)
                                    {
                                        ProgressChanged(this, new DownloadProgressEventArgs
                                        {
                                            TotalDownloaded = currentDownloaded,
                                            TotalSize = totalBytesToRead,
                                            SpeedBytesPerSecond = _currentSpeed,
                                            EstimatedTimeRemaining = eta,
                                            Percentage = Math.Min(100.0, Math.Max(0.0, pct))
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isPaused && !_isCancelled)
                {
                    throw ex;
                }
            }
            finally
            {
                if (request != null) UnregisterActiveRequest(request);
            }

            if (_isPaused || _isCancelled) return;

            _isRunning = false;
            RaiseStatus("Download complete!");
            if (DownloadCompleted != null) DownloadCompleted(this, DestinationPath);
        }

        private void MergeChunks(string tempDir)
        {
            if (File.Exists(DestinationPath))
            {
                try { File.Delete(DestinationPath); } catch { }
            }

            using (FileStream destStream = new FileStream(DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[256 * 1024]; // 256KB merge buffer
                foreach (var chunk in _chunks)
                {
                    if (File.Exists(chunk.TempFilePath))
                    {
                        using (FileStream partStream = new FileStream(chunk.TempFilePath, FileMode.Open, FileAccess.Read))
                        {
                            int read;
                            while ((read = partStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                destStream.Write(buffer, 0, read);
                            }
                        }
                    }
                }
            }

            CleanupParts();
        }

        public void CleanupParts()
        {
            try
            {
                foreach (var chunk in _chunks)
                {
                    if (File.Exists(chunk.TempFilePath))
                    {
                        try { File.Delete(chunk.TempFilePath); } catch { }
                    }
                }
                string tempDir = GetPartsDirectory();
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
            catch { }
        }

        private long GetTotalDownloadedBytes()
        {
            long sum = 0;
            foreach (var c in _chunks) sum += c.DownloadedBytes;
            return sum;
        }

        private void ReportOverallProgress()
        {
            long downloaded = GetTotalDownloadedBytes();
            DateTime now = DateTime.UtcNow;
            double elapsed = (now - _lastSpeedCheckTime).TotalSeconds;

            if (elapsed >= 0.25)
            {
                double bytesDelta = downloaded - _lastDownloadedForSpeed;
                if (bytesDelta >= 0)
                {
                    double instantSpeed = bytesDelta / elapsed;
                    if (_currentSpeed <= 0)
                        _currentSpeed = instantSpeed;
                    else
                        _currentSpeed = (_currentSpeed * 0.65) + (instantSpeed * 0.35);
                }
                _lastDownloadedForSpeed = downloaded;
                _lastSpeedCheckTime = now;
            }

            double pct = TotalFileSize > 0 ? ((double)downloaded / TotalFileSize * 100.0) : 0;
            TimeSpan eta = (!_isPaused && _currentSpeed > 512 && TotalFileSize > downloaded)
                ? TimeSpan.FromSeconds((TotalFileSize - downloaded) / _currentSpeed)
                : TimeSpan.Zero;

            if (ProgressChanged != null)
            {
                ProgressChanged(this, new DownloadProgressEventArgs
                {
                    TotalDownloaded = downloaded,
                    TotalSize = TotalFileSize,
                    SpeedBytesPerSecond = _isPaused ? 0 : _currentSpeed,
                    EstimatedTimeRemaining = eta,
                    Percentage = Math.Min(100.0, Math.Max(0.0, pct))
                });
            }
        }

        private void RaiseStatus(string status)
        {
            if (StatusChanged != null) StatusChanged(this, status);
        }
    }
}
