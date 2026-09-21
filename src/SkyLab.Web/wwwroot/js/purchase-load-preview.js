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
        lines.forEach(line => line.addEventListener("click", () => lines.forEach(item => item.classList.toggle("is-selected", item === line))));

        const navigationFields = Array.from(edit.querySelectorAll("input:not([readonly]):not([disabled]), select:not([disabled]), textarea:not([readonly]):not([disabled])"));
        navigationFields.forEach((field, index) => field.addEventListener("keydown", event => {
            if (event.key !== "Enter" || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
            event.preventDefault();
            navigationFields[(index + 1) % navigationFields.length]?.focus();
        }));
    }
})();
