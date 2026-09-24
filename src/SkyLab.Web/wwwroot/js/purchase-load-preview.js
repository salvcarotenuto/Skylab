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
        applyFilters();
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
        const lineHasData = line => Array.from(line?.cells ?? []).some(cell => !cell.hasAttribute("data-article-state") && cell.textContent.trim());
        const selectLine = line => {
            if (!lineHasData(line)) return;
            lines.forEach(item => item.classList.toggle("is-selected", item === line));
        };
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
        const lineOverlay = edit.querySelector("[data-stock-load-line-overlay]");
        const lineTitle = lineOverlay?.querySelector("[data-stock-load-line-title]");
        const lineFields = {
            code: lineOverlay?.querySelector("[data-line-code]"),
            description: lineOverlay?.querySelector("[data-line-description]"),
            unit: lineOverlay?.querySelector("[data-line-unit]"),
            quantity: lineOverlay?.querySelector("[data-line-quantity]"),
            price: lineOverlay?.querySelector("[data-line-price]"),
            discount: lineOverlay?.querySelector("[data-line-discount]"),
            vat: lineOverlay?.querySelector("[data-line-vat]"),
            amount: lineOverlay?.querySelector("[data-line-amount]")
        };
        let targetLine = null;
        let lineArticleFound = null;
        const parseLineNumber = value => {
            let text = String(value ?? "").trim().replace(/\s|%/g, "");
            if (!text) return 0;
            if (text.includes(",")) text = text.replace(/\./g, "").replace(",", ".");
            const number = Number(text);
            return Number.isFinite(number) ? number : 0;
        };
        const formatLineNumber = (value, digits) => Number(value || 0).toLocaleString("it-IT", { minimumFractionDigits: digits, maximumFractionDigits: digits });
        const calculateLineAmount = () => {
            const quantity = parseLineNumber(lineFields.quantity?.value);
            const price = parseLineNumber(lineFields.price?.value);
            const discount = parseLineNumber(lineFields.discount?.value);
            if (lineFields.amount) lineFields.amount.value = formatLineNumber(quantity * price * (1 - discount / 100), 2);
        };
        const clearLineDialog = () => { Object.values(lineFields).forEach(field => { if (field) field.value = ""; }); lineArticleFound = null; };
        const selectedLine = () => lines.find(line => line.classList.contains("is-selected") && lineHasData(line)) ?? null;
        const firstFreeLine = () => lines.find(line => !lineHasData(line)) ?? null;
        const closeLineDialog = () => {
            if (!lineOverlay) return;
            lineOverlay.hidden = true;
            document.body.classList.remove("purchase-load-line-open");
            targetLine = null;
        };
        const openLineDialog = row => {
            if (!lineOverlay || !row) {
                showMessage("Non ci sono righe libere nella griglia.");
                return;
            }
            targetLine = row;
            clearLineDialog();
            if (lineHasData(row)) {
                const values = Array.from(row.cells).map(cell => cell.textContent.trim());
                [lineFields.code, lineFields.description, lineFields.unit, lineFields.quantity, lineFields.price, lineFields.discount, lineFields.amount, lineFields.vat].forEach((field, index) => { if (field) field.value = values[index] ?? ""; });
                lineArticleFound = values[8] === "1" ? true : values[8] === "-1" ? false : null;
                if (lineTitle) lineTitle.textContent = "Modifica articolo";
            } else if (lineTitle) lineTitle.textContent = "Inserimento articolo";
            lineOverlay.hidden = false;
            document.body.classList.add("purchase-load-line-open");
            requestAnimationFrame(() => lineFields.code?.focus());
        };
        const applyArticle = article => {
            if (!article) return;
            lineFields.code.value = article.code ?? "";
            lineFields.description.value = article.description ?? "";
            lineFields.unit.value = article.unitMeasure ?? "";
            lineFields.price.value = formatLineNumber(article.price, 3);
            lineFields.vat.value = formatLineNumber(article.vatRate, 2);
            lineArticleFound = true;
            calculateLineAmount();
        };
        const findArticle = async () => {
            const code = lineFields.code?.value.trim();
            if (!code) {
                lineFields.description.value = "";
                lineFields.unit.value = "";
                return;
            }
            const article = await window.SkyLabZoom?.find?.("articoli", "code", code);
            if (!article) {
                lineArticleFound = false;
                showMessage("Articolo non trovato.");
                lineFields.description.value = "";
                lineFields.unit.value = "";
                lineFields.code.focus();
                return;
            }
            applyArticle(article);
        };
        const updateLoadTotal = () => {
            const value = lines.reduce((sum, row) => sum + parseLineNumber(row.cells[6]?.textContent), 0);
            if (total) total.textContent = formatLineNumber(value, 2);
        };
        const clearLine = row => {
            Array.from(row.cells).forEach((cell, index) => { cell.textContent = cell.hasAttribute("data-article-state") ? "0" : index === 0 ? "\u00a0" : ""; });
            row.classList.remove("is-selected", "is-missing-article");
            row.removeAttribute("title");
        };
        const compactLines = () => {
            const values = lines.filter(lineHasData).map(row => ({ values: Array.from(row.cells).map(cell => cell.textContent), missing: row.classList.contains("is-missing-article"), title: row.title }));
            lines.forEach(clearLine);
            values.forEach((item, index) => {
                item.values.forEach((value, cellIndex) => { lines[index].cells[cellIndex].textContent = value; });
                lines[index].classList.toggle("is-missing-article", item.missing);
                if (item.title) lines[index].title = item.title;
            });
        };
        const confirmLine = () => {
            if (!targetLine) return closeLineDialog();
            if (!lineFields.code?.value.trim()) return showMessage("Campo Articolo obbligatorio.");
            if (parseLineNumber(lineFields.quantity?.value) === 0) return showMessage("Campo Quantità obbligatorio.");
            calculateLineAmount();
            const values = [lineFields.code.value.trim(), lineFields.description.value.trim(), lineFields.unit.value.trim(), formatLineNumber(parseLineNumber(lineFields.quantity.value), 3), formatLineNumber(parseLineNumber(lineFields.price.value), 3), formatLineNumber(parseLineNumber(lineFields.discount.value), 2), formatLineNumber(parseLineNumber(lineFields.amount.value), 2), `${formatLineNumber(parseLineNumber(lineFields.vat.value), 2)}%`, lineArticleFound === true ? "1" : lineArticleFound === false ? "-1" : "0"];
            values.forEach((value, index) => { targetLine.cells[index].textContent = value; });
            targetLine.classList.toggle("is-missing-article", lineArticleFound === false);
            selectLine(targetLine);
            updateLoadTotal();
            closeLineDialog();
        };
        edit.querySelector("[data-stock-load-line-open]")?.addEventListener("click", () => openLineDialog(firstFreeLine()));
        edit.querySelector("[data-stock-load-line-edit]")?.addEventListener("click", () => {
            const row = selectedLine();
            if (!row) return showMessage("Selezionare una riga da modificare.");
            openLineDialog(row);
        });
        edit.querySelector("[data-stock-load-line-delete]")?.addEventListener("click", () => {
            const row = selectedLine();
            if (!row) return showMessage("Selezionare una riga da cancellare.");
            window.SkyLabMessageBox?.show?.({ title: "Carico per acquisti", message: "Cancellare la riga selezionata?", mode: "confirm", variant: "confirm", okText: "OK", cancelText: "Annulla", onConfirm: () => { clearLine(row); compactLines(); const first = lines.find(lineHasData); if (first) selectLine(first); updateLoadTotal(); } });
        });
        lineOverlay?.querySelector("[data-line-lookup]")?.addEventListener("click", async () => applyArticle(await window.SkyLabZoom?.open?.("articoli", { current: lineFields.code?.value ?? "" })));
        lineOverlay?.querySelector("[data-line-confirm]")?.addEventListener("click", confirmLine);
        lineOverlay?.querySelector("[data-line-cancel]")?.addEventListener("click", closeLineDialog);
        lineFields.code?.addEventListener("change", () => void findArticle());
        [lineFields.quantity, lineFields.price, lineFields.discount].forEach(field => field?.addEventListener("input", calculateLineAmount));
        lineOverlay?.addEventListener("click", event => { if (event.target === lineOverlay) closeLineDialog(); });
        lineOverlay?.addEventListener("keydown", event => {
            if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); closeLineDialog(); }
            else if (event.key === "Enter" && event.target === lineFields.code) { event.preventDefault(); void findArticle().then(() => lineFields.quantity?.focus()); }
        });
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
                Array.from(row.cells).forEach((cell, index) => { cell.textContent = cell.hasAttribute("data-article-state") ? "0" : index === 0 ? "\u00a0" : ""; });
            });
            importedRows.slice(0, lines.length).forEach((item, index) => {
                const row = lines[index];
                const values = [item.articleCode, item.description, item.unitMeasure, item.quantity, item.price, item.discount, item.amount, item.vatRate, item.articleFound === true ? "1" : item.articleFound === false ? "-1" : "0"];
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
            if (loadType) {
                loadType.value = result.documentType === "TD04" ? "12" : "10";
                loadType.dispatchEvent(new Event("change", { bubbles: true }));
            }
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
        const importXmlPath = new URLSearchParams(window.location.search).get("importXml");
        if (importXmlPath) {
            selectedElectronicFile = {
                name: importXmlPath.split(/[\\/]/).pop() || "",
                fullPath: importXmlPath
            };
            const importOnLoad = () => void acceptElectronicInvoice();
            if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", importOnLoad, { once: true });
            else requestAnimationFrame(importOnLoad);
        }
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

        const parameters = new URLSearchParams(window.location.search);
        const assignValue = (selector, parameter) => {
            const element = document.querySelector(selector);
            const value = parameters.get(parameter);
            if (element && value !== null) element.value = value;
        };
        assignValue("[data-stock-load-document-number]", "documentNumber");
        assignValue("[data-stock-load-document-date]", "documentDate");
        assignValue("[data-stock-load-supplier-code]", "supplierCode");
        assignValue("[data-stock-load-supplier-name]", "supplierName");
        assignValue("[data-stock-load-store]", "storeCode");
        const importXmlPath = parameters.get("importXml");
        if (importXmlPath) {
            const invoiceName = document.querySelector("[data-electronic-invoice-name]");
            const invoicePath = document.querySelector("[data-electronic-invoice-full-path]");
            if (invoiceName) invoiceName.value = importXmlPath.split(/[\\/]/).pop() || "Nessun file caricato";
            if (invoicePath) invoicePath.value = importXmlPath;
        }

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
                row.classList.remove("is-selected", "is-missing-article");
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

(() => {
    const page = document.querySelector('[data-purchase-load-edit]');
    if (!page) return;

    const loadType = page.querySelector('[data-stock-load-type]');
    const partyLabel = page.querySelector('[data-stock-load-party-label]');
    const partyCode = page.querySelector('[data-stock-load-supplier-code]');
    const partyName = page.querySelector('[data-stock-load-supplier-name]');
    const lookupButton = page.querySelector('[data-stock-load-party-lookup]');
    if (!loadType || !partyLabel || !partyCode || !partyName || !lookupButton) return;

    const isReturn = () => loadType.value === '12';
    const catalog = () => isReturn() ? 'clienti' : 'fornitori';
    const updatePartyPresentation = () => {
        const label = isReturn() ? 'Cliente' : 'Fornitore';
        partyLabel.textContent = label;
        partyCode.setAttribute('aria-label', `Codice ${label.toLocaleLowerCase('it')}`);
        partyName.setAttribute('aria-label', `Denominazione ${label.toLocaleLowerCase('it')}`);
        lookupButton.setAttribute('aria-label', `Ricerca ${label.toLocaleLowerCase('it')}`);
        lookupButton.title = `Seleziona ${label.toLocaleLowerCase('it')}`;
    };
    const applyParty = party => {
        if (!party) return;
        partyCode.value = String(party.code ?? '').padStart(5, '0');
        partyName.value = party.name ?? '';
    };

    loadType.addEventListener('change', () => {
        partyCode.value = '';
        partyName.value = '';
        updatePartyPresentation();
    });
    partyCode.addEventListener('input', () => { partyName.value = ''; });
    partyCode.addEventListener('change', async () => {
        const code = partyCode.value.trim();
        if (!code) return;
        applyParty(await window.SkyLabZoom?.find?.(catalog(), 'code', code.padStart(5, '0')));
    });
    lookupButton.addEventListener('click', async () => {
        applyParty(await window.SkyLabZoom?.open?.(catalog(), {
            opener: lookupButton,
            current: partyCode.value
        }));
    });

    updatePartyPresentation();
})();

(() => {
    const economicFields = new Map([
        ['lineQuantity', 3],
        ['linePrice', 3],
        ['lineDiscount', 2],
        ['lineVat', 2],
        ['lineVatRate', 2],
        ['lineAmount', 2]
    ]);

    const fieldSettings = element => {
        if (!(element instanceof HTMLInputElement)) return null;
        for (const [name, decimals] of economicFields) {
            if (name in element.dataset) return { decimals };
        }
        return null;
    };

    const filterDecimal = (value, decimals) => {
        const cleaned = value.replace(/\s/g, '').replace(/[^0-9,.]/g, '');
        const separator = cleaned.search(/[,.]/);
        if (separator < 0) return cleaned.replace(/\D/g, '');
        const integer = cleaned.slice(0, separator).replace(/\D/g, '');
        const fraction = cleaned.slice(separator + 1).replace(/\D/g, '').slice(0, decimals);
        return `${integer},${fraction}`;
    };

    document.addEventListener('input', event => {
        const settings = fieldSettings(event.target);
        if (!settings || event.target.readOnly) return;
        const filtered = filterDecimal(event.target.value, settings.decimals);
        if (event.target.value !== filtered) event.target.value = filtered;
    }, true);

    document.addEventListener('blur', event => {
        const settings = fieldSettings(event.target);
        if (!settings || event.target.value.trim() === '') return;
        const filtered = filterDecimal(event.target.value, settings.decimals);
        const value = Number(filtered.replace(',', '.'));
        if (!Number.isFinite(value)) {
            event.target.value = '';
            return;
        }
        event.target.value = value.toLocaleString('it-IT', {
            minimumFractionDigits: settings.decimals,
            maximumFractionDigits: settings.decimals
        });
    }, true);
})();

(() => {
    const body = document.querySelector('[data-stock-load-lines]');
    if (!body) return;

    const rowHasData = row => Array.from(row.cells).some(cell => !cell.hasAttribute('data-article-state') && cell.textContent.trim() !== '');
    const updateEmptyRows = () => {
        const rows = Array.from(body.querySelectorAll('tr[data-purchase-load-line]'));
        const populated = rows.some(rowHasData);
        rows.forEach(row => { row.hidden = populated && !rowHasData(row); });
    };

    new MutationObserver(updateEmptyRows).observe(body, {
        childList: true,
        characterData: true,
        subtree: true
    });
    updateEmptyRows();
})();

(() => {
    const overlay = document.querySelector('[data-stock-load-line-overlay]');
    if (!overlay) return;

    const navigation = [
        '[data-line-code]',
        '[data-line-unit]',
        '[data-line-price]',
        '[data-line-vat]',
        '[data-line-quantity]',
        '[data-line-discount]'
    ];

    overlay.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        const fields = navigation.map(selector => overlay.querySelector(selector)).filter(Boolean);
        const index = fields.indexOf(event.target);
        if (index < 0) return;
        event.preventDefault();
        event.stopPropagation();
        const next = fields[index + 1] || overlay.querySelector('[data-line-confirm]');
        next?.focus();
        if (next instanceof HTMLInputElement) next.select();
    });
})();

(() => {
    const overlay = document.querySelector('[data-stock-load-line-overlay]');
    if (!overlay) return;

    const focusable = () => Array.from(overlay.querySelectorAll('input:not([type="hidden"]):not([readonly]):not([disabled]), select:not([disabled]), button:not([disabled]), [href], [tabindex]:not([tabindex="-1"])'))
        .filter(element => !element.hidden && element.offsetParent !== null);

    overlay.addEventListener('keydown', event => {
        if (event.key !== 'Tab' || overlay.hidden) return;
        const controls = focusable();
        if (!controls.length) return;
        const first = controls[0];
        const last = controls[controls.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    });

    overlay.addEventListener('mousedown', event => {
        if (event.target !== overlay || overlay.hidden) return;
        event.preventDefault();
        const current = document.activeElement;
        if (current instanceof HTMLElement && overlay.contains(current)) current.focus();
        else focusable()[0]?.focus();
    });
})();

(() => {
    const button = document.querySelector('[data-stock-load-article-card]');
    const addButton = document.querySelector('[data-stock-load-article-add]');
    const replaceButton = document.querySelector('[data-stock-load-article-replace]');
    const modal = document.querySelector('[data-stock-load-article-modal]');
    const frame = document.querySelector('[data-stock-load-article-modal-frame]');
    if (!button || !modal || !frame) return;
    let modalMode = 'view';

    const selectedArticleCode = () => {
        const row = document.querySelector('[data-stock-load-lines] tr.is-selected[data-purchase-load-line]:not([hidden])');
        return String(row?.cells?.[0]?.textContent ?? '').replace(/\u00a0/g, '').trim();
    };
    const showMessage = (message, variant = 'info') => window.SkyLabMessageBox?.show?.({ title: 'Carico per acquisti', message, variant });
    const close = () => {
        if (modal.hidden) return;
        modal.hidden = true;
        frame.removeAttribute('src');
        document.body.classList.remove('purchase-load-article-card-open');
        requestAnimationFrame(() => (modalMode === 'add' ? addButton : button)?.focus());
    };
    const open = () => {
        const articleCode = selectedArticleCode();
        if (!articleCode) {
            showMessage('Selezionare una riga con codice articolo.');
            return;
        }
        const url = new URL('/Magazzino/Articolo', window.location.origin);
        url.searchParams.set('codice', articleCode);
        url.searchParams.set('Azione', '101');
        modalMode = 'view';
        frame.src = `${url.pathname}${url.search}`;
        modal.hidden = false;
        document.body.classList.add('purchase-load-article-card-open');
        frame.focus();
    };
    const openAdd = async () => {
        const row = document.querySelector('[data-stock-load-lines] tr.is-selected[data-purchase-load-line]:not([hidden])');
        const articleCode = String(row?.cells?.[0]?.textContent ?? '').replace(/\u00a0/g, '').trim();
        if (!row || !articleCode) {
            showMessage('Selezionare una riga con codice articolo.');
            return;
        }
        const stateCell = row.querySelector('[data-article-state]') || row.cells[8];
        let state = String(stateCell?.textContent ?? '').trim() || '0';
        if (state === '1') {
            showMessage('L’articolo selezionato è già presente in archivio.', 'error');
            return;
        }
        if (state === '0') {
            let existing;
            try {
                existing = await window.SkyLabZoom?.find?.('articoli', 'code', articleCode);
            } catch {
                showMessage('Non è stato possibile verificare il codice nell’archivio Articoli.', 'error');
                return;
            }
            state = existing ? '1' : '-1';
            if (stateCell) stateCell.textContent = state;
            if (state === '1') {
                showMessage('L’articolo selezionato è già presente in archivio.', 'error');
                return;
            }
        }
        const url = new URL('/Magazzino/Articolo', window.location.origin);
        url.searchParams.set('nuovo', 'true');
        url.searchParams.set('Azione', '702');
        url.searchParams.set('codice', articleCode);
        const description = String(row.cells[1]?.textContent ?? '').trim();
        const purchaseUnit = String(row.cells[2]?.textContent ?? '').trim();
        const supplierCode = String(document.querySelector('[data-stock-load-supplier-code]')?.value ?? '').trim();
        if (description) url.searchParams.set('description', description);
        if (purchaseUnit) url.searchParams.set('purchaseUnit', purchaseUnit);
        if (supplierCode) url.searchParams.set('supplierCode', supplierCode);
        modalMode = 'add';
        frame.src = `${url.pathname}${url.search}`;
        modal.hidden = false;
        document.body.classList.add('purchase-load-article-card-open');
        frame.focus();
    };
    const replaceArticle = async () => {
        const row = document.querySelector('[data-stock-load-lines] tr.is-selected[data-purchase-load-line]:not([hidden])');
        const currentCode = String(row?.cells?.[0]?.textContent ?? '').replace(/\u00a0/g, '').trim();
        if (!row || !currentCode) {
            showMessage('Selezionare una riga valorizzata.', 'error');
            return;
        }
        const article = await window.SkyLabZoom?.open?.('articoli', { opener: replaceButton, current: currentCode });
        if (!article) return;
        row.cells[0].textContent = article.code ?? currentCode;
        row.cells[1].textContent = article.description ?? row.cells[1].textContent;
        const stateCell = row.querySelector('[data-article-state]') || row.cells[8];
        if (stateCell) stateCell.textContent = '1';
        row.classList.remove('is-missing-article');
        row.removeAttribute('title');
        document.querySelector('.purchase-load-lines-frame')?.focus({ preventScroll: true });
    };

    button.addEventListener('click', open);
    addButton?.addEventListener('click', openAdd);
    replaceButton?.addEventListener('click', replaceArticle);
    modal.addEventListener('mousedown', event => {
        if (event.target === modal) close();
    });
    frame.addEventListener('load', () => {
        try {
            const childDocument = frame.contentDocument;
            const closeButton = childDocument?.querySelector('.customer-form-toolbar .customer-menu-button');
            closeButton?.addEventListener('click', event => {
                event.preventDefault();
                close();
            });
            childDocument?.addEventListener('keydown', event => {
                if (event.key !== 'Escape') return;
                event.preventDefault();
                close();
            });
        } catch { }
    });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || modal.hidden) return;
        event.preventDefault();
        event.stopPropagation();
        close();
    }, true);
    window.addEventListener('message', event => {
        if (event.origin !== window.location.origin || modal.hidden || event.data?.type !== 'skylab:article-saved') return;
        const row = document.querySelector('[data-stock-load-lines] tr.is-selected[data-purchase-load-line]:not([hidden])');
        if (row) {
            row.cells[0].textContent = event.data.code || row.cells[0].textContent;
            row.cells[1].textContent = event.data.description || row.cells[1].textContent;
            row.cells[2].textContent = event.data.unitMeasure || row.cells[2].textContent;
            const stateCell = row.querySelector('[data-article-state]') || row.cells[8];
            if (stateCell) stateCell.textContent = '1';
            row.classList.remove('is-missing-article');
            row.removeAttribute('title');
        }
        window.SkyLabZoom?.clearCache?.('articoli');
        close();
    });
})();

(() => {
    const page = document.querySelector('[data-purchase-load-edit]');
    if (!page) return;

    const tabFields = [
        page.querySelector('[data-stock-load-document-number]'),
        page.querySelector('[data-stock-load-document-date]'),
        page.querySelector('[data-stock-load-type]'),
        page.querySelector('[data-stock-load-supplier-code]'),
        page.querySelector('[data-stock-load-store]'),
        page.querySelector('.purchase-load-lines-frame')
    ].filter(Boolean);

    tabFields.forEach(field => { field.tabIndex = 0; });
    const modalOpen = () => Array.from(document.querySelectorAll('[role="dialog"][aria-modal="true"]'))
        .some(dialog => !dialog.closest('[hidden]') && dialog.offsetParent !== null);
    const focusField = field => {
        field?.focus({ preventScroll: true });
        if (field instanceof HTMLInputElement && field.type !== 'date') field.select();
    };

    document.addEventListener('keydown', event => {
        if (event.key !== 'Tab' || modalOpen()) return;
        const index = tabFields.indexOf(event.target);
        if (index < 0) {
            if (!page.contains(event.target)) return;
            event.preventDefault();
            focusField(event.shiftKey ? tabFields.at(-1) : tabFields[0]);
            return;
        }
        event.preventDefault();
        const nextIndex = event.shiftKey
            ? (index - 1 + tabFields.length) % tabFields.length
            : (index + 1) % tabFields.length;
        focusField(tabFields[nextIndex]);
    }, true);

    document.addEventListener('focusin', event => {
        if (modalOpen() || page.contains(event.target)) return;
        focusField(tabFields[0]);
    });

    requestAnimationFrame(() => {
        if (!modalOpen()) focusField(tabFields[0]);
    });
})();

(() => {
    const page = document.querySelector('[data-purchase-load-edit]');
    const saveButton = page?.querySelector('[data-stock-load-save]');
    if (!page || !saveButton) return;
    const returnTarget = new URLSearchParams(window.location.search).get('returnTo')?.toLocaleLowerCase('it') ?? '';
    const returnToPurchaseInvoice = returnTarget === 'purchaseinvoice';

    const fieldValue = selector => page.querySelector(selector)?.value ?? '';
    const parseNumber = raw => {
        const normalized = String(raw ?? '').trim().replace(/\s/g, '').replace(/\./g, '').replace(',', '.').replace(/[^0-9.-]/g, '');
        const number = Number.parseFloat(normalized);
        return Number.isFinite(number) ? number : 0;
    };
    const collectRows = () => Array.from(page.querySelectorAll('[data-purchase-load-line]'))
        .map((row, index) => ({
            rowNumber: index + 1,
            articleCode: String(row.cells[0]?.textContent ?? '').replace(/\u00a0/g, '').trim(),
            description: String(row.cells[1]?.textContent ?? '').trim(),
            unitMeasure: String(row.cells[2]?.textContent ?? '').trim(),
            quantity: parseNumber(row.cells[3]?.textContent),
            price: parseNumber(row.cells[4]?.textContent),
            discount: parseNumber(row.cells[5]?.textContent),
            amount: parseNumber(row.cells[6]?.textContent),
            vatRate: parseNumber(row.cells[7]?.textContent),
            articleState: Number.parseInt(row.querySelector('[data-article-state]')?.textContent ?? '0', 10) || 0
        }))
        .filter(row => row.articleCode);
    const payload = (allowMissingArticles, allowDuplicate = false) => ({
        id: Number.parseInt(page.dataset.stockLoadId || '0', 10) || 0,
        year: Number.parseInt(page.dataset.stockLoadYear || '0', 10) || 0,
        code: Number.parseInt(page.dataset.stockLoadCode || '0', 10) || 0,
        causeCode: Number.parseInt(fieldValue('[data-stock-load-type]'), 10) || 0,
        documentNumber: String(fieldValue('[data-stock-load-document-number]')).trim(),
        documentDate: fieldValue('[data-stock-load-document-date]') || null,
        partyCode: Number.parseInt(fieldValue('[data-stock-load-supplier-code]'), 10) || 0,
        storeCode: Number.parseInt(fieldValue('[data-stock-load-store]'), 10) || 0,
        electronicInvoiceName: String(fieldValue('[data-electronic-invoice-name]')).trim(),
        electronicInvoicePath: String(fieldValue('[data-electronic-invoice-full-path]')).trim(),
        allowMissingArticles,
        allowDuplicate,
        rows: collectRows()
    });
    const message = (text, focusTarget = null) => window.SkyLabMessageBox?.show?.({
        title: 'Carico per acquisti',
        message: text,
        variant: 'error',
        onConfirm: () => focusTarget?.focus?.()
    });
    const clientError = data => {
        if (!data.documentNumber) return ['Numero documento obbligatorio.', '[data-stock-load-document-number]'];
        if (!data.documentDate) return ['Data documento obbligatoria.', '[data-stock-load-document-date]'];
        if (![10, 12].includes(data.causeCode)) return ['Tipo carico obbligatorio o non valido.', '[data-stock-load-type]'];
        if (!data.partyCode) return [data.causeCode === 12 ? 'Cliente obbligatorio.' : 'Fornitore obbligatorio.', '[data-stock-load-supplier-code]'];
        if (!data.rows.length) return ['Inserire almeno una riga articolo.', '.purchase-load-lines-frame'];
        return null;
    };
    const post = async (data, handler = '') => {
        const url = new URL(window.location.href);
        if (handler) url.searchParams.set('handler', handler); else url.searchParams.delete('handler');
        const body = new FormData();
        body.append('StockLoadPayload', JSON.stringify(data));
        body.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '');
        const response = await fetch(url, { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
        let result;
        try { result = await response.json(); }
        catch { result = { success: false, message: 'Risposta del server non leggibile.' }; }
        return { response, result };
    };
    const closeProgress = callback => {
        if (window.SkyProg?.Close) window.SkyProg.Close(callback);
        else callback?.();
    };
    const confirmMissing = (data, result) => window.SkyLabMessageBox?.show?.({
        title: 'Conferma registrazione',
        message: result.missingArticles?.length
            ? `I seguenti articoli non sono presenti in anagrafica e non saranno registrati: ${result.missingArticles.join(', ')}. Procedere con il salvataggio delle altre righe?`
            : result.message,
        mode: 'confirm',
        variant: 'confirm',
        okText: 'Salva',
        cancelText: 'Annulla',
        onConfirm: () => void save({ ...data, allowMissingArticles: true })
    });
    const continueAfterDuplicate = (data, result) => {
        if (result.requiresMissingConfirmation && !data.allowMissingArticles) {
            confirmMissing(data, result);
            return;
        }
        void save(data);
    };
    const confirmDuplicate = (data, result) => window.SkyLabMessageBox?.show?.({
        title: 'Conferma registrazione',
        message: result.message || 'Il documento risulta già registrato. Registrarlo nuovamente?',
        mode: 'confirm',
        variant: 'confirm',
        okText: 'Salva',
        cancelText: 'Annulla',
        onConfirm: () => continueAfterDuplicate({ ...data, allowDuplicate: true }, result)
    });
    const save = async data => {
        if (saveButton.disabled) return;
        saveButton.disabled = true;
        window.SkyProg?.Show?.();
        try {
            const { response, result } = await post(data);
            closeProgress(() => {
                saveButton.disabled = false;
                if (!response.ok || !result.success) {
                    if (result.requiresDuplicateConfirmation) confirmDuplicate(data, result);
                    else if (result.requiresMissingConfirmation) confirmMissing(data, result);
                    else message(result.message || 'Salvataggio non riuscito.');
                    return;
                }
                const destination = result.listUrl || './Index';
                const completeSave = () => {
                    if (returnToPurchaseInvoice) {
                        window.parent?.postMessage({
                            type: 'micronote:stock-load-saved',
                            id: result.id,
                            year: result.year,
                            code: result.code
                        }, window.location.origin);
                        return;
                    }
                    window.location.href = destination;
                };
                if (result.electronicInvoiceArchiveWarning) {
                    window.SkyLabDeferredWarning?.set?.(result.electronicInvoiceArchiveWarning);
                }
                completeSave();
            });
        } catch {
            closeProgress(() => {
                saveButton.disabled = false;
                message('Impossibile completare il salvataggio.');
            });
        }
    };
    const beginSave = async () => {
        const data = payload(false);
        const invalid = clientError(data);
        if (invalid) {
            message(invalid[0], page.querySelector(invalid[1]));
            return;
        }
        try {
            const { response, result } = await post(data, 'Validate');
            if (!response.ok || (!result.success && !result.requiresMissingConfirmation && !result.requiresDuplicateConfirmation)) {
                message(result.message || 'Controllo dei dati non riuscito.');
                return;
            }
            if (result.requiresDuplicateConfirmation) {
                confirmDuplicate(data, result);
                return;
            }
            if (result.requiresMissingConfirmation) {
                confirmMissing(data, result);
                return;
            }
            await save(data);
        } catch {
            message('Impossibile controllare i dati da registrare.');
        }
    };

    saveButton.addEventListener('click', () => void beginSave());
    page.querySelector('[data-stock-load-cancel]')?.addEventListener('click', event => {
        if (!returnToPurchaseInvoice) return;
        event.preventDefault();
        window.parent?.postMessage({ type: 'micronote:stock-load-cancel' }, window.location.origin);
    });
})();
