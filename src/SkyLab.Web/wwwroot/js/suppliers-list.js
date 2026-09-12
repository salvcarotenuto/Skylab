(() => {
  const page = document.querySelector("[data-supplier-page]");
  if (!page) return;

  const rows = [...page.querySelectorAll("[data-row]")];
  const search = page.querySelector("[data-search]");
  const clear = page.querySelector("[data-clear]");
  const count = page.querySelector("[data-count]");
  const empty = page.querySelector("[data-empty]");
  const gridFrame = page.querySelector(".supplier-grid-frame");
  let selected = null;
  let sortKey = "name";
  let ascending = true;
  let searchTimer = 0;
  let lastScrollTop = gridFrame?.scrollTop || 0;

  const showMessage = (message, title = "Fornitori", variant = "info", onConfirm = null) => {
    if (window.SkyLabMessageBox?.show) {
      window.SkyLabMessageBox.show({ title, message, variant, onConfirm });
      return;
    }
    window.alert(message);
    if (typeof onConfirm === "function") onConfirm();
  };
  const showConfirm = (message, title, onConfirm) => {
    if (window.SkyLabMessageBox?.show) {
      window.SkyLabMessageBox.show({
        title,
        message,
        mode: "confirm",
        variant: "confirm",
        okText: "Elimina",
        cancelText: "Annulla",
        onConfirm
      });
      return;
    }
    if (window.confirm(message)) onConfirm();
  };
  const syncSortHeaders = () => {
    page.querySelectorAll("th[data-key]").forEach(heading => {
      const active = heading.dataset.key === sortKey;
      heading.classList.toggle("is-sorted", active);
      if (active) heading.dataset.direction = ascending ? "asc" : "desc";
      else delete heading.dataset.direction;
    });
  };
  const updateQuery = () => {
    const url = new URL(location.href);
    const query = (search.value || "").trim();
    if (query) url.searchParams.set("Cerca", query);
    else url.searchParams.delete("Cerca");
    if (url.href !== location.href) location.href = url.href;
  };
  const scheduleQueryUpdate = () => {
    clearTimeout(searchTimer);
    searchTimer = window.setTimeout(updateQuery, 450);
  };
  const visibleRows = () => [...page.querySelectorAll("[data-row]")].filter(row => !row.hidden);
  const rowIsVisibleInFrame = (row) => {
    if (!row || !gridFrame) return false;
    const frameRect = gridFrame.getBoundingClientRect();
    const rowRect = row.getBoundingClientRect();
    return rowRect.top >= frameRect.top && rowRect.bottom <= frameRect.bottom;
  };
  const firstVisibleRowInFrame = (direction = 1) => {
    if (!gridFrame) return visibleRows()[0] || null;
    const frameRect = gridFrame.getBoundingClientRect();
    const visible = visibleRows().filter(row => {
      const rowRect = row.getBoundingClientRect();
      return rowRect.bottom > frameRect.top && rowRect.top < frameRect.bottom;
    });
    return direction >= 0 ? visible[0] || null : visible.at(-1) || null;
  };
  const choose = (row, focus = false) => {
    rows.forEach(item => item.classList.remove("selected"));
    selected = row;
    if (!row) return;
    row.classList.add("selected");
    if (focus) {
      row.focus({ preventScroll: true });
      row.scrollIntoView({ block: "nearest", inline: "nearest" });
    }
  };
  const move = (current, key) => {
    const visible = visibleRows();
    if (!visible.length) return;
    let index = Math.max(0, visible.indexOf(current));
    if (key === "ArrowDown") index = Math.min(visible.length - 1, index + 1);
    else if (key === "ArrowUp") index = Math.max(0, index - 1);
    else if (key === "Home") index = 0;
    else if (key === "End") index = visible.length - 1;
    choose(visible[index], true);
  };

  rows.forEach(row => {
    row.addEventListener("click", () => choose(row, true));
    row.addEventListener("dblclick", () => { location.href = row.dataset.edit; });
    row.addEventListener("keydown", event => {
      if (event.key === "Enter") {
        event.preventDefault();
        location.href = row.dataset.edit;
      } else if (["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
        event.preventDefault();
        move(row, event.key);
      }
    });
  });

  const filter = () => {
    const query = (search.value || "").trim().toLocaleLowerCase("it");
    let visibleCount = 0;
    rows.forEach(row => {
      const show = !query || [row.dataset.code, row.dataset.name, row.dataset.city, row.dataset.vat]
        .some(value => (value || "").toLocaleLowerCase("it").includes(query));
      row.hidden = !show;
      if (show) visibleCount++;
    });
    if (selected?.hidden) choose(null);
    if (!selected && visibleCount > 0) choose(visibleRows()[0]);
    count.textContent = visibleCount;
    empty.hidden = visibleCount > 0;
    clear.hidden = !query;
  };

  search.addEventListener("input", () => {
    filter();
    scheduleQueryUpdate();
  });
  search.addEventListener("keydown", event => {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    event.preventDefault();
    const visible = visibleRows();
    if (visible.length) choose(event.key === "ArrowDown" ? visible[0] : visible.at(-1), true);
  });
  clear.addEventListener("click", () => {
    search.value = "";
    filter();
    search.focus();
    updateQuery();
  });

  // Stesso gestore collaudato della lista Articoli.
  document.addEventListener("keydown", event => {
    const target = event.target;
    const isEditable = target instanceof HTMLInputElement
      || target instanceof HTMLSelectElement
      || target instanceof HTMLTextAreaElement
      || target instanceof HTMLButtonElement
      || target?.isContentEditable;
    if (event.defaultPrevented || isEditable || event.altKey || event.ctrlKey || event.metaKey) return;
    if (event.key.length === 1) {
      event.preventDefault();
      search.value += event.key;
      search.focus();
      filter();
      scheduleQueryUpdate();
    } else if (event.key === "Backspace" && search.value) {
      event.preventDefault();
      search.value = search.value.slice(0, -1);
      search.focus();
      filter();
      scheduleQueryUpdate();
    }
  });

  page.querySelectorAll("th[data-key]").forEach(heading => heading.addEventListener("click", () => {
    const key = heading.dataset.key;
    if (sortKey === key) ascending = !ascending;
    else { sortKey = key; ascending = true; }
    const body = heading.closest("table").tBodies[0];
    rows.sort((a, b) => {
      const av = key === "code" ? Number(a.dataset.code) : a.dataset[key] || "";
      const bv = key === "code" ? Number(b.dataset.code) : b.dataset[key] || "";
      const comparison = typeof av === "number"
        ? av - bv
        : String(av).localeCompare(String(bv), "it", { sensitivity: "base" });
      return comparison * (ascending ? 1 : -1);
    }).forEach(row => body.append(row));
    syncSortHeaders();
    if (selected) choose(selected, true);
  }));

  gridFrame?.addEventListener("scroll", () => {
    const direction = gridFrame.scrollTop >= lastScrollTop ? 1 : -1;
    lastScrollTop = gridFrame.scrollTop;
    if (!selected || !rowIsVisibleInFrame(selected)) {
      const next = firstVisibleRowInFrame(direction);
      if (next) choose(next);
    }
  }, { passive: true });

  const requireSelection = () => {
    if (selected) return true;
    showMessage("Selezionare un fornitore.", "Fornitori", "error");
    return false;
  };
  page.querySelector('[data-action="edit"]').onclick = () => {
    if (requireSelection()) location.href = selected.dataset.edit;
  };
  page.querySelector('[data-action="delete"]').onclick = () => {
    if (!requireSelection()) return;
    showConfirm(`Eliminare il fornitore ${selected.dataset.code} - ${selected.dataset.label}?`, "Elimina fornitore", () => {
      page.querySelector("[data-delete-code]").value = selected.dataset.code;
      page.querySelector("[data-delete-form]").submit();
    });
  };

  const preview = page.querySelector("[data-preview]");
  const documentBox = page.querySelector("[data-document]");
  const summary = page.querySelector("[data-print-summary]");
  let zoom = 1;
  const escapeHtml = value => String(value || "").replace(/[&<>"']/g, character => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
  })[character]);
  const render = () => {
    const visible = rows.filter(row => !row.hidden);
    summary.textContent = `${visible.length} fornitori`;
    documentBox.innerHTML = `<style>.p-title{font:700 24px Arial;color:#173e69;margin-bottom:6px}.p-sub{font:12px Arial;color:#52677d;margin-bottom:24px}.p-table{width:100%;border-collapse:collapse;font:11px Arial}.p-table th{background:#b9cee3;text-align:left;padding:7px;border:1px solid #7895b3}.p-table td{padding:6px;border:1px solid #b9c7d4}.p-foot{margin-top:12px;font:10px Arial;color:#667}</style><div class="p-title">SkyLab · Lista fornitori</div><div class="p-sub">Stampato il ${new Date().toLocaleString("it-IT")} · ${visible.length} record</div><table class="p-table"><thead><tr><th>Codice</th><th>Nome</th><th>Sede</th><th>Prov.</th><th>Partita IVA</th><th>Telefono</th><th>E-mail</th><th>Unità locale</th></tr></thead><tbody>${visible.map(row => `<tr><td>${String(row.dataset.code).padStart(5, "0")}</td><td>${escapeHtml(row.dataset.name)}</td><td>${escapeHtml(row.dataset.city)}</td><td>${escapeHtml(row.dataset.province)}</td><td>${escapeHtml(row.dataset.vat)}</td><td>${escapeHtml(row.dataset.phone)}</td><td>${escapeHtml(row.dataset.email)}</td><td>${escapeHtml(row.dataset.unit)}</td></tr>`).join("")}</tbody></table><div class="p-foot">SkyLab - Anagrafica fornitori</div>`;
  };
  const setZoom = value => {
    zoom = Math.max(.5, Math.min(1.5, value));
    documentBox.style.transform = `scale(${zoom})`;
    page.querySelector("[data-zoom-label]").textContent = `${Math.round(zoom * 100)}%`;
  };
  page.querySelector('[data-action="print"]').onclick = () => { render(); setZoom(1); preview.hidden = false; };
  page.querySelector("[data-close]").onclick = () => { preview.hidden = true; };
  page.querySelector("[data-zoom-out]").onclick = () => setZoom(zoom - .1);
  page.querySelector("[data-zoom-in]").onclick = () => setZoom(zoom + .1);
  page.querySelector("[data-print-now]").onclick = () => window.print();
  page.querySelector("[data-pdf]").onclick = () => {
    showMessage("Nella finestra di stampa scegliere “Salva come PDF”.", "Stampa fornitori", "info", () => window.print());
  };

  filter();
  syncSortHeaders();
})();
