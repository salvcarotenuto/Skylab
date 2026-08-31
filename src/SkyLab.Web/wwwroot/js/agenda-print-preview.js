document.addEventListener("DOMContentLoaded", () => {
  const preview = document.querySelector("[data-agenda-print-preview]");
  const documentView = preview?.querySelector("[data-agenda-preview-document]");
  const zoomLabel = preview?.querySelector("[data-agenda-preview-zoom-label]");
  if (!preview || !documentView) return;

  let previewZoom = 0.8;
  const updatePreviewZoom = () => {
    documentView.style.setProperty("--report-preview-zoom", String(previewZoom));
    if (zoomLabel) zoomLabel.textContent = `${Math.round(previewZoom * 100)}%`;
  };

  preview.querySelector("[data-agenda-preview-zoom-out]")?.addEventListener("click", () => {
    previewZoom = Math.max(0.5, previewZoom - 0.1);
    updatePreviewZoom();
  });
  preview.querySelector("[data-agenda-preview-zoom-in]")?.addEventListener("click", () => {
    previewZoom = Math.min(1.8, previewZoom + 0.1);
    updatePreviewZoom();
  });
  preview.querySelector("[data-agenda-preview-print]")?.addEventListener("click", () => window.print());
  preview.querySelector("[data-agenda-preview-close]")?.addEventListener("click", () => history.back());
  document.addEventListener("keydown", event => { if (event.key === "Escape") history.back(); });
  updatePreviewZoom();
});
