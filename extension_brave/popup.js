// Popup script for Turbo X 8K/4K Extension
document.addEventListener("DOMContentLoaded", () => {
  const statusBadge = document.getElementById("statusBadge");
  const statusText = document.getElementById("statusText");
  const interceptToggle = document.getElementById("interceptToggle");
  const urlInput = document.getElementById("urlInput");
  const videoDetectInfo = document.getElementById("videoDetectInfo");
  const videoTitleText = document.getElementById("videoTitleText");
  const qualityBadge = document.getElementById("qualityBadge");
  const selQuality = document.getElementById("selQuality");
  const selFormat = document.getElementById("selFormat");
  const btnDownload = document.getElementById("btnDownload");
  const btnDownloadText = document.getElementById("btnDownloadText");
  const btnOpenApp = document.getElementById("btnOpenApp");
  const btnUseCurrentTab = document.getElementById("btnUseCurrentTab");
  const chips = document.querySelectorAll(".chip");

  let currentDetectedTitle = "";

  // 1. Check Turbo X PC App Connection Status
  function checkStatus() {
    chrome.runtime.sendMessage({ action: "checkStatus" }, (response) => {
      if (response && response.online) {
        statusBadge.className = "status-badge status-online";
        statusText.textContent = "Connected";
      } else {
        statusBadge.className = "status-badge status-offline";
        statusText.textContent = "Offline";
      }
    });
  }

  // 2. Detect Current Active Browser Tab (YouTube, TikTok, Facebook, etc.)
  function detectActiveTab() {
    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
      if (tabs && tabs.length > 0) {
        const tab = tabs[0];
        const url = tab.url || "";
        if (url && !url.startsWith("chrome://") && !url.startsWith("about:") && !url.startsWith("edge://")) {
          urlInput.value = url;
          currentDetectedTitle = tab.title || "Video";

          if (url.includes("youtube.com") || url.includes("youtu.be") || url.includes("tiktok.com") || url.includes("facebook.com")) {
            videoTitleText.textContent = currentDetectedTitle;
            videoDetectInfo.style.display = "block";
          }
        }
      }
    });
  }

  btnUseCurrentTab.addEventListener("click", () => {
    detectActiveTab();
  });

  // 3. Quality Chip Interaction
  chips.forEach((chip) => {
    chip.addEventListener("click", () => {
      chips.forEach((c) => c.classList.remove("active"));
      chip.classList.add("active");

      const q = chip.dataset.quality;
      selQuality.value = q;
      updateQualityBadge(q);

      if (q === "MP3" || q === "M4A") {
        selFormat.value = q.toLowerCase();
      } else if (selFormat.value === "mp3" || selFormat.value === "m4a") {
        selFormat.value = "mp4";
      }
    });
  });

  selQuality.addEventListener("change", () => {
    const q = selQuality.value;
    updateQualityBadge(q);

    chips.forEach((c) => {
      if (c.dataset.quality === q) c.classList.add("active");
      else c.classList.remove("active");
    });

    if (q === "MP3" || q === "M4A") {
      selFormat.value = q.toLowerCase();
    } else if (selFormat.value === "mp3" || selFormat.value === "m4a") {
      selFormat.value = "mp4";
    }
  });

  function updateQualityBadge(q) {
    if (q === "8K") {
      qualityBadge.textContent = "8K ULTRA";
      qualityBadge.style.color = "#ec4899";
    } else if (q === "4K") {
      qualityBadge.textContent = "4K UHD";
      qualityBadge.style.color = "#38bdf8";
    } else if (q === "2K") {
      qualityBadge.textContent = "2K QHD";
      qualityBadge.style.color = "#818cf8";
    } else if (q === "1080p") {
      qualityBadge.textContent = "1080p FHD";
      qualityBadge.style.color = "#4ade80";
    } else if (q === "720p") {
      qualityBadge.textContent = "720p HD";
      qualityBadge.style.color = "#fbbf24";
    } else if (q === "MP3") {
      qualityBadge.textContent = "AUDIO 320K";
      qualityBadge.style.color = "#f43f5e";
    } else {
      qualityBadge.textContent = q;
      qualityBadge.style.color = "#38bdf8";
    }
  }

  // 4. Load & Save Interception Toggle
  chrome.storage.local.get(["interceptDownloads"], (res) => {
    if (res.interceptDownloads !== undefined) {
      interceptToggle.checked = res.interceptDownloads;
    }
  });

  interceptToggle.addEventListener("change", () => {
    chrome.storage.local.set({ interceptDownloads: interceptToggle.checked });
  });

  // 5. Handle Download Button Click
  btnDownload.addEventListener("click", () => {
    const url = urlInput.value.trim();
    if (!url) {
      urlInput.focus();
      return;
    }

    const quality = selQuality.value;
    const format = selFormat.value;

    btnDownloadText.textContent = `Sending ${quality}...`;
    btnDownload.style.opacity = "0.8";

    chrome.runtime.sendMessage({
      action: "download",
      url: url,
      filename: currentDetectedTitle || "Video",
      quality: quality,
      format: format,
      referrer: url,
      autostart: true // Immediately starts downloading in selected quality
    }, (resp) => {
      btnDownload.style.opacity = "1";
      if (resp && resp.success) {
        btnDownloadText.textContent = `✓ Sent ${quality} to Turbo X!`;
        btnDownload.style.background = "#22c55e";
        setTimeout(() => {
          window.close();
        }, 1200);
      } else {
        btnDownloadText.textContent = "Failed (Check App Status)";
        btnDownload.style.background = "#ef4444";
        setTimeout(() => {
          btnDownloadText.textContent = "Download with Turbo X";
          btnDownload.style.background = "linear-gradient(135deg, #0284c7 0%, #38bdf8 100%)";
        }, 2200);
      }
    });
  });

  // 6. Open Turbo X PC Dashboard
  btnOpenApp.addEventListener("click", () => {
    chrome.runtime.sendMessage({ action: "openApp" }, (resp) => {
      if (resp && resp.success) {
        window.close();
      } else {
        alert("Turbo X is not running. Please launch IDMTurboDownloader.exe on your PC!");
      }
    });
  });

  // Initialize
  checkStatus();
  detectActiveTab();
});
