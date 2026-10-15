// Turbo X Universal Browser Integration Module - Background Service Worker
const APIS = ["http://127.0.0.1:19876", "http://localhost:19876"];

// Setup Context Menus on install and startup
chrome.runtime.onInstalled.addListener(() => {
  setupContextMenus();
  chrome.storage.local.get(["interceptDownloads"], (result) => {
    if (result.interceptDownloads === undefined) {
      chrome.storage.local.set({ interceptDownloads: true });
    }
  });
});

chrome.runtime.onStartup.addListener(() => {
  setupContextMenus();
});

function setupContextMenus() {
  try {
    chrome.contextMenus.removeAll(() => {
      if (chrome.runtime.lastError) {}

      // 1. Primary Download Option
      chrome.contextMenus.create({
        id: "turbox-dl-best",
        title: "⚡ Download with Turbo X (Best Quality)",
        contexts: ["link", "video", "audio", "page"]
      }, () => { if (chrome.runtime.lastError) {} });

      // 2. High Resolution Direct Quality Presets
      chrome.contextMenus.create({
        id: "turbox-dl-8k",
        title: "🌟 Download 8K Ultra HD (4320p)",
        contexts: ["link", "video", "page"]
      }, () => { if (chrome.runtime.lastError) {} });

      chrome.contextMenus.create({
        id: "turbox-dl-4k",
        title: "🎬 Download 4K Ultra HD (2160p)",
        contexts: ["link", "video", "page"]
      }, () => { if (chrome.runtime.lastError) {} });

      chrome.contextMenus.create({
        id: "turbox-dl-1080p",
        title: "🔥 Download 1080p Full HD",
        contexts: ["link", "video", "page"]
      }, () => { if (chrome.runtime.lastError) {} });

      chrome.contextMenus.create({
        id: "turbox-dl-mp3",
        title: "🎵 Download Audio Only (MP3 320k)",
        contexts: ["link", "video", "audio", "page"]
      }, () => { if (chrome.runtime.lastError) {} });
    });
  } catch (e) {
    console.warn("Context menu setup:", e);
  }
}

// Handle Context Menu clicks
chrome.contextMenus.onClicked.addListener((info, tab) => {
  let url = info.linkUrl || info.srcUrl || info.pageUrl || tab?.url;
  if (!url) return;

  let quality = "Best";
  let format = "mp4";
  let autostart = false;

  if (info.menuItemId === "turbox-dl-8k") {
    quality = "8K";
    autostart = true;
  } else if (info.menuItemId === "turbox-dl-4k") {
    quality = "4K";
    autostart = true;
  } else if (info.menuItemId === "turbox-dl-1080p") {
    quality = "1080p";
    autostart = true;
  } else if (info.menuItemId === "turbox-dl-mp3") {
    quality = "MP3";
    format = "mp3";
    autostart = true;
  }

  sendToTurboX(url, tab?.title || "", tab?.url || "", quality, format, autostart)
    .then((sent) => {
      if (sent) showBadge("OK", "#00d2ff");
      else showBadge("ERR", "#ff3b30");
    });
});

// Debounce map to prevent duplicate rapid downloads
const recentInterceptedUrls = new Map();

// Intercept browser downloads
chrome.downloads.onCreated.addListener(async (item) => {
  try {
    const settings = await chrome.storage.local.get(["interceptDownloads"]);
    if (settings && settings.interceptDownloads === false) {
      return; // Interception disabled by user
    }

    const url = item.finalUrl || item.url;
    if (!url || url.startsWith("blob:") || url.startsWith("data:") || url.startsWith("file:")) {
      return;
    }

    // Prevent spamming Turbo X with duplicate download triggers (5 second debounce)
    const now = Date.now();
    if (recentInterceptedUrls.has(url) && (now - recentInterceptedUrls.get(url)) < 5000) {
      return;
    }
    recentInterceptedUrls.set(url, now);
    if (recentInterceptedUrls.size > 80) {
      for (const [k, v] of recentInterceptedUrls.entries()) {
        if (now - v > 10000) recentInterceptedUrls.delete(k);
      }
    }

    // Check if Turbo X is running
    const isOnline = await checkTurboXOnline();
    if (!isOnline) {
      return; // Don't interrupt if Turbo X is not running
    }

    // Send to Turbo X FIRST
    const sent = await sendToTurboX(url, item.filename || "", item.referrer || "", "Best", "mp4", false);
    if (sent) {
      showBadge("OK", "#00d2ff");

      // Cancel browser download only AFTER Turbo X has accepted the download
      chrome.downloads.cancel(item.id, () => {
        if (chrome.runtime.lastError) {}
        if (chrome.downloads.erase) {
          chrome.downloads.erase({ id: item.id }, () => {
            if (chrome.runtime.lastError) {}
          });
        }
      });
    }
  } catch (e) {
    console.error("Failed to route download to Turbo X:", e);
  }
});

// Check if Turbo X API is active (tries 127.0.0.1 and localhost for Brave/Chrome/Firefox compatibility)
async function checkTurboXOnline() {
  for (const api of APIS) {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 1200);
      const res = await fetch(`${api}/status`, {
        method: "GET",
        signal: controller.signal,
        cache: "no-store"
      });
      clearTimeout(timeoutId);
      if (res.ok) return true;
    } catch (err) {}
  }
  return false;
}

// Send download request with quality to Turbo X (tries both 127.0.0.1 and localhost)
async function sendToTurboX(url, filename, referrer, quality, format, autostart) {
  const payload = JSON.stringify({
    url: url,
    filename: filename || "",
    referrer: referrer || "",
    quality: quality || "Best",
    format: format || "mp4",
    autostart: !!autostart
  });

  for (const api of APIS) {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 3500);
      const res = await fetch(`${api}/download`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: payload,
        signal: controller.signal
      });
      clearTimeout(timeoutId);
      if (res.ok) {
        const data = await res.json();
        return data && data.success;
      }
    } catch (err) {
      console.warn(`Could not reach ${api}:`, err.message);
    }
  }
  return false;
}

// Request Turbo X App window to focus
async function openTurboXApp() {
  for (const api of APIS) {
    try {
      const res = await fetch(`${api}/focus`, { method: "GET" });
      if (res.ok) return true;
    } catch (e) {}
  }
  return false;
}

function showBadge(text, color) {
  const actionApi = chrome.action || chrome.browserAction;
  if (!actionApi) return;
  try {
    actionApi.setBadgeText({ text: text });
    actionApi.setBadgeBackgroundColor({ color: color });
    setTimeout(() => {
      try { actionApi.setBadgeText({ text: "" }); } catch (e) {}
    }, 2500);
  } catch (e) {}
}

// Listen for messages from content scripts and popup
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.action === "download") {
    sendToTurboX(
      message.url,
      message.filename,
      message.referrer || sender?.tab?.url,
      message.quality || "Best",
      message.format || "mp4",
      message.autostart || false
    )
      .then((success) => sendResponse({ success: success }))
      .catch((err) => sendResponse({ success: false, error: err.message }));
    return true; // async response
  }

  if (message.action === "checkStatus") {
    checkTurboXOnline()
      .then((online) => sendResponse({ online: online }))
      .catch(() => sendResponse({ online: false }));
    return true;
  }

  if (message.action === "openApp") {
    openTurboXApp()
      .then((ok) => sendResponse({ success: ok }))
      .catch(() => sendResponse({ success: false }));
    return true;
  }
});
