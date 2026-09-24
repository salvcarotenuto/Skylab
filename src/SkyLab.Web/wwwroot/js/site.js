(() => {
  const storageKey = "skylab:deferred-file-system-warning";

  window.SkyLabDeferredWarning = {
    set(message) {
      const text = String(message ?? "").trim();
      if (!text) return;
      try { sessionStorage.setItem(storageKey, text); }
      catch { console.warn(text); }
    }
  };

  document.addEventListener("DOMContentLoaded", () => {
    let message = "";
    try {
      message = sessionStorage.getItem(storageKey) ?? "";
      sessionStorage.removeItem(storageKey);
    } catch {
      return;
    }

    if (!message) return;
    window.SkyLabMessageBox?.show?.({
      title: "SkyLab - attenzione",
      message,
      variant: "error",
      okText: "OK"
    });
  });
})();
