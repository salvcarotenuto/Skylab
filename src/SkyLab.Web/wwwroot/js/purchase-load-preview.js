(() => {
    const showPreviewMessage = () => {
        const options = { title: "Carico per acquisti", message: "Funzione disponibile nella prossima fase.", detail: "Questa versione definisce esclusivamente la grafica del modulo." };
        if (window.SkyLabMessageBox?.show) window.SkyLabMessageBox.show(options);
    };

    document.querySelectorAll("[data-purchase-load-preview-command]").forEach(button => button.addEventListener("click", showPreviewMessage));
    const list = document.querySelector("[data-purchase-load-list]");
    if (list) {
        const rows = Array.from(list.querySelectorAll("[data-purchase-load-row]"));
        const count = list.querySelector("[data-purchase-load-count]");
        const frame = list.querySelector(".purchase-load-grid-frame.documents");
        const body = frame?.querySelector("tbody");
        const sortHeaders = Array.from(frame?.querySelectorAll("[data-sort-key]") ?? []);
        const detailRows = Array.from(list.querySelectorAll("[data-purchase-load-detail]"));
        let selected = rows[0] ?? null;
        let sortState = { key: "", direction: "asc" };
        const syncDetails = row => detailRows.forEach(detail => { detail.hidden = !row || detail.dataset.documentId !== row.dataset.id; });
        const selectRow = row => { rows.forEach(item => item.classList.toggle("is-selected", item === row)); selected = row; syncDetails(row); };
        const ensureVisible = row => { if (!row || !frame) return; const top = row.offsetTop; const bottom = top + row.offsetHeight; if (top < frame.scrollTop + 30) frame.scrollTop = Math.max(0, top - 30); else if (bottom > frame.scrollTop + frame.clientHeight) frame.scrollTop = bottom - frame.clientHeight; };
        rows.forEach(row => row.addEventListener("click", () => selectRow(row)));
        if (selected) selectRow(selected);
        const currentYearFilter = list.querySelector('[data-purchase-load-filter="year"]');
        if (currentYearFilter?.tagName === "INPUT") {
            const yearSelect = document.createElement("select");
            yearSelect.id = currentYearFilter.id;
            yearSelect.dataset.purchaseLoadFilter = "year";
            yearSelect.setAttribute("aria-label", "Anno");
            yearSelect.appendChild(new Option("Tutti", ""));
            [...new Set(rows.map(row => row.dataset.year).filter(Boolean))]
                .sort((left, right) => Number(right) - Number(left))
                .forEach(year => yearSelect.appendChild(new Option(year, year)));
            currentYearFilter.replaceWith(yearSelect);
        }
        const currentSupplierFilter = list.querySelector('[data-purchase-load-filter="supplier"]');
        if (currentSupplierFilter?.tagName === "INPUT") {
            const supplierSelect = document.createElement("select");
            supplierSelect.id = currentSupplierFilter.id;
            supplierSelect.dataset.purchaseLoadFilter = "supplier";
            supplierSelect.appendChild(new Option("Tutti", ""));
            const suppliers = new Map();
            rows.forEach(row => {
                const supplier = (row.dataset.supplier || "").trim();
                const separator = supplier.indexOf(" ");
                const code = separator >= 0 ? supplier.slice(0, separator) : supplier;
                const name = separator >= 0 ? supplier.slice(separator + 1) : supplier;
                if (code) suppliers.set(code, name);
            });
            [...suppliers.entries()]
                .sort((left, right) => left[1].localeCompare(right[1], "it", { sensitivity: "base" }))
                .forEach(([code, name]) => supplierSelect.appendChild(new Option(name, code)));
            currentSupplierFilter.replaceWith(supplierSelect);
        }
        const articleFilter = list.querySelector('[data-purchase-load-filter="article"]');
        const storeLabel = list.querySelector('label[for="load-store"]');
        if (storeLabel) storeLabel.textContent = "Un. locale";
        const articleGroup = articleFilter?.closest(".purchase-load-filter.article");
        let articleDescription = articleGroup?.querySelector("#load-article-description");
        if (articleGroup && !articleDescription) {
            articleFilter.placeholder = "Codice";
            articleDescription = document.createElement("input");
            articleDescription.id = "load-article-description";
            articleDescription.readOnly = true;
            articleDescription.tabIndex = -1;
            articleDescription.setAttribute("aria-label", "Descrizione articolo");
            articleGroup.insertBefore(articleDescription, articleGroup.querySelector("button"));
        }
        if (articleFilter && articleDescription) {
            const selectTypedArticle = async () => {
                const article = await window.SkyLabZoom?.find?.("articoli", "code", articleFilter.value);
                articleDescription.value = article?.description ?? "";
                if (article?.code) articleFilter.value = article.code;
            };
            articleFilter.addEventListener("input", () => { articleDescription.value = ""; });
            articleFilter.addEventListener("change", selectTypedArticle);
            articleFilter.addEventListener("keydown", event => {
                if (event.key !== "Enter") return;
                event.preventDefault();
                void selectTypedArticle();
            });
        }
        const filters = Array.from(list.querySelectorAll("[data-purchase-load-filter]"));
        const applyFilters = () => {
            rows.forEach(row => { row.hidden = !filters.every(filter => { const value = filter.value.trim().toLocaleLowerCase("it"); return !value || (row.dataset[filter.dataset.purchaseLoadFilter] || "").toLocaleLowerCase("it").includes(value); }); });
            const visibleRows = rows.filter(row => !row.hidden); if (count) count.textContent = String(visibleRows.length); if (!selected || selected.hidden) selectRow(visibleRows[0] ?? null);
        };
        filters.forEach(filter => {
            filter.addEventListener(filter.tagName === "SELECT" ? "change" : "input", applyFilters);
            if (filter.tagName !== "SELECT") filter.addEventListener("change", applyFilters);
        });
        sortHeaders.forEach(header => header.addEventListener("click", () => {
            const key = header.dataset.sortKey;
            const direction = sortState.key === key && sortState.direction === "asc" ? "desc" : "asc";
            const numeric = header.dataset.sortType === "number";
            rows.sort((left, right) => {
                const leftValue = left.dataset[`sort${key[0].toUpperCase()}${key.slice(1)}`] ?? "";
                const rightValue = right.dataset[`sort${key[0].toUpperCase()}${key.slice(1)}`] ?? "";
                const comparison = numeric
                    ? (Number(leftValue) || 0) - (Number(rightValue) || 0)
                    : leftValue.localeCompare(rightValue, "it", { numeric: true, sensitivity: "base" });
                return direction === "asc" ? comparison : -comparison;
            });
            rows.forEach(row => body?.appendChild(row));
            sortState = { key, direction };
            sortHeaders.forEach(item => {
                const active = item === header;
                item.classList.toggle("is-sorted", active);
                item.dataset.sortDirection = active ? direction : "";
                item.setAttribute("aria-sort", active ? (direction === "asc" ? "ascending" : "descending") : "none");
            });
        }));
        frame?.addEventListener("keydown", event => {
            const visibleRows = rows.filter(row => !row.hidden); if (!visibleRows.length) return; const current = Math.max(0, visibleRows.indexOf(selected)); let next = current;
            if (event.key === "ArrowDown") next = Math.min(visibleRows.length - 1, current + 1); else if (event.key === "ArrowUp") next = Math.max(0, current - 1); else if (event.key === "Home") next = 0; else if (event.key === "End") next = visibleRows.length - 1; else if (event.key === "Enter") { location.href = `./CaricoAcquisti/Edit?azione=3&id=${selected?.dataset.id || ""}`; return; } else return;
            event.preventDefault(); selectRow(visibleRows[next]); ensureVisible(visibleRows[next]);
        });
        list.querySelectorAll("[data-purchase-load-action]").forEach(button => button.addEventListener("click", () => { if (button.dataset.purchaseLoadAction === "edit") { if (selected) location.href = `./CaricoAcquisti/Edit?azione=3&id=${selected.dataset.id}`; return; } showPreviewMessage(); }));
        document.addEventListener("keydown", event => { if (event.key === "Escape") list.querySelector("[data-purchase-load-exit]")?.click(); });
    }
    const edit = document.querySelector("[data-purchase-load-edit]");
    if (edit) {
        const lines = Array.from(edit.querySelectorAll("[data-purchase-load-line]"));
        const selectLine = line => lines.forEach(item => item.classList.toggle("is-selected", item === line));
        lines.forEach(line => line.addEventListener("click", () => selectLine(line)));

        const navigationFields = Array.from(edit.querySelectorAll("input:not([readonly]):not([disabled]), select:not([disabled]), textarea:not([readonly]):not([disabled])"));
        navigationFields.forEach((field, index) => field.addEventListener("keydown", event => {
            if (event.key !== "Enter" || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
            event.preventDefault();
            navigationFields[(index + 1) % navigationFields.length]?.focus();
        }));

        const showMessage = (message, detail = "") => {
            if (window.SkyLabMessageBox?.show) {
                window.SkyLabMessageBox.show({ title: "Carico per acquisti", message, detail });
                return;
            }
            window.alert(message);
        };
        const showElectronicInvoiceMessage = (message, variant = "error") => {
            const messages = window.SkyLabElectronicInvoiceDialog;
            if (variant === "info") messages?.info?.(message);
            else messages?.error?.(message);
            if (!messages) showMessage(message);
        };
        const electronicOpen = edit.querySelector("[data-electronic-invoice-open]");
        const electronicCurrentPreview = edit.querySelector("[data-electronic-invoice-current-preview]");
        const electronicName = edit.querySelector("[data-electronic-invoice-name]");
        const electronicPath = edit.querySelector("[data-electronic-invoice-full-path]");
        const dialog = document.querySelector("[data-electronic-invoice-dialog]");
        const filesBody = document.querySelector("[data-electronic-invoice-files]");
        const search = document.querySelector("[data-electronic-invoice-search]");
        const count = document.querySelector("[data-electronic-invoice-count]");
        const accept = document.querySelector("[data-electronic-invoice-accept]");
        const viewer = document.querySelector("[data-electronic-invoice-viewer]");
        const viewerFrame = document.querySelector("[data-electronic-invoice-viewer-frame]");
        const viewerTitle = document.querySelector("[data-electronic-invoice-viewer-title]");
        const documentNumber = edit.querySelector("[data-stock-load-document-number]");
        const documentDate = edit.querySelector("[data-stock-load-document-date]");
        const loadType = edit.querySelector("[data-stock-load-type]");
        const supplierCode = edit.querySelector("[data-stock-load-supplier-code]");
        const supplierName = edit.querySelector("[data-stock-load-supplier-name]");
        const store = edit.querySelector("[data-stock-load-store]");
        const total = edit.querySelector(".purchase-load-total strong");
        let selectedElectronicFile = null;

        const endpoint = handler => {
            const url = new URL("/FattureAcquisto/Edit", window.location.origin);
            url.searchParams.set("handler", handler);
            return url;
        };
        const selectedRow = row => {
            Array.from(filesBody?.querySelectorAll("tr[data-full-path]") ?? []).forEach(item => item.classList.toggle("is-selected", item === row));
            selectedElectronicFile = row ? { name: row.dataset.fileName ?? "", fullPath: row.dataset.fullPath ?? "" } : null;
        };
        const renderFiles = files => {
            if (!filesBody) return;
            filesBody.innerHTML = "";
            files.forEach(file => {
                const row = document.createElement("tr");
                row.dataset.fileName = file.name ?? "";
                row.dataset.fullPath = file.fullPath ?? "";
                [file.name, file.type, file.lastModified, file.size].forEach(value => {
                    const cell = document.createElement("td");
                    cell.textContent = value ?? "";
                    row.appendChild(cell);
                });
                row.addEventListener("click", () => selectedRow(row));
                row.addEventListener("dblclick", () => void acceptElectronicInvoice());
                filesBody.appendChild(row);
            });
            const first = filesBody.querySelector("tr[data-full-path]");
            if (first) selectedRow(first);
        };
        const loadFiles = async () => {
            const url = endpoint("ElectronicInvoiceFiles");
            url.searchParams.set("search", search?.value ?? "");
            try {
                const response = await fetch(url, { headers: { Accept: "application/json" } });
                const responseText = await response.text();
                let result = null;
                try { result = JSON.parse(responseText); } catch { /* risposta non JSON */ }
                if (!response.ok) throw new Error(result?.message || `Errore del server (${response.status}).`);
                if (!result) throw new Error("Il server non ha restituito un esito valido per la fattura selezionata.");
                if (count) count.value = `${result.count ?? 0} file`;
                renderFiles(result.files ?? []);
                if (result.error) showElectronicInvoiceMessage(result.error);
            } catch {
                if (count) count.value = "0 file";
                renderFiles([]);
                showElectronicInvoiceMessage("Non e' stato possibile leggere le fatture elettroniche.");
            }
        };
        const openDialog = () => {
            if (!dialog) return;
            dialog.hidden = false;
            document.body.classList.add("lookup-open");
            void loadFiles();
        };
        const closeDialog = () => {
            if (!dialog) return;
            dialog.hidden = true;
            document.body.classList.remove("lookup-open");
        };
        const closeViewer = () => {
            if (!viewer) return;
            viewer.hidden = true;
            if (viewerFrame) viewerFrame.src = "about:blank";
            document.body.classList.remove("purchase-invoice-xml-open");
        };
        const viewFile = file => {
            if (!file?.name || !viewer || !viewerFrame) {
                showMessage("Fattura elettronica non disponibile.");
                return;
            }
            const url = endpoint("ElectronicInvoiceRaw");
            url.searchParams.set("fileName", file.name);
            if (viewerTitle) viewerTitle.textContent = file.name;
            viewerFrame.src = url.toString();
            viewer.hidden = false;
            document.body.classList.add("purchase-invoice-xml-open");
        };
        const setStore = value => {
            if (!store || value === null || value === undefined) return;
            const normalized = String(value);
            if (!Array.from(store.options).some(option => option.value === normalized)) {
                store.add(new Option(normalized, normalized));
            }
            store.value = normalized;
        };
        const fillRows = importedRows => {
            const linesBody = edit.querySelector("[data-stock-load-lines]");
            while (linesBody && lines.length < importedRows.length) {
                const row = document.createElement("tr");
                row.dataset.purchaseLoadLine = "";
                for (let index = 0; index < 8; index += 1) row.appendChild(document.createElement("td"));
                row.addEventListener("click", () => selectLine(row));
                linesBody.appendChild(row);
                lines.push(row);
            }
            lines.forEach(row => {
                row.classList.remove("is-selected", "is-missing-article");
                row.removeAttribute("title");
                Array.from(row.cells).forEach((cell, index) => { cell.textContent = index === 0 ? "\u00a0" : ""; });
            });
            importedRows.slice(0, lines.length).forEach((item, index) => {
                const row = lines[index];
                const values = [item.articleCode, item.description, item.unitMeasure, item.quantity, item.price, item.discount, item.amount, item.vatRate];
                values.forEach((value, cellIndex) => { row.cells[cellIndex].textContent = value ?? ""; });
                row.classList.toggle("is-missing-article", item.articleFound === false);
                if (item.articleFound === false) row.title = `Codice FE non trovato: ${item.electronicArticleCode ?? ""}`;
            });
            const first = lines.find(row => row.cells[0]?.textContent.trim() || row.cells[1]?.textContent.trim());
            if (first) selectLine(first);
        };
        const applyImport = result => {
            if (electronicName) electronicName.value = result.fileName ?? "";
            if (electronicPath) electronicPath.value = result.fullPath ?? "";
            if (documentNumber) documentNumber.value = result.documentNumber ?? "";
            if (documentDate) documentDate.value = result.documentDate ?? "";
            if (loadType) loadType.value = result.documentType === "TD04" ? "12" : "10";
            if (supplierCode) supplierCode.value = result.supplier?.codeDisplay ?? "";
            if (supplierName) supplierName.value = result.supplier?.name ?? "";
            setStore(result.supplier?.storeCode ?? 0);
            if (total) total.textContent = result.total || "0,00";
            fillRows(result.rows ?? []);
            closeDialog();
            if (Number(result.missingArticles ?? 0) > 0) {
                showElectronicInvoiceMessage(`Articoli non presenti o non associati al fornitore: ${result.missingArticles}.`, "info");
            }
        };
        async function acceptElectronicInvoice() {
            if (!selectedElectronicFile?.name) {
                showElectronicInvoiceMessage("Selezionare una fattura elettronica.");
                return;
            }
            const importFailureMessage = `Non e' stato possibile importare il file:\n${selectedElectronicFile.name}\n\nIl server non ha restituito un motivo specifico. Verificare che il file XML/P7M sia integro e che contenga i dati obbligatori di azienda, fornitore e documento.`;
            const url = endpoint("ElectronicInvoiceImport");
            url.searchParams.set("fileName", selectedElectronicFile.name);
            try {
                const response = await fetch(url, { headers: { Accept: "application/json" } });
                if (!response.ok) throw new Error();
                const result = await response.json();
                if (!result.success) {
                    if (result.reason === "supplierMissing") {
                        window.SkyLabElectronicInvoiceDialog?.confirmMissingSupplier(result, () => void acceptElectronicInvoice());
                        return;
                    }
                    showElectronicInvoiceMessage(result.message || importFailureMessage);
                    return;
                }
                applyImport(result);
            } catch (error) {
                showElectronicInvoiceMessage(error?.message || importFailureMessage);
            }
        }

        electronicOpen?.addEventListener("click", openDialog);
        electronicCurrentPreview?.addEventListener("click", () => viewFile({ name: electronicName?.value ?? "" }));
        accept?.addEventListener("click", () => void acceptElectronicInvoice());
        search?.addEventListener("input", () => void loadFiles());
        document.querySelector("[data-electronic-invoice-search-clear]")?.addEventListener("click", () => { if (search) search.value = ""; void loadFiles(); });
        document.querySelectorAll("[data-electronic-invoice-close]").forEach(button => button.addEventListener("click", closeDialog));
        document.querySelectorAll("[data-electronic-invoice-viewer-close]").forEach(button => button.addEventListener("click", closeViewer));
    }
    const resetNewPurchaseLoad = () => {
        const action = new URLSearchParams(window.location.search).get("azione");
        if (action !== "2" && action !== "102") {
            return;
        }

        const clearValue = (selector) => {
            const element = document.querySelector(selector);
            if (element) {
                if ("value" in element) {
                    element.value = "";
                } else {
                    element.textContent = "";
                }
            }
        };

        clearValue("[data-stock-load-document-number]");
        clearValue("[data-stock-load-document-date]");
        clearValue("[data-stock-load-supplier-code]");
        clearValue("[data-stock-load-supplier-name]");
        clearValue("[data-electronic-invoice-full-path]");

        const total = document.querySelector("[data-stock-load-total]");
        if (total) {
            total.textContent = "0,00";
        }

        const invoiceName = document.querySelector("[data-electronic-invoice-name]");
        if (invoiceName) {
            if ("value" in invoiceName) {
                invoiceName.value = "Nessun file caricato";
            } else {
                invoiceName.textContent = "Nessun file caricato";
            }
        }

        const linesBody = document.querySelector("[data-stock-load-lines]");
        if (linesBody) {
            linesBody.querySelectorAll("tr").forEach((row) => {
                row.classList.remove("is-missing-article");
                row.querySelectorAll("td").forEach((cell) => {
                    const control = cell.querySelector("input, select, textarea");
                    if (control) {
                        control.value = "";
                    } else {
                        cell.textContent = "";
                    }
                });
            });
        }
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", resetNewPurchaseLoad, { once: true });
    } else {
        resetNewPurchaseLoad();
    }
})();
