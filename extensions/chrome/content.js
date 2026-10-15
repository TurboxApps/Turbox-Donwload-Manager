// Turbo X Content Script - Universal 8K/4K Video & Audio Downloader
(function () {
  const QUALITIES = [
    { label: "8K Ultra HD (4320p)", short: "8K", tag: "ULTRA 8K", color: "#ec4899", desc: "Best Quality • Ultra 8K" },
    { label: "4K Ultra HD (2160p)", short: "4K", tag: "4K UHD", color: "#38bdf8", desc: "Best Quality • 2160p" },
    { label: "2K Quad HD (1440p)",  short: "2K", tag: "2K QHD", color: "#818cf8", desc: "High Def • 1440p" },
    { label: "1080p Full HD",       short: "1080p", tag: "1080p", color: "#4ade80", desc: "Full HD • 60fps" },
    { label: "720p HD",             short: "720p", tag: "720p", color: "#fbbf24", desc: "Standard HD • 720p" },
    { label: "Audio Only (MP3)",    short: "MP3", tag: "AUDIO", color: "#f43f5e", desc: "MP3 320kbps Audio", format: "mp3" }
  ];

  let activeFlyout = null;
  let activeFloatingBadge = null;

  // Listen for YouTube Single-Page-App navigation
  window.addEventListener("yt-navigate-finish", () => {
    setTimeout(scanForVideos, 500);
  });
  window.addEventListener("popstate", () => {
    setTimeout(scanForVideos, 500);
  });

  // Inject or attach floating download button to video players
  function scanForVideos() {
    const videos = document.querySelectorAll("video");
    videos.forEach((vid) => {
      if (vid.dataset.turboxBound) return;
      vid.dataset.turboxBound = "true";

      // Show floating Turbo X button on hover
      vid.addEventListener("mouseenter", () => handleVideoHover(vid));
    });

    // Special dedicated injector for YouTube
    if (window.location.hostname.includes("youtube.com")) {
      injectYouTubePlayerBar();
    }
  }

  // YouTube Dedicated Player Bar Button
  function injectYouTubePlayerBar() {
    if (document.getElementById("turbox-yt-bar-btn")) return;

    // Look for YouTube control bar or owner action bar
    const controls = document.querySelector(".ytp-right-controls") || 
                     document.querySelector("#top-level-buttons-computed") ||
                     document.querySelector("#actions-inner");
    if (!controls) return;

    const ytBtn = document.createElement("button");
    ytBtn.id = "turbox-yt-bar-btn";
    ytBtn.className = "ytp-button turbox-yt-btn";
    ytBtn.title = "Download Video with Turbo X (8K, 4K, 1080p, MP3)";
    ytBtn.innerHTML = `
      <div style="display: flex; align-items: center; justify-content: center; height: 100%; gap: 4px; padding: 0 8px; cursor: pointer;">
        <span style="color: #fbbf24; font-size: 15px; font-weight: bold; text-shadow: 0 0 8px rgba(251, 191, 36, 0.6);">⚡</span>
        <span style="font-size: 12px; font-weight: 700; color: #38bdf8; letter-spacing: 0.5px; font-family: 'Segoe UI', Roboto, sans-serif;">Turbo X</span>
      </div>
    `;

    Object.assign(ytBtn.style, {
      background: "rgba(11, 15, 23, 0.85)",
      border: "1px solid rgba(56, 189, 248, 0.4)",
      borderRadius: "6px",
      marginRight: "8px",
      height: "30px",
      alignSelf: "center",
      cursor: "pointer",
      boxShadow: "0 2px 8px rgba(0,0,0,0.5)",
      transition: "all 0.2s ease"
    });

    ytBtn.addEventListener("mouseenter", () => {
      ytBtn.style.borderColor = "#38bdf8";
      ytBtn.style.boxShadow = "0 0 12px rgba(56, 189, 248, 0.5)";
    });

    ytBtn.addEventListener("mouseleave", () => {
      ytBtn.style.borderColor = "rgba(56, 189, 248, 0.4)";
      ytBtn.style.boxShadow = "0 2px 8px rgba(0,0,0,0.5)";
    });

    ytBtn.addEventListener("click", (e) => {
      e.stopPropagation();
      e.preventDefault();
      const rect = ytBtn.getBoundingClientRect();
      openQualityFlyout(rect.left, rect.top - 10, window.location.href, document.title);
    });

    controls.prepend(ytBtn);
  }

  // Handle hover on standard HTML5 video elements (Facebook, TikTok, Twitter, Vimeo, Hoichoi, etc.)
  function handleVideoHover(videoElement) {
    const rect = videoElement.getBoundingClientRect();
    if (rect.width < 140 || rect.height < 90) return; // Skip tiny thumbnails

    if (activeFloatingBadge && activeFloatingBadge.parentElement) {
      activeFloatingBadge.remove();
    }

    const badge = document.createElement("div");
    badge.className = "turbox-video-hover-badge";
    badge.innerHTML = `
      <span style="color: #fbbf24; font-size: 14px; margin-right: 5px;">⚡</span>
      <span style="font-weight: 700; font-size: 12px; color: #fff; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;">Download</span>
      <span style="background: rgba(56, 189, 248, 0.2); color: #38bdf8; font-size: 10px; font-weight: 800; padding: 2px 6px; border-radius: 4px; margin-left: 6px; border: 1px solid rgba(56, 189, 248, 0.4);">4K/8K ▼</span>
    `;

    Object.assign(badge.style, {
      position: "fixed",
      top: `${Math.max(12, rect.top + 12)}px`,
      right: `${Math.max(12, window.innerWidth - rect.right + 12)}px`,
      zIndex: "2147483647",
      background: "rgba(10, 14, 22, 0.94)",
      padding: "6px 12px",
      borderRadius: "8px",
      cursor: "pointer",
      display: "flex",
      alignItems: "center",
      boxShadow: "0 6px 20px rgba(0, 0, 0, 0.7)",
      border: "1px solid rgba(56, 189, 248, 0.45)",
      backdropFilter: "blur(8px)",
      userSelect: "none",
      transition: "transform 0.15s ease, border-color 0.15s ease"
    });

    badge.addEventListener("mouseenter", () => {
      badge.style.transform = "scale(1.04)";
      badge.style.borderColor = "#38bdf8";
    });

    badge.addEventListener("mouseleave", () => {
      badge.style.transform = "scale(1)";
    });

    badge.addEventListener("click", (e) => {
      e.stopPropagation();
      e.preventDefault();

      let targetUrl = videoElement.currentSrc || videoElement.src;
      if (!targetUrl || targetUrl.startsWith("blob:") || targetUrl.includes("manifest") || targetUrl.includes("m3u8")) {
        targetUrl = window.location.href;
      }

      const bRect = badge.getBoundingClientRect();
      openQualityFlyout(bRect.left - 120, bRect.bottom + 6, targetUrl, document.title);
    });

    document.body.appendChild(badge);
    activeFloatingBadge = badge;

    const timer = setTimeout(() => {
      if (badge.parentElement && !activeFlyout) badge.remove();
    }, 4500);

    badge.addEventListener("mouseenter", () => clearTimeout(timer));
  }

  // Interactive High-End Neon Gaming Quality Flyout Menu
  function openQualityFlyout(x, y, targetUrl, title) {
    if (activeFlyout && activeFlyout.parentElement) {
      activeFlyout.remove();
    }

    const flyout = document.createElement("div");
    flyout.className = "turbox-quality-flyout";

    // Build quality options rows
    let rowsHtml = "";
    QUALITIES.forEach((q) => {
      rowsHtml += `
        <div class="turbox-quality-row" data-quality="${q.short}" data-format="${q.format || 'mp4'}" style="display: flex; align-items: center; justify-content: space-between; padding: 7px 10px; border-radius: 6px; cursor: pointer; margin-bottom: 3px; transition: background 0.15s ease;">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span style="font-size: 13px; font-weight: 700; color: #fff;">${q.label}</span>
          </div>
          <span style="font-size: 9.5px; font-weight: 800; padding: 2px 6px; border-radius: 4px; background: rgba(255,255,255,0.06); color: ${q.color}; border: 1px solid ${q.color}44;">${q.tag}</span>
        </div>
      `;
    });

    flyout.innerHTML = `
      <div style="padding: 10px 12px; border-bottom: 1px solid rgba(255,255,255,0.08); display: flex; align-items: center; justify-content: space-between;">
        <div style="display: flex; align-items: center; gap: 6px;">
          <span style="color: #fbbf24; font-size: 14px;">⚡</span>
          <span style="font-size: 12px; font-weight: 800; color: #38bdf8; letter-spacing: 0.5px;">SELECT DOWNLOAD QUALITY</span>
        </div>
        <span class="turbox-close-flyout" style="color: #94a3b8; font-size: 14px; cursor: pointer; padding: 0 4px;">✕</span>
      </div>
      <div style="padding: 8px;">
        ${rowsHtml}
        <div class="turbox-quality-row" data-quality="Best" data-format="mp4" style="display: flex; align-items: center; justify-content: space-between; padding: 7px 10px; border-radius: 6px; cursor: pointer; border-top: 1px solid rgba(255,255,255,0.08); margin-top: 4px;">
          <span style="font-size: 12px; font-weight: 600; color: #94a3b8;">⚡ Open Full Options Dialog...</span>
          <span style="font-size: 10px; color: #38bdf8;">Custom ➔</span>
        </div>
      </div>
    `;

    // Position flyout safely on screen
    const flyoutWidth = 280;
    const posX = Math.max(10, Math.min(x, window.innerWidth - flyoutWidth - 15));
    const posY = Math.max(10, Math.min(y, window.innerHeight - 340));

    Object.assign(flyout.style, {
      position: "fixed",
      top: `${posY}px`,
      left: `${posX}px`,
      width: `${flyoutWidth}px`,
      zIndex: "2147483647",
      background: "rgba(11, 15, 23, 0.97)",
      border: "1px solid rgba(56, 189, 248, 0.5)",
      borderRadius: "10px",
      boxShadow: "0 10px 30px rgba(0, 0, 0, 0.85), 0 0 15px rgba(56, 189, 248, 0.2)",
      backdropFilter: "blur(12px)",
      fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
      userSelect: "none",
      animation: "turboxFadeIn 0.15s ease"
    });

    document.body.appendChild(flyout);
    activeFlyout = flyout;

    // Attach row events
    const rows = flyout.querySelectorAll(".turbox-quality-row");
    rows.forEach((r) => {
      r.addEventListener("mouseenter", () => {
        r.style.background = "rgba(56, 189, 248, 0.12)";
      });
      r.addEventListener("mouseleave", () => {
        r.style.background = "transparent";
      });
      r.addEventListener("click", (e) => {
        e.stopPropagation();
        const selectedQuality = r.dataset.quality;
        const selectedFormat = r.dataset.format || "mp4";

        r.innerHTML = `<span style="color: #4ade80; font-weight: 700; font-size: 12px;">✓ Sending ${selectedQuality} to Turbo X...</span>`;

        chrome.runtime.sendMessage({
          action: "download",
          url: targetUrl || window.location.href,
          filename: title || document.title || "Video",
          quality: selectedQuality,
          format: selectedFormat,
          referrer: window.location.href,
          autostart: selectedQuality !== "Best" // Specific quality triggers auto-start
        }, (resp) => {
          setTimeout(() => {
            if (flyout.parentElement) flyout.remove();
            if (activeFloatingBadge && activeFloatingBadge.parentElement) activeFloatingBadge.remove();
          }, 800);
        });
      });
    });

    // Close button
    const closeBtn = flyout.querySelector(".turbox-close-flyout");
    closeBtn.addEventListener("click", () => {
      if (flyout.parentElement) flyout.remove();
      activeFlyout = null;
    });

    // Auto close when clicking outside
    const outsideClickHandler = (e) => {
      if (!flyout.contains(e.target)) {
        if (flyout.parentElement) flyout.remove();
        activeFlyout = null;
        document.removeEventListener("click", outsideClickHandler);
      }
    };
    setTimeout(() => document.addEventListener("click", outsideClickHandler), 50);
  }

  // Periodic heartbeat scanner for dynamic single-page web apps (YouTube, TikTok, Facebook)
  setInterval(() => {
    scanForVideos();
  }, 1500);

  // Initial scan
  scanForVideos();
})();
