(() => {
    const dialog = document.querySelector("[data-electronic-invoice-dialog]");
    const filesBody = dialog?.querySelector("[data-electronic-invoice-files]");
    const previewBox = dialog?.querySelector("[data-electronic-invoice-preview-box]");
    const gridFrame = dialog?.querySelector(".purchase-invoice-fe-grid-frame");
    const viewButton = dialog?.querySelector("[data-electronic-invoice-preview]");
    const deleteButton = dialog?.querySelector("[data-electronic-invoice-delete]");
    const countBox = dialog?.querySelector("[data-electronic-invoice-count]");
    const searchBox = dialog?.querySelector("[data-electronic-invoice-search]");
    const viewer = document.querySelector("[data-electronic-invoice-viewer]");
    const viewerFrame = document.querySelector("[data-electronic-invoice-viewer-frame]");
    const viewerTitle = document.querySelector("[data-electronic-invoice-viewer-title]");
    const viewerPanel = document.querySelector("[data-electronic-invoice-viewer-panel]");
    if (!dialog || !filesBody || !previewBox || !gridFrame || dialog.dataset.previewBound === "true") return;

    dialog.dataset.previewBound = "true";
    let previewAbort = null;
    let previewRow = null;
    let lastScrollTop = 0;
    let scrollFrame = 0;
    let supplierModal = null;
    let supplierModalFrame = null;
    let supplierSavedCallback = null;
    let restoreInvoiceDialog = false;

    gridFrame.tabIndex = 0;

    const rows = () => Array.from(filesBody.querySelectorAll("tr[data-full-path]"));
    const selectedRow = () => filesBody.querySelector("tr.is-selected[data-full-path]");
    const headerHeight = () => gridFrame.querySelector("thead")?.getBoundingClientRect().height ?? 0;
    const ensureRowVisible = row => {
        const rowTop = row.offsetTop;
        const rowBottom = rowTop + row.offsetHeight;
        const visibleTop = gridFrame.scrollTop + headerHeight();
        const visibleBottom = gridFrame.scrollTop + gridFrame.clientHeight;
        if (rowTop < visibleTop) {
            gridFrame.scrollTop = Math.max(0, rowTop - headerHeight());
        } else if (rowBottom > visibleBottom) {
            gridFrame.scrollTop = rowBottom - gridFrame.clientHeight;
        }
    };
    const selectRow = row => {
        if (!row) return;
        row.click();
        row.tabIndex = -1;
        row.focus({ preventScroll: true });
        ensureRowVisible(row);
    };
    const moveSelection = destination => {
        const availableRows = rows();
        if (!availableRows.length) return;
        const currentIndex = Math.max(0, availableRows.indexOf(selectedRow()));
        const nextIndex = destination === "home"
            ? 0
            : destination === "end"
                ? availableRows.length - 1
                : Math.max(0, Math.min(availableRows.length - 1, currentIndex + destination));
        selectRow(availableRows[nextIndex]);
    };
    const visibleRows = () => {
        const frameRect = gridFrame.getBoundingClientRect();
        const top = frameRect.top + headerHeight();
        return rows().filter(row => {
            const rect = row.getBoundingClientRect();
            return rect.bottom > top && rect.top < frameRect.bottom;
        });
    };
    const showMessage = (message, variant = "error") => {
        window.SkyLabMessageBox?.show?.({ title: "Fattura elettronica", message, variant });
    };
    const confirmDelete = (fileName, onConfirm) => {
        window.SkyLabMessageBox?.show?.({
            title: "Elimina fattura elettronica",
            message: `Confermi l'eliminazione del file ${fileName || "selezionato"}?`,
            mode: "confirm",
            variant: "confirm",
            okText: "Elimina",
            cancelText: "Annulla",
            onConfirm
        });
    };
    const deleteSelectedFile = () => {
        const row = selectedRow();
        if (!row) {
            showMessage("Fattura selezionata non disponibile.");
            return;
        }
        if (row._electronicInvoiceFile) {
            showMessage("L'eliminazione e' disponibile solo per file letti da percorso locale.", "info");
            return;
        }
        const fullPath = String(row.dataset.fullPath ?? "").trim();
        if (!fullPath) {
            showMessage("Percorso file non disponibile.");
            return;
        }

        const fileName = String(row.dataset.fileName ?? "").trim() || "file selezionato";
        confirmDelete(fileName, async () => {
            const url = new URL("/FattureAcquisto/Edit", window.location.origin);
            url.searchParams.set("handler", "DeleteElectronicInvoiceFile");
            const formData = new FormData();
            formData.append("path", fullPath);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const response = await fetch(url, {
                    method: "POST",
                    body: formData,
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                const result = await response.json();
                if (!result.success) {
                    showMessage(result.message || "Non e' stato possibile eliminare il file.");
                    return;
                }

                const nextRow = row.nextElementSibling || row.previousElementSibling;
                row.remove();
                hidePreview();
                if (countBox) countBox.value = `${rows().length} file`;
                if (nextRow?.matches?.("tr[data-full-path]")) selectRow(nextRow);
                else gridFrame.focus({ preventScroll: true });
                dialog.dispatchEvent(new CustomEvent("skylab:electronic-invoice-deleted", {
                    detail: { fileName, fullPath }
                }));
            } catch {
                showMessage("Non e' stato possibile eliminare il file.");
            }
        });
    };
    const closeSupplierModal = () => {
        if (!supplierModal) return;
        supplierModal.hidden = true;
        supplierModalFrame?.removeAttribute("src");
        document.body.classList.remove("electronic-invoice-supplier-modal-open");
        if (restoreInvoiceDialog) dialog.hidden = false;
        restoreInvoiceDialog = false;
        requestAnimationFrame(() => selectedRow()?.focus({ preventScroll: true }));
    };
    const ensureSupplierModal = () => {
        if (supplierModal && supplierModalFrame) return true;
        supplierModal = document.createElement("div");
        supplierModal.className = "electronic-invoice-supplier-modal-overlay";
        supplierModal.hidden = true;
        supplierModal.innerHTML = '<section class="electronic-invoice-supplier-modal" role="dialog" aria-modal="true" aria-label="Nuovo fornitore"><iframe title="Nuovo fornitore"></iframe></section>';
        supplierModal.addEventListener("click", event => {
            if (event.target === supplierModal) closeSupplierModal();
        });
        document.body.append(supplierModal);
        supplierModalFrame = supplierModal.querySelector("iframe");
        return Boolean(supplierModalFrame);
    };
    const openSupplierModal = supplier => {
        if (!ensureSupplierModal()) {
            showMessage("Scheda Fornitore non disponibile.");
            return;
        }
        const url = new URL("/Fornitori/Edit", window.location.origin);
        url.searchParams.set("azione", "102");
        url.searchParams.set("returnTo", "purchaseInvoiceXml");
        const parameters = {
            name: supplier.name,
            vat: supplier.vat,
            fiscalCode: supplier.fiscalCode,
            address: supplier.address,
            city: supplier.city,
            postalCode: supplier.postalCode,
            province: supplier.province,
            phone: supplier.phone,
            email: supplier.email,
            certifiedEmail: supplier.certifiedEmail
        };
        Object.entries(parameters).forEach(([key, value]) => {
            const normalized = String(value ?? "").trim();
            if (normalized) url.searchParams.set(key, normalized);
        });
        supplierModalFrame.src = `${url.pathname}${url.search}`;
        restoreInvoiceDialog = !dialog.hidden;
        if (restoreInvoiceDialog) dialog.hidden = true;
        supplierModal.hidden = false;
        document.body.classList.add("electronic-invoice-supplier-modal-open");
        supplierModalFrame.focus();
    };
    const confirmMissingSupplier = (result, onSaved) => {
        const supplier = result?.supplier ?? {};
        const name = String(supplier.name ?? "").trim() || "(nome non disponibile)";
        const vat = String(supplier.vat ?? "").trim() || "(partita IVA non disponibile)";
        supplierSavedCallback = typeof onSaved === "function" ? onSaved : null;
        window.SkyLabMessageBox?.show?.({
            title: "Fornitore non trovato",
            message: `Il fornitore\n${name}\npartita iva: ${vat}\nnon e' presente in archivio.\nVuoi aggiungerlo ora?`,
            mode: "confirm",
            variant: "confirm",
            okText: "Aggiungi",
            cancelText: "Annulla",
            onConfirm: () => requestAnimationFrame(() => openSupplierModal(supplier)),
            onCancel: () => { supplierSavedCallback = null; }
        });
    };
    window.SkyLabElectronicInvoiceDialog = Object.freeze({
        error: message => showMessage(message, "error"),
        info: message => showMessage(message, "info"),
        confirmMissingSupplier
    });
    const viewSelectedFile = () => {
        const row = selectedRow();
        if (!row) {
            showMessage("Selezionare una fattura elettronica.");
            return;
        }
        if (row._electronicInvoiceFile) {
            showMessage("La visualizzazione e' disponibile solo per file letti da percorso locale.");
            return;
        }
        const fileName = row.dataset.fileName ?? "";
        if (!fileName) {
            showMessage("Fattura selezionata non disponibile.");
            return;
        }
        if (!viewer || !viewerFrame) {
            showMessage("Visualizzatore fattura non disponibile.");
            return;
        }
        const url = new URL("/FattureAcquisto/Edit", window.location.origin);
        url.searchParams.set("handler", "ElectronicInvoiceRaw");
        url.searchParams.set("fileName", fileName);
        if (viewerTitle) viewerTitle.textContent = `Visualizzazione fattura elettronica - ${fileName}`;
        viewerFrame.src = url.toString();
        viewer.hidden = false;
        document.body.classList.add("purchase-invoice-xml-open");
        requestAnimationFrame(() => viewerPanel?.focus());
    };
    const closeViewer = () => {
        if (!viewer || viewer.hidden) return;
        viewer.hidden = true;
        if (viewerFrame) viewerFrame.src = "about:blank";
        document.body.classList.remove("purchase-invoice-xml-open");
        requestAnimationFrame(() => {
            const current = selectedRow();
            if (current) current.focus({ preventScroll: true });
            else gridFrame.focus({ preventScroll: true });
        });
    };

    deleteButton?.addEventListener("click", deleteSelectedFile);

    const hidePreview = () => {
        previewAbort?.abort();
        previewAbort = null;
        previewRow?.classList.remove("is-previewed");
        previewRow = null;
        previewBox.hidden = true;
    };

    const positionPreview = row => {
        const panel = dialog.querySelector(".purchase-invoice-fe-dialog");
        if (!panel) return;
        const panelRect = panel.getBoundingClientRect();
        const rowRect = row.getBoundingClientRect();
        previewBox.style.left = `${Math.min(Math.max(rowRect.left - panelRect.left + 215, 10), panelRect.width - 390)}px`;
        previewBox.style.top = `${Math.min(Math.max(rowRect.top - panelRect.top + 18, 10), panelRect.height - 130)}px`;
    };

    const renderPreview = result => {
        if (!result.success) {
            previewBox.textContent = result.message ?? "Anteprima non disponibile.";
            return;
        }

        previewBox.replaceChildren();
        [
            ["Fornitore", result.supplier],
            ["Cliente", result.customer],
            ["Numero", result.number],
            ["Data", result.date],
            ["Importo", result.amount]
        ].forEach(([label, value]) => {
            const line = document.createElement("div");
            const caption = document.createElement("span");
            const separator = document.createElement("span");
            const content = document.createElement("strong");
            caption.textContent = label;
            separator.textContent = ":";
            content.textContent = value || "";
            line.append(caption, separator, content);
            previewBox.append(line);
        });
    };

    const showPreview = async row => {
        if (previewRow === row) return;
        hidePreview();
        previewRow = row;
        row.classList.add("is-previewed");
        positionPreview(row);
        previewBox.hidden = false;
        previewBox.textContent = "Lettura fattura...";

        const fileName = row.dataset.fileName ?? "";
        if (!fileName) {
            previewBox.textContent = "Nome file non disponibile.";
            return;
        }

        previewAbort = new AbortController();
        const url = new URL("/FattureAcquisto/Edit", window.location.origin);
        url.searchParams.set("handler", "ElectronicInvoicePreview");
        url.searchParams.set("fileName", fileName);

        try {
            const response = await fetch(url, {
                headers: { Accept: "application/json" },
                signal: previewAbort.signal
            });
            if (!response.ok) throw new Error();
            renderPreview(await response.json());
        } catch (error) {
            if (error.name !== "AbortError") previewBox.textContent = "Anteprima non disponibile.";
        }
    };

    filesBody.addEventListener("mouseover", event => {
        const row = event.target.closest("tr[data-full-path]");
        if (!row || row.contains(event.relatedTarget)) return;
        void showPreview(row);
    });

    filesBody.addEventListener("mouseout", event => {
        const row = event.target.closest("tr[data-full-path]");
        if (!row || row.contains(event.relatedTarget)) return;
        hidePreview();
    });

    filesBody.addEventListener("click", event => {
        const row = event.target.closest("tr[data-full-path]");
        if (!row) return;
        row.tabIndex = -1;
        row.focus({ preventScroll: true });
    });

    gridFrame.addEventListener("keydown", event => {
        if (event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey && searchBox) {
            event.preventDefault();
            event.stopPropagation();
            searchBox.focus();
            searchBox.value += event.key;
            searchBox.dispatchEvent(new Event("input", { bubbles: true }));
            return;
        }
        if (!["ArrowUp", "ArrowDown", "Home", "End"].includes(event.key)) return;
        event.preventDefault();
        event.stopPropagation();
        if (event.key === "Home") moveSelection("home");
        else if (event.key === "End") moveSelection("end");
        else moveSelection(event.key === "ArrowUp" ? -1 : 1);
    });

    gridFrame.addEventListener("scroll", () => {
        if (scrollFrame) cancelAnimationFrame(scrollFrame);
        scrollFrame = requestAnimationFrame(() => {
            scrollFrame = 0;
            const current = selectedRow();
            const visible = visibleRows();
            if (visible.length && (!current || !visible.includes(current))) {
                selectRow(gridFrame.scrollTop >= lastScrollTop ? visible[0] : visible[visible.length - 1]);
            }
            lastScrollTop = gridFrame.scrollTop;
        });
    });

    dialog.querySelectorAll("[data-electronic-invoice-close]").forEach(button => {
        button.addEventListener("click", hidePreview);
    });

    viewButton?.addEventListener("click", viewSelectedFile);
    document.querySelectorAll("[data-electronic-invoice-viewer-close]").forEach(button => {
        button.addEventListener("click", closeViewer);
    });

    window.addEventListener("skylab:messagebox-closed", () => {
        if (dialog.hidden) return;
        requestAnimationFrame(() => {
            const current = selectedRow();
            if (current) {
                current.tabIndex = -1;
                current.focus({ preventScroll: true });
                ensureRowVisible(current);
            } else {
                gridFrame.focus({ preventScroll: true });
            }
        });
    });

    window.addEventListener("message", event => {
        if (event.origin !== window.location.origin || !supplierModal || supplierModal.hidden) return;
        if (event.data?.type === "skylab:supplier-cancel") {
            supplierSavedCallback = null;
            closeSupplierModal();
            return;
        }
        if (event.data?.type === "skylab:supplier-saved") {
            const callback = supplierSavedCallback;
            supplierSavedCallback = null;
            closeSupplierModal();
            callback?.(event.data);
        }
    });

    document.addEventListener("keydown", event => {
        const target = event.target;
        const editable = target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement || target?.isContentEditable;
        const listAvailable = !dialog.hidden && (!viewer || viewer.hidden) && (!supplierModal || supplierModal.hidden);
        if (listAvailable && !editable && event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey && searchBox) {
            event.preventDefault();
            event.stopPropagation();
            searchBox.focus();
            searchBox.value += event.key;
            searchBox.dispatchEvent(new Event("input", { bubbles: true }));
            return;
        }
        if (event.key !== "Escape") return;
        if (viewer && !viewer.hidden) {
            event.preventDefault();
            event.stopPropagation();
            closeViewer();
            return;
        }
        if (supplierModal && !supplierModal.hidden) {
            event.preventDefault();
            event.stopPropagation();
            supplierSavedCallback = null;
            closeSupplierModal();
            return;
        }
        if (dialog.hidden) return;
        event.preventDefault();
        event.stopPropagation();
        hidePreview();
        dialog.hidden = true;
        document.body.classList.remove("lookup-open");
    });

    new MutationObserver(() => {
        if (dialog.hidden) hidePreview();
    }).observe(dialog, { attributes: true, attributeFilter: ["hidden"] });
})();
