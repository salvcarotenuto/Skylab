(() => {
    const viewer = document.querySelector("[data-electronic-invoice-viewer]");
    const frame = document.querySelector("[data-electronic-invoice-viewer-frame]");
    const title = document.querySelector("[data-electronic-invoice-viewer-title]");
    if (!viewer || !frame) return;

    document.querySelectorAll("[data-sales-invoice-fe-view]").forEach(button => {
        button.addEventListener("click", () => {
            frame.src = button.dataset.salesInvoiceFeView;
            const fileName = button.closest("td")?.querySelector(".sales-invoice-fe-name")?.textContent.trim() || "XML";
            if (title) title.textContent = `Fattura elettronica - ${fileName}`;
            viewer.hidden = false;
            viewer.querySelector("[data-electronic-invoice-viewer-panel]")?.focus({ preventScroll: true });
        });
    });
})();
